using System.Collections.Generic;
using UnityEngine;

public enum SkillType
{
    Active = 10, Passive = 20, Normal = 0, Link = 99, Ultimate = 999
}

public enum SkillClassType
{
    Breaker, Buster, Supporter, Sniper
}

public enum SkillTargetType
{
    Enemy, Ally, Self, Position, Skill, SkillAOE, SkillRange, Field
}

public enum SkillElementType
{
    Fire, Ice, Earth, Wind, Dark, Electric
}

public enum SkillRangeType
{
    Melee, Ranged, Global
}

public enum SkillAoeType
{
    Single, Line, Circle, Cone
}

[System.Flags]
public enum SkillEffectType
{
    None = 0,
    Damage = 1 << 0,
    Push = 1 << 1,
    Heal = 1 << 2,
    Buff = 1 << 3,
    Debuff = 1 << 4,
    Summon = 1 << 5
}

public enum SkillStatusEffectType
{
    Stun, Paralysis, Slow, Burn, Freeze, Airborne
}

public enum SkillFieldEffectType
{
    Fire, Ice, Earth, Wind, Electric, Dark
}

public enum SkillTileFieldEffectType
{
    None,
    Fire,
    Electric,
    Ice,
    Wind,
    Earth,
    Dark
}

public enum SkillPushDirection
{
    AwayFromCaster,
    TowardCaster,
    Forward,
    Backward
}


[System.Flags]
public enum SkillAllowedClasses { None = 0, Breaker = 1, Buster = 2, Supporter = 4, Sniper = 8 }
public enum SkillTargetSelection { LegacyAll = 0, Random = 1, Direct = 2, Nearest = 3, Farthest = 4 }
public enum SkillActivationTrigger { Manual, TurnStart, TurnEnd, MoveComplete, AttackHit, Kill, Damaged, EnterField, Custom }
public enum SkillValueType { None, Integer, Boolean }
public enum SkillComparison { Equal, NotEqual, Greater, GreaterOrEqual, Less, LessOrEqual }
public enum SkillConditionLogic { All, Any }
public enum SkillDurationType { Instant, Turns, Uses, Permanent, UntilCondition }
public enum SkillEffectKind { Damage, Heal, Field, Status, Movement, Stats, Object, SkillOperation, Cooldown, Range, Resistance, PositionSwap, Zone, Resource, Action }
public enum SkillEffectTiming { OnCast, BeforeHit, OnHit, AfterHit, TurnStart, TurnEnd, OnExpire, OnStatusRemoved }

[System.Serializable]
public class SkillActivationCondition
{
    public string conditionKey;
    public SkillValueType valueType;
    public SkillComparison comparison;
    public int intValue;
    public bool boolValue;
}

[System.Serializable]
public class SkillConfiguredEffect
{
    public SkillEffectKind kind;
    public SkillEffectTiming timing;
    [Range(0, 100)] public int chance = 100;
    public SkillTargetType target = SkillTargetType.Enemy;
    [Min(0)] public int delayTurns;
    public SkillDurationType duration = SkillDurationType.Instant;
    [Min(0)] public int durationValue = 1;
    public int magnitude;
    public int armorDamage;
    public SkillElementType element;
    public SkillTileFieldEffectType fieldType;
    [Min(1)] public int fieldTurns = 1;
    public int fieldPower = 1;
    public SkillStatusEffectType status;
    public int statusStacks = 1;
    public SkillPushDirection movementDirection;
    [Min(0)] public int movementTiles;
    public string detailKey;
    public string notes;
}

[System.Serializable]
public class SkillPatternTile
{
    public Vector2Int position;
    public int distanceFromCaster;

    public int damage;
    public bool appliesDebuff;
    public SkillStatusEffectType debuffType;

    public SkillTileFieldEffectType fieldEffect;
    [Tooltip("화염/전기 피해량, 바람 이동 칸 수, 대지/암흑의 결계 피해 감소량입니다.")]
    [Min(1)] public int fieldEffectValue = 1;
    [Tooltip("적 턴 시작 시 적용할 횟수입니다. 같은 칸에 재시전하면 갱신합니다.")]
    [Min(1)] public int fieldEffectDuration = 3;
    public SkillPushDirection fieldPushDirection = SkillPushDirection.AwayFromCaster;

    public bool push;
    public SkillPushDirection pushDirection = SkillPushDirection.AwayFromCaster;

    [Tooltip("0이면 대상 수 제한 없음")]
    [Min(0)]
    public int pushTargetCount;

    [Min(0)]
    public int pushDistance;
}

[CreateAssetMenu(fileName = "Skill", menuName = "SkillContainer")]
public class SkillList : ScriptableObject
{
    [Header("공통")]
    public string skillName;
    [TextArea] public string description;
    public int id;
    public SkillType type;
    public SkillClassType classType;
    public SkillElementType elementType;
    public SkillEffectType effectType;
    public SkillTargetType targetType;
    public Sprite icon;
    public int cost;
    public string condition;
    public bool canRotate;
    public int cooldown;
    public int delay;
    public int level;
    public int MaxLevel;
    public List<SkillList> skillsList;


    [Header("사용 / 발동 조건 (신규)")]
    public bool restrictClasses;
    public SkillAllowedClasses allowedClasses = SkillAllowedClasses.None;
    [Tooltip("1~12 중 사용 가능 캐릭터 번호. 비어 있으면 제한 없음")]
    public List<int> allowedCharacterNumbers = new List<int>();
    public SkillActivationTrigger activationTrigger;
    public SkillConditionLogic conditionLogic;
    public List<SkillActivationCondition> activationConditions = new List<SkillActivationCondition>();
    [Range(0, 100)] public int activationChance = 100;

    [Header("타겟 및 공격 틱 (신규)")]
    public SkillTargetSelection targetSelection = SkillTargetSelection.LegacyAll;
    [Tooltip("공격 1틱당 공격 대상 수. 0 = 제한 없음 (기존 방식)")]
    [Min(0)] public int targetCount = 0;
    [Min(1)] public int minAttackTicks = 1;
    [Min(1)] public int maxAttackTicks = 1;

    [Header("지속 / 효과 목록 (신규)")]
    public SkillDurationType durationType = SkillDurationType.Instant;
    [Min(0)] public int durationValue;
    [Min(0)] public int maxActivationsPerTurnForSkill;
    public List<SkillConfiguredEffect> configuredEffects = new List<SkillConfiguredEffect>();

    [Header("타일 패턴")]
    public List<Vector2Int> rangePattern = new List<Vector2Int>();
    public List<SkillPatternTile> roePattern = new List<SkillPatternTile>();

    // Legacy fallback data. Kept serialized so existing skill assets continue to work.
 
    [HideInInspector] public SkillFieldEffectType fieldEffectType;
    [HideInInspector] public SkillStatusEffectType statusEffectType;
    [HideInInspector] public int range;
    [HideInInspector] public int aoe;
    [HideInInspector] public int damage;
    [HideInInspector] public int pushDistance;

    public bool HasRangePattern => rangePattern != null && rangePattern.Count > 0;
    public bool HasRoePattern => roePattern != null && roePattern.Count > 0;

    public virtual int CompareByType(SkillList other)
    {
        if (other == null) return 1;
        int result = type - other.type;
        if (result != 0) return result;
        return id - other.id;
    }
}
