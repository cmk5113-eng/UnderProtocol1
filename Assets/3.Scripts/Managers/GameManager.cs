using System.Collections;
using UnityEngine;

public delegate void InitializeEvent();
public delegate void UpdateEvent(float deltaTime);
public delegate void DestroyEvent();

public class GameManager : MonoBehaviour
{
    static GameManager _instance;
    public static GameManager Instance => _instance;

    UIManager _ui;
    public UIManager UI => _ui;
    
    CharacterManager _character;
    public CharacterManager Character => _character;
    
    DataManager _data;
    public DataManager Data => _data;

    ObjectManager _objectM;
    public ObjectManager ObjectM => _objectM;
    SaveManager _save;
    public SaveManager Save => _save;

    SettingManager _setting;
    public SettingManager Setting => _setting;

    LanguageManager _language;
    public LanguageManager Language => _language;

    AudioManager _audio;
    public AudioManager Audio => _audio;

    CameraManager _camera;
    public CameraManager Camera => _camera;

    InputManager _input;
    public InputManager Input => _input;


    PlacementManager _placement;
    public PlacementManager Placement => _placement;


    IEnumerator initializing;
    public static event InitializeEvent OnInitializeManager;
    public static event InitializeEvent OnInitializeController;
    public static event InitializeEvent OnInitializeCharacter;
    public static event InitializeEvent OnInitializeObject;

    public static event UpdateEvent OnUpdateManager;
    public static event UpdateEvent OnUpdateController;
    public static event UpdateEvent OnUpdateCharacter;
    public static event UpdateEvent OnUpdateObject;

    public static event UpdateEvent OnPhysicsCharacter;
    public static event UpdateEvent OnPhysicsObject;

    public static event DestroyEvent OnDestroyManager;
    public static event DestroyEvent OnDestroyController;
    public static event DestroyEvent OnDestroyCharacter;
    public static event DestroyEvent OnDestroyObject;

    bool isLoading = true;
    bool isPlaying = true;

    void Awake()
    {
        if (Instance == null)
        {

            _instance = this;

        }
        else
        {
            Destroy(this);
            return;
        }


        initializing = InitializeManagers();
        StartCoroutine(initializing);
        UIManager.ClaimCloseUI(UIType.Loading);
    }

    private void OnDestroy()
    {
        if (initializing != null) StopCoroutine(initializing);
        DeleteManagers();
    }



    IEnumerator InitializeManagers()
    {
        int totalLoadCount = 0;
        totalLoadCount += CreateManager(ref _ui).LoadCount;
        totalLoadCount += CreateManager(ref _data).LoadCount;
        totalLoadCount += CreateManager(ref _objectM).LoadCount;
        totalLoadCount += CreateManager(ref _save).LoadCount;
        totalLoadCount += CreateManager(ref _setting).LoadCount;
        totalLoadCount += CreateManager(ref _language).LoadCount;
        totalLoadCount += CreateManager(ref _audio).LoadCount;
        totalLoadCount += CreateManager(ref _camera).LoadCount;
        totalLoadCount += CreateManager(ref _input).LoadCount;
        totalLoadCount += CreateManager(ref _placement).LoadCount;
        totalLoadCount += CreateManager(ref _character).LoadCount;



        yield return UI.Initialize(this);
        UIBase loadingUI = UIManager.ClaimOpenScreen(UIType.Loading);
        IProgress<int> loadingProgress = loadingUI as IProgress<int>;
        loadingProgress?.Set(0, totalLoadCount);
        yield return  Data.Connect(this);
        loadingProgress?.AddCurrent(1);
        yield return ObjectM.Connect(this);
        loadingProgress?.AddCurrent(1);
        yield return UI.Connect(this);
        loadingProgress?.AddCurrent(1);
        yield return Save.Connect(this);
        loadingProgress?.AddCurrent(1);
        yield return  Setting.Connect(this);
        loadingProgress?.AddCurrent(1);
        yield return Language.Connect(this);
        loadingProgress?.AddCurrent(1);
        yield return Audio.Connect(this);
        loadingProgress?.AddCurrent(1);
        yield return Camera.Connect(this);
        loadingProgress?.AddCurrent(1);
        yield return Input.Connect(this);
        loadingProgress?.AddCurrent(1);
        yield return Placement.Connect(this);
        loadingProgress?.AddCurrent(1);
        yield return Character.Connect(this);
        loadingProgress?.AddCurrent(1);
        yield return null;
        UIManager.ClaimOpenScreen(UIType.Title, ScreenChangeType.ScreenChanger);
        isLoading = false;
    }

    void DeleteManagers()
    {
        Input?.Disconnect();
        ObjectM?.Disconnect();
        Audio?.Disconnect();
        Language?.Disconnect();
        Setting?.Disconnect();
        Save?.Disconnect();
        Camera?.Disconnect();
        UI?.Disconnect();
        Data?.Disconnect();
        Character?.Disconnect();
        Placement?.Disconnect();
    }
    ManagerType CreateManager<ManagerType>(ref ManagerType targetVariable) where ManagerType : ManagerBase
    {
        if (targetVariable == null)
        {
            targetVariable = gameObject.AddComponent<ManagerType>();
            targetVariable.Connect(this);
        }
        return targetVariable;
    }

    public static void QuitGame()
    { 
#if UNITY_EDITOR

        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public static void Pause()
    { Instance.isPlaying = false; }
    public static void UnPause()
    { Instance.isPlaying = true; }

    void InvokeInitializeEvent(ref InitializeEvent OriginEvent)
    {
        if (OriginEvent != null)
        {
            InitializeEvent currentInitializeCharacter = OriginEvent;
            OriginEvent = null;
            currentInitializeCharacter?.Invoke();
        }

    }
    void InvokeDestroyEvent(ref DestroyEvent OriginEvent)
    {
        if (OriginEvent != null)
        {
            DestroyEvent currentInitializeCharacter = OriginEvent;
            OriginEvent = null;
            currentInitializeCharacter?.Invoke();
        }
    }
    void Update()
    {
        if (isLoading) return;
        
        //초기화
        //매니저를 초기화한다
        InvokeInitializeEvent(ref OnInitializeManager);
        //캐릭터를 초기화한다
        InvokeInitializeEvent(ref OnInitializeCharacter);
        //컨트롤러를 초기화한다 => 캐릭터가 있는 상태에서 돌아가야 하니까!
        InvokeInitializeEvent(ref OnInitializeController);
        //오브젝트를 초기화한다
        InvokeInitializeEvent(ref OnInitializeObject);

        if (isPlaying)
        { 
            float deltaTime = Time.deltaTime;
            OnUpdateManager?.Invoke(deltaTime);
            //컨트롤러를 업데이트한다 => 먼저 판단하고
            OnUpdateController?.Invoke(deltaTime);
            //캐릭터를 업데이트한다 => 캐릭터가 수행하고
            OnUpdateCharacter?.Invoke(deltaTime);
            //오브젝트를 업데이트한다 => 오브젝트 진행
            OnUpdateObject?.Invoke(deltaTime);
        }

        //오브젝트를 제거한다
        InvokeDestroyEvent(ref OnDestroyObject);
        //컨트롤러를 제거한다
        InvokeDestroyEvent(ref OnDestroyController);
        //캐릭터를 제거한다
        InvokeDestroyEvent(ref OnDestroyCharacter);
        //매니저를 제거한다
        InvokeDestroyEvent(ref OnDestroyManager);

    }
    private void FixedUpdate()
    {
        if (isLoading || !isPlaying) return;
        float deltaTime = Time.fixedDeltaTime;
        OnPhysicsObject?.Invoke(deltaTime);
        OnPhysicsCharacter?.Invoke(deltaTime);
    }
}


