using UnityEngine;

public enum ItemType
{ 
Equipment = 0, Consumable = 1, Material = 2, Miscellaneous = 3 , Quest =4 , Important =5 ,
Length
}

[CreateAssetMenu(fileName = "ItemContainer", menuName = "Item/ItemBase")]
public class ItemContainer : InfoContainer
{
    [Header("Item Base Info")]
    public int id; 
    [Space]
    [Header("Item Detail")]
    public ItemType type;
    public int maxStack;
    public float weight;


public virtual int CompareByType(ItemContainer other)
    {
        if (other == null) return 1;
        int result = type - other.type;
        if (result != 0) return result;
        return id - other.id;
    }


  
}