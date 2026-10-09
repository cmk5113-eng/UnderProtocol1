using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class SkillExecuteResult
{
    public int hitCount;
    public int killCount;
    // Preserve exactly which enemies took damage for survivor-only follow-up attacks.
    public readonly List<CharacterBase> damagedTargets = new List<CharacterBase>();
}

public class ExecuteSkill : MonoBehaviour
{
    private sealed class SkillTargetHit
    {
        public CharacterBase target;
        public SkillPatternTile tile;
        public Vector3Int cell;
    }

    public static ExecuteSkill Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    public SkillExecuteResult ExecutePattern(
        CharacterBase caster,
        SkillList skill,
        Vector3Int pivotCell,
        int patternRotation = 0)
    {
        SkillExecuteResult result = new SkillExecuteResult();

        if (caster == null || skill == null || !skill.HasRoePattern)
            return result;

        if (PlacementManager.Instance == null ||
            PlacementManager.Instance.tilemap == null)
            return result;

        Tilemap tilemap = PlacementManager.Instance.tilemap;
        Vector3Int casterCell =
            tilemap.WorldToCell(caster.transform.position);
        casterCell.z = 0;
        pivotCell.z = 0;

        Vector3Int forward =
            GetPatternForward(patternRotation);

        // Resolve contacts and per-cell values exclusively from the skill editor's ROE.
        // Callers provide a pivot and rotation, never a replacement target/tile list.
        var hits = new List<SkillTargetHit>();
        foreach (SkillPatternTile tile in skill.roePattern)
        {
            if (tile == null) continue;
            Vector2Int offset = RotateOffset(tile.position, patternRotation);
            Vector3Int cell = pivotCell + new Vector3Int(offset.x, offset.y, 0);
            if (!tilemap.HasTile(cell)) continue;
            CharacterBase target = BattleTileOccupancy.FindAt(tilemap, cell);
            if (target == null || target.IsDead || !target.isEnemy) continue;
            hits.Add(new SkillTargetHit { target = target, tile = tile, cell = cell });
        }

        // Fields belong to ROE cells, including empty cells and cells whose enemy was killed.
        BattleManager battle = BattleManager.Instance;
        if (battle != null && battle.IsBattleActive && skill.roePattern != null)
            foreach (SkillPatternTile tile in skill.roePattern)
            {
                if (tile == null || tile.fieldEffect == SkillTileFieldEffectType.None) continue;
                Vector2Int offset = RotateOffset(tile.position, patternRotation);
                Vector3Int cell = pivotCell + new Vector3Int(offset.x, offset.y, 0);
                Vector3Int direction = GetPushDirection(tile.fieldPushDirection, casterCell, cell, forward);
                if (direction == Vector3Int.zero) direction = forward;
                battle.Fields.Apply(tilemap, cell, caster, tile.fieldEffect,
                    tile.fieldEffectValue, tile.fieldEffectDuration, direction);
            }

        var groupedHits = new Dictionary<CharacterBase, List<SkillTargetHit>>();
        foreach (SkillTargetHit hit in hits)
        {
            if (hit == null ||
                hit.target == null ||
                hit.target.IsDead ||
                hit.tile == null)
            {
                continue;
            }

            List<SkillTargetHit> tiles;
            if (!groupedHits.TryGetValue(hit.target, out tiles))
                groupedHits[hit.target] = tiles = new List<SkillTargetHit>();
            tiles.Add(hit);
        }

        foreach (var group in groupedHits)
        {
            CharacterBase target = group.Key;
            if (target == null || target.IsDead) continue;
            var damageByCell = new Dictionary<Vector3Int, int>();
            SkillPatternTile pushTile = null;
            SkillPatternTile debuffTile = null;
            foreach (SkillTargetHit hit in group.Value)
            {
                SkillPatternTile tile = hit.tile;
                Vector3Int cell = hit.cell;
                cell.z = 0;
                if (!BattleTileOccupancy.ContainsCell(target, tilemap, cell) || !tilemap.HasTile(cell)) continue;
                int previous;
                damageByCell.TryGetValue(cell, out previous);
                if (tile.damage > previous) damageByCell[cell] = tile.damage;
                if (tile.push && (pushTile == null || tile.pushDistance > pushTile.pushDistance)) pushTile = tile;
                if (tile.appliesDebuff && debuffTile == null) debuffTile = tile;
            }

            if (skill.effectType.HasFlag(SkillEffectType.Damage) &&
                damageByCell.Count > 0)
            {
                result.hitCount++;
                int dealt = BattleTileOccupancy.ApplyDamage(target, tilemap, damageByCell);
                if (dealt > 0) result.damagedTargets.Add(target);

                if (target == null || target.IsDead)
                {
                    result.killCount++;
                    continue;
                }
            }

            if (skill.effectType.HasFlag(SkillEffectType.Push) &&
                pushTile != null &&
                pushTile.pushDistance > 0)
            {
                Vector3Int targetCell =
                    tilemap.WorldToCell(target.transform.position);
                targetCell.z = 0;

                Vector3Int pushDirection = GetPushDirection(
                    pushTile.pushDirection,
                    casterCell,
                    targetCell,
                    forward
                );

                TryPush(
                    target,
                    pushDirection,
                    pushTile.pushDistance
                );
            }

            if (debuffTile != null)
            {
                Debug.Log(
                    $"[Skill] Debuff '{debuffTile.debuffType}' is configured " +
                    $"for {target.name}, but runtime debuff handling " +
                    "is not implemented yet."
                );
            }

        }

        return result;
    }

    private static Vector2Int RotateOffset(Vector2Int position, int rotation)
    {
        rotation = ((rotation % 4) + 4) % 4;
        switch (rotation)
        {
            case 1: return new Vector2Int(-position.y, position.x);
            case 2: return new Vector2Int(-position.x, -position.y);
            case 3: return new Vector2Int(position.y, -position.x);
            default: return position;
        }
    }

    private Vector3Int GetPatternForward(int rotation)
    {
        rotation = ((rotation % 4) + 4) % 4;

        switch (rotation)
        {
            case 1:
                return Vector3Int.up;
            case 2:
                return Vector3Int.left;
            case 3:
                return Vector3Int.down;
            default:
                return Vector3Int.right;
        }
    }

    private Vector3Int GetPushDirection(
        SkillPushDirection pushDirection,
        Vector3Int casterCell,
        Vector3Int targetCell,
        Vector3Int forward)
    {
        switch (pushDirection)
        {
            case SkillPushDirection.TowardCaster:
                return GetCardinalDirection(targetCell, casterCell);

            case SkillPushDirection.Forward:
                return forward;

            case SkillPushDirection.Backward:
                return -forward;

            case SkillPushDirection.AwayFromCaster:
            default:
                return GetCardinalDirection(casterCell, targetCell);
        }
    }

    /// <summary>
    /// Pushes one character in a cardinal tile direction.
    /// Stops before map bounds, missing tiles, or occupied tiles.
    /// Returns the number of tiles actually moved.
    /// </summary>
    public int TryPush(
        CharacterBase target,
        Vector3Int direction,
        int distance)
    {
        if (target == null || target.IsDead || distance <= 0)
            return 0;

        if (PlacementManager.Instance == null ||
            PlacementManager.Instance.tilemap == null)
            return 0;

        direction = NormalizeCardinal(direction);

        if (direction == Vector3Int.zero)
            return 0;

        Tilemap tilemap = PlacementManager.Instance.tilemap;

        if (target is MonsterBase monster)
        {
            Vector3Int anchor = monster.GetAnchorCell(tilemap);
            Vector3Int destination = anchor;
            int moved = 0;
            for (int i = 0; i < distance; i++)
            {
                Vector3Int next = destination + direction;
                if (!monster.CanPlaceAt(tilemap, next)) break;
                destination = next;
                moved++;
            }
            if (moved == 0 || !monster.TryPlace(tilemap, destination)) return 0;
            MovementModule movement = target.GetComponent<MovementModule>();
            if (movement != null) movement.StopMovement();
            return moved;
        }

        Vector3Int startCell = tilemap.WorldToCell(target.transform.position);
        startCell.z = 0;

        Vector3Int destinationCell = startCell;
        int movedDistance = 0;

        for (int i = 0; i < distance; i++)
        {
            Vector3Int nextCell = destinationCell + direction;

            if (!tilemap.HasTile(nextCell))
                break;

            TileData nextData = PlacementManager.Instance.GetTileData(nextCell);

            if (nextData == null || !nextData.isempty)
                break;

            destinationCell = nextCell;
            movedDistance++;
        }

        if (movedDistance <= 0)
        {
            Debug.Log($"[Push Blocked] {target.name} at {startCell}");
            return 0;
        }

        TileData startData = PlacementManager.Instance.GetTileData(startCell);
        if (startData != null)
        {
            startData.isempty = true;
            startData.Character = null;
        }

        TileData destinationData =
            PlacementManager.Instance.GetTileData(destinationCell);

        if (destinationData != null)
        {
            destinationData.isempty = false;
            destinationData.Character = target;
        }

        Vector3 destinationWorld =
            tilemap.GetCellCenterWorld(destinationCell);
        destinationWorld.z = target.transform.position.z;
        target.transform.position = destinationWorld;

        MoveTileModule moveTile = target.GetComponent<MoveTileModule>();
        if (moveTile != null)
        {
            moveTile.StopMovement();
            moveTile.UpdateCurrentTile();
        }

        Debug.Log(
            $"[Push] {target.name} : {startCell} -> {destinationCell} " +
            $"({movedDistance}/{distance} tiles)");

        return movedDistance;
    }

    private Vector3Int GetCardinalDirection(
        Vector3Int from,
        Vector3Int to)
    {
        int dx = to.x - from.x;
        int dy = to.y - from.y;

        if (Mathf.Abs(dx) >= Mathf.Abs(dy))
        {
            if (dx > 0) return Vector3Int.right;
            if (dx < 0) return Vector3Int.left;
        }
        else
        {
            if (dy > 0) return Vector3Int.up;
            if (dy < 0) return Vector3Int.down;
        }

        return Vector3Int.zero;
    }

    private Vector3Int NormalizeCardinal(Vector3Int direction)
    {
        if (Mathf.Abs(direction.x) >= Mathf.Abs(direction.y))
        {
            if (direction.x > 0) return Vector3Int.right;
            if (direction.x < 0) return Vector3Int.left;
        }
        else
        {
            if (direction.y > 0) return Vector3Int.up;
            if (direction.y < 0) return Vector3Int.down;
        }

        return Vector3Int.zero;
    }
}

