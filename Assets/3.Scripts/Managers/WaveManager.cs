using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// Stage Map Editor가 저장하는 맵별 설정. 목록 순서는 연결이나 클리어 ID에 영향을 주지 않는다.
[Serializable]
public class StageMapButton
{
    public WaveSetter button;
    // 같은 맵을 사용하는 여러 버튼의 기존 클리어 기록을 보존한다. -1이면 맵의 ID 사용.
    [Min(-1)] public int clearId = -1;
}

[Serializable]
public class StageMapEntry
{
    public Tilemap tilemap;
    [Min(0)] public int stageId;
    public List<StageMapButton> stageButtons = new List<StageMapButton>();
    public WaveData[] waves = Array.Empty<WaveData>();
    public List<MonsterData> monsterDatas = new List<MonsterData>();
    public StageMapData mapData;
    public GameObject postBattleScenario;

    public int GetStageId(WaveSetter button)
    {
        StageMapButton entry = stageButtons != null
            ? stageButtons.Find(item => item != null && item.button == button) : null;
        return entry != null && entry.clearId >= 0 ? entry.clearId : stageId;
    }
}

public class WaveManager : ManagerBase
{
    [SerializeField, HideInInspector] private List<StageMapEntry> maps = new List<StageMapEntry>();

    public IReadOnlyList<StageMapEntry> Maps => maps;
    public StageMapEntry ActiveMap { get; private set; }
    public WaveData[] selectedWaves => ActiveMap != null ? ActiveMap.waves : null;
    [NonSerialized] public int currentWaveIndex;
    [NonSerialized] public WaveData currentWave;

    public StageMapEntry GetMap(Tilemap tilemap)
    {
        if (tilemap == null) return null;
        StageMapEntry found = null;
        foreach (StageMapEntry map in maps)
        {
            if (map == null || map.tilemap != tilemap) continue;
            if (found != null) return null; // 중복 연결을 임의로 선택하지 않는다.
            found = map;
        }
        return found;
    }

    public StageMapEntry GetMap(WaveSetter button)
    {
        if (button == null) return null;
        StageMapEntry found = null;
        foreach (StageMapEntry map in maps)
        {
            if (map == null || map.stageButtons == null || !map.stageButtons.Exists(item => item != null && item.button == button)) continue;
            if (map.stageButtons.FindAll(item => item != null && item.button == button).Count != 1) return null;
            if (found != null) return null;
            found = map;
        }
        return found != null && GetMap(found.tilemap) == found ? found : null;
    }

    public bool SelectMap(Tilemap tilemap)
    {
        ResetSelection();
        ActiveMap = GetMap(tilemap);
        return ActiveMap != null;
    }

    public void ResetSelection()
    {
        ActiveMap = null;
        currentWaveIndex = 0;
        currentWave = null;
    }

#if UNITY_EDITOR
    public bool EditorAddMap(Tilemap tilemap)
    {
        if (tilemap == null || maps.Exists(map => map != null && map.tilemap == tilemap)) return false;
        int stageId = 0;
        foreach (StageMapEntry map in maps)
        {
            if (map == null) continue;
            stageId = Math.Max(stageId, map.stageId + 1);
            if (map.stageButtons != null) foreach (StageMapButton button in map.stageButtons)
                if (button != null) stageId = Math.Max(stageId, button.clearId + 1);
        }
        maps.Add(new StageMapEntry { tilemap = tilemap, stageId = stageId });
        return true;
    }

    public bool EditorRemoveMap(Tilemap tilemap)
    {
        StageMapEntry map = GetMap(tilemap);
        return map != null && maps.Remove(map);
    }
#endif

    protected override IEnumerator OnConnected(GameManager newManager) { yield break; }
    protected override void OnDisconnected() => ResetSelection();
}

