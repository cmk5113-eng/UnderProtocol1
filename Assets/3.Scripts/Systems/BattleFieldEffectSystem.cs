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
    private readonly Dictionary<Vector3Int, SpriteRenderer> fieldVisuals
        = new Dictionary<Vector3Int, SpriteRenderer>();
    private readonly Dictionary<SkillTileFieldEffectType, Sprite> fieldSprites
        = new Dictionary<SkillTileFieldEffectType, Sprite>();
    private Transform visualRoot;
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
        ShowField(cell, type);
        return true;
    }

    public void ResetBattle()
    {
        // Hide immediately: Destroy is deferred until the end of the Unity frame.
        if (visualRoot != null)
        {
            visualRoot.gameObject.SetActive(false);
            Object.Destroy(visualRoot.gameObject);
        }
        visualRoot = null;
        fieldVisuals.Clear();
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
        var affected = new Dictionary<CharacterBase, Dictionary<SkillTileFieldEffectType, Dictionary<Vector3Int, BattleFieldEffect>>>();
        foreach (CharacterBase target in characters)
        {
            if (!IsEnemy(target) || affected.ContainsKey(target)) continue;
            var effects = new Dictionary<SkillTileFieldEffectType, Dictionary<Vector3Int, BattleFieldEffect>>();
            foreach (Vector3Int cell in BattleTileOccupancy.Cells(target, map))
            {
                BattleFieldEffect field;
                if (!map.HasTile(cell) || !fields.TryGetValue(cell, out field)) continue;
                Dictionary<Vector3Int, BattleFieldEffect> contacts;
                if (!effects.TryGetValue(field.Type, out contacts))
                    effects[field.Type] = contacts = new Dictionary<Vector3Int, BattleFieldEffect>();
                contacts[cell] = field;
            }
            affected[target] = effects;
        }

        IsResolving = true;
        try
        {
            foreach (var targetEffects in affected)
            {
                CharacterBase target = targetEffects.Key;
                // Resolve damage at the original body cells before wind moves the whole body.
                foreach (SkillTileFieldEffectType type in new[] { SkillTileFieldEffectType.Fire, SkillTileFieldEffectType.Electric })
                {
                    if (!IsEnemy(target)) break;
                    Dictionary<Vector3Int, BattleFieldEffect> contacts;
                    if (!targetEffects.Value.TryGetValue(type, out contacts)) continue;
                    var damageByCell = new Dictionary<Vector3Int, int>();
                    BattleFieldEffect damageSource = null;
                    foreach (var contact in contacts)
                    {
                        damageByCell[contact.Key] = contact.Value.Value;
                        if (target is MonsterBase monster && monster.HasCellShield(map, contact.Key)) continue;
                        if (damageSource == null || contact.Value.Value > damageSource.Value) damageSource = contact.Value;
                    }
                    int dealt = BattleTileOccupancy.ApplyDamage(target, map, damageByCell);
                    if (dealt > 0 && !IsEnemy(target) && passives != null && damageSource != null)
                        passives.NotifyDamageKill(damageSource.source, true);
                }
                foreach (var effectContacts in targetEffects.Value)
                {
                    if (!IsEnemy(target)) continue;
                    if (effectContacts.Key == SkillTileFieldEffectType.Fire || effectContacts.Key == SkillTileFieldEffectType.Electric) continue;
                    BattleFieldEffect field = null;
                    foreach (BattleFieldEffect candidate in effectContacts.Value.Values)
                        if (field == null || candidate.Value > field.Value) field = candidate;
                    switch (field.Type)
                    {
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
        if (fieldVisuals.TryGetValue(cell, out SpriteRenderer visual))
        {
            if (visual != null)
            {
                visual.gameObject.SetActive(false);
                Object.Destroy(visual.gameObject);
            }
            fieldVisuals.Remove(cell);
        }
        fields.Remove(cell);
    }

    private void ShowField(Vector3Int cell, SkillTileFieldEffectType type)
    {
        if (fieldMap == null || !fieldMap.HasTile(cell)) return;
        Sprite sprite = SpriteFor(type);
        if (sprite == null) return;

        if (visualRoot == null)
        {
            GameObject root = new GameObject("BattleFieldEffects");
            root.layer = fieldMap.gameObject.layer;
            root.transform.SetParent(fieldMap.transform, false);
            visualRoot = root.transform;
        }
        if (!fieldVisuals.TryGetValue(cell, out SpriteRenderer visual) || visual == null)
        {
            GameObject obj = new GameObject($"Field_{cell.x}_{cell.y}", typeof(SpriteRenderer));
            obj.layer = fieldMap.gameObject.layer;
            obj.transform.SetParent(visualRoot, false);
            fieldVisuals[cell] = visual = obj.GetComponent<SpriteRenderer>();
        }

        // A separate renderer is visible from Apply until expiry/reset, independently of selection.
        // Parenting and local cell coordinates also follow scaled/moved stage maps.
        visual.sprite = sprite;
        visual.color = Color.white;
        visual.transform.localPosition = fieldMap.GetCellCenterLocal(cell);
        Vector3 size = fieldMap.cellSize;
        Vector3 spriteSize = sprite.bounds.size;
        visual.transform.localScale = new Vector3(
            Mathf.Abs(size.x) / Mathf.Max(0.0001f, spriteSize.x),
            Mathf.Abs(size.y) / Mathf.Max(0.0001f, spriteSize.y), 1f);

        TilemapRenderer ground = fieldMap.GetComponent<TilemapRenderer>();
        visual.sortingLayerID = ground != null ? ground.sortingLayerID : 0;
        visual.sortingOrder = ground != null ? Mathf.Min(32767, ground.sortingOrder + 1) : 1;
        if (ground != null) visual.sharedMaterial = ground.sharedMaterial;
        visual.enabled = true;
        visual.gameObject.SetActive(true);
    }

    private Sprite SpriteFor(SkillTileFieldEffectType type)
    {
        if (fieldSprites.TryGetValue(type, out Sprite sprite)) return sprite;
        string name;
        switch (type)
        {
            case SkillTileFieldEffectType.Fire: name = "Fire"; break;
            case SkillTileFieldEffectType.Ice: name = "Ice"; break;
            case SkillTileFieldEffectType.Electric: name = "Electric"; break;
            case SkillTileFieldEffectType.Earth: name = "Earth"; break;
            case SkillTileFieldEffectType.Wind: name = "Wind"; break;
            // Keep the serialized Dark value so existing skill/map data stays compatible.
            case SkillTileFieldEffectType.Dark: name = "Gravity"; break;
            default: return null;
        }
        sprite = Resources.Load<Sprite>("BattleFields/" + name);
        fieldSprites[type] = sprite;
        if (sprite == null) Debug.LogError($"[BattleFieldEffect] BattleFields/{name} 스프라이트가 없습니다.");
        return sprite;
    }

    // Previews only tint the ground. The field sprite keeps its own colors above it.
    public static Color TileColor(Tilemap map, Vector3Int cell)
    {
        return Color.white;
    }

    public static Color PreviewColor(Tilemap map, Vector3Int cell, Color preview)
    {
        return preview;
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
            case SkillTileFieldEffectType.Dark: label = "중력"; break;
            default: return string.Empty;
        }
        return $"필드: {label} · 세기 {field.Value} · 남은 {field.RemainingTurns}턴";
    }

}

