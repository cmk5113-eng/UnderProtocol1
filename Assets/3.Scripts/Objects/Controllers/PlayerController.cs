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
        if (move == null) move = GetComponent<MoveTileModule>(); // ���� üũ
        var tm = PlacementManager.Instance?.tilemap;
        if (tm == null) return;

        Vector3Int targetCell = tm.WorldToCell(worldPosition);
        // ��ǥ ���� ��� ����(����ư FindPath ���)
        move.MoveToTile(targetCell);
    }
    public void SelectCharacter(CharacterBase target)
    {
        if (target == null) return;
        if (!target.selectable) return;

        selectedCharacter = target;

        // ���� ĳ���� ���� + �� ĳ���� ����
        Possess(target);
    }
    private void SelectByMouse(bool value, Vector2 screenPos, Vector3 worldPos)
    {
        if (!value) return; // Ŭ�� ������ ����

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
    public void SummonAndSelect(ActiveSkill skill, Vector3 pos)
    {
        //if (skill == null || skill.summonPrefab == null) return;

        //GameObject obj = Instantiate(skill.summonPrefab, pos, Quaternion.identity);

        //var character = obj.GetComponent<CharacterBase>();
        //if (character == null) return;

        //SelectCharacter(character);
    }
}
