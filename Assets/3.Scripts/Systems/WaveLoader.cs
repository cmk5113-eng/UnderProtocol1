using System.Collections.Generic;
using UnityEngine;

public class WaveLoader : MonoBehaviour
{
    [SerializeField] private List<MonsterData> monsterDatas;
    private static WaveLoader instance;

    public static WaveLoader Instance
    {
        get
        {
            // 맵별 WaveLoader가 있을 때 이전 맵의 캐시를 재사용하지 않는다.
            if (PlacementManager.Instance != null && PlacementManager.Instance.tilemap != null)
            {
                var tilemap = PlacementManager.Instance.tilemap;
                WaveLoader mapLoader = tilemap.GetComponentInParent<WaveLoader>(true);
                if (mapLoader != null) return mapLoader;
                PlacementController map = tilemap.GetComponentInParent<PlacementController>(true);
                if (map != null)
                {
                    mapLoader = map.GetComponentInChildren<WaveLoader>(true);
                    if (mapLoader != null) return mapLoader;
                }
            }
            if (instance == null)
                instance = FindFirstObjectByType<WaveLoader>(FindObjectsInactive.Include);
            return instance;
        }
    }

    private void Awake() => instance = this;

    public void StartFirstWave()
    {
        WaveManager wave = GameManager.Instance != null ? GameManager.Instance.Wave : null;
        if (wave != null && wave.currentWaveIndex == 0) NextWave();
    }

    public void NextWave()
    {
        WaveManager wave = GameManager.Instance != null ? GameManager.Instance.Wave : null;
        BattleManager battle = BattleManager.Instance;
        if (wave == null || battle == null || !battle.IsBattleActive) return;
        if (wave.selectedWaves == null || wave.selectedWaves.Length == 0) return;
        if (BattleManager.HasRemainingMonsters()) return;

        if (wave.currentWaveIndex >= wave.selectedWaves.Length)
        {
            battle.CompleteBattle();
            return;
        }

        if (PlacementManager.Instance == null || PlacementManager.Instance.tilemap == null)
        {
            Debug.LogWarning("[WaveLoader] 웨이브를 생성할 Tilemap이 없습니다.");
            return;
        }

        WaveData targetWave = wave.selectedWaves[wave.currentWaveIndex];
        if (targetWave == null)
        {
            Debug.LogError($"[WaveLoader] selectedWaves[{wave.currentWaveIndex}]가 비어 있습니다.");
            return;
        }

        wave.currentWave = targetWave;
        // 잘못된 생성 데이터를 빈 웨이브 클리어로 처리하지 않는다.
        if (!LoadWave()) return;
        wave.currentWaveIndex++;
        if (StageUIController.Instance != null) StageUIController.Instance.UpdateWave();
    }

    public bool LoadWave()
    {
        WaveManager manager = GameManager.Instance != null ? GameManager.Instance.Wave : null;
        WaveData wave = manager != null ? manager.currentWave : null;
        if (wave == null || wave.monsters == null || monsterDatas == null
            || PlacementManager.Instance == null || PlacementManager.Instance.tilemap == null)
            return false;

        // 전체 설정을 먼저 검사해서 일부만 생성된 웨이브가 중복 생성되는 것을 방지한다.
        var spawnDatas = new List<MonsterData>();
        foreach (MonsterSpawnData spawn in wave.monsters)
        {
            MonsterData data = spawn == null ? null
                : monsterDatas.Find(item => item != null && item.id == spawn.monsterID);
            if (data == null || data.prefab == null || data.prefab.GetComponent<MonsterBase>() == null)
            {
                Debug.LogError("[WaveLoader] MonsterData/프리팹/MonsterBase 설정을 확인해주세요.");
                return false;
            }
            spawnDatas.Add(data);
        }

        MonsterBase._monsters.RemoveAll(monster => monster == null);
        for (int i = 0; i < wave.monsters.Count; i++)
        {
            Vector3 position = PlacementManager.Instance.tilemap.GetCellCenterWorld(wave.monsters[i].position);
            GameObject monster = Instantiate(spawnDatas[i].prefab, position, Quaternion.identity);
            MonsterBase._monsters.Add(monster);
        }
        return true;
    }
}
