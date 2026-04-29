using System;
using UnityEngine;

public class PlayerController : ControllerBase
{
    MoveTileModule move;

    void Awake()
    {
        move = GetComponent<MoveTileModule>();
    }

    void Update()
    {
        Vector2 input = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")
        );

        move.TryStepByInput(input);
    }

    protected override void OnPossess(CharacterBase newCharacter)
    {
        base.OnPossess(newCharacter);   
        InputManager.OnMouseRightButton -= MoveToMousePosition;
        InputManager.OnMouseRightButton += MoveToMousePosition;
        InputManager.OnMove -= MoveToDirection;
        InputManager.OnMove += MoveToDirection;

    }

    protected override void OnUnpossess(CharacterBase oldCharacter)
    {
        base.OnUnpossess(oldCharacter);
        InputManager.OnMouseRightButton -= MoveToMousePosition;
        InputManager.OnMove -= MoveToDirection;
    }

    public void MoveToMousePosition(bool value, Vector2 screenPosition, Vector3 worldPosition)
    {
        if (move == null) move = GetComponent<MoveTileModule>(); // 안전 체크
        var tm = PlacementManager.Instance?.tilemap;
        if (tm == null) return;

        Vector3Int targetCell = tm.WorldToCell(worldPosition);
        // 목표 셀로 경로 생성(맨해튼 FindPath 사용)
        move.MoveToTile(targetCell);
    }
    private void MoveToDirection(Vector2 value)
    {
        CommandMoveToDirection(value);
    }

}
