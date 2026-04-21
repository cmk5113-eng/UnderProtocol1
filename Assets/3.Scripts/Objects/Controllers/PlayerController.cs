                 using UnityEngine;

public class PlayerController : ControllerBase
{
    protected override void OnPossess(CharacterBase newCharacter)
    {
        base.OnPossess(newCharacter);   
        InputManager.OnMouseRightButton -= MoveToMousePosition;
        InputManager.OnMouseRightButton += MoveToMousePosition;
    } 


    protected override void OnUnpossess(CharacterBase oldCharacter)
    {
        base.OnUnpossess(oldCharacter);
        InputManager.OnMouseRightButton -= MoveToMousePosition;
    }

    public void MoveToMousePosition(bool value, Vector2 screenPosition, Vector3 worldPosition)
    {
        CommandMoveToDestination(worldPosition, 0.0f);
    }
}
