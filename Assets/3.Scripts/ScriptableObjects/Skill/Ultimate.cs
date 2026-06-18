using UnityEngine;

[CreateAssetMenu(fileName = "Skill", menuName = "UltimateSkill")]
public class UltimateSkill : SkillContainer
{

    public string UltimateName;
    public string UltimateDescription;
    public Sprite UltimateIcon;
    public virtual bool IsUsable(CharacterBase from, CharacterBase to) => true;
    public virtual void Onuse(CharacterBase from, CharacterBase to)
    { }
    public virtual bool IsUsable(CharacterBase from, Vector3 position) => true;
    public virtual void OnUse(CharacterBase from, Vector3 position)
    { }

}