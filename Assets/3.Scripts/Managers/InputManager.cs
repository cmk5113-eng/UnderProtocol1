
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
    //delegate : �븮�� => ����� �����س��� �����ϴ� ��

    public static event MouseButtonEvent OnMouseLeftButton;
    public static event MouseButtonEvent OnMouseRightButton;
    public static event MouseMoveEvent OnMouseMove;
    public static event MouseHoverEvent OnMouseHover;
    public static event ButtonEvent OnCancel;
    //ESCŰ UI���� �������
    //TŰ ���Ӿȿ����� �ൿ���
    public static event ButtonEvent OnNextTurn;
    //�����̽���
    //����
    // ��� �ʱ�ȭ : �齺���̽�
    
    public static event ButtonEvent OnShift;
    public static bool IsShift { get; private set; } = false;
    void ShiftInput(bool value)
    {
        IsShift = value;
        OnShift?.Invoke(value);
    }

    public static event ButtonEvent OnControl;

    public static bool IsControl { get; private set; } = false;
    void CtrlInput(bool value)
    {
        IsControl = value;
        OnControl?.Invoke(value);
    }


    public static event VectorEvent OnMove;

    PlayerInput targetInput;
    Dictionary<string, InputAction> actionDictionary = new();
    List<RaycastResult> cursorHitList = new();
    





    
    static Vector2 _cursorScreenPosition;
    public static Vector2 CursorScreenPosition => _cursorScreenPosition;

    static Vector3 _cursorWorldPosition;
    public static Vector3 CursorWorldPosition => _cursorWorldPosition;

    //static ISelectable _cursorHoverSelectable;
    //public static ISelectable CursorHoverSelectable => _cursorHoverSelectable;

    static GameObject _cursorHoverObject;
    public static GameObject CursorHoverObject => _cursorHoverObject;


    static bool _isCursorHoverOnUI;
    public static bool IsCursorHoverOnUI => _isCursorHoverOnUI;



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
        RefreshGameobjectUndercursor(_cursorScreenPosition);
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
        GameObject LastHoverObject = _cursorHoverObject;
        _cursorScreenPosition = screenPosition;
        _cursorWorldPosition = worldPosition;
        _cursorHoverObject = firstObject;
        
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


        InitializeAction("MouseLeftButton"          , (context) => OnMouseLeftButton?.Invoke(true, _cursorScreenPosition, _cursorWorldPosition) 
                                                    , (context) => OnMouseLeftButton?.Invoke(false, _cursorScreenPosition, _cursorWorldPosition));


        InitializeAction("MouseRightButton"         , (context) => OnMouseRightButton?.Invoke(true, _cursorScreenPosition, _cursorWorldPosition)
                                                    , (context) => OnMouseRightButton?.Invoke(false, _cursorScreenPosition, _cursorWorldPosition));

        InitializeAction("Cancel"                   , (context) => OnCancel?.Invoke(true));
        InitializeAction("Space"                    , (context) => OnNextTurn?.Invoke(true));
        InitializeAction("LShift"                   , (context) => ShiftInput(true)
                                                    , (context) => ShiftInput(false));
        InitializeAction("Ctrl"                     , (context) => CtrlInput(true)
                                                    , (context) => CtrlInput(false));
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


        OnMouseMove?.Invoke(_cursorScreenPosition, _cursorWorldPosition);
    }




}
