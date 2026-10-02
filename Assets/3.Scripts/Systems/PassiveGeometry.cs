using UnityEngine;

/// <summary>All positions are cells on the current battle Tilemap, independent of world scale.</summary>
public static class PassiveGeometry
{
    public static int Distance(Vector3Int a, Vector3Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    public static bool IsOnBoard(Vector3Int cell, BoundsInt bounds)
    {
        return cell.x >= bounds.xMin && cell.x < bounds.xMax
            && cell.y >= bounds.yMin && cell.y < bounds.yMax;
    }

    public static bool IsOnEdge(Vector3Int cell, BoundsInt bounds)
    {
        return IsOnBoard(cell, bounds)
            && (cell.x == bounds.xMin || cell.x == bounds.xMax - 1
                || cell.y == bounds.yMin || cell.y == bounds.yMax - 1);
    }

    public static bool IsCorner(Vector3Int cell, BoundsInt bounds)
    {
        return IsOnBoard(cell, bounds)
            && (cell.x == bounds.xMin || cell.x == bounds.xMax - 1)
            && (cell.y == bounds.yMin || cell.y == bounds.yMax - 1);
    }

    public static bool AreSideNeighbours(Vector3Int owner, Vector3Int ally, BoundsInt bounds)
    {
        // Along the perimeter: left/right on horizontal edges, up/down on vertical edges.
        // At corners either neighbouring perimeter tile is accepted. No diagonal adjacency.
        return Distance(owner, ally) == 1
            && IsOnEdge(owner, bounds) && IsOnEdge(ally, bounds);
    }

    public static bool AreOpposite(Vector3Int owner, Vector3Int ally, BoundsInt bounds)
    {
        if (owner == ally || !IsOnBoard(owner, bounds) || !IsOnBoard(ally, bounds))
            return false;

        return (owner.x == ally.x
                && ((owner.y == bounds.yMin && ally.y == bounds.yMax - 1)
                    || (ally.y == bounds.yMin && owner.y == bounds.yMax - 1)))
            || (owner.y == ally.y
                && ((owner.x == bounds.xMin && ally.x == bounds.xMax - 1)
                    || (ally.x == bounds.xMin && owner.x == bounds.xMax - 1)));
    }

    public static Vector3Int FrontCell(Vector3Int owner, Vector3Int aim, BoundsInt bounds)
    {
        Vector3Int direction = CardinalDirection(owner, aim);
        // Corners have two inward directions. Prefer the attack's direction when valid.
        if (IsCorner(owner, bounds) && direction != Vector3Int.zero
            && IsOnBoard(owner + direction, bounds))
            return owner + direction;

        if (owner.x == bounds.xMin) return owner + Vector3Int.right;
        if (owner.x == bounds.xMax - 1) return owner + Vector3Int.left;
        if (owner.y == bounds.yMin) return owner + Vector3Int.up;
        if (owner.y == bounds.yMax - 1) return owner + Vector3Int.down;
        return owner + direction;
    }

    private static Vector3Int CardinalDirection(Vector3Int from, Vector3Int to)
    {
        int dx = to.x - from.x;
        int dy = to.y - from.y;
        if (Mathf.Abs(dx) >= Mathf.Abs(dy))
            return dx > 0 ? Vector3Int.right : dx < 0 ? Vector3Int.left : Vector3Int.zero;
        return dy > 0 ? Vector3Int.up : Vector3Int.down;
    }
}
