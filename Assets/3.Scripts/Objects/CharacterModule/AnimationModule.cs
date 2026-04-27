using UnityEngine;

public class AnimationModule : CharacterModule
{
    MovementModule movementModule;

    [SerializeField] Animator anim;
    [SerializeField] bool isRotationByMogvement;
    public sealed override System.Type RegistrationType => typeof(AnimationModule);

    public override void OnRegistration(CharacterBase newOwner)
    {
        base.OnRegistration(newOwner);
        newOwner.OnLookAt -= AnimationByLookRotation;
        newOwner.OnLookAt += AnimationByLookRotation;
        newOwner.OnMovement -= AnimationByMovement;
        newOwner.OnMovement += AnimationByMovement;
    }
    public override void OnUnregistration(CharacterBase oldOwner)
    {
        base.OnUnregistration(oldOwner);
        oldOwner.OnLookAt -= AnimationByLookRotation;
        oldOwner.OnLookAt -= AnimationByMovement;
    }
    
    public void AnimationByLookRotation(Vector3 lookRotation)
    {
        if (!anim) return;
        anim.SetFloat("MoveX", lookRotation.x);
        anim.SetFloat("MoveY", lookRotation.y);
    }
    public void AnimationByMovement(Vector3 moveDelta)
    {
        if (!anim) return;
        if (isRotationByMogvement && moveDelta.sqrMagnitude > 0)
        { 
        AnimationByLookRotation (moveDelta);
        }
        anim.SetFloat("MoveSpeed", moveDelta.magnitude / Time.fixedDeltaTime);
    }

    
}
