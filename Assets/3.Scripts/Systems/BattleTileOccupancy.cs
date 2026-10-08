using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>Queries use occupied cells rather than a large unit's visual center.</summary>
public static class BattleTileOccupancy
{
    public static IEnumerable<Vector3Int> Cells(CharacterBase character, Tilemap map)
    {
        if (character == null || map == null) yield break;
        if (character is MonsterBase monster)
        {
            foreach (Vector3Int cell in monster.GetOccupiedCells(map)) yield return cell;
        }
        else
        {
            Vector3Int cell = map.WorldToCell(character.transform.position); cell.z = 0;
            yield return cell;
        }
    }

    public static bool ContainsCell(CharacterBase character, Tilemap map, Vector3Int cell)
    {
        foreach (Vector3Int occupied in Cells(character, map)) if (occupied == cell) return true;
        return false;
    }

    public static bool IsOnMap(CharacterBase character, Tilemap map)
    {
        if (map == null) return false;
        foreach (Vector3Int cell in Cells(character, map)) if (map.HasTile(cell)) return true;
        return false;
    }

    public static bool Intersects(CharacterBase character, Tilemap map, ICollection<Vector3Int> area)
    {
        if (area == null) return false;
        foreach (Vector3Int cell in Cells(character, map)) if (area.Contains(cell)) return true;
        return false;
    }

    public static int Distance(CharacterBase character, Tilemap map, Vector3Int origin)
    {
        int distance = int.MaxValue;
        foreach (Vector3Int cell in Cells(character, map))
            distance = Mathf.Min(distance, Mathf.Abs(cell.x - origin.x) + Mathf.Abs(cell.y - origin.y));
        return distance;
    }

    public static bool TryGetClosestCell(CharacterBase character, Tilemap map, Vector3Int origin,
        out Vector3Int closest, ICollection<Vector3Int> area = null)
    {
        closest = new Vector3Int();
        int distance = int.MaxValue;
        foreach (Vector3Int cell in Cells(character, map))
        {
            if (!map.HasTile(cell) || (area != null && !area.Contains(cell))) continue;
            int candidate = Mathf.Abs(cell.x - origin.x) + Mathf.Abs(cell.y - origin.y);
            if (candidate >= distance) continue;
            closest = cell;
            distance = candidate;
        }
        return distance != int.MaxValue;
    }

    public static int ApplyDamage(CharacterBase character, Tilemap map, IDictionary<Vector3Int, int> damageByCell)
    {
        if (character == null || character.IsDead || damageByCell == null) return 0;
        if (character is MonsterBase monster) return monster.TakeDamageOnCells(map, damageByCell);
        int damage = 0;
        foreach (var hit in damageByCell)
            if (ContainsCell(character, map, hit.Key)) damage = Mathf.Max(damage, hit.Value);
        int before = character.currentHP;
        if (damage > 0) character.TakeDamage(damage);
        return before - character.currentHP;
    }

    public static CharacterBase FindAt(Tilemap map, Vector3Int cell)
    {
        if (map == null || !map.HasTile(cell)) return null;
        CharacterBase target = PlacementManager.Instance != null ? PlacementManager.Instance.GetTileData(cell).Character : null;
        if (IsLive(target) && ContainsCell(target, map, cell)) return target;
        // Legacy/manual enemies may not have registered their occupancy yet.
        foreach (Collider2D hit in Physics2D.OverlapPointAll(map.GetCellCenterWorld(cell)))
        {
            target = hit.GetComponentInParent<CharacterBase>();
            if (IsLive(target) && ContainsCell(target, map, cell)) return target;
        }
        return null;
    }

    private static bool IsLive(CharacterBase target) => target != null && target.gameObject.activeInHierarchy
        && (!(target.isEnemy || target is MonsterBase) || !target.IsDead);
}
