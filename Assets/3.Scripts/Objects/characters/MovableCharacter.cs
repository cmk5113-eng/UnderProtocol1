using Unity.VisualScripting;
using UnityEngine;


//pawn: 조종할 수 있지만 이동할수 없 는 캐릭터

public class MovableCharacter : CharacterBase, IRunnable, IFunctionable
{
    [SerializeField] Animator anim;

    protected    Vector3? targetDestination =   null;
    protected Vector3? targetDirection= null;
    protected float targetTolerance;

  
    public void RegistrationFunctions()
    {


        GameManager.OnPhysicsCharacter -= MovementUpdate;
        GameManager.OnPhysicsCharacter += MovementUpdate;
    }
    public void UnregistrationFunctions()
    {
        GameManager.OnPhysicsCharacter -= MovementUpdate;

    }

    public void MovementUpdate(float deltaTime)
    { 
        Vector3 originPosition = transform.position;
    PhysicsUpdate(deltaTime);
        Vector3 positionDelta = transform.position - originPosition;
    AnimationUpdate(positionDelta);
    }
    public void AnimationUpdate(Vector3 moveDelta)
    {
        if (!anim) return;
        anim.SetFloat("MoveX",LookRotation.x);
        anim.SetFloat("MoveY",LookRotation.y);
        anim.SetFloat("MoveSpeed",moveDelta.magnitude/Time.fixedDeltaTime);
    }
    public virtual void PhysicsUpdate(float deltaTime)
    {
        UpdateToDirection(deltaTime);
        UpdateToDestination(deltaTime);
    }

    public virtual float GetMoveSpeed() => 5.0f;
    public virtual float GetMoveSpeed(float deltaTime) => GetMoveSpeed()*deltaTime;

    public void Translate(Vector3 delta)    
    {
        transform.position += delta;
        _lookRotation = delta.normalized;
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
