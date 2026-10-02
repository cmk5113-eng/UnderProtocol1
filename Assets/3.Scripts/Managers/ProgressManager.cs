using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public class ProgressManager : ManagerBase
{
    public static ProgressManager Instance;
    public static event Action OnProgressChanged;

    public static int progress;
    private static readonly HashSet<int> clearedStageIds = new HashSet<int>();
    private static readonly Dictionary<int, StageClearRecord> stageClearRecords = new Dictionary<int, StageClearRecord>();

    // 기존 Progress 읽기/쓰기와 디버그 증가 버튼도 UI 갱신을 거친다.
    public static int Progress
    {
        get => progress;
        set
        {
            progress = Mathf.Max(0, value);
            OnProgressChanged?.Invoke();
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRuntimeState()
    {
        Instance = null;
        progress = 0;
        clearedStageIds.Clear();
        stageClearRecords.Clear();
        OnProgressChanged = null;
    }

    public static bool IsStageCleared(int stageId) => clearedStageIds.Contains(stageId);

    public static bool MarkStageCleared(int stageId)
    {
        if (stageId < 0 || !clearedStageIds.Add(stageId))
            return false;

        stageClearRecords[stageId] = LegacyRecord(stageId);
        Progress++;
        return true;
    }

    // 재클리어도 기록하며 진행도는 최초 클리어 때만 증가한다.
    public static bool RecordStageClear(int stageId, int turns, float remainingHP, int waveCount)
    {
        if (stageId < 0) return false;
        if (clearedStageIds.Add(stageId) && progress < int.MaxValue) progress++;
        if (!stageClearRecords.TryGetValue(stageId, out StageClearRecord record))
        {
            record = new StageClearRecord { stageId = stageId, cleared = true };
            stageClearRecords.Add(stageId, record);
        }
        if (record.clearCount < int.MaxValue) record.clearCount++;
        record.lastClearTurn = Mathf.Max(1, turns);
        record.lastRemainingHP = Mathf.Max(0f, remainingHP);
        record.lastWaveCount = Mathf.Max(0, waveCount);
        if (record.bestClearTurn <= 0 || record.lastClearTurn < record.bestClearTurn)
            record.bestClearTurn = record.lastClearTurn;
        record.bestRemainingHP = Mathf.Max(record.bestRemainingHP, record.lastRemainingHP);
        string now = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
        // 예전 세이브의 최초 클리어 시각은 알 수 없으므로 새로 지어내지 않는다.
        if (record.clearCount == 1) record.firstClearedUtc = now;
        record.lastClearedUtc = now;
        OnProgressChanged?.Invoke();
        return true;
    }

    public static StageClearRecord GetStageClearRecord(int stageId)
        => stageClearRecords.TryGetValue(stageId, out StageClearRecord record) ? record.Copy() : null;

    public static List<StageClearRecord> GetStageClearRecords()
    {
        var result = new List<StageClearRecord>();
        foreach (int id in GetClearedStageIds())
            result.Add(stageClearRecords.TryGetValue(id, out StageClearRecord record)
                ? record.Copy() : LegacyRecord(id));
        return result;
    }

    private static StageClearRecord LegacyRecord(int id)
        => new StageClearRecord { stageId = id, cleared = true, clearCount = 1 };

    public static List<int> GetClearedStageIds()
    {
        var result = new List<int>(clearedStageIds);
        result.Sort();
        return result;
    }

    public static void RestoreProgress(int savedProgress, List<int> savedStageIds,
        List<StageClearRecord> savedRecords = null)
    {
        clearedStageIds.Clear();
        stageClearRecords.Clear();
        if (savedRecords != null)
        {
            foreach (StageClearRecord saved in savedRecords)
            {
                if (saved == null || saved.stageId < 0 || !saved.cleared
                    || stageClearRecords.ContainsKey(saved.stageId)) continue;
                StageClearRecord record = saved.Copy();
                record.clearCount = Mathf.Max(1, record.clearCount);
                record.lastClearTurn = Mathf.Max(0, record.lastClearTurn);
                record.bestClearTurn = Mathf.Max(0, record.bestClearTurn);
                record.lastRemainingHP = Mathf.Max(0f, record.lastRemainingHP);
                record.bestRemainingHP = Mathf.Max(record.lastRemainingHP, record.bestRemainingHP);
                record.lastWaveCount = Mathf.Max(0, record.lastWaveCount);
                stageClearRecords.Add(record.stageId, record);
                clearedStageIds.Add(record.stageId);
            }
        }
        if (savedStageIds != null)
        {
            foreach (int stageId in savedStageIds)
                if (stageId >= 0) clearedStageIds.Add(stageId);
        }
        foreach (int id in clearedStageIds)
            if (!stageClearRecords.ContainsKey(id)) stageClearRecords.Add(id, LegacyRecord(id));

        Progress = Mathf.Max(savedProgress, clearedStageIds.Count);
    }

    private void Awake() => Instance = this;

    protected override IEnumerator OnConnected(GameManager newManager)
    {
        yield break;
    }

    protected override void OnDisconnected()
    {
        if (Instance == this) Instance = null;
    }
}
