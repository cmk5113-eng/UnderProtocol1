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

public class SkillTargetHit
{
    public CharacterBase target;
    public SkillPatternTile tile;

    public SkillTargetHit(CharacterBase target, SkillPatternTile tile)
    {
        this.target = target;
        this.tile = tile;
    }
}

public class ExecuteSkill : MonoBehaviour
{
    public static ExecuteSkill Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    public SkillExecuteResult Execute(
        CharacterBase caster,
        SkillList skill,
        List<CharacterBase> targets)
    {
        SkillExecuteResult result = new SkillExecuteResult();

        if (caster == null || skill == null)
            return result;

        if (skill.effectType.HasFlag(SkillEffectType.Damage))
        {
            AttackSkill(skill, targets, result);
        }

        if (skill.effectType.HasFlag(SkillEffectType.Push))
        {
            PushSkill(caster, skill, targets);
        }

        return result;
    }

    public SkillExecuteResult ExecutePattern(
        CharacterBase caster,
        SkillList skill,
        Vector3Int pivotCell,
        List<SkillTargetHit> hits,
        int patternRotation = 0)
    {
        SkillExecuteResult result = new SkillExecuteResult();

        if (caster == null || skill == null || hits == null)
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

        foreach (SkillTargetHit hit in hits)
        {
            if (hit == null ||
                hit.target == null ||
                hit.target.IsDead ||
                hit.tile == null)
            {
                continue;
            }

            CharacterBase target = hit.target;
            SkillPatternTile tile = hit.tile;

            if (skill.effectType.HasFlag(SkillEffectType.Damage) &&
                tile.damage > 0)
            {
                result.hitCount++;
                if (!result.damagedTargets.Contains(target)) result.damagedTargets.Add(target);
                target.TakeDamage(tile.damage);

                if (target == null || target.IsDead)
                {
                    result.killCount++;
                    continue;
                }
            }

            if (skill.effectType.HasFlag(SkillEffectType.Push) &&
                tile.push &&
                tile.pushDistance > 0)
            {
                Vector3Int targetCell =
                    tilemap.WorldToCell(target.transform.position);
                targetCell.z = 0;

                Vector3Int pushDirection = GetPushDirection(
                    tile.pushDirection,
                    casterCell,
                    targetCell,
                    forward
                );

                TryPush(
                    target,
                    pushDirection,
                    tile.pushDistance
                );
            }

            if (tile.appliesDebuff)
            {
                Debug.Log(
                    $"[Skill] Debuff '{tile.debuffType}' is configured " +
                    $"for {target.name}, but runtime debuff handling " +
                    "is not implemented yet."
                );
            }

            if (tile.fieldEffect != SkillTileFieldEffectType.None)
            {
                Debug.Log(
                    $"[Skill] Field effect '{tile.fieldEffect}' is " +
                    "configured, but runtime field effect handling " +
                    "is not implemented yet."
                );
            }
        }

        return result;
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

    private void AttackSkill(
        SkillList skill,
        List<CharacterBase> targets,
        SkillExecuteResult result)
    {
        if (targets == null)
            return;

        foreach (CharacterBase target in targets)
        {
            if (target == null || target.IsDead || skill.damage <= 0)
                continue;

            result.hitCount++;
            if (!result.damagedTargets.Contains(target)) result.damagedTargets.Add(target);
            target.TakeDamage(skill.damage);

            if (target == null || target.IsDead)
            {
                result.killCount++;
            }
        }
    }

    private void PushSkill(
        CharacterBase caster,
        SkillList skill,
        List<CharacterBase> targets)
    {
        if (caster == null || targets == null || skill.pushDistance <= 0)
            return;

        if (PlacementManager.Instance == null ||
            PlacementManager.Instance.tilemap == null)
            return;

        Tilemap tilemap = PlacementManager.Instance.tilemap;
        Vector3Int casterCell = tilemap.WorldToCell(caster.transform.position);
        casterCell.z = 0;

        foreach (CharacterBase target in targets)
        {
            if (target == null || target.IsDead)
                continue;

            Vector3Int targetCell = tilemap.WorldToCell(target.transform.position);
            targetCell.z = 0;

            Vector3Int direction = GetCardinalDirection(casterCell, targetCell);

            if (direction == Vector3Int.zero)
                continue;

            TryPush(target, direction, skill.pushDistance);
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
