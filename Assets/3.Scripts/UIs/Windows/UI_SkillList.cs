using System.Collections.Generic;
using UnityEngine;

public class UI_SkillList : UI_ScreenBase
{
    [SerializeField] private Transform contentPanel;
    [SerializeField] private GameObject skillPrefab;

    private List<UI_SkillSlotInfo> uiSlots = new List<UI_SkillSlotInfo>();

    public void InitInventoryUI()
    {
        // 씬에 있는 진짜 인벤토리를 코드가 자동으로 찾아옵니다.
        Inventory targetInventory = FindFirstObjectByType<Inventory>();

        if (targetInventory == null || targetInventory.slots == null)
        {
            Debug.LogError("[UI_SkillList] 인벤토리 데이터를 찾을 수 없습니다.");
            return;
        }

        // 기존 생성된 UI 프리패브들 깔끔하게 삭제
        foreach (Transform child in contentPanel)
        {
            if (child != null) Destroy(child.gameObject);
        }
        uiSlots.Clear();

        // 원래 원하셨던 바둑판 크기(25~30개)만큼 깨끗하게 생성
        int rows = targetInventory.rows;
        int columns = targetInventory.columns;

        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < columns; j++)
            {
                SkillSlot dataSlot = targetInventory.slots[i, j];

                GameObject go = Instantiate(skillPrefab, contentPanel);
                UI_SkillSlotInfo uiSlot = go.GetComponent<UI_SkillSlotInfo>();

                if (uiSlot != null && dataSlot != null)
                {
                    uiSlot.ConnectSlot(dataSlot);
                    uiSlots.Add(uiSlot);
                }
            }
        }
    }
}