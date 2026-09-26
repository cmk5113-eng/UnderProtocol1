using System.Collections;
using System.IO;
using UnityEngine;

public class SaveManager : ManagerBase
{
    public SaveData[] saveDatas;
    public int currentSlot = 0; // 현재 활성화된 슬롯

    private void Awake()
    {
        saveDatas = new SaveData[3];

        for (int i = 0; i < saveDatas.Length; i++)
        {
            if (PlayerPrefs.HasKey("SaveData" + i))
            {
                string json = PlayerPrefs.GetString("SaveData" + i);
                saveDatas[i] = JsonUtility.FromJson<SaveData>(json);
            }
            else
            {
                saveDatas[i] = new SaveData();
            }
        }

        // 마지막으로 접근했던 슬롯 번호 가져오기 (없으면 default 0)
        currentSlot = PlayerPrefs.GetInt("LastUsedSlot", 0);
    }

    private void Start()
    {
        // 게임 시작 시 마지막 슬롯 데이터를 자동으로 불러와 컨트롤러에 반영
        Load(currentSlot);
    }

    public void Save(int slot)
    {
        if (slot < 0 || slot >= saveDatas.Length) return;

        saveDatas[slot].currentGold = ProgressManager.Progress;

        string json = JsonUtility.ToJson(saveDatas[slot]);

        PlayerPrefs.SetString("SaveData" + slot, json);
        PlayerPrefs.SetInt("LastUsedSlot", slot); // 마지막 저장 슬롯 기록
        PlayerPrefs.Save();

        currentSlot = slot;
        Debug.Log($"{slot}번 슬롯 저장 완료");
    }

    public void Load(int slot)
    {
        if (slot < 0 || slot >= saveDatas.Length) return;

        if (!PlayerPrefs.HasKey("SaveData" + slot))
        {
            Debug.Log($"{slot}번 슬롯에 저장된 데이터가 없습니다.");
            return;
        }

        string json = PlayerPrefs.GetString("SaveData" + slot);
        saveDatas[slot] = JsonUtility.FromJson<SaveData>(json);

        currentSlot = slot;
        PlayerPrefs.SetInt("LastUsedSlot", slot);
        PlayerPrefs.Save();

        // 컨트롤러 및 UI 업데이트
        if (tempcontroller.Instance != null)
        {
            ProgressManager.Progress = saveDatas[slot].currentGold;
            tempcontroller.Instance.UpdateProgressUI();
        }

    }

    protected override IEnumerator OnConnected(GameManager newManager)
    {
        yield return null;
    }

    protected override void OnDisconnected()
    {
    }
}