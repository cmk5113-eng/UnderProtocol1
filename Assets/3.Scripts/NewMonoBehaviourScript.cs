using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Character")]
public class CharacterData : ScriptableObject
{
    public string characterName;
    public JobType job;
    public ElementType element;

    public List<SkillData> skills; // ÇÙ½É
}