using System.Collections.Generic;
using System.IO.Enumeration;
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

//public enum SkillCostType
//{
//    Mana, Stamina, Health, Energy
//}
public enum SkillEffectType
{
    Damage, Heal, Buff, Debuff, Summon
}
//public enum SkillCooldownType
//{
//    Turn, Time, Action
//}
//public enum SkillDamageType
//{
//    Physical, Magical, True
//} 
//public enum SkillTargetingType
//{
//    Single, Multi, Area
//}
public enum SkillStatusEffectType
{
    Stun, Paralysis, Slow, Burn, Freeze, Airborne
}
public enum SkillFieldEffectType
{
    Fire, Water, Earth, Wind, Light, Dark
}


[CreateAssetMenu(fileName = "Skill", menuName = "SkillContainer")]
public class SkillList : ScriptableObject
{
    public string skillName;   
    public string description;
    public int id;
    public List<SkillList> skillsList;
    public SkillType type;
    public SkillClassType classType;
    public SkillElementType elementType;
    public SkillRangeType rangeType;
    public SkillAoeType aoeType;
    public SkillEffectType effectType;
    public SkillTargetType targetType;
    public SkillFieldEffectType fieldEffectType;
    public SkillStatusEffectType statusEffectType;
    public Sprite icon;
    public int range;
    public int aoe;
    public int cost;
    public int cooldown;
    public int delay;
    public int damage;
    public int level;
    public int MaxLevel;

    public virtual int CompareByType(SkillList other)
    {
        if (other == null) return 1;
        int result = type - other.type;
        if (result != 0) return result;
        return id - other.id;
    }

}