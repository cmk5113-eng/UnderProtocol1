using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

// --- 전역 열거형 (어떤 스크립트에서도 접근 가능하도록 클래스 밖에 배치) ---
public enum JobType { Warrior, Archer, Mage, Builder }
public enum ElementType { None, Fire, Water, Electric, Earth }

public class CharacterBase : MonoBehaviour
{
    ControllerBase _controller;
    public ControllerBase Controller => _controller;

    protected Vector3 _lookRotation;
    protected Vector3 LookRotation =>_lookRotation;

    public virtual string DisplayName => "character";
    public virtual void OnPossessed(ControllerBase newcontroller)
    {

    }
    public ControllerBase Possessed(ControllerBase from)
    {

        if(_controller) Unpossessed();
        _controller = from;
       OnPossessed(Controller);
        return Controller;
    }


    public virtual void OnUnpossessed(ControllerBase oldcontroller)
    { 
    }
    public void Unpossessed()
    {

        if(Controller)OnUnpossessed(_controller);
        _controller = null;
    }
    public bool Unpossessed(ControllerBase oldController)
    {
        if (Controller != oldController) return false;
        Unpossessed();
        return true;
    
    }





















    // --- 1. 데이터 구조 설계 ---
    [System.Serializable]
    public struct CharacterInfo
    {
        public string name;
        public int jobIndex;     // JobType (0:Warrior, 1:Archer...)
        public int elementIndex; // ElementType (0:None, 1:Fire...)
    }

    [Header("Character Data List")]
    // 이 리스트가 클래스 안에 있어야 유니티 인스펙터에서 12명을 편집할 수 있습니다.
    public List<CharacterInfo> characterList = new List<CharacterInfo>();

    [Header("UI References")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI jobText;
    public TextMeshProUGUI elementText;
    public Button nextButton;

    private int currentIndex = 0;

    // --- 2. 초기화 및 이벤트 연결 ---
    void Awake()
    {
        // 게임 시작 시 12명의 명단을 생성합니다.
        InitCharacters();
    }

    void Start()
    {
        // UI 버튼 클릭 시 다음 캐릭터로 넘어가도록 설정
        if (nextButton != null)
            nextButton.onClick.AddListener(ShowNextCharacter);

        // 첫 번째 캐릭터 정보를 화면에 표시
        UpdateUI();
    }

    // --- 3. 핵심 로직 ---
    void InitCharacters()
    {
        // 리스트에 12명의 데이터를 추가 (기획하신 Under Protocol 캐릭터들)
        characterList.Add(new CharacterInfo { name = "남궁인", jobIndex = 0, elementIndex = 1 });
        characterList.Add(new CharacterInfo { name = "류해랑", jobIndex = 1, elementIndex = 2 });
        characterList.Add(new CharacterInfo { name = "서은하", jobIndex = 2, elementIndex = 3 });
        characterList.Add(new CharacterInfo { name = "백진우", jobIndex = 0, elementIndex = 4 });
        characterList.Add(new CharacterInfo { name = "강철수", jobIndex = 3, elementIndex = 0 });
        characterList.Add(new CharacterInfo { name = "이유리", jobIndex = 1, elementIndex = 1 });
        characterList.Add(new CharacterInfo { name = "한도윤", jobIndex = 0, elementIndex = 3 });
        characterList.Add(new CharacterInfo { name = "최지아", jobIndex = 2, elementIndex = 2 });
        characterList.Add(new CharacterInfo { name = "정재희", jobIndex = 3, elementIndex = 4 });
        characterList.Add(new CharacterInfo { name = "박민호", jobIndex = 1, elementIndex = 3 });
        characterList.Add(new CharacterInfo { name = "송하늘", jobIndex = 2, elementIndex = 1 });
        characterList.Add(new CharacterInfo { name = "윤가람", jobIndex = 0, elementIndex = 2 });
    }

    public void ShowNextCharacter()
    {
        if (characterList.Count == 0) return;

        // 인덱스를 순환시킴 (11 다음은 다시 0)
        currentIndex = (currentIndex + 1) % characterList.Count;
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (characterList.Count <= currentIndex) return;

        var info = characterList[currentIndex];

        // UI 텍스트 업데이트 (Enum 캐스팅 활용)
        if (nameText) nameText.text = $"이름: {info.name}";
        if (jobText) jobText.text = $"직업: {(JobType)info.jobIndex}";
        if (elementText) elementText.text = $"속성: {(ElementType)info.elementIndex}";

        Debug.Log($"[UI 업데이트] {info.name} 표시 중");
    }

    // 소환 시스템에서 "지금 UI에 떠 있는 캐릭터 누구야?"라고 물어볼 때 사용
    public CharacterInfo GetCurrentCharacterInfo()
    {
        return characterList[currentIndex];
    }
}