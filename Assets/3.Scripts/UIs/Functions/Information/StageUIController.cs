using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UI;

public class StageUIController : MonoBehaviour
{
    public static StageUIController Instance { get; private set; }
  

    [SerializeField] private Image portrait;
    [SerializeField] private Image[] skill = new Image[4];

    [SerializeField] private Image[] unit = new Image[4];
    [SerializeField] private TMPro.TextMeshProUGUI[] unitname = new TMPro.TextMeshProUGUI[4];
    [SerializeField] private TMPro.TextMeshProUGUI[] AP = new TMPro.TextMeshProUGUI[4];
    [SerializeField] private TMPro.TextMeshProUGUI[] SP = new TMPro.TextMeshProUGUI[4];
    [SerializeField] public TMPro.TextMeshProUGUI currentwave;
    [SerializeField] public TMPro.TextMeshProUGUI currentturn;


   

    private CharacterData currentData;

    private CharacterBase asCharacter;
    [SerializeField] private List<GameObject> UIcharacterList;
    [SerializeField] private List<CharacterData> newCharacters;
    
    public CharacterBase CurrentCharacter => asCharacter;
    public CharacterData CurrentData => currentData;
    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        UpdateTurn();
        UpdateWave();
    }


    public void Allreset()
    {
        asCharacter = null;
        currentData = null;
        if (portrait != null) portrait.sprite = null;
        // Image 컴포넌트의 연결은 유지하고 표시 내용만 초기화한다.
        foreach (Image image in skill)
            if (image != null) image.sprite = null;
        if (SelectionManager.Instance != null) SelectionManager.Instance.unitOnStage.Clear();
        resetunit();
        UpdateTurn();
        UpdateWave();
    }
    public void Refresh()
    {
        if (SelectionManager.CharacterBase == null)
        {
            return;
        }

        asCharacter = SelectionManager.CharacterBase;

        if (asCharacter == null)
        {
            return;
        }

        int index = -1;
        for (int i = 0; i < UIcharacterList.Count; i++)
        {
            if (UIcharacterList[i] != null && UIcharacterList[i].name == SelectionManager.CharacterBase.gameObject.name)
            {
                index = i;
                break;
            }
        }

        // 💡 하드코딩 대신 인덱스 범위 안전 검사 후 리스트에서 다이렉트로 가져옵니다.
        CharacterData data = SelectionManager._characterData;

        if (index >= 0 && index < newCharacters.Count)
        {
            data = newCharacters[index];
        }
        else
        {
            // 여전히 못 찾은 경우를 대비한 예외 처리 (수동 디버깅 용이)
            Debug.LogError($"[UI Error] '{SelectionManager.CharacterBase.name}'에 매칭되는 캐릭터 데이터를 newCharacters에서 찾을 수 없습니다. (인덱스: {index})");
            return;
        }

        // 데이터 반영
        if (data != null)
        {
            portrait.sprite = data.Portrait;

            // 스킬 데이터 안전성 검사(? 연산자를 사용해 데이터가 부족해도 크래시 방지)
            skill[0].sprite = data.active != null && data.active.Length > 0 ? data.active[0]?.icon : null;
            skill[1].sprite = data.active != null && data.active.Length > 1 ? data.active[1]?.icon : null;
            skill[2].sprite = data.ultimateSkill?.icon;
            skill[3].sprite = data.normalSkill?.icon;

            currentData = data;

            Summon();
            UpdateTurn();
            UpdateWave();
        }
    }



    public void OnNextTurn()
    {
        // 행동력 복구와 UI 갱신은 BattleManager의 턴 종료 완료 후 처리한다.
        if (BattleManager.Instance != null)
        {
            BattleManager.Instance.EndTurn();
        }
    }
    public void OnNextWave()
    {
        OnNextTurn();
    }
    public void UpdateTurn()
    {

        if (currentturn != null)
            currentturn.SetText(BattleManager.currentTurn.ToString());
    }
    public void UpdateWave()
    {
        if (currentwave == null) return;

        WaveManager waveManager = GameManager.Instance != null ? GameManager.Instance.Wave : null;
        currentwave.SetText(waveManager != null && waveManager.currentWave != null
            ? waveManager.currentWave.ToString()
            : "0");
    }


    public void Summon()
    {
        // 1. 싱글톤 매니저의 선택 정보와 리스트에 먼저 등록합니다.
        SelectionManager.SelectCharacter(asCharacter);

        if (!SelectionManager.Instance.unitOnStage.Contains(asCharacter))
        {
            SelectionManager.Instance.unitOnStage.Add(asCharacter);
        }

        // 2. 데이터 등록이 완전히 끝난 후 UI를 새로고침합니다.
        resetunit();
    }
    public void UnSummon()
    {
        SelectionManager.Instance.unitOnStage.Remove(asCharacter);
        resetunit() ;
    }
    public void OnClickSkill1()
    {
        // 시전자(asCharacter)와 데이터(currentData)가 정상적으로 존재하고, 스킬이 있는지 체크
        if (asCharacter != null && currentData != null && currentData.active != null && currentData.active.Length > 0)
        {
            ActiveSkill targetSkill = currentData.active[0];
            if (targetSkill != null)
            {
                // 하이라이트 매니저 작동! (스킬 정보와 캐릭터 위치/정보 전달)
                UseSkill.Instance.StartSkillTargeting(targetSkill, asCharacter);
            }
        }
    }

    // 2번 스킬 버튼에 연결할 함수
    public void OnClickSkill2()
    {
        if (asCharacter != null && currentData != null && currentData.active != null && currentData.active.Length > 1)
        {
            ActiveSkill targetSkill = currentData.active[1];
            if (targetSkill != null)
            {
                UseSkill.Instance.StartSkillTargeting(targetSkill, asCharacter);
            }
        }
    }

    // 궁극기(3번) 스킬 버튼에 연결할 함수

    public void resetunit()
    {
        List<CharacterBase> characters = SelectionManager.Instance != null
            ? SelectionManager.Instance.unitOnStage : null;
        for (int i = 0; i < unit.Length; i++)
        {
            CharacterBase character = characters != null && i < characters.Count ? characters[i] : null;
            if (unit[i] != null)
            {
                unit[i].sprite = character != null ? character.portrait : null;
                unit[i].gameObject.SetActive(character != null);
            }
            if (i < unitname.Length && unitname[i] != null)
                unitname[i].SetText(character != null ? character.Name : "");
            if (i < AP.Length && AP[i] != null)
                AP[i].SetText(character != null ? character.actionPoint.ToString() : "");
            if (i < SP.Length && SP[i] != null)
                SP[i].SetText(character != null ? character.steminaPoint.ToString() : "");
        }
    }
}
