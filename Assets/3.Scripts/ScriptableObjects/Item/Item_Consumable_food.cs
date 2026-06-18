using UnityEngine;

[CreateAssetMenu(fileName = "Item_Consumable_food", menuName = "Scriptable Objects/Item_Consumable_food")]
public class Item_Consumable_food : Item_Consumable
{
    public float hungerChange = 10.0f;
    public float ThirstyChange = -5.0f;
    public override void Onuse(CharacterBase from, CharacterBase to)
    {
        base.Onuse(from, to);
    }
}
