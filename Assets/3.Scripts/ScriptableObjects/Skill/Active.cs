using UnityEngine;

[CreateAssetMenu(fileName = "Skill", menuName = "ActiveSkill")]
public class ActiveSkill : SkillContainer
{

    public string ActiveName;
    public string ActiveDescription;
    public Sprite Icon;
    
    public virtual bool IsUsable(CharacterBase from, CharacterBase to) => true;
    public virtual void Onuse(CharacterBase from, CharacterBase to)
    { }
    public virtual bool IsUsable(CharacterBase from, Vector3 position) => true;
    public virtual void OnUse(CharacterBase from, Vector3 position)
    { }

}