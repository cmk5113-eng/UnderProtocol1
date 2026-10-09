using System.Collections.Generic;
using UnityEngine;

public class WaveLoader : MonoBehaviour
{
    private static WaveLoader instance;

    public static WaveLoader Instance
    {
        get
        {
            // 맵별 WaveLoader가 있을 때 이전 맵의 캐시를 재사용하지 않는다.
            if (PlacementManager.Instance != null && PlacementManager.Instance.tilemap != null)
            {
                var tilemap = PlacementManager.Instance.tilemap;
                WaveLoader mapLoader = tilemap.GetComponentInChildren<WaveLoader>(true);
                if (mapLoader == null) mapLoader = tilemap.GetComponentInParent<WaveLoader>(true);
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
        bool loaded = LoadWave();
 

        if (!loaded) return;

        wave.currentWaveIndex++;

        Debug.Log(
            $"[생성 후] index={wave.currentWaveIndex}, " +
            $"remaining={BattleManager.HasRemainingMonsters()}"
        );
        if (StageUIController.Instance != null) StageUIController.Instance.UpdateWave();
    }

    public bool LoadWave()
    {
        WaveManager manager = GameManager.Instance != null ? GameManager.Instance.Wave : null;
        WaveData wave = manager != null ? manager.currentWave : null;
        StageMapEntry settings = PlacementManager.Instance != null && manager != null
            ? manager.GetMap(PlacementManager.Instance.tilemap) : null;
        List<MonsterData> monsterDatas = settings != null ? settings.monsterDatas : null;
        if (wave == null || wave.monsters == null || monsterDatas == null
            || manager.ActiveMap != settings
            || settings.waves == null || System.Array.IndexOf(settings.waves, wave) < 0
            || PlacementManager.Instance == null || PlacementManager.Instance.tilemap == null)
            return false;

        var monstersById = new Dictionary<int, MonsterData>();
        foreach (MonsterData data in monsterDatas)
        {
            if (data == null) continue;
            if (monstersById.ContainsKey(data.id))
            {
                Debug.LogError($"[WaveLoader] Stage Map Editor의 Monster ID {data.id}가 중복 등록되어 있습니다.");
                return false;
            }
            monstersById.Add(data.id, data);
        }

        // 전체 설정을 먼저 검사해서 일부만 생성된 웨이브가 중복 생성되는 것을 방지한다.
        var spawnDatas = new List<MonsterData>();
        var occupied = new HashSet<Vector3Int>();
        var map = PlacementManager.Instance.tilemap;
        foreach (MonsterSpawnData spawn in wave.monsters)
        {
            MonsterData data = null;
            if (spawn != null) monstersById.TryGetValue(spawn.monsterID, out data);
            if (data == null || data.prefab == null || data.prefab.GetComponent<MonsterBase>() == null)
            {
                Debug.LogError("[WaveLoader] MonsterData/프리팹/MonsterBase 설정을 확인해주세요.");
                return false;
            }
            foreach (Vector3Int cell in data.GetOccupiedCells(spawn.position))
            {
                if (!map.HasTile(cell) || !occupied.Add(cell) || !PlacementManager.Instance.GetTileData(cell).isempty)
                {
                    Debug.LogError($"[WaveLoader] {data.monsterName}의 점유 영역 {cell}이 맵 밖이거나 다른 유닛/장애물과 겹칩니다.");
                    return false;
                }
            }
            spawnDatas.Add(data);
        }

        MonsterBase._monsters.RemoveAll(monster => monster == null);
        var createdMonsters = new List<GameObject>();
        for (int i = 0; i < wave.monsters.Count; i++)
        {
            Vector3 position = PlacementManager.Instance.tilemap.GetCellCenterWorld(wave.monsters[i].position);
           
            GameObject monster = Instantiate(spawnDatas[i].prefab, position, Quaternion.identity);
            MonsterBase unit = monster.GetComponent<MonsterBase>();
            unit.Initialize(spawnDatas[i]);
            if (!unit.TryPlace(map, wave.monsters[i].position))
            {
                // Prefab callbacks may change occupancy after validation; roll back this load atomically.
                unit.ReleaseOccupancy();
                monster.SetActive(false);
                Destroy(monster);
                foreach (GameObject created in createdMonsters)
                {
                    if (created == null) continue;
                    created.GetComponent<MonsterBase>().ReleaseOccupancy();
                    created.SetActive(false);
                    MonsterBase._monsters.Remove(created);
                    Destroy(created);
                }
                return false;
            }
            createdMonsters.Add(monster);
            var grid = map.layoutGrid;

            Debug.Log(
                $"[맵 변환] active={map.gameObject.activeInHierarchy}, " +
                $"localScale={map.transform.localScale.ToString("F6")}, " +
                $"lossyScale={map.transform.lossyScale.ToString("F6")}, " +
                $"cellSize={grid.cellSize}, cellGap={grid.cellGap}");

            MonsterBase._monsters.Add(monster);
        }
        return true;
    }
}

