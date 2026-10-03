using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveManager : ManagerBase
{
    [SerializeField] private WaveData[] stage1Waves = new WaveData[5];
    [SerializeField] private WaveData[] stage2Waves = new WaveData[5];
    [SerializeField] private WaveData[] stage3Waves = new WaveData[5];
    [SerializeField] private WaveData[] stage4Waves = new WaveData[5];
    [SerializeField] private WaveData[] stage5Waves = new WaveData[5];

    public  List<WaveData[]> StageWaveIndex = new List<WaveData[]>();
    public WaveData[] selectedWaves;
    public int currentWaveIndex = 0;
    public WaveData currentWave;


    protected override IEnumerator OnConnected(GameManager newManager)
    {
        StageWaveIndex.Clear();
        StageWaveIndex.Add(stage1Waves);
        StageWaveIndex.Add(stage2Waves);
        StageWaveIndex.Add(stage3Waves);
        StageWaveIndex.Add(stage4Waves);
        StageWaveIndex.Add(stage5Waves);
        yield break;
    }
   
    protected override void OnDisconnected()
    {
    }

    public WaveData[] GetStageWaves(int stageIndex)
    {
        // OnConnected는 Play Mode에서 실행된다. 에디터도 저장된 배열을 바로 읽는다.
        switch (stageIndex)
        {
            case 0: return stage1Waves;
            case 1: return stage2Waves;
            case 2: return stage3Waves;
            case 3: return stage4Waves;
            case 4: return stage5Waves;
            default: return null;
        }
    }

    public bool SetStageWave(int stageIndex, int waveIndex, WaveData waveData)
    {
        WaveData[] waves = GetStageWaves(stageIndex);
        if (waves == null || waveIndex < 0 || waveIndex >= waves.Length) return false;
        waves[waveIndex] = waveData;
        return true;
    }

    public void SetWave(WaveData waveData)
    {
        currentWave = waveData;
    }

 
    public void ExecuteWave()
    {
        if (currentWave == null)
        {
            Debug.LogError("���� ���̺갡 �����ϴ�.");
            return;
        }

        foreach (MonsterSpawnData monster in currentWave.monsters)
        {
            Debug.Log(
                $"���� ID : {monster.monsterID}, " +
                $"��ǥ : {monster.position}"
            );

            // ���⼭ ���� ����
        }
    }
}
