using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UIElements.Experimental;
using Unity.VisualScripting;
using System.Linq;

public class Inventory : MonoBehaviour
{

    public int columns;
    public int rows;
    ItemSlot[,] slots;
    public void Initialize()
    {
        slots = new ItemSlot[rows, columns];

        for (int row = 0; row < columns; row++)
        {
            for (int column = 0; column < rows; column++)
            {
                slots[row, column] = new ItemSlot();
            }

        }
    }
    public void HealPotionPlus() // 나중에 안지우면 죽여버리겠다. 
    {
        AddItem( DataManager.LoadDataFile<ItemContainer>("LesserHealingPotion"));
    }
    public void Sort(System.Comparison<ItemContainer>Method)
    { }
    public void AutoQuickInsert(Inventory other)
    { 
    }
    public void AutoQuickInsert(Inventory other,ItemContainer target)
    { }
    public bool InsertAll()
    { return default; }

    public bool InsertAll(ItemContainer target) { return default; }
    public void LockSlot(int wantRow, int wantColumn)
    { }
    public void UnlockSlot(int wantRow, int wantColumn)
    { }

    public int CountItem(ItemContainer wantItem, out List<ItemSlot>returnSlots)
    {
        returnSlots = default;
        return default;
    }
    public IEnumerable<ItemSlot> FindFirstItem(ItemContainer target)
    {
        foreach (ItemSlot currentSlot in GetAllSlot())
        { 
        if(currentSlot.GetItem()==target)yield return currentSlot;
        }
    }
    public IEnumerable<ItemSlot> FindLastItem(ItemContainer target)
    {
        foreach (ItemSlot currentSlot in GetAllSlotReverse())
        {
            if (currentSlot.GetItem() == target) yield return currentSlot;
        }
    }

    public IEnumerable<ItemSlot> GetAllSlot()
    {
        ItemSlot[] result = new ItemSlot[slots.Length];

        int height = slots.GetLength(0);
        int width = slots.GetLength(1);
        for (int row = 0; row < height; row++)
        {
            for (int column = 0; column < width; column++)
            {
                if (slots[row, column] is null) continue;
                yield return slots[row, column];
            }
        }
    }
    public IEnumerable<ItemSlot> GetAllSlotReverse()
    {

        int height = slots.GetLength(0);
        int width = slots.GetLength(1);
        for (int row = height -1; row >= 0;  row--)
        {
            for (int column = width - 1; column >= 0; column--)
            {
                yield return slots[row, column];
            }
        }
    }
    public ItemSlot FIndItem(ItemContainer target)
    { return default; }

    public ItemSlot FindItem(ItemType wantType)
    { return default; }
    public ItemSlot FindItem(int wantRow, int wantColumn)
    {
        if (wantRow <0 || wantColumn <0) return null;
        if (wantRow >= slots.GetLength(0)) return null;
        if (wantColumn >=slots.GetLength(1)) return null;
        return slots[wantRow, wantColumn]; }

    public ItemSlot FindItem(string containWord)
    { return default; }
    public IEnumerable<ItemSlot> FindFirstEmptySlot()
    {
        foreach (ItemSlot currentSlot in GetAllSlot())
        {
            if (currentSlot.GetIsEmpty()) yield return currentSlot;
        }

       
    }    
        
        
        
    public IEnumerable<ItemSlot> FindLastEmptySlot()
    {
        foreach (ItemSlot currentSlot in GetAllSlot())
        {
            if (currentSlot.GetIsEmpty()) yield return currentSlot;
        }
    }
    public int AddItem(ItemContainer wantItem, int amount = 1)
    {

       amount = AddItemOnExistSlots(wantItem, amount);
       if (amount <= 0) return 0;
       return AddItemOnEmptySlots(wantItem, amount);  
    }

    public int AddItemOnExistSlots(ItemContainer wantItem, int amount)
    {
        foreach (ItemSlot currentSlot in FindFirstItem(wantItem))
        {
            if (amount <= 0) return 0;
            amount = currentSlot.AddItem(wantItem, amount);
            currentSlot.NoticeChanged();
        }
        return amount;
    }
    public int AddItemOnEmptySlots(ItemContainer wantItem, int amount)
    {
        foreach (ItemSlot currentSlot in FindFirstEmptySlot())
        {
            if (amount <= 0) return 0;
            amount = currentSlot.AddItem(wantItem, amount);
            currentSlot.NoticeChanged();
        }
        return amount;
    }
    public int AddItemToLocation(ItemContainer wantItem, int amount)
    { return default; }

    public int RemoveItem(System.Predicate<ItemContainer>condition)
    {
        return default;
    }
    public int RemoveItem(ItemContainer wantItem, int amount)
    { return default;}
    public void RemoveItemOnExitSlot(ItemContainer wantItem, int amout)
    { }
    public int RemoveItemFromLocation(int row, int column,  int amount)
    { return default; }
    public void MoveItem(int startRow, int startColumn, Inventory targetInventory, int targetRow, int targetColumn, int amount = -1)
    { }
    public bool UseItem(ItemContainer target) 
    { return default; }

    public bool UseItem() 
    { return default; }

}
