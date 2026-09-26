using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProgressManager : ManagerBase
{
    public static ProgressManager Instance;
    public static event Action OnProgressChanged;

    private static int progress;
    private static readonly HashSet<int> clearedStageIds = new HashSet<int>();

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
        OnProgressChanged = null;
    }

    public static bool IsStageCleared(int stageId) => clearedStageIds.Contains(stageId);

    public static bool MarkStageCleared(int stageId)
    {
        if (stageId < 0 || !clearedStageIds.Add(stageId))
            return false;

        Progress++;
        return true;
    }

    public static List<int> GetClearedStageIds()
    {
        var result = new List<int>(clearedStageIds);
        result.Sort();
        return result;
    }

    public static void RestoreProgress(int savedProgress, List<int> savedStageIds)
    {
        clearedStageIds.Clear();
        if (savedStageIds != null)
        {
            foreach (int stageId in savedStageIds)
                if (stageId >= 0) clearedStageIds.Add(stageId);
        }

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
