using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Character")]
public class CharacterData : ScriptableObject
{
    public string characterName;
    public JobType job;
    public ElementType element;

    // 개별적으로 관리할 경우
    public SkillData skill1;
    public SkillData skill2;
    public SkillData skill3;
    public SkillData skill4;

    // 혹은 리스트로 한꺼번에 관리
    public List<SkillData> skills = new List<SkillData>();
}