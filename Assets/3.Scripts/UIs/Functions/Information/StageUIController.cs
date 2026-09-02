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
        // 1. 턴 종료 처리
        if (BattleManager.Instance != null)
        {
            BattleManager.Instance.EndTurn();
        }

        // 💡 [핵심 수정] unitOnStage 변수 대신, 현재 씬(필드)에 실제로 생성되어 있는 모든 캐릭터 컴포넌트를 직접 찾습니다.
        CharacterBase[] activeCharacters = FindObjectsOfType<CharacterBase>();

        foreach (CharacterBase character in activeCharacters)
        {
            if (character != null)
            {
                // 진짜 오브젝트의 데이터를 직접 수정합니다.
                character.actionPoint = 1;
                character.steminaPoint = character.maxStemina;
                character.UpdateActionStateVisual();
                // 이동 잠금(IsMoving) 상태도 함께 안전하게 풀어줍니다.
                MovementModule moveModule = character.GetComponent<MovementModule>();
                if (moveModule != null)
                {
                    // 목적지 도달 후 잔여 데이터가 남지 않도록 초기화
                    moveModule.StopMovement();
                }

                }
        }

        // 3. UI 글자 갱신
        UpdateTurn();
        resetunit();
    }
    public void OnNextWave()
    {
        //나중에 꼭 지울것!!!!!!!!!!!!!!!!
        BattleManager.Instance.EndMonsterTurn();
        UpdateTurn();
    }
    public void UpdateTurn()
    {

        currentturn.SetText(BattleManager.currentTurn.ToString());
    }
    public void UpdateWave()
    {
        currentwave.SetText(BattleManager.currentWave.ToString());
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
        // 1번째 슬롯 (0번 인덱스)
        if (SelectionManager.Instance.unitOnStage.Count > 0)
        {
            unit[0].gameObject.SetActive(true);
            // 💡 [수정] unit[1]이 아니라 unit[0]의 이미지를 바꿔야 합니다.
            unit[0].sprite = SelectionManager.Instance.unitOnStage[0].portrait;
            unitname[0].SetText(SelectionManager.Instance.unitOnStage[0].Name);
            AP[0].SetText(SelectionManager.Instance.unitOnStage[0].actionPoint.ToString());
            SP[0].SetText(SelectionManager.Instance.unitOnStage[0].steminaPoint.ToString());
        }
        else
        {
            unit[0].gameObject.SetActive(false);
            unitname[0].SetText(""); // 💡 [수정] 0번 텍스트 초기화
        }

        // 2번째 슬롯 (1번 인덱스)
        if (SelectionManager.Instance.unitOnStage.Count > 1)
        {
            unit[1].gameObject.SetActive(true);
            unit[1].sprite = SelectionManager.Instance.unitOnStage[1].portrait;
            unitname[1].SetText(SelectionManager.Instance.unitOnStage[1].Name);
            AP[1].SetText(SelectionManager.Instance.unitOnStage[1].actionPoint.ToString());
            SP[1].SetText(SelectionManager.Instance.unitOnStage[1].steminaPoint.ToString());
        }
        else
        {
            unit[1].gameObject.SetActive(false);
            unitname[1].SetText("");
        }

        // 3번째 슬롯 (2번 인덱스)
        if (SelectionManager.Instance.unitOnStage.Count > 2)
        {
            unit[2].gameObject.SetActive(true);
            unit[2].sprite = SelectionManager.Instance.unitOnStage[2].portrait;
            unitname[2].SetText(SelectionManager.Instance.unitOnStage[2].Name);
            AP[2].SetText(SelectionManager.Instance.unitOnStage[2].actionPoint.ToString());
            SP[2].SetText(SelectionManager.Instance.unitOnStage[2].steminaPoint.ToString());
        }
        else
        {
            unit[2].gameObject.SetActive(false);
            unitname[2].SetText("");
        }

        // 4번째 슬롯 (3번 인덱스)
        if (SelectionManager.Instance.unitOnStage.Count > 3)
        {
            unit[3].gameObject.SetActive(true);
            unit[3].sprite = SelectionManager.Instance.unitOnStage[3].portrait;
            unitname[3].SetText(SelectionManager.Instance.unitOnStage[3].Name); 
            AP[3].SetText(SelectionManager.Instance.unitOnStage[3].actionPoint.ToString());
            SP[3].SetText(SelectionManager.Instance.unitOnStage[3].steminaPoint.ToString());
        }
        else
        {
            unit[3].gameObject.SetActive(false);
        }
    }
}

