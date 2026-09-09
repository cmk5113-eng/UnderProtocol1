using System.Collections.Generic;
using UnityEngine;

public class MonsterBase : CharacterBase
{
    private MonsterData data;
    public static List<GameObject> _monsters = new List<GameObject>();
    public void Initialize(MonsterData data)
    {
        this.data = data;

        // 데이터 적용
        // HP
        // 공격력
        // 방어력
        // 스킬 등
    }
}