using UnityEngine;

public class SkillSlot
{
    [SerializeField] private SkillList skill;      // 현재 슬롯에 담긴 스킬 데이터
    [SerializeField] private int currentStack;    // 현재 쌓인 개수

    // --- 데이터 반환용 메서드들 ---
    public SkillList GetSkill() => skill;
    public int GetStack() => currentStack;
    public bool GetIsEmpty() => skill == null;

    public bool GetIsMax()
    {
        if (skill == null) return false;
        return currentStack >= 100;
    }

    // --- 1. AddSkill 복구 (스킬 추가 및 초과량 반환) ---
    public int AddSkill(SkillList wantSkill, int amount)
    {
        if (skill == null)
        {
            skill = wantSkill;
            currentStack = 0;
        }

        int maxStack = 100;
        int roomLeft = maxStack - currentStack;

        if (amount <= roomLeft)
        {
            currentStack += amount;
            return 0; // 다 채웠으므로 남은 수량 0
        }
        else
        {
            currentStack = maxStack;
            return 0; // 수용량 초과하여 남은 수량 반환
        }
    }

    // --- 2. RemoveSkill 복구 (단순 제거 및 개수 차감) ---
    public int RemoveSkill()
    {
        int removedAmount = currentStack;
        skill = null;
        currentStack = 0;
        return removedAmount;
    }
    public int RemoveSkill(SkillList wantSkill)
    {
        if (skill != wantSkill) return 0;

        return RemoveSkill();
    }



    public int RemoveSkill(SkillList wantSkill, int amount)
    {
        if (skill != wantSkill) return amount;

        if (currentStack <= amount)
        {
            int remainingAmount = amount - currentStack;
            skill = null;
            currentStack = 0;
            return remainingAmount; // 덜 지워진 남은 개수 반환
        }
        else
        {
            currentStack -= amount;
            return 0; // 다 지웠으므로 남은 차감 수량 0
        }
    }

    // --- 3. ExchangeSkill 복구 (정렬용 두 슬롯 데이터 스왑) ---
    public void ExchangeSkill(SkillSlot other)
    {
        if (other == null) return;

        // 두 컴포넌트 간의 데이터 백업 및 교환
        SkillList tempSkill = this.skill;
        int tempStack = this.currentStack;

        this.skill = other.skill;
        this.currentStack = other.currentStack;

        other.skill = tempSkill;
        other.currentStack = tempStack;

        // 데이터가 교환되었으므로 UI 갱신 유도
        this.NoticeChanged();
        other.NoticeChanged();
    }

    public void NoticeChanged()
    {
        // 💡 102번째 줄 근처의 코드를 아래처럼 안전장치로 감싸주세요.
        try
        {
            // 기존에 작성되어 있던 GetComponent나 UI 갱신 코드들...
            // 예: GetComponent<Image>().sprite = ...
        }
        catch (System.NullReferenceException)
        {
            // 💡 만약 UI 컴포넌트가 없는 순수 데이터 슬롯이라면 에러를 무시하고 넘어갑니다.
            // 어차피 UI_SkillList에서 켜질 때 ConnectSlot으로 다시 그려주기 때문에 무시해도 안전합니다.
            Debug.LogWarning("순수 데이터 SkillSlot이므로 UI 갱신(NoticeChanged)을 건너뜁니다.");
        }
    }
}