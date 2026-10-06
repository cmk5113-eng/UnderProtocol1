using System.Collections.Generic;
using UnityEngine;

public class MonsterBase : CharacterBase
{
    private MonsterData data;
    public MonsterData MonsterData => data;
    public static List<GameObject> _monsters = new List<GameObject>();
    public void Initialize(MonsterData data)
    {
        if (data == null) return;
        this.data = data;

        // 데이터의 HP로 시작한다. 프리팹에 남은 현재 HP는 사용하지 않는다.
        MaxHP = Mathf.Max(1, data.hp);
        InitializeHP();
    }
}
