using System;
using System.Collections;
using UnityEngine;

public class SaveManager : ManagerBase
{
    public SaveData[] saveDatas;
    public int currentSlot = 0;
    [SerializeField] private SaveCatalog saveCatalog;
    private CharacterLoadoutPersistence loadouts;
    public static event Action OnSaveLoaded;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState() => OnSaveLoaded = null;

    private void Awake() => EnsureInitialized();
    private void Start() => Load(currentSlot);

    private void EnsureInitialized()
    {
        if (loadouts != null) return;
        if (saveCatalog == null) saveCatalog = Resources.Load<SaveCatalog>("SaveCatalog");
        loadouts = new CharacterLoadoutPersistence(saveCatalog);
        saveDatas = new SaveData[3];
        for (int i = 0; i < saveDatas.Length; i++)
            if (TryReadSlot(i, out SaveData data)) saveDatas[i] = data;
        currentSlot = Mathf.Clamp(PlayerPrefs.GetInt("LastUsedSlot", 0), 0, saveDatas.Length - 1);
    }

    private bool IsValidSlot(int slot) => slot >= 0 && slot < saveDatas.Length;

    private static bool TryReadSlot(int slot, out SaveData data)
    {
        data = null;
        if (!PlayerPrefs.HasKey("SaveData" + slot)) { data = new SaveData(); return true; }
        try
        {
            data = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString("SaveData" + slot));
            if (data == null) throw new ArgumentException("세이브 데이터가 비어 있습니다.");
            if (data.saveVersion > SaveData.CurrentVersion)
                throw new ArgumentException("현재 게임보다 새로운 버전의 세이브입니다.");
            return true;
        }
        catch (Exception error)
        {
            // 오류 슬롯을 빈 데이터로 덮어쓰거나 현재 플레이 상태로 적용하지 않는다.
            Debug.LogError($"[Save] {slot}번 슬롯을 읽을 수 없습니다: {error.Message}");
            data = null;
            return false;
        }
    }

    // 기존 버튼/전투 자동 저장 연결을 그대로 유지한다.
    public void Save(int slot) => TrySave(slot);

    public bool TrySave(int slot)
    {
        EnsureInitialized();
        if (!IsValidSlot(slot) || !loadouts.IsReady || !TryReadSlot(slot, out SaveData previous)
            || !loadouts.TryCapture(out var characterSkills)) return false;

        var data = new SaveData
        {
            saveVersion = SaveData.CurrentVersion,
            progressVersion = 1,
            stage = previous.stage,
            progress = ProgressManager.Progress,
            currentGold = ProgressManager.Progress, // 예전 진행도 저장 형식과 호환
            clearedStageIds = ProgressManager.GetClearedStageIds(),
            characterSkills = characterSkills,
            stageClearRecords = ProgressManager.GetStageClearRecords()
        };
        data.cleared = data.clearedStageIds.Count > 0;
        PlayerPrefs.SetString("SaveData" + slot, JsonUtility.ToJson(data));
        PlayerPrefs.SetInt("LastUsedSlot", slot);
        PlayerPrefs.Save();
        saveDatas[slot] = data;
        currentSlot = slot;
        Debug.Log($"[Save] {slot}번 슬롯 저장 완료 (캐릭터 {characterSkills.Count}명, 클리어 기록 {data.stageClearRecords.Count}개)");
        return true;
    }

    public void Load(int slot) => TryLoad(slot);

    public bool TryLoad(int slot)
    {
        EnsureInitialized();
        if (!IsValidSlot(slot) || !loadouts.IsReady || !TryReadSlot(slot, out SaveData data)) return false;

        // 읽기에 성공한 뒤 전투를 닫아 이전 슬롯의 전투 결과가 새 슬롯에 섞이지 않게 한다.
        if (UseSkill.Instance != null) UseSkill.Instance.ClearAllHighlights();
        if (BattleManager.Instance != null && BattleManager.Instance.IsBattleActive)
            BattleManager.Instance.AbortBattle();

        loadouts.Restore(data.characterSkills);
        ProgressManager.RestoreProgress(data.progressVersion >= 1 ? data.progress : data.currentGold,
            data.clearedStageIds, data.stageClearRecords);
        saveDatas[slot] = data;
        currentSlot = slot;
        PlayerPrefs.SetInt("LastUsedSlot", slot);
        PlayerPrefs.Save();
        OnSaveLoaded?.Invoke();
        Debug.Log($"[Save] {slot}번 슬롯 로드 완료");
        return true;
    }

    protected override IEnumerator OnConnected(GameManager newManager) { yield break; }
    protected override void OnDisconnected() => loadouts?.ResetToDefaults();
    private void OnDestroy() => loadouts?.ResetToDefaults();
}
