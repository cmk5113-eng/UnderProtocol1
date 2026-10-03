using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class StageWaveData
{
    public WaveData[] waves = Array.Empty<WaveData>();
}

public class WaveManager : ManagerBase
{
    [SerializeField] private List<StageWaveData> stages = new List<StageWaveData>();
    [SerializeField, HideInInspector] private bool stageDataMigrated;

    // 기존 Scene/Prefab의 직렬화 이름을 유지해 연결된 WaveData를 한 번 이관한다.
    // 새 WaveManager에는 이 필드들의 초기 배열을 만들지 않는다.
    [SerializeField, HideInInspector] private WaveData[] stage1Waves;
    [SerializeField, HideInInspector] private WaveData[] stage2Waves;
    [SerializeField, HideInInspector] private WaveData[] stage3Waves;
    [SerializeField, HideInInspector] private WaveData[] stage4Waves;
    [SerializeField, HideInInspector] private WaveData[] stage5Waves;

    public bool NeedsStageMigration => !stageDataMigrated;
    public int StageCount { get { MigrateLegacyStages(); return stages.Count; } }

    // 기존 조회 코드 호환용. 에디터와 런타임 모두 동일한 저장 목록을 읽는다.
    public List<WaveData[]> StageWaveIndex
    {
        get
        {
            MigrateLegacyStages();
            var result = new List<WaveData[]>(stages.Count);
            foreach (StageWaveData stage in stages) result.Add(stage != null ? stage.waves : null);
            return result;
        }
    }
    public WaveData[] selectedWaves;
    public int currentWaveIndex = 0;
    public WaveData currentWave;

    private void Reset()
    {
        // 새 컴포넌트는 이관 대상이 아니다. 저장 시 null 배열이 빈 배열로
        // 정규화되더라도 이전 다섯 Stage로 오인하지 않도록 버전을 기록한다.
        stages = new List<StageWaveData>();
        stageDataMigrated = true;
        stage1Waves = stage2Waves = stage3Waves = stage4Waves = stage5Waves = null;
    }


    protected override IEnumerator OnConnected(GameManager newManager)
    {
        MigrateLegacyStages();
        yield break;
    }
   
    protected override void OnDisconnected()
    {
    }

    public WaveData[] GetStageWaves(int stageIndex)
    {
        MigrateLegacyStages();
        return stageIndex >= 0 && stageIndex < stages.Count && stages[stageIndex] != null
            ? stages[stageIndex].waves : null;
    }

    public bool MigrateLegacyStages()
    {
        if (stageDataMigrated) return false;
        if (stages == null) stages = new List<StageWaveData>();
        if (stages.Count == 0 && (stage1Waves != null || stage2Waves != null || stage3Waves != null || stage4Waves != null || stage5Waves != null))
        {
            // 하드코딩된 다섯 필드는 이전 파일을 읽을 때만 사용한다.
            foreach (WaveData[] legacy in new[] { stage1Waves, stage2Waves, stage3Waves, stage4Waves, stage5Waves })
                stages.Add(new StageWaveData { waves = legacy ?? Array.Empty<WaveData>() });
        }
        stageDataMigrated = true;
        return true;
    }

    public int AddStage()
    {
        MigrateLegacyStages();
        stages.Add(new StageWaveData());
        return stages.Count - 1;
    }

    public bool RemoveStage(int stageIndex)
    {
        MigrateLegacyStages();
        if (stageIndex < 0 || stageIndex >= stages.Count) return false;
        stages.RemoveAt(stageIndex);
        return true;
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
