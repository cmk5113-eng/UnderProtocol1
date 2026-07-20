using UnityEngine;

[CreateAssetMenu(fileName = "Skill", menuName = "ActiveSkill")]
public class ActiveSkill : SkillList
{

    
    
    public virtual bool IsUsable(CharacterBase from, CharacterBase to) => true;
    public virtual void Onuse(CharacterBase from, CharacterBase to)
    { }
    public virtual bool IsUsable(CharacterBase from, Vector3 position) => true;
    public virtual void OnUse(CharacterBase from, Vector3 position)
    { }

}