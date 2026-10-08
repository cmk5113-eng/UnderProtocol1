using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MonsterBase : CharacterBase
{
    private MonsterData data;
    public MonsterData MonsterData => data;
    public static List<GameObject> _monsters = new List<GameObject>();
    private Tilemap occupiedMap;
    private Vector3Int anchorCell;
    private readonly List<TileData> occupiedTiles = new List<TileData>();
    private SpriteRenderer footprintRenderer;
    private Vector3 originalVisualScale;
    private bool capturedVisualScale;
    // Body-local offsets keep broken shields on the same body part after movement.
    private readonly HashSet<Vector2Int> shieldCells = new HashSet<Vector2Int>();
    public int ShieldCount => shieldCells.Count;
    public int ShieldCapacity { get; private set; }
    public Tilemap OccupiedMap => occupiedMap;
    public Vector2Int FootprintSize => data != null ? data.FootprintSize : new Vector2Int(1, 1);

    public void Initialize(MonsterData data)
    {
        if (data == null) return;
        ReleaseOccupancy();
        this.data = data;
        isEnemy = true;
        shieldCells.Clear();
        if (data.hasShield)
            for (int x = 0; x < FootprintSize.x; x++)
                for (int y = 0; y < FootprintSize.y; y++)
                    shieldCells.Add(new Vector2Int(x, y));
        ShieldCapacity = shieldCells.Count;
        if (footprintRenderer == null) footprintRenderer = GetComponentInChildren<SpriteRenderer>(true);
        if (footprintRenderer != null)
        {
            if (!capturedVisualScale)
            {
                originalVisualScale = footprintRenderer.transform.localScale;
                capturedVisualScale = true;
            }
            footprintRenderer.transform.localScale = originalVisualScale;
        }

        // 데이터의 HP로 시작한다. 프리팹에 남은 현재 HP는 사용하지 않는다.
        MaxHP = Mathf.Max(1, data.hp);
        InitializeHP();
    }

    private bool TryGetBodyOffset(Tilemap map, Vector3Int cell, out Vector2Int offset)
    {
        offset = new Vector2Int(0, 0);
        if (map == null || (occupiedMap != null && occupiedMap != map) || !map.HasTile(cell)) return false;
        Vector3Int anchor = GetAnchorCell(map);
        Vector2Int size = FootprintSize;
        int x = cell.x - anchor.x, y = cell.y - anchor.y;
        if (cell.z != anchor.z || x < 0 || y < 0 || x >= size.x || y >= size.y) return false;
        offset = new Vector2Int(x, y);
        return true;
    }

    public bool HasCellShield(Tilemap map, Vector3Int cell)
        => TryGetBodyOffset(map, cell, out Vector2Int offset) && shieldCells.Contains(offset);

    public int TakeDamageAtCell(Tilemap map, Vector3Int cell, int damage)
        => TakeDamageOnCells(map, new Dictionary<Vector3Int, int> { [cell] = damage });

    /// <summary>Each contacted shield blocks that whole hit; exposed cells deal their maximum damage once.</summary>
    public int TakeDamageOnCells(Tilemap map, IDictionary<Vector3Int, int> damageByCell)
    {
        if (IsDead || damageByCell == null) return 0;
        int damage = 0;
        foreach (var hit in damageByCell)
        {
            if (hit.Value <= 0 || !TryGetBodyOffset(map, hit.Key, out Vector2Int offset)) continue;
            if (shieldCells.Remove(offset)) continue;
            damage = Mathf.Max(damage, hit.Value);
        }
        int before = currentHP;
        if (damage > 0) base.TakeDamage(damage);
        return before - currentHP;
    }

    // Callers without an impact coordinate retain a deterministic single-cell damage path.
    public override void TakeDamage(int damage)
    {
        if (IsDead || damage <= 0) return;
        if (ShieldCapacity == 0) { base.TakeDamage(damage); return; }
        Tilemap map = occupiedMap != null ? occupiedMap : PlacementManager.Instance != null ? PlacementManager.Instance.tilemap : null;
        if (map != null) TakeDamageAtCell(map, GetAnchorCell(map), damage);
        else if (!shieldCells.Remove(new Vector2Int(0, 0))) base.TakeDamage(damage);
    }

    public Vector3Int GetAnchorCell(Tilemap map)
    {
        if (map == null) return anchorCell;
        if (occupiedMap == map && map != null) return anchorCell;
        Vector3Int cell = map.WorldToCell(transform.position); cell.z = 0;
        return cell;
    }

    public IEnumerable<Vector3Int> GetOccupiedCells(Tilemap map)
    {
        if (map == null || (occupiedMap != null && occupiedMap != map)) yield break;
        Vector3Int anchor = GetAnchorCell(map);
        Vector2Int size = FootprintSize;
        for (int x = 0; x < size.x; x++)
            for (int y = 0; y < size.y; y++)
                yield return new Vector3Int(anchor.x + x, anchor.y + y, anchor.z);
    }

    public bool CanPlaceAt(Tilemap map, Vector3Int anchor)
    {
        PlacementManager placement = PlacementManager.Instance;
        if (map == null || placement == null || placement.tilemap != map) return false;
        Vector2Int size = FootprintSize;
        for (int x = 0; x < size.x; x++)
            for (int y = 0; y < size.y; y++)
            {
                Vector3Int cell = new Vector3Int(anchor.x + x, anchor.y + y, anchor.z);
                if (!map.HasTile(cell)) return false;
                TileData tile = placement.GetTileData(cell);
                if (tile == null || (!tile.isempty && tile.Character != this)) return false;
            }
        return true;
    }

    public bool TryPlace(Tilemap map, Vector3Int anchor)
    {
        if (!CanPlaceAt(map, anchor)) return false;
        // Check every destination before releasing any old cells, including self-overlapping moves.
        ReleaseOccupancy();
        occupiedMap = map;
        anchorCell = anchor;
        foreach (Vector3Int cell in GetOccupiedCells(map))
        {
            TileData tile = PlacementManager.Instance.GetTileData(cell);
            tile.Character = this;
            occupiedTiles.Add(tile);
        }
        Vector2Int size = FootprintSize;
        Vector3 first = map.GetCellCenterWorld(anchor);
        Vector3 last = map.GetCellCenterWorld(new Vector3Int(anchor.x + size.x - 1, anchor.y + size.y - 1, anchor.z));
        Vector3 center = (first + last) * 0.5f;
        center.z = transform.position.z;
        transform.position = center;
        FitVisual(map, anchor);
        return true;
    }

    private void FitVisual(Tilemap map, Vector3Int anchor)
    {
        if (data == null || !data.fitSpriteToFootprint || footprintRenderer == null || footprintRenderer.sprite == null) return;
        Vector3 bounds = footprintRenderer.bounds.size;
        if (bounds.x <= 0f || bounds.y <= 0f) return;
        Vector2Int size = FootprintSize;
        Vector3 center = map.GetCellCenterWorld(anchor);
        float width = (map.GetCellCenterWorld(new Vector3Int(anchor.x + 1, anchor.y, anchor.z)) - center).magnitude * size.x;
        float height = (map.GetCellCenterWorld(new Vector3Int(anchor.x, anchor.y + 1, anchor.z)) - center).magnitude * size.y;
        Vector3 scale = footprintRenderer.transform.localScale;
        footprintRenderer.transform.localScale = new Vector3(scale.x * width / bounds.x, scale.y * height / bounds.y, scale.z);
    }

    public void ReleaseOccupancy()
    {
        foreach (TileData tile in occupiedTiles)
            if (tile != null && tile.Character == this) tile.Character = null;
        // Also support one-cell enemies registered by a legacy/manual spawn path.
        PlacementManager placement = PlacementManager.Instance;
        if (occupiedTiles.Count == 0 && placement != null && placement.tilemap != null)
        {
            Vector3Int cell = placement.tilemap.WorldToCell(transform.position); cell.z = 0;
            TileData tile = placement.GetTileData(cell);
            if (tile != null && tile.Character == this) tile.Character = null;
        }
        occupiedTiles.Clear();
        occupiedMap = null;
    }

    protected override void Die()
    {
        shieldCells.Clear();
        ReleaseOccupancy();
        base.Die();
    }

    private void OnDisable()
    {
        shieldCells.Clear();
        ReleaseOccupancy();
    }
}
