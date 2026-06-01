using System.Collections.Specialized;
using Unity.VisualScripting;
using UnityEngine;

public delegate void ItemSlotChangeEvent(ItemSlot changedSlot);
public class ItemSlot
{
    [SerializeField] ItemContainer item;
    [SerializeField] int currentStack;
    public event ItemSlotChangeEvent OnItemSlotChanged;

    public void NoticeChanged() => OnItemSlotChanged?.Invoke(this);
    public virtual bool Containable(ItemContainer wantItem)
    {
        if (wantItem is null) return false;

        if (item && item != wantItem) return false;
        if (GetIsMax()) return false;
        return true;
    }

    public ItemContainer GetItem() => item;
    public int GetStack() => currentStack;
    public bool GetIsMax() => item ? currentStack >= item.maxStack : false;
    public bool GetIsEmpty() => item is null || currentStack <= 0;
    public int AddItem(ItemContainer wantItem, int amount)
    {
        if (amount <= 0) return 0;
        if (!Containable(wantItem)) return amount;
        item = wantItem;
        
        int stackable = Mathf.Min(item.maxStack - currentStack, amount);
        currentStack += stackable;
        return amount- stackable;
    }
    public int Clear()
    {
        item = null; //일단 아이템을 비움!
        int removed = currentStack; //비우기 전에 몇개 있었는지 저장하고
        currentStack = 0; //스택을 비움!
        return removed; //얼마나 비웠는지 리턴할 수 있다!
    }

    public int RemoveItem(ItemContainer wantItem)
    {        //제거하지 않아도 되는 순간?
        //아이템 없잖아!
        if (!wantItem) return 0;
        //나.. 빈털터리야..
        if (GetIsEmpty()) return 0;
        //그건 내가 가지고 있지 않아!
        if (item != wantItem) return 0;
        //슬롯 싹 비우고 개수만 보내줌!
        return Clear();


    }
    public int RemoveItem(ItemContainer wantItem,int amount)
    {
        //제거하지 않아도 되는 순간?
        //지울게 없는데 여기는 왜 온거니?
        if (amount <= 0) return 0;
        //아이템 없잖아!
        if (!wantItem) return 0;
        //나.. 빈털터리야..
        if (GetIsEmpty()) return amount;
        //그건 내가 가지고 있지 않아!
        if (item != wantItem) return amount;
        //가진것보다 많이 요구하는 경우     요구량 - 지운개수
        if (amount >= currentStack) return amount - Clear();
        //현재 개수에서 원하는 만큼만 빼준다!
        currentStack -= amount;
        //이제 더 지우지 않아도 돼. 내가 다 처리했어.
        return 0;
    }
       
}