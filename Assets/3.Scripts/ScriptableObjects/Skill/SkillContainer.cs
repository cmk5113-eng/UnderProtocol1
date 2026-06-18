using System.Collections.Generic;
using System.IO.Enumeration;
using UnityEngine;

public enum SkillType
{
   Active = 10, Passive = 20, Normal = 0, Link = 99, Ultimate = 999
}
[CreateAssetMenu(fileName = "Skill", menuName = "SkillContainer")]
public class SkillContainer : ScriptableObject
{
    public List<SkillContainer> skillsList;
    public SkillType type;
}

