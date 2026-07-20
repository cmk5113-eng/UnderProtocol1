using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    // 🔴 [핵심] 어디서나 Inventory.Instance로 접근 가능한 싱글톤 선언
    public static Inventory Instance { get; private set; }

    public static SkillSlot cursorSlot;
    public int columns;
    public int rows;
    [SerializeField] TMP_InputField amountInput;

    public SkillSlot[,] slots;
    [SerializeField] private SkillList ActiveList;
    [SerializeField] private SkillList PassiveList;
    private void Awake()
    {
        // 싱글톤 인스턴스 중복 체크 및 지정
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;

        // 씬이 바뀌어도 파괴되지 않도록 설정 (선택 사항, 필요 없다면 주석 처리 가능)
        DontDestroyOnLoad(gameObject);

        // 안전하게 실행 시점에 공장 빌드 및 초기화 수행
        BuildFactory();
        Initialize();
    }
    public void Initialize()
    {
        BuildFactory();

        // 🔴 [중요] slots 2차원 배열의 모든 칸에 진짜 SkillSlot 객체를 채워넣어 null을 방지합니다.
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < columns; j++)
            {
                if (slots[i, j] == null)
                {
                    slots[i, j] = new SkillSlot();
                }
            }
        }
    }
    public void AddAllActiveSkillsFromSource()
    {
        // 1. 데이터 소스 및 리스트 검증
        if (ActiveList == null || ActiveList.skillsList == null || ActiveList.skillsList.Count == 0)
        {
            Debug.LogWarning("Skill Data Source 또는 내부 Skills List가 비어있습니다!");
            return;
        }

        Debug.Log($"[액티브 추가 시작] 총 {ActiveList.skillsList.Count}개의 스킬 항목을 추가합니다.");

        // 2. 리스트를 순회하며 순서대로 인벤토리에 추가
        foreach (SkillList skill in ActiveList.skillsList)
        {
            if (skill == null)
            {
                Debug.LogWarning("⚠️ [ActiveList] 리스트에 null 데이터(빈 칸)가 포함되어 있습니다.");
                continue;
            }

            // 🔴 스킬 이름과 아이콘 Null 체크 디버그 로그 추가
            string skillName = string.IsNullOrEmpty(skill.name) ? "이름 없음(Null 또는 Empty)" : skill.name;

            if (skill.icon == null)
            {
                Debug.LogError($"❌ [스킬 에셋 불량] 스킬명: '{skillName}' -> 아이콘(Sprite)이 Null입니다! 인스펙터에서 이미지를 등록했는지 확인하세요.");
            }
            else
            {
                Debug.Log($"🟢 [스킬 에셋 검증 완료] 스킬명: '{skillName}' -> 아이콘 정상 등록됨 ({skill.icon.name})");
            }

            AddSkill(skill, 1);
        }
    }
    public void RemoveAllActiveSkillsFromSource()
    {
        if (ActiveList == null || ActiveList == null) return;

        foreach (SkillList skill in ActiveList.skillsList)
        {
            if (skill == null) continue;
            RemoveSkill(skill, 1);
        }
    }

    public void AddAllPassiveSkillsFromSource()
    {
        // ❌ 여기도 Initialize(); 호출이 있다면 삭제하거나 들어오지 못하게 막아야 합니다.
        if (PassiveList == null || PassiveList.skillsList == null || PassiveList.skillsList.Count == 0)
        {
            Debug.LogWarning("Skill Data Source 또는 내부 Skills List가 비어있습니다!");
            return;
        }

        Debug.Log($"[패시브 추가 시작] 총 {PassiveList.skillsList.Count}개의 스킬 항목을 추가합니다.");

        foreach (SkillList skill in PassiveList.skillsList)
        {
            if (skill == null) continue;
            AddSkill(skill, 1);
        }
    }

    public void RemoveAllSkillsFromSource()
    {
        if (PassiveList == null || PassiveList.skillsList == null) return;

        foreach (SkillList skill in PassiveList.skillsList)
        {
            if (skill == null) continue;
            RemoveSkill(skill, 1);
        }
    }

    // 💡 방을 새로 짜는 팩토리
    public void BuildFactory()
    {
        slots = new SkillSlot[rows, columns];

        for (int x = 0; x < rows; x++)
        {
            for (int y = 0; y < columns; y++)
            {
                slots[x, y] = new SkillSlot();
            }
        }

    }

    // 💡 방 안의 알맹이만 비우는 함수
    public void RegisterSlot(int index, SkillSlot instantiatedSlot)
    {
        if (slots is null) BuildFactory(); // slots 자체가 null이면 새로 생성

        int width = slots.GetLength(1);
        int row = index / width;
        int column = index % width;

        slots[row, column] = instantiatedSlot;
    }

    // --- 이하 기존 유저님 정렬 및 검색 소스 유지 ---
    public void Sort(System.Comparison<SkillSlot> Method)
    {
        int totalLength = slots.Length;
        if (slots is null || totalLength <= 1) return;
        int width = slots.GetLength(1);

        int lastFinder = totalLength - 1;

        while (lastFinder > 0)
        {
            int currentFinder = -1;
            for (int i = 0; i < lastFinder; i++)
            {
                SkillSlot left = GetSlot(i, width);
                SkillSlot right = GetSlot(i + 1, width);
                int comparisonResult = Method(left, right);
                if (comparisonResult > 0)
                {
                    currentFinder = i;
                    left.ExchangeSkill(right);
                }
            }
            lastFinder = currentFinder; // 💡 무한 루프 버그 방지를 위해 -1 대신 currentFinder 대입으로 수정
        }
    }

    int SkillTypeComparison(SkillSlot left, SkillSlot right)
    {
        int result;
        if (SkillExistComparison(left, right, out result)) return result;

        if (result != 0) return result;
        result = left.GetStack() - right.GetStack();
        return result;
    }

    int? SkillExistComparison(SkillSlot left, SkillSlot right)
    {
        if (left is null)
        {
            if (right is null) return 0;
            else return -1;
        }
        if (right is null) return 1;

        SkillList leftSkill = left.GetSkill();
        if (leftSkill == null) return 1;
        SkillList rightSkill = right.GetSkill();
        if (rightSkill == null) return -1;

        return leftSkill.type - rightSkill.type;
    }

    bool SkillExistComparison(SkillSlot left, SkillSlot right, out int result)
    {
        int? calculated = SkillExistComparison(left, right);
        result = calculated ?? 0;
        return calculated.HasValue;
    }

    public void SortbyType() => Sort(SkillTypeComparison);

    public int CountSkill(SkillList wantSkill)
    {
        if (!wantSkill) return 0;
        int result = 0;
        foreach (SkillSlot currentSlot in FindFirstSkill(wantSkill))
        {
            result += currentSlot.GetStack();
        }
        return result;
    }

    public int CountSkill(SkillList wantSkill, out List<SkillSlot> returnSlots)
    {
        returnSlots = new List<SkillSlot>();
        if (!wantSkill) return 0;
        int result = 0;

        foreach (SkillSlot currentSlot in FindFirstSkill(wantSkill))
        {
            returnSlots.Add(currentSlot);
            result += currentSlot.GetStack();
        }
        return result;
    }

    public SkillSlot GetSlot(int index, int width) => slots[index / width, index % width];

    public SkillSlot GetSlot(int index)
    {
        if (slots is null || index < 0 || slots.Length <= index) return null;
        int width = slots.GetLength(1);
        return slots[index / width, (index % width)];
    }

    public IEnumerable<SkillSlot> FindFirstSkill(SkillList target)
    {
        foreach (SkillSlot currentSlot in GetAllSlot())
        {
            if (currentSlot.GetSkill() == target) yield return currentSlot;
        }
    }

    public IEnumerable<SkillSlot> FindLastSkill(SkillList target)
    {
        foreach (SkillSlot currentSlot in GetAllSlotReverse())
        {
            if (currentSlot.GetSkill() == target) yield return currentSlot;
        }
    }

    public IEnumerable<SkillSlot> GetAllSlot()
    {
        if (slots is null) yield break;

        int height = slots.GetLength(0);
        int width = slots.GetLength(1);
        for (int row = 0; row < height; row++)
        {
            for (int column = 0; column < width; column++)
            {
                if (slots[row, column] is null) continue;
                yield return slots[row, column];
            }
        }
    }

    public IEnumerable<SkillSlot> GetAllSlotReverse()
    {
        if (slots is null) yield break;

        int height = slots.GetLength(0);
        int width = slots.GetLength(1);
        for (int row = height - 1; row >= 0; row--)
        {
            for (int column = width - 1; column >= 0; column--)
            {
                if (slots[row, column] is null) continue;
                yield return slots[row, column];
            }
        }
    }

    public IEnumerable<SkillSlot> FindFirstEmptySlot()
    {
        foreach (SkillSlot currentSlot in GetAllSlot())
        {
            if (currentSlot.GetIsEmpty()) yield return currentSlot;
        }
    }

    public int AddSkill(SkillList wantSkill, int amount = 1)
    {
        amount = AddSkillOnExistSlots(wantSkill, amount);
        if (amount <= 0) return 0;
        return AddSkillOnEmptySlots(wantSkill, amount);
    }

    public int AddSkillOnExistSlots(SkillList wantSkill, int amount)
    {
        foreach (SkillSlot currentSlot in FindFirstSkill(wantSkill))
        {
            if (amount <= 0) return 0;
            amount = currentSlot.AddSkill(wantSkill, amount);
            currentSlot.NoticeChanged();
        }
        return amount;
    }

    public int AddSkillOnEmptySlots(SkillList wantSkill, int amount)
    {
        if (slots == null)
        {
            BuildFactory();
            Initialize();
        }
        int height = slots.GetLength(0);
        int width = slots.GetLength(1);

        for (int row = 0; row < height; row++)
        {
            for (int column = 0; column < width; column++)
            {
                if (amount <= 0) return 0;

                SkillSlot currentSlot = slots[row, column];

                if (currentSlot is null)continue;

                // 이제 slots 내부가 채워졌으므로 GetIsEmpty()가 무사히 실행됩니다.
                if (currentSlot.GetIsEmpty())
                {
                    amount = currentSlot.AddSkill(wantSkill, amount);
                    currentSlot.NoticeChanged();

                    if (amount <= 0) return 0;
                }
            }
        }
        return amount;
    }
    public int RemoveSkill(SkillList wantSkill)
    {
        int result = 0;
        foreach (SkillSlot currentSlot in FindLastSkill(wantSkill))
        {
            result += currentSlot.RemoveSkill(wantSkill, 1);
            currentSlot.NoticeChanged();
        }
        return result;
    }

    public int RemoveSkill(SkillList wantSkill, int amount)
    {
        foreach (SkillSlot currentSlot in FindLastSkill(wantSkill))
        {
            if (amount <= 0) return 0;
            amount = currentSlot.RemoveSkill(wantSkill, amount);
            currentSlot.NoticeChanged();
        }
        return amount;
    }
}