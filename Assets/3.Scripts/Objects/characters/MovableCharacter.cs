using Unity.VisualScripting;
using UnityEngine;


//pawn: 조종할 수 있지만 이동할수 없 는 캐릭터

public class MovableCharacter : CharacterBase, IRunnable, IFunctionable
{
    protected    Vector3? targetDestination =   null;
    protected Vector3? targetDirection= null;
    protected float targetTolerance;

  
    public void RegistrationFunctions()
    {


        GameManager.OnPhysicsCharacter -= PhysicsUpdate;
        GameManager.OnPhysicsCharacter += PhysicsUpdate;
    }
    public void UnregistrationFunctions()
    {
        GameManager.OnPhysicsCharacter -= PhysicsUpdate;

    }
    public void PhysicsUpdate(float deltaTime)
    {
        UpdateToDirection(deltaTime);
        UpdateToDestination(deltaTime);
    }
    public void UpdateToDirection(float deltaTime)
    {
        if (targetDirection is null) return;
        float speed = deltaTime * 5.0f;
        transform.position += speed * targetDirection.Value;

    }

    public void UpdateToDestination(float deltaTime)
    {
        if (targetDestination is null) return;

        Vector3 currentMoveDirection = (targetDestination.Value - transform.position);
        float distance = currentMoveDirection.magnitude;

        if (distance > targetTolerance)
        { 
            currentMoveDirection.Normalize();
            float speed = deltaTime * 5.0f;
            float resultMoveSpeed = Mathf.Min(speed, distance);
            transform.position += resultMoveSpeed * currentMoveDirection;
        } 
    }

    public void MoveToDestination(Vector3 destination, float tolerance)
    {
        targetDirection = null;
        targetDestination = destination;   
        targetTolerance = tolerance;

    }

    public void MoveToDirection(Vector3 direction)
    {
        targetDirection = direction.normalized;
        targetDestination = null;

    }
    public void StopMovement()
    {
        targetDirection = null;
        targetDestination = null;
    }
}
