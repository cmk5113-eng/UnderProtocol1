
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
public delegate void MouseButtonEvent(bool value, Vector2 screenPosition, Vector3 worldPosition);
public delegate void MouseMoveEvent(Vector2 screenPosition, Vector3 worldPosition);
public delegate void ButtonEvent(bool value);
public delegate void VectorEvent(Vector2 value);
public delegate void AxisEvent(float value);
public delegate void MouseHoverEvent(GameObject newTarget, GameObject oldTarget);   

[RequireComponent(typeof(PlayerInput))]

public class InputManager : ManagerBase
{
    //delegate : 대리자 => 기술을 전수해놓고 시전하는 놈

    public static event MouseButtonEvent OnMouseLeftButton;
    public static event MouseButtonEvent OnMouseRightButton;
    public static event MouseButtonEvent OnMouseWheelButton;
    public static event MouseButtonEvent OnMouseWheelMove;
    public static event MouseMoveEvent OnMouseMove;
    public static event MouseHoverEvent OnMouseHover;
    public static event ButtonEvent OnCancel;
    //ESC키 UI관련 취소조작
    public static event ButtonEvent OnRevirt;
    //T키 게임안에서의 행동취소
    public static event ButtonEvent OnNextTurn;
    //스페이스바
    public static event ButtonEvent OnConfirm;
    //엔터
    public static event ButtonEvent OnReset;
    // 편성 초기화 : 백스페이스
    public static event ButtonEvent OnSweep;
    // 소탕
    public static event ButtonEvent Ontip;
    //Q : 추천행동
    public static event ButtonEvent OnShowStatus;




    public static event VectorEvent OnMove;

    public static event VectorEvent OnLook;

    PlayerInput targetInput;
    Dictionary<string, InputAction> actionDictionary = new();
    List<RaycastResult> cursorHitList = new();
    
    GameObject cursorHoverObject;
    Vector2 cursorScreenPosition;
    Vector3 cursorWorldPosition;

 



    protected override IEnumerator OnConnected(GameManager newManager)
    {

        targetInput = GetComponent<PlayerInput>();
        LoadAllActions();
        InitializeAllActions();
        GameManager.OnUpdateManager -= UpdateEvent;
        GameManager.OnUpdateManager += UpdateEvent;

        yield return null;
    }

    protected override void OnDisconnected()
    {
        GameManager.OnUpdateManager -= UpdateEvent;
    }

    public void UpdateEvent(float deltaTime)
    {
        RefreshGameobjectUndercursor(cursorScreenPosition);
    }



    void RefreshGameobjectUndercursor(Vector2 screenPosition)
    {
        cursorHitList.Clear();
        GameManager.Instance.Camera.GetRaycastResult(screenPosition, cursorHitList);
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(screenPosition);
        GameObject firstObject = null;
        if (cursorHitList.Count > 0 && cursorHitList[0].element !=null)
        {
            firstObject = cursorHitList[0].gameObject;
        }
        if (GameManager.is2D)
        {
            worldPosition.z = 0;
            float GetValue(RaycastResult target)
            {
                return target.sortingOrder + target.sortingLayer * 100000;

            }
            RaycastResult nearest = cursorHitList.GetMaximum<RaycastResult>(GetValue);
            firstObject = nearest.gameObject;

        }

        else
        {
            float GetDistance(RaycastResult target)
            {
                return target.distance;
            }

            RaycastResult nearest = cursorHitList.GetMinimum<RaycastResult>(GetDistance);
            firstObject = nearest.gameObject;
            worldPosition = nearest.worldPosition;
        }
        GameObject LastHoverObject = cursorHoverObject;
        cursorScreenPosition = screenPosition;
        cursorWorldPosition = worldPosition;
        cursorHoverObject = firstObject;
        
        if (LastHoverObject != firstObject)
        {
            OnMouseHover?.Invoke(firstObject, LastHoverObject);

        }



    }
   
    public GameObject GetGameObjectUnderCursor()
    {
        if (cursorHitList.Count == 0) return null;
        return cursorHitList[0].gameObject;
    }
    void LoadAllActions()
    {
        foreach (var currentAction in targetInput.actions)
        {
            actionDictionary.TryAdd(currentAction.name, currentAction);
        }
    }

    void InitializeAllActions()
    {
        if (actionDictionary == null || actionDictionary.Count == 0) return;
        InitializeAction("CursorPositionChanged", (context) => CursorPositionChanged(GetVector2Value(context)));
        InitializeAction("Move"
                                                    , (context) => OnMove?.Invoke(GetVector2Value(context))
                                                    , (context) => OnMove?.Invoke(Vector2.zero));


        InitializeAction("MouseLeftButton"          , (context) => OnMouseLeftButton?.Invoke(true, cursorScreenPosition, cursorWorldPosition) 
                                                    , (context) => OnMouseLeftButton?.Invoke(false, cursorScreenPosition, cursorWorldPosition));


        InitializeAction("MouseRightButton"         , (context) => OnMouseRightButton?.Invoke(true, cursorScreenPosition, cursorWorldPosition)
                                                    , (context) => OnMouseRightButton?.Invoke(false, cursorScreenPosition, cursorWorldPosition));

        InitializeAction("Cancel", (context) => OnCancel?.Invoke(true));
        InitializeAction("Space", (context) => OnNextTurn?.Invoke(true));

    }
    void InitializeAction(string actionName, Action<InputAction.CallbackContext> actionMethod,Action<InputAction.CallbackContext>cancelMethod = null)
    {
        if (actionDictionary == null) return;
        if (actionDictionary.TryGetValue(actionName, out InputAction currentInput))
        {
            if(actionMethod is not null) currentInput.performed += actionMethod;
            if(cancelMethod is not null) currentInput.canceled += cancelMethod;                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                              
        }
    }

    T GetInputValue<T>(InputAction.CallbackContext context) where T : struct
    { 
        if(context.valueType != typeof(T)) return default;
        return context.ReadValue<T>();
        
    }
    Vector2 GetVector2Value(InputAction.CallbackContext context) => GetInputValue<Vector2>(context);


    void CursorPositionChanged(Vector2 screenPosition)
    {


        RefreshGameobjectUndercursor(screenPosition);


        OnMouseMove?.Invoke(cursorScreenPosition, cursorWorldPosition);
    }




}
