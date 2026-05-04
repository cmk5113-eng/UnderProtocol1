using System.Collections;
using UnityEngine;

public class SelectionManager : ManagerBase
{
    public static SelectionManager Instance { get; private set; }

    public static SkillObject selectedSkill;
    public static CharacterBase selectedCharacter;
    protected override IEnumerator OnConnected(GameManager newManager)
    {
        // 싱글턴 설정
        Instance = this;    

        // 입력/업데이트 이벤트 구독 (중복 구독 방지 위해 먼저 해제)
        InputManager.OnMouseLeftButton -= HandleMouseLeft;
        InputManager.OnMouseLeftButton += HandleMouseLeft;

        InputManager.OnMouseMove -= HandleMouseMove;
        InputManager.OnMouseMove += HandleMouseMove;

        InputManager.OnConfirm -= HandleConfirm;
        InputManager.OnConfirm += HandleConfirm;

        GameManager.OnUpdateManager -= UpdateEvent;
        GameManager.OnUpdateManager += UpdateEvent;

        yield return null;
    }

    protected override void OnDisconnected()
    {
        // 이벤트 정리
        InputManager.OnMouseLeftButton -= HandleMouseLeft;
        InputManager.OnMouseMove -= HandleMouseMove;
        InputManager.OnConfirm -= HandleConfirm;
        GameManager.OnUpdateManager -= UpdateEvent;

        if (Instance == this) Instance = null;
    }

    // 외부에서 SkillObject 등록/해제
    public static void SetSelectedSkill(SkillObject skill)
    {
        selectedSkill = skill;
        Debug.Log($"SelectionManager: SetSelectedSkill -> {skill?.data?.skillName}");
        // TODO: 시각화 / 커서 변경 등
    }

    public static void ClearSelectedSkill()
    {
        selectedSkill = null;
        Debug.Log("SelectionManager: ClearSelectedSkill");
        // TODO: 시각화 제거
    }

    public static void SetSelectedCharacter(CharacterBase character)
    {
        selectedCharacter = character;
        Debug.Log($"SelectionManager: SetSelectedCharacter -> {character?.name}");
        // TODO: 시각화 / 커서 변경 등
    }

    public static void ClearSelectedCharacter()
    {
        selectedCharacter = null;
        Debug.Log("SelectionManager: ClearSelectedSkill");
        // TODO: 시각화 제거
    }


    // 기존: worldPosition(Vector3)으로 바로 전달
    public void ProvideTarget(Vector3 worldPosition, GameObject target = null)
    {
        if (selectedSkill == null) return;
        selectedSkill.ReceiveTarget(worldPosition, target);
    }

    // 추가: screenPosition(Vector2)을 받아 카메라로 변환 후 전달 (호출자가 Vector2만 가지고 있을 때 안전하게 사용)
    public void ProvideTarget(Vector2 screenPosition, GameObject target = null)
    {
        Camera cam = GameManager.Instance?.Camera?.MainCamera ?? Camera.main;
        Vector3 world = Vector3.zero;
        if (cam != null)
        {
            // ScreenToWorldPoint는 z를 카메라 대비 거리로 사용하므로 2D일 때 z를 0으로 고정
            world = cam.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, cam.nearClipPlane));
            if (GameManager.Instance?.Input?.is2D ?? true)
                world.z = 0f;
        }

        ProvideTarget(world, target);
    }

    // 확인 시 스킬 실행
    public void ConfirmSelection()
    {
        if (selectedSkill == null) return;
        selectedSkill.Execute();
    }

    // ----- 입력 이벤트 핸들러 -----
    // MouseButtonEvent(bool value, Vector2 screenPosition, Vector3 worldPosition)
    void HandleMouseLeft(bool pressed, Vector2 screenPosition, Vector3 worldPosition)
    {
        if (!pressed) return; // 눌렀을 때만 처리
        if (selectedSkill == null) return;

        // 커서 아래 오브젝트를 확인해 전달
        GameObject under = GameManager.Instance?.Input?.GetGameObjectUnderCursor();

        // 안전하게 둘 중 하나로 전달: worldPosition이 유효하면 그걸, 아니라면 screenPosition 오버로드 사용
        if (worldPosition != Vector3.zero)
            ProvideTarget(worldPosition, under);
        else
            ProvideTarget(screenPosition, under);
    }

    // MouseMoveEvent(Vector2 screenPosition, Vector3 worldPosition)
    void HandleMouseMove(Vector2 screenPosition, Vector3 worldPosition)
    {
        if (selectedSkill == null) return;

        GameObject under = GameManager.Instance?.Input?.GetGameObjectUnderCursor();

        if (worldPosition != Vector3.zero)
            selectedSkill.ReceiveTarget(worldPosition, under);
        else
            // 즉시 미리보기용으로 화면좌표 기반 변환 사용
            ProvideTarget(screenPosition, under);
    }

    // ButtonEvent(bool value)
    void HandleConfirm(bool pressed)
    {
        if (!pressed) return;
        ConfirmSelection();
    }

    // 프레임 기반 업데이트 필요 시 사용
    void UpdateEvent(float deltaTime)
    {
        // 현재는 빈 구현. 선택 시 범위 표시/타이머 등 필요하면 추가.
    }
}
