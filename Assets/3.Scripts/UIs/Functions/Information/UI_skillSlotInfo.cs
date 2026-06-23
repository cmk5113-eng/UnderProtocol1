using System;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class UI_SkillSlotInfo : UIBase
{
    [SerializeField] Image iconImage;
    [SerializeField] TextMeshProUGUI amountText;

    [SerializeField] Sprite noneIcon;

    protected SkillSlot connectedSlot;

    public SkillSlot ConnectedSlot => connectedSlot;

    public void ConnectSlot(SkillSlot targetSlot)
    {
        DisconnectSlot(); //기존 연결은 끊고!
        if (targetSlot is null) return;
        connectedSlot = targetSlot;
        //아이템 슬롯이 바뀌면              비주얼 업데이트를 할래!
        connectedSlot.OnSkillSlotChanged -= VisualUpdate;
        connectedSlot.OnSkillSlotChanged += VisualUpdate;
        VisualUpdate(connectedSlot);
    }
    public void SetupSlot(SkillContainer skillData, int stackCount)
    {
        if (skillData != null)
        {
            // 예시: skillData 내부에 sprite나 skillName이 있다고 가정
            // iconImage.sprite = skillData.skillIcon; 
            if (amountText != null)
                amountText.text = stackCount > 0 ? stackCount.ToString() : "";
        }
        else
        {
            // 스킬 데이터가 없는 빈 슬롯 처리
            if (iconImage != null) iconImage.sprite = noneIcon;
            if (amountText != null) amountText.text = "";
        }
    }
    public void DisconnectSlot()
    {
        if (connectedSlot is null) return; //연결된게 없는데? 안함!
        connectedSlot.OnSkillSlotChanged -= VisualUpdate; //이제 너랑 안놀아!
        connectedSlot = null; //연결된 것이 없다고 표시!
    }

    public void SetSkill(SkillList skill)
    {

    }



    protected virtual void VisualUpdate(SkillSlot targetSlot)
    {
        if (targetSlot is null) return;
        SkillList targetSkill = targetSlot.GetSkill();
        if (iconImage)
        {
            if (targetSkill)
            {
                //            targetItem의 아이콘 없으면 noneIcon
                iconImage.sprite = targetSkill.icon ?? noneIcon;
                iconImage.enabled = true; //아이템이 있어야 이미지가 켜짐!
            }
            else
            {
                iconImage.enabled = false; //아이템이 없으면 이미지를 끄기!
            }
        }
        if (amountText)
        {
            int targetStack = targetSlot.GetStack();
            if (!targetSkill || targetSkill.maxStack <= 1 || targetStack <= 0)
            {
                amountText.SetText("");
            }
            else
            {
                //bool isMax = targetSlot.GetMax(); //너, 다 찬거니?
                //if(isMax) amountText.color = Color.yellow;
                //else	    amountText.color = Color.white;
                amountText.SetText($"{targetStack}");
            }
        }
    }
}