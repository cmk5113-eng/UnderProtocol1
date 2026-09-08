using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class MonsterSpawnData
{
    public int monsterID;
    public Vector3Int position;
}

[CreateAssetMenu(menuName = "Game/Wave Data")]
public class WaveData : ScriptableObject
{
    public List<MonsterSpawnData> monsters;
}