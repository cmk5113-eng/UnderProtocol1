using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Collections;
using UnityEngine;

public delegate  void EventCharacterChanged(CharacterBase newCharacter);

public class SelectionManager : ManagerBase
{
    // 싱글톤 인스턴스 (StageUIController가 참조할 수 있도록 유지)
    public static SelectionManager Instance { get; set; }
    public static EventCharacterChanged OnCharacterChanged;

    // 현재 선택된 캐릭터 (static 변수)
    public static CharacterBase _characterBase;
    public static CharacterBase CharacterBase
    {
        get => _characterBase;
        set
        {
            if (_characterBase != value)
            {
                _characterBase?.OnDeSelected();
                _characterBase = value;
                _selectedPrefab = value?.gameObject;
                _characterBase?.OnSelected();
                OnCharacterChanged?.Invoke(_characterBase);
            }
        }
    }
    public static CharacterData _characterData;
    public static CharacterData CharacterData
    {
        get => _characterData;
        set
        {
            if (_characterData != value)
            {
                _characterData = value;

                if (Instance == null)
                    return;

                int index = Array.IndexOf(Instance.characterDatas, _characterData);

                if (index >= 0 && index < Instance.characterBases.Length)
                {
                    CharacterBase = Instance.characterBases[index];
                }
                else
                {
                    CharacterBase = null;
                }
            }
        }
    }
    static GameObject _selectedPrefab;
    public static GameObject SelectedPrefab
    {
        get => _selectedPrefab;
        set
        {
            if (_selectedPrefab != value)
            {
                _selectedPrefab = value;
                _characterBase?.OnDeSelected();
                _characterBase = value?.GetComponent<CharacterBase>();
                _characterBase?.OnSelected();
                OnCharacterChanged?.Invoke(_characterBase);
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

    /// <summary>
    /// 외부에서 캐릭터를 선택할 때 호출하는 static 함수
    /// </summary>
    public static void SetSelectedCharacter(CharacterBase character)
    {
        if (character == null) return;

        CharacterBase = character;
        Debug.Log($"[Selection] 현재 선택된 캐릭터가 {character.Name}(으)로 변경되었습니다.");
    }

    /// <summary>
    /// 선택을 해제할 때 호출하는 static 함수
    /// </summary>
    public static void ClearSelectedCharacter()
    {
       CharacterBase = null;
        Debug.Log("[Selection] 캐릭터 선택이 해제되었습니다.");
    }
    
    // 기존의 명칭과 호환성을 위한 역호환성 함수
    public void DeselectCharacter()
    {
        ClearSelectedCharacter();
    }
}