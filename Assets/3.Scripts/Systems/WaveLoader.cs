using System.Collections.Generic;
using UnityEngine;

public class WaveLoader : MonoBehaviour
{

    [SerializeField] private List<MonsterData> monsterDatas;

    public void LoadWave()
    {
        foreach (MonsterSpawnData spawnData in GameManager.Instance.Wave.currentwave.monsters)
        {
            MonsterData data = monsterDatas.Find(x => x.id == spawnData.monsterID);

            if (data == null)
            {
                Debug.LogError($"Monster ID {spawnData.monsterID}를 찾을 수 없습니다.");
                continue;
            }

            Vector3 worldPosition =
                PlacementManager.Instance.tilemap
                .GetCellCenterWorld(spawnData.position);

            // 몬스터 생성
            GameObject monsterObject = Instantiate(
                data.prefab,
                worldPosition,
                Quaternion.identity
            );

            // 몬스터 리스트에 추가
            MonsterBase monster = monsterObject.GetComponent<MonsterBase>();

            if (monster != null)
            {
                MonsterBase._monsters.Add(monsterObject);
            }
            else
            {
                Debug.LogError(
                    $"생성된 몬스터 {monsterObject.name}에 MonsterBase가 없습니다."
                );
            }
        }
    }
}