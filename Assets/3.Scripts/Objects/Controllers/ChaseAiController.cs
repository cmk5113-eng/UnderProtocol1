using UnityEngine;

public class ChaseAiController : AIController
{

    protected override void OnPossess(CharacterBase newcontroller)
    {
        GameManager.OnUpdateController -= Think;
        GameManager.OnUpdateController += Think;
    }
    protected override void OnUnpossess(CharacterBase oldcontroller)
    {
        GameManager.OnUpdateController -= Think;
    }

    protected override void Think(float deltaTime)
    {
        if (!FocusTarget) return;
        CommandMoveToDestination(FocusTarget.transform.position, 1.0f);
    }
}


