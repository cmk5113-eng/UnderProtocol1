using JetBrains.Annotations;
using System.Collections.Generic;
using UnityEngine;

public class WaveLoader : MonoBehaviour
{

    [SerializeField] private List<MonsterData> monsterDatas;

    int index;

    public static WaveLoader Instance;
    private void Awake()
    {
        Instance = this;
    }
    public void NextWave()
    {
        if (GameManager.Instance.Wave.selectedWaves == null ||
            GameManager.Instance.Wave.selectedWaves.Length == 0)
        {
            Debug.LogWarning("선택된 웨이브가 없습니다.");
            return;
        }

        if (GameManager.Instance.Wave.currentWaveIndex >=
            GameManager.Instance.Wave.selectedWaves.Length)
        {
            Debug.Log("모든 웨이브를 클리어했습니다.");
            return;
        }

        // 1. selectedWaves에서 현재 웨이브 가져오기
        GameManager.Instance.Wave.currentWave =
            GameManager.Instance.Wave.selectedWaves[
                GameManager.Instance.Wave.currentWaveIndex
            ];

        // 2. currentWave 소환
        LoadWave();

        // 3. 인덱스 증가
        GameManager.Instance.Wave.currentWaveIndex++;
    }
    public void LoadWave()

    {
        foreach (MonsterSpawnData spawnData in GameManager.Instance.Wave.currentWave.monsters)
        {
            MonsterData data = monsterDatas.Find(x => x.id == spawnData.monsterID);

            if (data == null)
            {
                Debug.LogError($"Monster ID {spawnData.monsterID}�� ã�� �� �����ϴ�.");
                continue;
            }

            Vector3 worldPosition =
                PlacementManager.Instance.tilemap
                .GetCellCenterWorld(spawnData.position);

            // ���� ����
            GameObject monsterObject = Instantiate(
                data.prefab,
                worldPosition,
                Quaternion.identity
            );

            // ���� ����Ʈ�� �߰�
            MonsterBase monster = monsterObject.GetComponent<MonsterBase>();

            if (monster != null)
            {
                MonsterBase._monsters.Add(monsterObject);
            }
            else
            {
                Debug.LogError(
                    $"������ ���� {monsterObject.name}�� MonsterBase�� �����ϴ�."
                );
            }
        }
    }
}