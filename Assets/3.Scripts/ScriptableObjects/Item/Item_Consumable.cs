using UnityEngine;

[CreateAssetMenu(fileName = "Item_Consumable", menuName = "Scriptable Objects/Item_Consumable")]
public class Item_Consumable : ItemContainer
{
    public virtual bool IsUsable(CharacterBase from, CharacterBase to) => true;
    public virtual void Onuse(CharacterBase from, CharacterBase to)
    { }
    public virtual bool IsUsable(CharacterBase from, Vector3 position) => true;
    public virtual void OnUse(CharacterBase from, Vector3 position)
    { }

}