using JetBrains.Annotations;
using System.Collections.Generic;
using UnityEngine;

public class WaveLoader : MonoBehaviour
{

    [SerializeField] private List<MonsterData> monsterDatas;

    int index;

    private static WaveLoader instance;
    public static WaveLoader Instance
    {
        get
        {
            // 스테이지 UI가 아직 비활성이어도 진입 시 첫 웨이브를 생성할 수 있다.
            if (instance == null)
                instance = FindFirstObjectByType<WaveLoader>(FindObjectsInactive.Include);

            return instance;
        }
    }

    private void Awake()
    {
        Debug.Log(
            $"[WaveLoader Awake] " +
            $"오브젝트={gameObject.name}, " +
            $"InstanceID={GetInstanceID()}, " +
            $"MonsterDatas={monsterDatas.Count}"
        );

        instance = this;
    }

    public void StartFirstWave()
    {
        Debug.Log("===== StartFirstWave =====");

        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager 없음");
            return;
        }

        if (GameManager.Instance.Wave == null)
        {
            Debug.LogError("WaveManager 없음");
            return;
        }

        Debug.Log($"currentWaveIndex = {GameManager.Instance.Wave.currentWaveIndex}");
        Debug.Log($"selectedWaves = {GameManager.Instance.Wave.selectedWaves}");

        if (GameManager.Instance.Wave.selectedWaves != null)
            Debug.Log($"selectedWaves.Length = {GameManager.Instance.Wave.selectedWaves.Length}");

        Debug.Log($"PlacementManager = {PlacementManager.Instance}");
        Debug.Log($"tilemap = {PlacementManager.Instance?.tilemap}");

        if (GameManager.Instance.Wave.currentWaveIndex == 0)
            NextWave();
    }
    public void NextWave()
    {
       

        if (GameManager.Instance == null || GameManager.Instance.Wave == null)
        {
            Debug.LogError("[NextWave] GameManager/Wave 없음");
            return;
        }

      
        if (GameManager.Instance.Wave.selectedWaves == null ||
            GameManager.Instance.Wave.selectedWaves.Length == 0)
        {
            Debug.LogWarning("선택된 웨이브가 없습니다.");
            return;
        }

        if (GameManager.Instance.Wave.currentWaveIndex >=
            GameManager.Instance.Wave.selectedWaves.Length)
        {
            return;
        }

        if (PlacementManager.Instance == null ||
            PlacementManager.Instance.tilemap == null)
        {
            Debug.LogWarning("웨이브를 생성할 Tilemap이 없습니다.");
            return;
        }

        WaveData targetWave =
            GameManager.Instance.Wave.selectedWaves[
                GameManager.Instance.Wave.currentWaveIndex
            ];

        Debug.Log($"[NextWave] targetWave = {targetWave}");

        if (targetWave == null)
        {
            Debug.LogError(
                $"[NextWave] selectedWaves[{GameManager.Instance.Wave.currentWaveIndex}]가 null"
            );
            return;
        }

        Debug.Log($"[NextWave] monsters = {targetWave.monsters}");

        if (targetWave.monsters != null)
            Debug.Log($"[NextWave] monster Count = {targetWave.monsters.Count}");

        GameManager.Instance.Wave.currentWave = targetWave;

        Debug.Log($"[NextWave] currentWave 설정 완료 = {GameManager.Instance.Wave.currentWave}");

        LoadWave();

        GameManager.Instance.Wave.currentWaveIndex++;

        Debug.Log(
            $"[NextWave] 완료. 다음 index = {GameManager.Instance.Wave.currentWaveIndex}"
        );

        if (StageUIController.Instance != null)
            StageUIController.Instance.UpdateWave();
    }
    public void LoadWave()
    {
        

        for (int i = 0; i < monsterDatas.Count; i++)
        {
            if (monsterDatas[i] == null)
            {
                Debug.Log($"monsterDatas[{i}] = NULL");
                continue;
            }

           
        }
        

        WaveData wave = GameManager.Instance.Wave.currentWave;

        
        int spawnIndex = 0;

        foreach (MonsterSpawnData spawnData in wave.monsters)
        {
            

            MonsterData data =
                monsterDatas.Find(x => x.id == spawnData.monsterID);

            if (data == null)
            {
                Debug.LogError(
                    $"[Spawn {spawnIndex}] ID {spawnData.monsterID}의 MonsterData 없음"
                );

                spawnIndex++;
                continue;
            }

            if (data.prefab == null)
            {
               
                spawnIndex++;
                continue;
            }

            Vector3 worldPosition =
                PlacementManager.Instance.tilemap
                    .GetCellCenterWorld(spawnData.position);


            GameObject monsterObject = Instantiate(
                data.prefab,
                worldPosition,
                Quaternion.identity
            );


            MonsterBase monster =
                monsterObject.GetComponent<MonsterBase>();

            if (monster != null)
            {
                MonsterBase._monsters.Add(monsterObject);

                Debug.Log(
                    $"[Spawn {spawnIndex}] MonsterBase 등록 성공 / " +
                    $"현재 _monsters 수: {MonsterBase._monsters.Count}"
                );
            }
            else
            {
                Debug.LogError(
                    $"[Spawn {spawnIndex}] {monsterObject.name}에 MonsterBase 없음"
                );
            }

            spawnIndex++;
        }

      
    }
}
