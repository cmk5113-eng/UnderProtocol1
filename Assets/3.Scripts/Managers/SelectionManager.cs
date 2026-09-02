using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Collections;
using UnityEngine;
using UnityEngine.TextCore.Text;

public delegate  void EventCharacterChanged(CharacterBase newCharacter);

public class SelectionManager : ManagerBase
{
    // 싱글톤 인스턴스 (StageUIController가 참조할 수 있도록 유지)
    public static SelectionManager Instance { get; set; }
    public static EventCharacterChanged OnCharacterChanged;

    // 현재 선택된 캐릭터 (static 변수)
    public static CharacterBase _characterBase;
    public static CharacterBase CharacterBase=> _characterBase;
    public static CharacterData _characterData;

    static GameObject _selectedPrefab;
    public static GameObject SelectedPrefab
    {
        get => _selectedPrefab;
        set
        {
            if (_selectedPrefab != value)
            {
                _selectedPrefab = value;
            }
        }
    }



    // 스테이지 상에 배치된 아군 리스트
    public List<CharacterBase> unitOnStage = new List<CharacterBase>();


    [SerializeField]public CharacterBase[] characterBases = new CharacterBase[12];
    [SerializeField]public CharacterData[] characterDatas = new CharacterData[12];
    [SerializeField]public GameObject[] characterPrefabs = new GameObject[12];

    protected override IEnumerator OnConnected(GameManager newManager)
    {
        // 매니저 연결 시 싱글톤 등록
        Instance = this;
        yield return null;
    }

    protected override void OnDisconnected()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Start()
    {
        Instance = this;
    }
    public static void SelectCharacter(CharacterBase newCharacter)
    {
        _characterBase?.OnDeSelected();
        if (newCharacter)
        {
            _characterBase = newCharacter;
            _characterBase?.OnSelected();
   
        }
        else
        {
            _characterBase = null;
        }
        OnCharacterChanged?.Invoke(_characterBase);
    }

    public static void DeselectCharacter()
    {
        if (_characterBase == null) { return; }
        _characterBase.OnDeSelected();
        _characterBase = null;
        OnCharacterChanged?.Invoke(_characterBase);
        Debug.Log("[Selection] 캐릭터 선택이 해제되었습니다.");
    }

    public void InitCharacter(CharacterBase character)
    {
        if (character == null)
            return;

        character.actionPoint = character.maxAP;
        character.steminaPoint = character.maxStemina;
        character.isSpawned = false;
        Debug.Log($"{character.Name}초기화완료");
        Debug.Log($"{character.isSpawned}");

    }
}