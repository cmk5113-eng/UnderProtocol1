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

        move.TryStepByInput(input);
    }

    protected override void OnPossess(CharacterBase newCharacter)
    {
        base.OnPossess(newCharacter);   
        InputManager.OnMouseRightButton -= MoveToMousePosition;
        InputManager.OnMouseRightButton += MoveToMousePosition;
        InputManager.OnMove -= MoveToDirection;
        InputManager.OnMove += MoveToDirection;
        InputManager.OnMouseLeftButton -= SelectByMouse;
        InputManager.OnMouseLeftButton += SelectByMouse;
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
    public void SelectCharacter(CharacterBase target)
    {
        if (target == null) return;
        if (!target.selectable) return;

        selectedCharacter = target;

        // 기존 캐릭터 해제 + 새 캐릭터 빙의
        Possess(target);
    }
    private void SelectByMouse(bool value, Vector2 screenPos, Vector3 worldPos)
    {
        if (!value) return; // 클릭 눌렸을 때만

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
        CommandMoveToDirection(value);
    }
    public void SummonAndSelect(SkillData skill, Vector3 pos)
    {
        if (skill == null || skill.summonPrefab == null) return;

        GameObject obj = Instantiate(skill.summonPrefab, pos, Quaternion.identity);

        var character = obj.GetComponent<CharacterBase>();
        if (character == null) return;

        SelectCharacter(character);
    }
}
