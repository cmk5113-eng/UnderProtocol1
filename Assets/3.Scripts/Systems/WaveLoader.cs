using System.Collections.Generic;
using UnityEngine;

public class WaveLoader : MonoBehaviour
{

    [SerializeField] private List<MonsterData> monsterDatas;
    public void notice()
    {
        Debug.Log($"GameManager : {GameManager.Instance}");
        Debug.Log($"Wave : {GameManager.Instance.Wave}");
        Debug.Log($"CurrentWave : {GameManager.Instance.Wave.currentwave}");
    }
    public void LoadWave()
    {
        Debug.Log($"monsterDatas : {monsterDatas}");
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

            Instantiate(
                data.prefab,
                worldPosition,
                Quaternion.identity
            );
        }
    }
}