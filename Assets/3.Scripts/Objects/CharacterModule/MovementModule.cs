using UnityEngine;

public class MovementModule : CharacterModule, IRunnable
{

    protected Vector3? targetDestination = null;
    protected Vector3? targetDirection = null;
    protected float targetTolerance;

    public sealed override System.Type RegistrationType => typeof(MovementModule);


    public override void OnRegistration(CharacterBase newOwner)
    {
        base.OnRegistration(newOwner);
        GameManager.OnPhysicsCharacter -= MovementUpdate;
        GameManager.OnPhysicsCharacter += MovementUpdate;

    }
    public override void OnUnregistration(CharacterBase oldOwner)
    {
        base.OnUnregistration(oldOwner);
        GameManager.OnPhysicsCharacter -= MovementUpdate;

    }

    public void MovementUpdate(float deltaTime)
    {
        Vector3 originPosition = transform.position;
        PhysicsUpdate(deltaTime);
        Vector3 positionDelta = transform.position - originPosition;
        Owner.MovementNotify(positionDelta);
    }
    public virtual void PhysicsUpdate(float deltaTime)
    {
        UpdateToDirection(deltaTime);
        UpdateToDestination(deltaTime);
    }
    public virtual float GetMoveSpeed() => 5.0f;
    public virtual float GetMoveSpeed(float deltaTime) => GetMoveSpeed() * deltaTime;
    public void Translate(Vector3 delta)
    {
        transform.position += delta;
    }
    public void UpdateToDirection(float deltaTime)
    {
        if (targetDirection is null) return;
        float speed = GetMoveSpeed(deltaTime);
        Translate(speed * targetDirection.Value);

    }
    public void UpdateToDestination(float deltaTime)
    {
        if (targetDestination is null) return;

        Vector3 currentMoveDirection = (targetDestination.Value - transform.position);
        float distance = currentMoveDirection.magnitude;

        if (distance > targetTolerance)
        {
            currentMoveDirection.Normalize();
            float speed = GetMoveSpeed(deltaTime);
            float resultMoveSpeed = Mathf.Min(speed, distance);
            Translate(resultMoveSpeed * currentMoveDirection);
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
