
using System.Collections.Specialized;
using Unity.VisualScripting;
using UnityEngine;

public class ItemCursorSlot : UI_ItemSlotInfo
{
    public override void Registration(UIManager manager)
    {
        base.Registration(manager);
        ConnectSlot(Inventory.cursorSlot);

        InputManager.OnMouseMove -= MoveToMouse;
        InputManager.OnMouseMove += MoveToMouse;
        InputManager.OnMouseLeftButton -= LeftButton;
        InputManager.OnMouseLeftButton += LeftButton;

    }
    public override void Unregistration(UIManager manager)
    {
        base.Unregistration(manager);
        DisconnectSlot();
        InputManager.OnMouseMove -= MoveToMouse;
        InputManager.OnMouseLeftButton -= LeftButton;

    }

    void LeftButton(bool value, Vector2 screenPosition, Vector3 worldPosition)
    { 
        if(!value) return;
        GameObject currenthover = InputManager.CursorHoverObject;
        if (currenthover is null) return;
        if (currenthover.TryGetComponent(out UI_ItemSlotInfo currentSlotInfo))
        { 
        ConnectedSlot.LeftClick(currentSlotInfo.ConnectedSlot);
        ConnectedSlot?.NoticeChanged();
            currentSlotInfo.ConnectedSlot?.NoticeChanged();
        }

    }
    void MoveToMouse(Vector2 screenPosition, Vector3 worldPosition)
    {   
        transform.position = screenPosition;
    }
}