using System.Collections;
using UnityEngine;

public class SaveManager : ManagerBase
{
    public SaveData[] saveDatas;
    public int currentSlot = 0;

    private void Awake()
    {
        saveDatas = new SaveData[3];
        for (int i = 0; i < saveDatas.Length; i++)
            saveDatas[i] = ReadSlot(i);
        currentSlot = Mathf.Clamp(PlayerPrefs.GetInt("LastUsedSlot", 0), 0, saveDatas.Length - 1);
    }

    private void Start() => Load(currentSlot);

    private SaveData ReadSlot(int slot)
    {
        if (!PlayerPrefs.HasKey("SaveData" + slot)) return new SaveData();
        return JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString("SaveData" + slot)) ?? new SaveData();
    }

    public void Save(int slot)
    {
        if (saveDatas == null || slot < 0 || slot >= saveDatas.Length) return;
        if (saveDatas[slot] == null) saveDatas[slot] = new SaveData();

        SaveData data = saveDatas[slot];
        data.progressVersion = 1;
        data.progress = ProgressManager.Progress;
        // 기존 버전과 호환: 종전에는 currentGold에 진행도를 저장했다.
        data.currentGold = data.progress;
        data.clearedStageIds = ProgressManager.GetClearedStageIds();
        data.cleared = data.clearedStageIds.Count > 0;
        PlayerPrefs.SetString("SaveData" + slot, JsonUtility.ToJson(data));
        PlayerPrefs.SetInt("LastUsedSlot", slot);
        PlayerPrefs.Save();
        currentSlot = slot;
        Debug.Log($"{slot}번 슬롯 저장 완료");
    }

    public void Load(int slot)
    {
        if (saveDatas == null || slot < 0 || slot >= saveDatas.Length) return;
        // 전투 도중 슬롯을 바꾸면 이전 슬롯의 전투 결과가 새 슬롯에 기록되지 않게 한다.
        if (BattleManager.Instance != null && BattleManager.Instance.IsBattleActive)
            BattleManager.Instance.AbortBattle();

        SaveData data = ReadSlot(slot);
        saveDatas[slot] = data;
        currentSlot = slot;
        PlayerPrefs.SetInt("LastUsedSlot", slot);
        PlayerPrefs.Save();
        ProgressManager.RestoreProgress(data.progressVersion >= 1 ? data.progress : data.currentGold,
            data.clearedStageIds);
        Debug.Log($"{slot}번 슬롯 로드 완료");
    }

    protected override IEnumerator OnConnected(GameManager newManager)
    {
        yield break;
    }

    protected override void OnDisconnected() { }
}
