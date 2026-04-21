using UnityEngine;


//pawn: 조종할 수 있지만 이동할수 없 는 캐릭터

public class MovableCharacter : CharacterBase, IRunnable
{
    protected    Vector3 targetDestination;
    protected float targetTolerance;

    void Start()
    {
        RegistrationFunctions(); 
    }
    public void RegistrationFunctions()
    {


        GameManager.OnUpdateCharacter -= PhysicsUpdate;
        GameManager.OnUpdateCharacter += PhysicsUpdate;
    }
    public void UnRegistrationFunctions()
    {
        GameManager.OnUpdateCharacter -= PhysicsUpdate;

    }
    public void PhysicsUpdate(float deltaTime)
    {
      Vector3 currentMoveDirection = (targetDestination - transform.position); 
      float distance = currentMoveDirection.magnitude;
        if (distance > targetTolerance)
        { 
            currentMoveDirection.Normalize();

            transform.position += deltaTime * 5.0f * currentMoveDirection;
           
        }
    
    }

    public void MoveToDestination(Vector3 destination, float tolerance)
    {
        targetDestination = destination;   
        targetTolerance = tolerance;

    }

    public void MoveToDirection(Vector3 direction)
    {

    }
    public void StopMovement()
    { 

    }
}
