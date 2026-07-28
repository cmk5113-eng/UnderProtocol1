using System;
using UnityEngine;

public class PlayerController : ControllerBase
{
    MoveTileModule move;
    CharacterBase selectedCharacter;

    void Awake()
    {
    }

    void Update()
    {
        Vector2 input = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")
        );
    }

    protected override void OnPossess(CharacterBase newCharacter)
    {
        base.OnPossess(newCharacter);

        // 💡 기존 중복 구독 방지 후 이벤트 등록
        UnsubscribeInputEvents();
        SubscribeInputEvents();
    }

    protected override void OnUnpossess(CharacterBase oldCharacter)
    {
        base.OnUnpossess(oldCharacter);

        // 💡 빙의 해제 시 모든 이벤트 해제
        UnsubscribeInputEvents();
    }

    // 💡 [핵심] 오브젝트가 파괴되거나 비활성화될 때 구독 해제 (MissingReferenceException 방지!)
    private void OnDisable()
    {
        UnsubscribeInputEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeInputEvents();
    }

    // 💡 이벤트 구독 전담 메서드
    private void SubscribeInputEvents()
    {
        //InputManager.OnMouseRightButton += MoveToMousePosition;
        //InputManager.OnMove += MoveToDirection;
        InputManager.OnMouseLeftButton += SelectByMouse;
    }

    // 💡 이벤트 구독 해제 전담 메서드
    private void UnsubscribeInputEvents()
    {
        //InputManager.OnMouseRightButton -= MoveToMousePosition;
        InputManager.OnMove -= MoveToDirection;
        InputManager.OnMouseLeftButton -= SelectByMouse;
    }

    //public void MoveToMousePosition(bool value, Vector2 screenPosition, Vector3 worldPosition)
    //{
    //    // 💡 [안전장치] 만약 파괴 과정에서 호출되더라도 예외가 터지지 않게 this(null) 검사
    //    if (this == null) return;

    //    if (move == null) move = GetComponent<MoveTileModule>();
    //    var tm = PlacementManager.Instance?.tilemap;
    //    if (tm == null) return;

    //    Vector3Int targetCell = tm.WorldToCell(worldPosition);

    //    if (move != null)
    //    {
    //        move.MoveToTile(targetCell);
    //    }
    //}

    public void SelectCharacter(CharacterBase target)
    {
        if (target == null) return;
        if (!target.selectable) return;

        selectedCharacter = target;
        Possess(target);
    }

    private void SelectByMouse(bool value, Vector2 screenPos, Vector3 worldPos)
    {
        if (this == null) return;
        if (!value) return;

        Ray ray = Camera.main.ScreenPointToRay(screenPos);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            var character = hit.collider.GetComponent<CharacterBase>();

            if (character != null)
            {
                SelectCharacter(character);
            }
        }
    }

    private void MoveToDirection(Vector2 value)
    {
        if (this == null) return;
        CommandMoveToDirection(value);
    }

    public void SummonAndSelect(ActiveSkill skill, Vector3 pos)
    {
    }
}