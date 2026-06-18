using System;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class UI_ItemSlotInfo : UIBase
{
    [SerializeField] Image iconImage;
    [SerializeField] TextMeshProUGUI amountText;

    [SerializeField] Sprite noneIcon;

    protected ItemSlot connectedSlot;

    public ItemSlot ConnectedSlot => connectedSlot;

    public void ConnectSlot(ItemSlot targetSlot)
    {
        DisconnectSlot(); //기존 연결은 끊고!
        if (targetSlot is null) return;
        connectedSlot = targetSlot;
        //아이템 슬롯이 바뀌면              비주얼 업데이트를 할래!
        connectedSlot.OnItemSlotChanged -= VisualUpdate;
        connectedSlot.OnItemSlotChanged += VisualUpdate;
        VisualUpdate(connectedSlot);
    }

    public void DisconnectSlot()
    {
        if (connectedSlot is null) return; //연결된게 없는데? 안함!
        connectedSlot.OnItemSlotChanged -= VisualUpdate; //이제 너랑 안놀아!
        connectedSlot = null; //연결된 것이 없다고 표시!
    }

    public void SetSkill(SkillContainer skill)
    {

    }



    protected virtual void VisualUpdate(ItemSlot targetSlot)
    {
        if (targetSlot is null) return;
        ItemContainer targetItem = targetSlot.GetItem();
        if (iconImage)
        {
            if (targetItem)
            {
                //            targetItem의 아이콘 없으면 noneIcon
                iconImage.sprite = targetItem.icon ?? noneIcon;
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
            if (!targetItem || targetItem.maxStack <= 1 || targetStack <= 0)
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