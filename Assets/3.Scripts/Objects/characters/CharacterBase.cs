using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEditor.Experimental.GraphView;


public delegate void MovementEvent(Vector3 move);
public delegate void LookAtEvent(Vector3 direction);
public delegate void DamageEvent(GameObject damageCauser,ControllerBase instigator, float damage);
// --- 전역 열거형 (어떤 스크립트에서도 접근 가능하도록 클래스 밖에 배치) ---
public enum JobType { Warrior, Archer, Mage, Builder }
public enum ElementType { None, Fire, Water, Electric, Earth }





public class CharacterBase : MonoBehaviour
{
public bool selectable = true;

    public int actionPoint = 0;
    public int mobility = 0;


    public event MovementEvent OnMovement;
    public void MovementNotify(Vector3 move) => OnMovement?.Invoke(move);
    
    
    
    public event LookAtEvent OnLookAt;
    public void LookAtNotify(Vector3 direction) => OnLookAt?.Invoke(direction);

    public event DamageEvent OnDamage;
    public void DamageNotify(GameObject damageCauser, ControllerBase instigator, float damage)
    => OnDamage?.Invoke(damageCauser, instigator, damage);

    ControllerBase _controller;
    public ControllerBase Controller => _controller;

    protected Vector3 _lookRotation;
    protected Vector3 LookRotation =>_lookRotation;

    public virtual string DisplayName => "character";



    Dictionary<System.Type, CharacterModule> moduleDictionary = new();
    //추가 /제거 /검색

    public void AddModule(System.Type wantType, CharacterModule wantModule)
    {
        if (moduleDictionary.TryAdd(wantType, wantModule))
        {
            wantModule.OnRegistration(this);
        }

    }

    public void AddAllModuleFromObject(GameObject target)
    {

        if (!target) return;
        foreach (CharacterModule currentModule in target.GetComponentsInChildren<CharacterModule>())
        {
            AddModule(currentModule.RegistrationType, currentModule);
        }

    }

    public void RemoveModule(System.Type wantType)
    {
        if (moduleDictionary.ContainsKey(wantType))
        {
            moduleDictionary[wantType]?.OnUnregistration(this);
            moduleDictionary.Remove(wantType);

        }

    }
    public void RemoveAllModule()
    {
        foreach (CharacterModule currentModule in moduleDictionary.Values)
        {
            currentModule.OnUnregistration(this);
        }
    }


    public T GetModule<T>() where T : CharacterModule
    {
        moduleDictionary.TryGetValue(typeof(T), out CharacterModule result);
        return result as T;
    }
    public virtual void OnPossessed(ControllerBase newcontroller)
    {

    }
    public ControllerBase Possessed(ControllerBase from)
    {

        if(_controller) Unpossessed();
        _controller = from;
        AddAllModuleFromObject(gameObject);
        OnPossessed(Controller);
        return Controller;
    }


    public virtual void OnUnpossessed(ControllerBase oldcontroller)
    { 
    }
    public void Unpossessed()
    {

        if(Controller)OnUnpossessed(_controller);
        RemoveAllModule();
        _controller = null;
    }
    public bool Unpossessed(ControllerBase oldController)
    {
        if (Controller != oldController) return false;
        Unpossessed();
        return true;
    
    }

    public bool CanAct()
    {
        return false;
    }
    public bool CanMove() 
    {
        return false;
    }
    public bool CanUseSkill()
    {
        return false;
    }

}