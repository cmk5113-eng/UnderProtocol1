using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public sealed class BattleFieldEffect
{
    public SkillTileFieldEffectType Type { get; internal set; }
    public int Value { get; internal set; }
    public int RemainingTurns { get; internal set; }
    internal CharacterBase source;
    internal Vector3Int direction;
}

/// <summary>Battle-local fields. One effect per cell; casting again replaces and refreshes it.</summary>
public sealed class BattleFieldEffectSystem
{
    private Tilemap fieldMap;
    private readonly Dictionary<Vector3Int, BattleFieldEffect> fields
        = new Dictionary<Vector3Int, BattleFieldEffect>();
    private readonly HashSet<CharacterBase> frozenThisTurn = new HashSet<CharacterBase>();
    private readonly Dictionary<CharacterBase, int> damageReductions = new Dictionary<CharacterBase, int>();
    private int lastProcessedTurn = -1;

    public int Count => fields.Count;
    public bool IsResolving { get; private set; }

    public bool TryGetField(Tilemap map, Vector3Int cell, out BattleFieldEffect field)
    {
        cell.z = 0;
        field = null;
        return map != null && map == fieldMap && map.HasTile(cell) && fields.TryGetValue(cell, out field);
    }

    public bool Apply(Tilemap map, Vector3Int cell, CharacterBase source,
        SkillTileFieldEffectType type, int value, int duration, Vector3Int direction)
    {
        cell.z = 0;
        if (map == null || !map.HasTile(cell) || type == SkillTileFieldEffectType.None) return false;
        if (fieldMap != map) ResetBattle();
        fieldMap = map;
        fields[cell] = new BattleFieldEffect
        {
            Type = type, Value = Mathf.Max(1, value), RemainingTurns = Mathf.Max(1, duration),
            source = source, direction = direction
        };
        Paint(cell, ColorFor(type));
        return true;
    }

    public void ResetBattle()
    {
        foreach (Vector3Int cell in fields.Keys) Paint(cell, Color.white);
        fields.Clear();
        fieldMap = null;
        ClearTurnModifiers();
        lastProcessedTurn = -1;
        IsResolving = false;
    }

    public void ClearTurnModifiers()
    {
        frozenThisTurn.Clear();
        damageReductions.Clear();
    }

    public bool ShouldSkipMonsterAction(CharacterBase target) => target != null && frozenThisTurn.Contains(target);

    public int MonsterDamage(CharacterBase target, int baseDamage)
    {
        int reduction;
        return Mathf.Max(0, baseDamage - (target != null && damageReductions.TryGetValue(target, out reduction) ? reduction : 0));
    }

    /// <summary>Fields tick before enemies deal shared HP damage, once per completed player turn.</summary>
    public void BeforeMonsterTurn(int turn, IList<CharacterBase> characters, Tilemap map,
        ExecuteSkill executor, BattlePassiveSystem passives)
    {
        if (IsResolving || lastProcessedTurn == turn || characters == null || map == null) return;
        lastProcessedTurn = turn;
        ClearTurnModifiers();
        if (map != fieldMap)
        {
            ResetBattle();
            lastProcessedTurn = turn;
            return;
        }

        // Snapshot occupants so wind cannot cause a second field tick by moving into another cell.
        var affected = new Dictionary<CharacterBase, Dictionary<SkillTileFieldEffectType, BattleFieldEffect>>();
        foreach (CharacterBase target in characters)
        {
            if (!IsEnemy(target) || affected.ContainsKey(target)) continue;
            var effects = new Dictionary<SkillTileFieldEffectType, BattleFieldEffect>();
            foreach (Vector3Int cell in BattleTileOccupancy.Cells(target, map))
            {
                BattleFieldEffect field;
                if (!map.HasTile(cell) || !fields.TryGetValue(cell, out field)) continue;
                BattleFieldEffect existing;
                if (!effects.TryGetValue(field.Type, out existing) || field.Value > existing.Value)
                    effects[field.Type] = field;
            }
            affected[target] = effects;
        }

        IsResolving = true;
        try
        {
            foreach (var targetEffects in affected)
            {
                CharacterBase target = targetEffects.Key;
                foreach (BattleFieldEffect field in targetEffects.Value.Values)
                {
                    if (!IsEnemy(target)) continue;
                    switch (field.Type)
                    {
                        case SkillTileFieldEffectType.Fire:
                        case SkillTileFieldEffectType.Electric:
                            target.TakeDamage(field.Value);
                            if (!IsEnemy(target) && passives != null)
                                passives.NotifyDamageKill(field.source, true);
                            break;
                        case SkillTileFieldEffectType.Ice:
                            frozenThisTurn.Add(target);
                            break;
                        case SkillTileFieldEffectType.Wind:
                            if (executor != null) executor.TryPush(target, field.direction, field.Value);
                            break;
                        case SkillTileFieldEffectType.Earth:
                        case SkillTileFieldEffectType.Dark:
                            int reduction;
                            damageReductions.TryGetValue(target, out reduction);
                            damageReductions[target] = Mathf.Max(reduction, field.Value);
                            break;
                    }
                }
            }
            foreach (Vector3Int cell in new List<Vector3Int>(fields.Keys))
            {
                if (!map.HasTile(cell)) { Remove(cell); continue; }
                BattleFieldEffect field = fields[cell];
                field.RemainingTurns--;
                if (field.RemainingTurns <= 0) Remove(cell);
            }
        }
        finally { IsResolving = false; }
    }

    private static bool IsEnemy(CharacterBase target) => target != null && !target.IsDead
        && target.gameObject.activeInHierarchy && (target.isEnemy || target is MonsterBase);

    private void Remove(Vector3Int cell)
    {
        Paint(cell, Color.white);
        fields.Remove(cell);
    }

    private void Paint(Vector3Int cell, Color color)
    {
        if (fieldMap == null || !fieldMap.HasTile(cell)) return;
        fieldMap.SetTileFlags(cell, TileFlags.None);
        fieldMap.SetColor(cell, color);
    }

    // Skill/movement previews temporarily cover fields; clearing a preview restores their tint.
    public static Color TileColor(Tilemap map, Vector3Int cell)
    {
        BattleFieldEffect field;
        BattleManager battle = BattleManager.Instance;
        return battle != null && battle.Fields.TryGetField(map, cell, out field) ? ColorFor(field.Type) : Color.white;
    }

    public static string Describe(Tilemap map, Vector3Int cell)
    {
        BattleFieldEffect field;
        BattleManager battle = BattleManager.Instance;
        if (battle == null || !battle.Fields.TryGetField(map, cell, out field)) return string.Empty;
        string label;
        switch (field.Type)
        {
            case SkillTileFieldEffectType.Fire: label = "화염"; break;
            case SkillTileFieldEffectType.Electric: label = "전기"; break;
            case SkillTileFieldEffectType.Ice: label = "얼음"; break;
            case SkillTileFieldEffectType.Wind: label = "바람"; break;
            case SkillTileFieldEffectType.Earth: label = "대지"; break;
            default: label = "암흑"; break;
        }
        return $"필드: {label} · 세기 {field.Value} · 남은 {field.RemainingTurns}턴";
    }

    private static Color ColorFor(SkillTileFieldEffectType type)
    {
        switch (type)
        {
            case SkillTileFieldEffectType.Fire: return new Color(1f, 0.45f, 0.25f);
            case SkillTileFieldEffectType.Electric: return new Color(1f, 0.85f, 0.2f);
            case SkillTileFieldEffectType.Ice: return new Color(0.4f, 0.85f, 1f);
            case SkillTileFieldEffectType.Wind: return new Color(0.4f, 1f, 0.65f);
            case SkillTileFieldEffectType.Earth: return new Color(0.75f, 0.55f, 0.3f);
            case SkillTileFieldEffectType.Dark: return new Color(0.65f, 0.4f, 0.85f);
            default: return Color.white;
        }
    }
}
