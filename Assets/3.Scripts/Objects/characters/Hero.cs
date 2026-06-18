//using System.Collections.Generic;
//using UnityEngine;

//public class Hero : CharacterBase
//{
//    public List<SkillData> skills = new List<SkillData>();

//    // UI ��ư���� ȣ��: hero.UseSkill(skillData)
//    public void UseSkill(SkillData data)
//    {
//        if (data == null)
//        {
//            Debug.LogWarning("UseSkill called with null data");
//            return;
//        }

//        if (data.summonPrefab == null)
//        {
//            //Debug.LogWarning($"Skill '{data.skillName}' has no summonPrefab");
//            return;
//        }

//        // ��ų prefab �ν��Ͻ�ȭ (���� ��ġ/ȸ���� prefab�� ���� ����)
//        GameObject obj = Instantiate(data.summonPrefab, transform.position, Quaternion.identity);

//        // SkillObject ������Ʈ�� ������ ����ϰ�, ������ ��Ÿ�ӿ� �߰�
//        SkillObject skillObj = obj.GetComponent<SkillObject>();
//        if (skillObj == null)
//            skillObj = obj.AddComponent<SkillObject>();

//        skillObj.Init(this, data);
//    }
//}