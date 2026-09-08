using System.Collections;
using UnityEngine;

public class WaveManager : ManagerBase
{
    public WaveData currentwave;

    protected override IEnumerator OnConnected(GameManager newManager)
    {
        yield break;
    }

    protected override void OnDisconnected()
    {
    }

    public void SetWave(WaveData waveData)
    {
        currentwave = waveData;
    }

    public void ExecuteWave()
    {
        if (currentwave == null)
        {
            Debug.LogError("현재 웨이브가 없습니다.");
            return;
        }

        foreach (MonsterSpawnData monster in currentwave.monsters)
        {
            Debug.Log(
                $"몬스터 ID : {monster.monsterID}, " +
                $"좌표 : {monster.position}"
            );

            // 여기서 몬스터 생성
        }
    }
}