using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Save Catalog")]
public class SaveCatalog : ScriptableObject
{
    public SaveCharacterEntry[] characters = Array.Empty<SaveCharacterEntry>();
    public SaveSkillEntry[] skills = Array.Empty<SaveSkillEntry>();
}

[Serializable]
public class SaveCharacterEntry
{
    public string id;
    public CharacterData character;
    public ActiveSkill[] defaultActive = new ActiveSkill[2];
    public PassiveSkill[] defaultPassive = new PassiveSkill[4];
}

[Serializable]
public class SaveSkillEntry
{
    public string id;
    public SkillList skill;
}
