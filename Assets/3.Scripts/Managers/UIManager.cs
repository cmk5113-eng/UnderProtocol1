using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UI;

public enum UIType
{
    None, Loading, Title, Movable,Menu, Info, Skill, Inven, Option, Action, Stage, Scenario, Save, Card,  GameQuit, Map, Hero, Mission, MiniMap,Item, HQ,
    World,Dialog, CharacterSelect,ScreenFilter,
    _Length
}
public enum ScreenChangeType
{ 
None, ScreenChanger, _Length
}

public delegate void PopUpEvent(string title, string context, string confirm);

public class UIManager : ManagerBase
{
    public static event PopUpEvent OnPopUp;

    Canvas _mainCanvas;
    public Canvas MainCanvas => _mainCanvas;
    UIBase _movableScreen;
    RectTransform switcherTransform;
    RectTransform changerTransform;
    RectTransform createdTransform;
    GraphicRaycaster _raycaster;
    public GraphicRaycaster raycaster => _raycaster;

    //어떤 창을 열어주세요!
    //         이 타입  어떤 오브젝트!
    Dictionary<UIType, UIBase> uiDictionary = new();
    Dictionary<ScreenChangeType, UI_ScreenChanger> screenChnagerDictionary = new();
    Rect _uiBoundary;
    public static Rect UIBoundary => GameManager.Instance?.UI?._uiBoundary ?? Rect.zero;

    UIType _currentScreenType = UIType.None;
    public static UIType CurrentScreen => GameManager.Instance?.UI?._currentScreenType ?? UIType.None;
    UI_ScreenChanger currentScreenChanger;
    float _uiScale = 1.0f;
    public static float UIScale => GameManager.Instance?.UI?._uiScale ?? 1.0f;
    public IEnumerator Initialize(GameManager newManager)
    {

        SetMainCanvas(GetComponentInChildren<Canvas>());


        if (_mainCanvas)
        {
            _raycaster = _mainCanvas.GetComponent<GraphicRaycaster>();
        }
        //GameObject.FindGameObjectWithTag("MainCanvas");
        SetUI(UIType.Loading, GetComponentInChildren<UI_LoadingScreen>());

        yield return null;
    }


    protected void SetMainCanvas(Canvas newCanvas)
    {
        _mainCanvas = newCanvas;
        if (_mainCanvas)
        {
            _raycaster = _mainCanvas.GetComponent<GraphicRaycaster>();
            if (MainCanvas.transform is RectTransform mainRectTransform)
            {
                _uiScale = MainCanvas.transform.lossyScale.x;
                _uiBoundary = mainRectTransform.rect;


            }

        }
        else
        {
            _raycaster = null;
        }
    }

    public RectTransform CreateFullScreen(string wantName)
    {
        GameObject instance = new GameObject(wantName);
        RectTransform result = instance.AddComponent<RectTransform>();
        result.SetParent(MainCanvas.transform);
        result.SetAsFirstSibling();
        result.anchorMin = Vector3.zero;
        result.anchorMax = Vector3.one;
        result.offsetMin = Vector3.zero;
        result.offsetMax = Vector3.zero;
        result.localScale = Vector3.one;
        return result;
    }
    protected override IEnumerator OnConnected(GameManager newManager)
    {
        createdTransform = CreateFullScreen("CreateUI");
        _movableScreen = CreateUI(UIType.Movable, "S_Movable", MainCanvas?.transform);
        switcherTransform = CreateFullScreen("ScreenSwitcher");
        _movableScreen.SetChild(ObjectManager.CreateObject("W_menu"));

        CreateUI(UIType.Title, "S_Title", switcherTransform);
        CreateUI(UIType.Option, "S_Option", switcherTransform);
        CreateUI(UIType.Stage, "S_Stage", switcherTransform);
        CreateUI(UIType.World, "S_World", switcherTransform);
        CreateUI(UIType.Scenario, "W_Scenario", switcherTransform);

        CreateUI(UIType.Menu, "W_Menu");    
        CreateUI(UIType.Save, "W_Save", switcherTransform);
        CreateUI(UIType.Info, "W_Info", switcherTransform);
        CreateUI(UIType.Map, "W_Map", switcherTransform);
        CreateUI(UIType.GameQuit, "W_GameQuit");
        CreateUI(UIType.Hero, "W_Hero");
        CreateUI(UIType.MiniMap, "W_MiniMap");
        CreateUI(UIType.Item, "W_Item");
        CreateUI(UIType.Mission, "W_Mission");
        CreateUI(UIType.HQ, "W_HQ", switcherTransform);
        CreateUI(UIType.Action, "W_Action", switcherTransform);
        CreateUI(UIType.Dialog, "W_Dialog", switcherTransform);
        CreateUI(UIType.CharacterSelect, "W_CharacterSelect", switcherTransform);
        CreateUI(UIType.ScreenFilter, "W_ScreenFilter", switcherTransform);
        CreateUI(UIType.Skill, "W_Skill", switcherTransform);
        CreateUI(UIType.Inven, "W_Inven", switcherTransform);


        foreach (Transform currentTransform in switcherTransform)
        {
            currentTransform.gameObject.SetActive(false);
        }
        foreach (Transform child in _movableScreen.transform)
        {
            child.gameObject.SetActive(false);
        }
        foreach (Transform _createdTransform in createdTransform)
            { _createdTransform.gameObject.SetActive(false);}
        changerTransform = CreateFullScreen("ScreenChanger");
        changerTransform.SetAsLastSibling();


        for (ScreenChangeType currentChanger = (ScreenChangeType)1;
            currentChanger < ScreenChangeType._Length;
            currentChanger++)
        {

            GameObject instance = ObjectManager.CreateObject(currentChanger.ToString(), changerTransform);
            if (instance?.TryGetComponent(out UI_ScreenChanger asChanger) ?? false)
            {
                screenChnagerDictionary.Add(currentChanger, asChanger);
            }

            instance?.SetActive(false);
        }



        yield return null;

    }

    protected override void OnDisconnected()
    {
        UnSetAllUI();
    }

    protected UIBase CreateUI(UIType wantType, string wantName, Transform parent)
    {
        GameObject instance = ObjectManager.CreateObject(wantName, parent);
        UIBase result = instance?.GetComponent<UIBase>();
        return SetUI(wantType, result);


    }
    protected UIBase CreateUI(UIType wantType, string wantName)
    {
        UIBase result = CreateUI(wantType, wantName, createdTransform ?? MainCanvas?.transform);
        if (result?.GetComponentInChildren<UI_DraggableWindows>())
        {
            _movableScreen?.SetChild(result.gameObject);
        }
        return result;
    }

    public static UIBase ClaimCreateUI(UIType wantType, string wantName) => GameManager.Instance?.UI?.CreateUI(wantType, wantName);
    protected void UnsetUI(UIType wantType)//담당 공무원의 부서 알고 있는 경우
    {
        if (uiDictionary.TryGetValue(wantType, out UIBase found))
        {
            UnsetUI(found);
            uiDictionary.Remove(wantType);
        }
    }
    protected void UnsetUI(UIBase wantUI)//담당 공무원의 이름을 알고 있는 경우
    {
        if (!wantUI) return;
        wantUI.Unregistration(this);
    }
    public static void ClaimUnsetUI(UIBase wantUI) => GameManager.Instance?.UI?.UnsetUI(wantUI);
    public static void ClaimUnsetUI(GameObject wantObject) => ClaimUnsetUI(wantObject?.GetComponent<UIBase>());

    protected void UnSetAllUI()
    {
        foreach (UIBase ui in uiDictionary.Values)
        {
            UnsetUI(ui);
        }
        uiDictionary.Clear();
    }

    public UIBase SetUI(UIBase wantUI)
    {
        wantUI?.Registration(this);
        return wantUI;

    }
    public static UIBase ClaimSetUI(UIBase wantUI) => GameManager.Instance?.UI?.SetUI(wantUI);
    public static UIBase ClaimSetUI(GameObject wantObject) => ClaimSetUI(wantObject?.GetComponent<UIBase>());
    public UIBase SetUI(UIType wantType, UIBase wantUI)
    {
        //Set UI를 하려고 하는데 문제가 무엇일까!
        //InventoryType, InventoryInstance
        if (wantUI == null) return null; // 승상께서 나를 더 필요로 하시지 않는구나

        //어? 뭐야? 이미 Inventory는 있는데? 너는 누구냐! => 서생원
        //일단 문전박대 => 프로그래밍에서는요? 똑같은 기능을 하는 친구면
        //음.. 너가 원본인 건 무슨 상관인데?
        //뒤이어서 들어온 친구는 치워버리겠다!
        if (uiDictionary.TryGetValue(wantType, out UIBase origin)) return origin;

        //두 가지의 시련을 모두 통과하다니. 너는 등록될 수 있는 자격을 갖추었다.
        uiDictionary.Add(wantType, wantUI);

        return SetUI(wantUI);
    }
    public static UIBase ClaimSetUI(UIType wantType, UIBase wantUI) => GameManager.Instance?.UI?.SetUI(wantType, wantUI);
    public UIBase GetUI(UIType wantType)
    {
        if (uiDictionary.TryGetValue(wantType, out UIBase result)) return result; //있으면 result반환
        else return null; //없으면 null
    }
    public static UIBase ClaimGetUI(UIType wantType) => GameManager.Instance?.UI?.GetUI(wantType);
    public UIBase OpenUI(UIType wantType)
    {
        //Result가 누군지 전혀 모름!  리스코프 치환 원칙
        //IOpenable이면 열게 해준다! 세부 요소는 모르겠는데, 상위 요소만으로 실행하기
        //이게 "열 수 있는"인 건 어떻게 확인할까요?
        //IOpenable인지 체크해보면 열 수 있는지 알 수 있습니다.
        //IOpenable로서 활동 할 수 있으면 IOpenable
        //result는 IOpenable인 opener인가?

        //아랫줄이랑 같은 의미예요!
        //IOpenable opener = result as IOpenable;
        //if(opener != null) opener.Open();
        UIBase result = GetUI(wantType);
        if (result is IOpenable asOpenable) asOpenable.Open();

        if (result) EventSystem.current.SetSelectedGameObject(result.gameObject);

        return result;
    }

    
    public static UIBase ClaimOpenUI(UIType wantType) => GameManager.Instance?.UI?.OpenUI(wantType);
    public UIBase CloseUI(UIType wantType)
    {
        UIBase result = GetUI(wantType);
        if (result is IOpenable asOpenable) asOpenable.Close();
        return result;
    }
    public static UIBase ClaimCloseUI(UIType wantType) => GameManager.Instance?.UI?.CloseUI(wantType);
    public UIBase ToggleUI(UIType wantType)
    {
        UIBase result = GetUI(wantType);
        if (result is IOpenable asOpenable) asOpenable.Toggle();
        return result;
    }
    public static UIBase ClaimToggleUI(UIType wantType) => GameManager.Instance?.UI?.ToggleUI(wantType);
    protected UIBase OpenScreen(UIType wantType)
    {
        CloseUI(CurrentScreen);
        _currentScreenType = wantType;
        return OpenUI(wantType);
    }
    public static UIBase ClaimOpenScreen(UIType wantType) => GameManager.Instance?.UI?.OpenScreen(wantType);

    protected void OpenScreen(UIType wantScreen, ScreenChangeType changeType)
    {
        ClaimScreenChangeEffect(changeType, () => OpenScreen(wantScreen));
    }
    public static void ClaimOpenScreen(UIType wantType, ScreenChangeType changeType) => GameManager.Instance?.UI?.OpenScreen(wantType,changeType);
    protected void ScreenChangeEffectStart(ScreenChangeType wantType,System.Action endFunction = null)
    {
        if (screenChnagerDictionary.TryGetValue(wantType, out UI_ScreenChanger result))
        {

            if (!result) return;
            result.gameObject.SetActive(true);
            result?.ChangeStart(endFunction);
            currentScreenChanger = result;
        }
    }
    public static void ClaimScreenChangeEffectStart(ScreenChangeType wantType, System.Action endFunction = null) => GameManager.Instance?.UI?.ScreenChangeEffectStart(wantType, endFunction);
    public static void ClaimScreenChangeEffect(ScreenChangeType wantType, System.Action endFunction = null) => GameManager.Instance?.UI?.ScreenChangeEffectStart(wantType, endFunction + ClaimScreenChangeEffectEnd);

    protected void ScreenChangeEffectEnd()
    {
    if(!currentScreenChanger) return;
      GameObject targetObject = currentScreenChanger.gameObject;
        currentScreenChanger.ChangeEnd(() => targetObject.SetActive(false));
        currentScreenChanger = null;
    }

                                                                                                                                    
    public static void ClaimScreenChangeEffectEnd() => GameManager.Instance?.UI?.ScreenChangeEffectEnd();
    
    public static void ClaimPopUp(string title, string context, string confirm)
    {
        OnPopUp?.Invoke(title, context, confirm);
    }
    public static void ClaimErrorMessage(string context)
    {
        OnPopUp?.Invoke("Error", context, "Confirm");
    }


}

