using System.Collections.Generic;
using UnityEngine;

public class Hero : CharacterBase
{
    public List<SkillData> skills = new List<SkillData>();

    // UI 버튼에서 호출: hero.UseSkill(skillData)
    public void UseSkill(SkillData data)
    {
        if (data == null)
        {
            Debug.LogWarning("UseSkill called with null data");
            return;
        }

        if (data.summonPrefab == null)
        {
            Debug.LogWarning($"Skill '{data.skillName}' has no summonPrefab");
            return;
        }

        // 스킬 prefab 인스턴스화 (씬에 위치/회전은 prefab에 의해 결정)
        GameObject obj = Instantiate(data.summonPrefab, transform.position, Quaternion.identity);

        // SkillObject 컴포넌트가 있으면 사용하고, 없으면 런타임에 추가
        SkillObject skillObj = obj.GetComponent<SkillObject>();
        if (skillObj == null)
            skillObj = obj.AddComponent<SkillObject>();

        skillObj.Init(this, data);
    }
}