using UnityEngine;

[CreateAssetMenu(fileName = "Skill", menuName = "PassiveSkill")]
public class PassiveSkill : SkillContainer
{

    public string PassiveName;
    public string PassiveDescription;
    public Sprite PassiveIcon;
    public virtual bool IsUsable(CharacterBase from, CharacterBase to) => true;
    public virtual void Onuse(CharacterBase from, CharacterBase to)
    { }
    public virtual bool IsUsable(CharacterBase from, Vector3 position) => true;
    public virtual void OnUse(CharacterBase from, Vector3 position)
    { }

}