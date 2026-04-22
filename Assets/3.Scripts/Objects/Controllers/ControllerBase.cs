using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.InputSystem.XR;

public class ControllerBase : MonoBehaviour, IFunctionable
{
    CharacterBase _character;
    public CharacterBase Character => _character;


    public void RegistrationFunctions()
    { 
        Possess(GetComponent<CharacterBase>());
    
    }
    public void UnregistrationFunctions()
    {
        
    }

    protected virtual void OnPossess(CharacterBase newcontroller)
    {
    }

    public void Possess(CharacterBase target)
    {
        if (!target) return;
        ControllerBase result = target.Possessed(this);
        if (result == this) 
        {
            _character = target;
            OnPossess(target);
        }
        
    }


    protected virtual void OnUnpossess(CharacterBase oldcontroller) { }
    public void Unpossess()
    {
        if (Character)
        {
            if (Character.Unpossessed(this))
            { 
             OnUnpossess(Character);
            }
        }
        _character = null;
    }
    public void CommandMoveToDirection(Vector3 diraction)
    { 
        if(Character is IRunnable target) target.MoveToDirection(diraction);
    }
    public void CommandMoveToDestination(Vector3 destination, float tolerance)
    {

        if (Character is IRunnable target) target.MoveToDestination(destination, tolerance);
    }

    public void CommandStop() 
    {
        if (Character is IRunnable target) target.StopMovement();                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                 
    }
}


