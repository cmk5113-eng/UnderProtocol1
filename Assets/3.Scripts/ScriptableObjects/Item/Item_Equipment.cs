using UnityEngine;

[CreateAssetMenu(fileName = "Item_Equipment", menuName = "Scriptable Objects/Item_Equipment")]
public class Item_Equipment : ScriptableObject
{
    public virtual void OnEquip(CharacterBase target)
    { }

    public virtual void OnUnequip(CharacterBase target)
    {

    }

}
