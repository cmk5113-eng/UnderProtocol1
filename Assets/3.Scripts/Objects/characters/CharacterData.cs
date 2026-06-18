using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public enum JobType { Warrior, Archer, Mage, Builder }
public enum ElementType { None, Fire, Water, Electric, Earth }


[CreateAssetMenu(menuName = "Game/Character")]
public class CharacterData : ScriptableObject
{
    public string characterName;
    public JobType job;
    public ElementType element;
    public Sprite Portrait;
    public GameObject CharacterObject;
    public string Dialogue;
    public string explainText;

    // ���������� ������ ���
    public ActiveSkill[] active = new ActiveSkill[2];
    public PassiveSkill[] pasive = new PassiveSkill[4];
    public PassiveSkill staticpassive;
    public NormalSkill normalSkill;
    public LinkSkill linkSkill;
    public UltimateSkill ultimateSkill;

    public int ActionPoint;
    public int MovePoint;
    public int SkillPoint;
}