using UnityEngine;

public enum ItemType
{ 
Equipment, Consumable, Material, Miscellaneous, Quest, Important,
Length
}

[CreateAssetMenu(fileName = "ItemContainer", menuName = "Item/ItemBase")]
public class ItemContainer : InfoContainer
{
    public ItemType type;
    public int maxStack;
    public float weight;

}
