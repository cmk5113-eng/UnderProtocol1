using System.Collections.Generic;
using UnityEngine;

public class UI_SkillList : UI_ScreenBase
{
    [SerializeField] private Inventory targetInventory; // 연동할 인벤토리 컴포넌트
    [SerializeField] private Transform contentPanel;
    [SerializeField] private GameObject skillPrefab;

    private List<UI_SkillSlotInfo> uiSlots = new List<UI_SkillSlotInfo>();

    private void Start()
    {
        // 인벤토리가 있다면 초기화 후 UI 생성
        if (targetInventory != null)
        {
            targetInventory.Initialize();
            InitInventoryUI();
        }
    }

    // 1. 인벤토리 크기에 맞게 UI 슬롯 프리팹들을 미리 생성하고 데이터 슬롯과 연결
    public void InitInventoryUI()
    {
        // 기존 UI 삭제
        foreach (Transform child in contentPanel) Destroy(child.gameObject);
        uiSlots.Clear();

        int totalSlots = targetInventory.rows * targetInventory.columns;

        for (int i = 0; i < totalSlots; i++)
        {
            GameObject go = Instantiate(skillPrefab, contentPanel);
            UI_SkillSlotInfo uiSlot = go.GetComponent<UI_SkillSlotInfo>();

            if (uiSlot != null)
            {
                // 인벤토리의 실제 데이터 슬롯을 가져옴
                SkillSlot dataSlot = targetInventory.GetSlot(i);

                // UI 슬롯과 데이터 슬롯을 연결 (이 시점에 OnSkillSlotChanged 이벤트 자동 구독됨)
                uiSlot.ConnectSlot(dataSlot);
                uiSlots.Add(uiSlot);
            }
        }
    }

    // 2. 만약 전체 리스트를 강제로 다시 그려야 할 때 사용 (기존에 작성하신 뼈대 유지용)
    public void RefreshAllUI()
    {
        foreach (var uiSlot in uiSlots)
        {
            if (uiSlot.ConnectedSlot != null)
            {
                // 데이터 슬롯에 변경을 알려 VisualUpdate를 강제 실행 유도
                uiSlot.ConnectedSlot.NoticeChanged();
            }
        }
    }
}