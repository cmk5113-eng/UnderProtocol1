using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class UI_MovableScreen : UI_ScreenBase
{
    [SerializeField] List<UIBase> popupList = new();
    Vector3 popupPosition = Vector3.zero;
    Vector3 popupShift = new(20.0f, -20.0f);

    UI_DraggableWindows currentDragTarget = null;

    public override void Registration(UIManager manager)
    {
        base.Registration(manager);
        InputManager.OnCancel += (value) => UIManager.ClaimToggleUI(UIType.Menu);
        InputManager.OnMouseMove -= MouseMove;
        InputManager.OnMouseMove += MouseMove;
        InputManager.OnMouseLeftButton -= MouseLeft;
        InputManager.OnMouseLeftButton += MouseLeft;
        UIManager.OnPopUp -= PopUp;
        UIManager.OnPopUp += PopUp;
    }


    public override void Unregistration(UIManager manager)
    {
        base.Unregistration(manager);
        InputManager.OnMouseMove -= MouseMove;
        InputManager.OnMouseLeftButton -= MouseLeft;
        UIManager.OnPopUp -= PopUp;

    }

    private void MouseLeft(bool value, Vector2 screenPosition, Vector3 worldPosition)
    {
       if (!value) currentDragTarget = null;
    }

    protected override GameObject OnSetChild(GameObject newChild)
    {
        UIManager.ClaimSetUI(newChild);


        if (newChild)
        {
            UI_DraggableWindows asDraggable = newChild.GetComponentInChildren<UI_DraggableWindows>();
            if (asDraggable)
            {
                asDraggable.OnDragStart -= SetDragTarget;
                asDraggable.OnDragStart += SetDragTarget;

            }
        }
        return base.OnSetChild(newChild);

    }
    
    protected override void OnUnsetChild(GameObject oldChild)
    {

        UIManager.ClaimUnsetUI(oldChild);
        if (oldChild)
        {
            UI_DraggableWindows asDraggable = oldChild.GetComponentInChildren<UI_DraggableWindows>();
            if (asDraggable)
            {
                asDraggable.OnDragStart -= SetDragTarget;
                asDraggable.OnDragStart += SetDragTarget;

            }
        }
        base.OnUnsetChild(oldChild);

    }

    void SetDragTarget(UI_DraggableWindows dragTarget, Vector2 startPosition)
    {
        currentDragTarget = dragTarget;
        if (currentDragTarget)
        {
            currentDragTarget.SetMouseStartPosition(startPosition);
        }


    }
    private void MouseMove(Vector2 screenPosition, Vector3 worldPosition)
    {
        if (currentDragTarget)
        {
            currentDragTarget.SetMouseCurrentPosition(screenPosition);
        }
    }
    private void PopUp(string title, string context, string confirm)
    {


        GameObject newChild = SetChild(ObjectManager.CreateObject("W_PopUp"));
        if (newChild)
        {
            newChild.transform.localPosition = GetNextPopupPosition();
            if (newChild.TryGetComponent(out UIBase NewUI))
            {
                if (!popupList.Contains(NewUI)) popupList.Add(NewUI);
            }
            if (newChild.TryGetComponent(out ISystemMessagePossible target))
            {
                target.SetSystemMessage(title, context, confirm);
            }
            if (newChild.TryGetComponent(out IConfirmable confirmTarget))
            {
                confirmTarget.SetConfirmAction(() =>
                {
                    if (NewUI) popupList.Remove(NewUI);
                    UnsetChild(newChild);
                    ObjectManager.DestroyObject(newChild);
                });
            }

            Vector3 bestScore = Vector3.zero;
            foreach (UIBase currentPopup in popupList)
            {
                Vector3 currentScore = currentPopup.transform.localPosition;
                if (bestScore.x < currentScore.x) bestScore.x = currentScore.x;
                if (bestScore.y > currentScore.y) bestScore.y = currentScore.y;
            }
            newChild.transform.localPosition = bestScore;
            popupPosition += popupShift;

        }
    }
    public Vector3 GetNextPopupPosition()
    {
        Vector3 bestScore = Vector3.zero;

        if (popupList.Count == 0) return bestScore;
        foreach (UIBase currentPopup in popupList)
        {
            Vector3 currentScore = currentPopup.transform.localPosition;
            if (bestScore.x < currentScore.x) bestScore.x = currentScore.x;
            if (bestScore.y > currentScore.y) bestScore.y = currentScore.y;
        }

        return bestScore + popupShift;
    }


    public void SetMouseStartPosition(Vector2 screenPosition)
    { 
    
    }

    public void SetMouseCurrentPosition(Vector2 screenPosition)
    {
    
    }

}

