using UnityEngine;

public class GameManager : MonoBehaviour
{
    static GameManager _instance;
    public static GameManager Instance => _instance;

    UIManager _ui;
    UIManager UI =>_ui;

    DataManager _data;
    DataManager Data =>_data;

    SaveManager _save;
    SaveManager Save =>_save;

    SettingManager _setting;
    SettingManager Setting =>_setting;

    LanguageManager _language;
    LanguageManager Language =>_language;

    AudioManager _audio;
    AudioManager Audio =>_audio;

    CameraManager _camera;
    CameraManager Camera =>_camera;

    InputManager _input;
    InputManager Input =>_input;

    void Awake()
    {
        if (Instance == null)
        {

            _instance = this;

        }
        else
        {
            Destroy(this);
        }
    }


    void InitializeManagers()
    {
        CreateManager(ref _ui);
        CreateManager(ref _data);
        CreateManager(ref _save);
        CreateManager(ref _setting);
        CreateManager(ref _language);
        CreateManager(ref _audio);
        CreateManager(ref _camera);
        CreateManager(ref _input);

        if (_ui == null)
        { 
        _ui = gameObject.AddComponent<UIManager>();
            _ui.Connect(this);
        }
        if (_data == null)
        {
            _ui = gameObject.AddComponent<UIManager>();
            _ui.Connect(this);
        }
        if (_audio == null)
        {
            _ui = gameObject.AddComponent<UIManager>();
            _ui.Connect(this);
        }
        if (_language == null)
        {
            _ui = gameObject.AddComponent<UIManager>();
            _ui.Connect(this);
        }
        if (_input == null)
        {
            _ui = gameObject.AddComponent<UIManager>();
            _ui.Connect(this);
        }
        if (_camera == null)
        {
            _ui = gameObject.AddComponent<UIManager>();
            _ui.Connect(this);
        }
        if (_save == null) 
        {
            _ui = gameObject.AddComponent<UIManager>();
            _ui.Connect(this);
        }

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
            

    void Update()
    {

    }
}   
