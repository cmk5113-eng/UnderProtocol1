using System;
using System.Collections.Generic;
using UnityEngine;

public enum StageFieldEffectType
{
    None,
    Fire,
    Ice,
    Electric,
    Wind,
    Dark,
    Custom
}

[Serializable]
public class FieldEffectSpawnData
{
    public Vector3Int position;
    public StageFieldEffectType effectType;
    [Min(0)] public int value;
    [Min(0)] public int duration;
}

[Serializable]
public class ObstacleSpawnData
{
    public GameObject prefab;
    public Vector3Int position;
    [Range(0, 3)] public int rotationQuarterTurns;
    public bool blocksMovement = true;
    public bool blocksAttack;
    [Min(0)] public int hitPoint;
}

[CreateAssetMenu(menuName = "Game/Stage Map Data")]
public class StageMapData : ScriptableObject
{
    [Header("Waves loaded by this map")]
    public List<WaveData> waves = new List<WaveData>();

    [Header("Map objects")]
    public List<FieldEffectSpawnData> fieldEffects = new List<FieldEffectSpawnData>();
    public List<ObstacleSpawnData> obstacles = new List<ObstacleSpawnData>();
}
