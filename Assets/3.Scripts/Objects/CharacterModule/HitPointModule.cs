using System;
using UnityEditor.Build.Pipeline;
using UnityEngine;

public class HitPointModule : CharacterModule
{

    float _hpMax;
    public float HPMax => _hpMax;
    
    float _hp;
    public float HP => _hp;

    public bool getdead = false;
    public sealed override Type RegistrationType => typeof(HitPointModule);

    public float IncreaseHp(float value)
    {
        _hp += value;
        _hp = Math.Min(_hpMax,_hp);
        return _hp;
    }
    public float DecreaseHp(float value)
    {
        _hp += value;
        _hp = Math.Max(0, _hp);
        if (HP == 0) getdead = true;
        return _hp;
    }
    public float SetHp(float damage, float heal)
    {
        float value = heal - damage;
        return value;
    }
    public float Damage()
    {
        float damage = 0;
        //대충데미지계산식
        return damage;
    }
    public float Heal()
    {
        float heal = 0;
        //대충힐계산식
        return heal;
    }
    
}
