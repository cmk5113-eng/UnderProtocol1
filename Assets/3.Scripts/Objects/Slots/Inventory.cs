using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UIElements.Experimental;
using Unity.VisualScripting;
using System.Linq;
using TMPro;

public class Inventory : MonoBehaviour
{
    public static SkillSlot cursorSlot = new SkillSlot();
    public int columns;
    public int rows;
   [SerializeField] TMP_InputField amountInput;


    SkillSlot[,] slots;
    public void Initialize()
    {
        slots = new SkillSlot[rows, columns];

        for (int row = 0; row < columns; row++)
        {
            for (int column = 0; column < rows; column++)
            {
                slots[row, column] = new SkillSlot();
            }

        }
    }
    readonly string[] skillList = { "LesserHealingPotion" };
    public void HealPotionPlus() // 나중에 안지우면 죽여버리겠다. 
    {
        int index = Random.Range(0, skillList.Length);
        Debug.Log(amountInput.text);
        int amount = int.Parse(amountInput.text);
        SkillList potion = DataManager.LoadDataFile<SkillList>(skillList[index]);
        AddSkill(potion,amount);
    }
    public void HealPotionMinus() // 나중에 안지우면 죽여버리겠다. 
    {

        int amount = int.Parse(amountInput.text);
        SkillList potion = DataManager.LoadDataFile<SkillList>("LesserHealingPotion");
        RemoveSkill(potion, amount);
    }
    public void Sort(System.Comparison<SkillSlot> Method)
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
             SkillSlot left = GetSlot(i,width);
             SkillSlot right = GetSlot(i+1,width);
             int comparisonResult = Method(left, right);
                 if (comparisonResult > 0)
                {
                currentFinder = i;
                left.ExchangeSkill(right);
                 }

                 
            }
            lastFinder = -1;
        }
    }

    public void SortByType()
    { 
    }
    int SkillTypeComparison(SkillSlot left, SkillSlot right)
    {
        int result;
        if (SkillExistComparison(left, right, out result)) return result;

        SkillList leftSkill = left.GetSkill();
        SkillList rightSkill = right.GetSkill();

        if(result!= 0) return result;
        result = left.GetStack()-right.GetStack();
        

        return result;


    }
    int? SkillExistComparison(SkillSlot left, SkillSlot right)
    {
        if (left is null)
        {
            if (right is null) return 0;
            else return -1;
        }
        if (right is null) return 1;

        SkillList leftSkill = left.GetSkill();
        SkillList rightSkill = right.GetSkill();

        return leftSkill.type - rightSkill.type;
 
    }
    bool SkillExistComparison(SkillSlot left, SkillSlot right, out int result)
    { int? calculated = SkillExistComparison(left, right);
        result = calculated ?? 0;
        return calculated.HasValue;
    }
    public void SortbyType() => Sort(SkillTypeComparison);
    public void AutoQuickInsert(Inventory other)
    { 
    }                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                        
    public void AutoQuickInsert(Inventory other,SkillList target)
    { }
    public bool InsertAll()
    { return default; }

    public bool InsertAll(SkillList target) { return default; }
    public void LockSlot(int wantRow, int wantColumn)
    { }
    public void UnlockSlot(int wantRow, int wantColumn)
    { }

    public int CountSkill(SkillList wantSkill)
    {
        if (!wantSkill) return 0;
        int result = 0;
        foreach (SkillSlot currentSlot in FindFirstSkill(wantSkill))
        {
            result += currentSlot.GetStack();

        }
        return result;
    }

    public void MergeSkill(SkillList wantSkill)
    {

        if (!wantSkill) return;
        if (wantSkill.maxStack <= 1) return;
        int totalCount = CountSkill(wantSkill, out List <SkillSlot> containSlots);
        if(containSlots is null||containSlots.Count<=1)return;
        for(int i =0; i< containSlots.Count;i++)
            {
            SkillSlot currentSlot = containSlots[i];
            if (currentSlot.GetIsMax()) continue;
        }
                
    }
    public int CountSkill(SkillList wantSkill, out List<SkillSlot>returnSlots)
    {
        returnSlots = new();
        if(!wantSkill) return 0;
        int result = 0;
        

        foreach (SkillSlot currentSlot in FindFirstSkill(wantSkill))
        {
            returnSlots.Add(currentSlot);
            result += currentSlot.GetStack();
        }
        return result;
    }
    public SkillSlot GetSlot(int index, int width) => slots[index / width, index % width];

    public SkillSlot GetSlot(int index)
    {
        if (slots is null || index < 0 || slots.Length == 0|| slots.Length <= index) return null;
        int width = slots.GetLength(1);
        return slots[index / width, (index % width)];
    }

   
    public IEnumerable<SkillSlot> FindFirstSkill(SkillList target)
    {
        foreach (SkillSlot currentSlot in GetAllSlot())
        { 
        if(currentSlot.GetSkill()==target)yield return currentSlot;
        }
    }
    public IEnumerable<SkillSlot> FindLastSkill(SkillList target)
    {
        foreach (SkillSlot currentSlot in GetAllSlotReverse())
        {
            if (currentSlot.GetSkill() == target) yield return currentSlot;
        }
    }

    public IEnumerable<SkillSlot> GetAllSlot()
    {
        SkillSlot[] result = new SkillSlot[slots.Length];

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
    public IEnumerable<SkillSlot> GetAllSlotReverse()
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
    public SkillSlot FindSkill(SkillList target)
    { return default; }

    public SkillSlot FindSkill(SkillType wantType)
    { return default; }
    public SkillSlot FindSkill(int wantRow, int wantColumn)
    {
        if (wantRow <0 || wantColumn <0) return null;
        if (wantRow >= slots.GetLength(0)) return null;
        if (wantColumn >=slots.GetLength(1)) return null;
        return slots[wantRow, wantColumn]; }

    public SkillSlot FindSkill(string containWord)
    { return default; }
    public IEnumerable<SkillSlot> FindFirstEmptySlot()
    {
        foreach (SkillSlot currentSlot in GetAllSlot())
        {
            if (currentSlot.GetIsEmpty()) yield return currentSlot;
        }

       
    }    
        
        
        
    public IEnumerable<SkillSlot> FindLastEmptySlot()
    {
        foreach (SkillSlot currentSlot in GetAllSlot())
        {
            if (currentSlot.GetIsEmpty()) yield return currentSlot;
        }
    }
    public int AddSkill(SkillList wantSkill, int amount = 1)
    {
    

        amount = AddSkillOnExistSlots(wantSkill, amount);
        if (amount <= 0) return 0;
        return AddSkillOnEmptySlots(wantSkill, amount);
    }
    public int AddSkills(SkillList wantSkill, int amount)
    {

        amount = AddSkillOnExistSlots(wantSkill, amount);
        if (amount <= 0) return 0;
        amount = AddSkillOnEmptySlots(wantSkill, amount);
        return amount;
    }

    public int AddSkillOnExistSlots(SkillList wantSkill, int amount)
    {
        foreach (SkillSlot currentSlot in FindFirstSkill(wantSkill))
        {
            if (amount <= 0) return 0;
            amount = currentSlot.AddSkill(wantSkill, amount);
            currentSlot.NoticeChanged();
        }
        return amount;
    }
    public int AddSkillOnEmptySlots(SkillList wantSkill, int amount)
    {
        foreach (SkillSlot currentSlot in FindFirstEmptySlot())
        {
            if (amount <= 0) return 0;
            amount = currentSlot.AddSkill(wantSkill, amount);
            currentSlot.NoticeChanged();
        }
        return amount;
    }
    public int AddSkillToLocation(SkillList wantSkill, int amount)
    { return default; }

    public int RemoveSkill(System.Predicate<SkillList> condition)
    {
        return default;
    }
    public int RemoveSkill(SkillList wantSkill)
    {
        int result = 0;
        foreach (SkillSlot currentSlot in FindLastSkill(wantSkill))
        {
            result += currentSlot.RemoveSkill(wantSkill);
            currentSlot.NoticeChanged();
        }

        return result;

    }
    public int RemoveSkill(SkillList wantSkill, int amount)
    {

        foreach (SkillSlot currentSlot in FindLastSkill(wantSkill))
        {
            if (amount <= 0) return 0;
            amount = currentSlot.RemoveSkill(wantSkill, amount);
            currentSlot.NoticeChanged();
        }
    
        return amount;

    }

    public void RemoveSkillOnExitSlot(SkillList wantSkill, int amout)
    { }
    public int RemoveSkillFromLocation(int row, int column,  int amount)
    { return default; }
    public void MoveSkill(int startRow, int startColumn, Inventory targetInventory, int targetRow, int targetColumn, int amount = -1)
    { 
    
    }

    public void ExchangeSkill(int startRow, int startColumn, int targetRow, int targetColumn)
    {
        ExchangeSkill(startRow, startColumn, this, targetRow, targetColumn);
    }

    public void ExchangeSkill(int startRow, int startColumn, SkillSlot targetslot)
    {
        if (targetslot == null) return;
        SkillSlot first = FindSkill(startRow, startColumn);
        if (first == null) return;
        first.ExchangeSkill(targetslot);
        first.NoticeChanged();
        targetslot.NoticeChanged();
    }


    public void ExchangeSkill(int startRow, int startColumn, Inventory targetInventory, int targetRow, int targetColumn, int amount = -1)
    {

        SkillSlot first = FindSkill(startRow, startColumn);
        if(first == null) return;
        if (!targetInventory) return;
        SkillSlot second =targetInventory.FindSkill(targetRow, targetColumn);
        if(second == null) return;

        first.ExchangeSkill(second);
        first.NoticeChanged();
        second.NoticeChanged();
    }
    public bool UseSkill(SkillList target) 
    { return default; }

    public bool UseSkill() 
    { return default; }

}
