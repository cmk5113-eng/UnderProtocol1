using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UIElements.Experimental;
using Unity.VisualScripting;
using System.Linq;
using TMPro;

public class Inventory : MonoBehaviour
{
    public static ItemSlot cursorSlot = new ItemSlot();
    public int columns;
    public int rows;
   [SerializeField] TMP_InputField amountInput;


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
    readonly string[] itemList = { "LesserHealingPotion" };
    public void HealPotionPlus() // 나중에 안지우면 죽여버리겠다. 
    {
        int index = Random.Range(0, itemList.Length);
        Debug.Log(amountInput.text);
        int amount = int.Parse(amountInput.text);
        ItemContainer potion = DataManager.LoadDataFile<ItemContainer>(itemList[index]);
        AddItem(potion,amount);
    }
    public void HealPotionMinus() // 나중에 안지우면 죽여버리겠다. 
    {

        int amount = int.Parse(amountInput.text);
        ItemContainer potion = DataManager.LoadDataFile<ItemContainer>("LesserHealingPotion");
        RemoveItem(potion, amount);
    }
    public void Sort(System.Comparison<ItemSlot>Method)
    { 
        int totalLength = slots.Length;
        if (slots is null || totalLength <= 1) return;
        int width = slots.GetLength(1);


        int lastFinder = totalLength - 1;
       
        while(lastFinder>0)
        {
            int currentFinder = -1;
            for(int i = 0; i < lastFinder; i++)
            { 
             ItemSlot left = GetSlot(i,width);
             ItemSlot right = GetSlot(i+1,width);
             int comparisonResult = Method(left, right);
                 if (comparisonResult > 0)
                {
                currentFinder = i;
                left.ExchangeItem(right);
                 }

                 
            }
            lastFinder = -1;
        }
    }

    public void SortByType()
    { 
    }
    int ItemTypeComparison(ItemSlot left, ItemSlot right)
    {
        int result;
        if (ItemExistComparison(left, right, out result)) return result;

        ItemContainer leftItem = left.GetItem();
        ItemContainer rightItem = right.GetItem();


        result = leftItem.CompareByType(rightItem);
        if(result!= 0) return result;
        result = left.GetStack()-right.GetStack();
        

        return result;


    }
    int? ItemExistComparison(ItemSlot left, ItemSlot right)
    {
        if (left is null)
        {
            if (right is null) return 0;
            else return -1;
        }
        if (right is null) return 1;

        ItemContainer leftItem = left.GetItem();
        ItemContainer rightItem = right.GetItem();

        return leftItem.type - rightItem.type;
        if (!leftItem)
        {
            if (rightItem is null) return 0;
            else return -1;
        }
        if (right is null) return 1;
        return null;
    }
    bool ItemExistComparison(ItemSlot left, ItemSlot right, out int result)
    { int? calculated = ItemExistComparison(left, right);
        result = calculated ?? 0;
        return calculated.HasValue;
    }
    public void SortbyType() => Sort(ItemTypeComparison);
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

    public int CountItem(ItemContainer wantItem)
    {
        if (!wantItem) return 0;
        int result = 0;
        foreach (ItemSlot currentSlot in FindFirstItem(wantItem))
        {
            result += currentSlot.GetStack();

        }
        return result;
    }

    public void MergeItem(ItemContainer wantItem)
    {

        if (!wantItem) return;
        if (wantItem.maxStack <= 1) return;
        int totalCount = CountItem(wantItem, out List < ItemSlot > containSlots);
        if(containSlots is null||containSlots.Count<=1)return;
        for(int i =0; i< containSlots.Count;i++)
            {
            ItemSlot currentSlot = containSlots[i];
            if (currentSlot.GetIsMax()) continue;
        }
                
    }
    public int CountItem(ItemContainer wantItem, out List<ItemSlot>returnSlots)
    {
        returnSlots = new();
        if(!wantItem) return 0;
        int result = 0;
        

        foreach (ItemSlot currentSlot in FindFirstItem(wantItem))
        {
            returnSlots.Add(currentSlot);
            result += currentSlot.GetStack();
        }
        return result;
    }
    public ItemSlot GetSlot(int index, int width) => slots[index / width, index % width];

    public ItemSlot GetSlot(int index)
    {
        if (slots is null || index < 0 || slots.Length == 0|| slots.Length <= index) return null;
        int width = slots.GetLength(1);
        return slots[index / width, (index % width)];
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
    public ItemSlot FindItem(ItemContainer target)
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
    public int AddItems(ItemContainer wantItem, int amount)
    {

        amount = AddItemOnExistSlots(wantItem, amount);
        if (amount <= 0) return 0;
        amount = AddItemOnEmptySlots(wantItem, amount);
        return amount;
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
    public int RemoveItem(ItemContainer wantItem)
    {
        int result = 0;
        foreach (ItemSlot currentSlot in FindLastItem(wantItem))
        {
            result += currentSlot.RemoveItem(wantItem);
            currentSlot.NoticeChanged();
        }

        return result;

    }
    public int RemoveItem(ItemContainer wantItem, int amount)
    {

        foreach (ItemSlot currentSlot in FindLastItem(wantItem))
        {
            if (amount <= 0) return 0;
            amount = currentSlot.RemoveItem(wantItem, amount);
            currentSlot.NoticeChanged();
        }
    
        return amount;

    }

    public void RemoveItemOnExitSlot(ItemContainer wantItem, int amout)
    { }
    public int RemoveItemFromLocation(int row, int column,  int amount)
    { return default; }
    public void MoveItem(int startRow, int startColumn, Inventory targetInventory, int targetRow, int targetColumn, int amount = -1)
    { 
    
    }

    public void ExchangeItem(int startRow, int startColumn, int targetRow, int targetColumn)
    {
        ExchangeItem(startRow, startColumn, this, targetRow, targetColumn);
    }

    public void ExchangeItem(int startRow, int startColumn, ItemSlot targetslot)
    {
        if (targetslot == null) return;
        ItemSlot first = FindItem(startRow, startColumn);
        if (first == null) return;
        first.ExchangeItem(targetslot);
        first.NoticeChanged();
        targetslot.NoticeChanged();
    }


    public void ExchangeItem(int startRow, int startColumn, Inventory targetInventory, int targetRow, int targetColumn, int amount = -1)
    {

        ItemSlot first = FindItem(startRow, startColumn);
        if(first == null) return;
        if (!targetInventory) return;
        ItemSlot second =targetInventory.FindItem(targetRow, targetColumn);
        if(second == null) return;

        first.ExchangeItem(second);
        first.NoticeChanged();
        second.NoticeChanged();
    }
    public bool UseItem(ItemContainer target) 
    { return default; }

    public bool UseItem() 
    { return default; }

}
