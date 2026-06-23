using System.Collections.Specialized;
using Unity.VectorGraphics;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public delegate void SkillSlotChangeEvent(SkillSlot changedSlot);
public class SkillSlot : MonoBehaviour
{
    [SerializeField] SkillList skill;
    [SerializeField] int currentStack;
    public event SkillSlotChangeEvent OnSkillSlotChanged;



    public void NoticeChanged() => OnSkillSlotChanged?.Invoke(this);
    public virtual bool Containable(SkillList wantSkill)
    {
        if (wantSkill is null) return false;

        if (skill && skill != wantSkill) return false;
        if (GetIsMax()) return false;
        return true;
    }
    public SkillList GetSkill() => skill;

    public int GetStackable(SkillList wantSkill) => Containable(wantSkill) ? wantSkill.maxStack - currentStack : 0;
    public int Getstackable() => GetStackable(skill);
    public int GetStack() => currentStack;
    public bool GetIsMax() => skill ? currentStack >= skill.maxStack : false;
    public bool GetIsEmpty() => skill is null || currentStack <= 0;



    public int AddSkill(SkillList wantSkill, int amount = 1)
    {

        if (amount <= 0) return 0;
        if (!Containable(wantSkill)) return amount;
        skill = wantSkill;

        int stackable = Mathf.Min(skill.maxStack - currentStack, amount);
        currentStack += stackable;
        return amount - stackable;
    }
    public int Clear()
    {
        skill = null; //일단 아이템을 비움!
        int removed = currentStack; //비우기 전에 몇개 있었는지 저장하고
        currentStack = 0; //스택을 비움!
        return removed; //얼마나 비웠는지 리턴할 수 있다!
    }

    public int RemoveSkill(SkillList wantSkill)
    {        //제거하지 않아도 되는 순간?
        //아이템 없잖아!
        if (!wantSkill) return 0;
        //나.. 빈털터리야..
        if (GetIsEmpty()) return 0;
        //그건 내가 가지고 있지 않아!
        if (skill != wantSkill) return 0;
        //슬롯 싹 비우고 개수만 보내줌!
        return Clear();


    }
    public int RemoveSkill(SkillList wantSkill, int amount)
    {
        //제거하지 않아도 되는 순간?
        //지울게 없는데 여기는 왜 온거니?
        if (amount <= 0) return 0;
        //아이템 없잖아!
        if (!wantSkill) return 0;
        //나.. 빈털터리야..
        if (GetIsEmpty()) return amount;
        //그건 내가 가지고 있지 않아!
        if (skill != wantSkill) return amount;
        //가진것보다 많이 요구하는 경우     요구량 - 지운개수
        if (amount >= currentStack) return amount - Clear();
        //현재 개수에서 원하는 만큼만 빼준다!
        currentStack -= amount;
        //이제 더 지우지 않아도 돼. 내가 다 처리했어.
        return 0;
    }
    public void ExchangeSkill(SkillSlot wantSlot)
    {
        if (wantSlot == null) return;
        SkillList wasSkill = skill;
        int wasStack = currentStack;

        skill = wantSlot.skill;
        currentStack = wantSlot.currentStack;

        wantSlot.skill = wasSkill;
        wantSlot.currentStack = wasStack;


    }
    public int GiveSkill(SkillSlot wantSlot) => GiveSkill(wantSlot,currentStack);
    public int GiveSkill(SkillSlot wantSlot, int amount)
    {
        if (wantSlot == null) return amount;
        if (!skill) return amount;
        if (currentStack <= 0 || amount <= 0) return amount;
        SkillList targetSkill = skill;
        amount = Mathf.Min(amount,wantSlot.GetStackable(targetSkill));

        amount -= RemoveSkill(targetSkill, amount);
        wantSlot.AddSkill(targetSkill, amount);
        return amount;
    }
    public void LeftClick(SkillSlot wantSlot)
    {
        if (wantSlot is null) return;
        if (InputManager.IsShift)
        {
            if (wantSlot.GetIsEmpty())
            {
                if (GetIsEmpty()) return;
                else if (wantSlot.Containable(skill))
                {
                    GiveSkill(wantSlot, Mathf.CeilToInt(currentStack * 0.5f));
                }
            }

            else if (Containable(wantSlot.skill))
            {
               wantSlot.GiveSkill(this, Mathf.CeilToInt(wantSlot.currentStack * 0.5f));



            }



        }
        if (InputManager.IsControl)
        {
            if (wantSlot.GetIsEmpty())
            {
                if (GetIsEmpty()) return;
                else if (wantSlot.Containable(skill))
                {

                }
            }

            if (Containable(wantSlot.skill))
            {


                SkillList targetSkill = wantSlot.skill;

                wantSlot.RemoveSkill(targetSkill, 1);

                AddSkill(targetSkill, 1);




            }



        }
        //
        else
        {
            if (wantSlot.Containable(skill))
            {
                GiveSkill(wantSlot);
            }

            else
            {
                ExchangeSkill(wantSlot);
            }
            NoticeChanged();
            wantSlot.NoticeChanged();
        }

    }
}