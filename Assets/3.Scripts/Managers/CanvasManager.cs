using UnityEngine;

public class CanvasManager : MonoBehaviour
{

    public static Inventory SkillList = new Inventory();    

    // 싱글톤
    public static CanvasManager Instance { get; private set; }

    // 현재 어떤 슬롯(Active1, Passive2...)을 선택했는지 저장
    public SkillLoadButton.SkillGroupType CurrentSkillGroup { get; private set; }

    [SerializeField] private ObjectManager objectManager;
    [SerializeField] private Transform uiParentTransform;
    private GameObject activeSkillListWindow;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void OpenSkillListWithData(SkillLoadButton.SkillGroupType groupType)
    {
        CloseSkillList();
        CurrentSkillGroup = groupType;

        // 1. UI 창을 정확하게 엽니다.
        UI_SkillList skillListUI = UIManager.ClaimOpenUI(UIType.SkillList) as UI_SkillList;
        if (skillListUI == null) return;

        activeSkillListWindow = skillListUI.gameObject;

        // 2. 🔴 [핵심 수정] FindFirstObjectByType을 절대 쓰지 마세요!
        // 방금 열린 프리패브(skillListUI)의 본체나 자식에 붙어있는 "진짜 연결된 인벤토리"만 가져옵니다.
        Inventory inventory = skillListUI.GetComponent<Inventory>();
        if (inventory == null) inventory = skillListUI.GetComponentInChildren<Inventory>(true);

        if (inventory == null)
        {
            Debug.LogError("[CanvasManager] 이 스킬리스트 UI 프리패브에는 Inventory 컴포넌트가 없습니다!");
            return;
        }

        // 3. 이제 이 진짜 인벤토리에만 데이터를 정확히 빌드하고 채웁니다.
        inventory.BuildFactory();
        inventory.Initialize();

        switch (groupType)
        {
            case SkillLoadButton.SkillGroupType.Active1:
            case SkillLoadButton.SkillGroupType.Active2:
                inventory.AddAllActiveSkillsFromSource();
                break;

            case SkillLoadButton.SkillGroupType.Passive1:
            case SkillLoadButton.SkillGroupType.Passive2:
            case SkillLoadButton.SkillGroupType.Passive3:
            case SkillLoadButton.SkillGroupType.Passive4:
                inventory.AddAllPassiveSkillsFromSource();
                break;
        }

        // 4. 데이터가 채워진 그 인벤토리를 기반으로 UI를 그리라고 신호를 줍니다.
        skillListUI.InitInventoryUI();
    }
    public void CloseSkillList()
    {
        if (activeSkillListWindow != null)
        {
            // ObjectManager에 파괴 함수가 있다면 그것을 사용하시고, 
            // 없다면 아래처럼 기본 Destroy를 사용하시면 됩니다.
            
            activeSkillListWindow = null;
            Debug.Log("스킬 장착 완료: W_SkillList 창을 성공적으로 닫았습니다.");
        }

    }
}