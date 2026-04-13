using TMPro;
using UnityEngine;

public class UI_CharcterSelectWindows : UI_ScreenBase
{
    // 변수는 크기나 중요도 순으로 배치 (보통 컴포넌트 -> 기본 자료형)
    public TextMeshProUGUI Current;
    public TextMeshProUGUI Max;

    public int currentCharacterCount = 0;
    public int maxCharacterCount = 12; // 기본값 설정

    // UI 텍스트를 업데이트하는 내부 함수
    private void RefreshUI()
    {
        if (Current != null) Current.text = currentCharacterCount.ToString();
        if (Max != null) Max.text = maxCharacterCount.ToString();
    }
    private void OnEnable()
    {
        // 창이 켜질 때 실행하고 싶은 초기화 로직
        // 예: 매니저에서 현재 카운트 값을 가져와서 세팅
        // 지금은 일단 임시로 0, 12를 넣어서 실행해 볼게요.
        InitializeWindow();
    }
    public void InitializeWindow()
    {
        // 위에서 만든 Set 함수를 여기서 호출
        Set(0, 12);
    }

    // 1. 단순히 카운트만 1 올리고 싶을 때 호출
    public void AddCount()
    {
        if (currentCharacterCount < maxCharacterCount)
        {
            currentCharacterCount++;
            Set(currentCharacterCount, maxCharacterCount);
        }
        if (currentCharacterCount == maxCharacterCount)
        {
            UIManager.ClaimPopUp("띠딩", "인원 초과", "롸져");
        }
    }

    // 2. 외부(매니저 등)에서 특정 수치를 주입할 때 사용
    public int Set(int newCurrent, int newMax)
    {
        currentCharacterCount = newCurrent;
        maxCharacterCount = newMax;

        RefreshUI();

        return currentCharacterCount;
    }

    // 3. 초기화 시 사용
    public int ResetCount()
    {
        currentCharacterCount = 0;
        RefreshUI();
        return 0;
    }

    public void Toggle() => gameObject.SetActive(!IsOpen);
}

