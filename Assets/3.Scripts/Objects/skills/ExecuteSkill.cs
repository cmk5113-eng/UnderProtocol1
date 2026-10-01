using System.Collections.Generic;
using UnityEngine;

public class SkillExecuteResult
{
    public int hitCount;
    public int killCount;
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
            PushSkill(skill, targets);
        }

        return result;
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
            if (target == null || target.IsDead)
                continue;

            result.hitCount++;
            target.TakeDamage(skill.damage);

            if (target == null || target.IsDead)
            {
                result.killCount++;
            }
        }
    }

    private void PushSkill(
        SkillList skill,
        List<CharacterBase> targets)
    {
        if (targets == null || skill.pushDistance <= 0)
            return;

        foreach (CharacterBase target in targets)
        {
            if (target == null || target.IsDead)
                continue;

            Debug.Log($"[Push] {target.name} : {skill.pushDistance} tiles");

            // Actual tile movement will be implemented after occupancy rules are defined.
        }
    }
}
