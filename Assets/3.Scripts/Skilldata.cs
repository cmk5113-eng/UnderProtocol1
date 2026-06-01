using UnityEngine;


public enum skill { water, fire, earth}
public class SkillData : ScriptableObject
{
    public string skillName = "";
    public Sprite Image;
    public GameObject summonPrefab; // 스킬을 실행할 prefab (SkillObject가 있어야 하거나 SkillObject를 AddComponent 함)
    public int damage = 0;
    public float range = 1.0f;
    public bool requiresTarget = true;
    // 필요 시 추가 데이터(쿨다운, 비용 등) 추가
}
