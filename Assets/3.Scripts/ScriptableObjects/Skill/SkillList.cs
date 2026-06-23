using System.Collections.Generic;
using System.IO.Enumeration;
using UnityEngine;

public enum SkillType
{
   Active = 10, Passive = 20, Normal = 0, Link = 99, Ultimate = 999
}
[CreateAssetMenu(fileName = "Skill", menuName = "SkillContainer")]
public class SkillList : ScriptableObject
{
    public int id;
    public List<SkillList> skillsList;
    public SkillType type;
    public Sprite icon;
    public int maxStack;
    public int currentStack;

    public virtual int CompareByType(SkillList other)
    {
        if (other == null) return 1;
        int result = type - other.type;
        if (result != 0) return result;
        return id - other.id;
    }

}

