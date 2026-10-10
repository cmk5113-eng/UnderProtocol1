using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UI;

public class StageUIController : MonoBehaviour
{
    public static StageUIController Instance { get; private set; }

    [SerializeField] private Sprite defaultImage;
    [SerializeField] private Image portrait;
    [SerializeField] private TMPro.TextMeshProUGUI characterName;
    [SerializeField] private Image[] skill = new Image[5];

    [SerializeField] private Image[] unit = new Image[12];
    [SerializeField] private TMPro.TextMeshProUGUI AP;
    [SerializeField] private TMPro.TextMeshProUGUI SP;

    [SerializeField] public TMPro.TextMeshProUGUI currentwave;
    [SerializeField] public TMPro.TextMeshProUGUI currentturn;
    [SerializeField] private TMPro.TextMeshProUGUI currentenemy;
    private UI_SkillTooltip skillTooltip;


   

    private CharacterData currentData;

    private CharacterBase asCharacter;
    [SerializeField] private List<GameObject> UIcharacterList;
    [SerializeField] private List<CharacterData> newCharacters;
    
    public CharacterBase CurrentCharacter => asCharacter;
    public CharacterData CurrentData => currentData;
    private void Awake()
    {
        Instance = this;
        BindStageInfo();
    }

    private void BindStageInfo()
    {
        Transform wave = transform.Find("Top/StageInfo/wave");
        Transform enemy = transform.Find("Top/StageInfo/enemy");
        if (currentwave == null && wave != null) currentwave = wave.GetComponent<TMPro.TextMeshProUGUI>();
        if (currentenemy == null && enemy != null) currentenemy = enemy.GetComponent<TMPro.TextMeshProUGUI>();
    }

    private void OnEnable()
    {
        BindStageInfo();
        UpdateTurn();
        UpdateWave();
    }

    private void LateUpdate()
    {
        UpdateWave();
    }

    private void OnDisable() => HideSkillTooltip();

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }


    public void Allreset()
    {
        HideSkillTooltip();
        asCharacter = null;
        currentData = null;
        if (portrait != null) portrait.sprite = defaultImage;
        // Image 컴포넌트의 연결은 유지하고 표시 내용만 초기화한다.
        foreach (Image image in skill)
            if (image != null) image.sprite = defaultImage;
        if (SelectionManager.Instance != null) SelectionManager.Instance.unitOnStage.Clear();
        resetunit();
        UpdateTurn();
        UpdateWave();
    }
    public void Refresh()
    {
        HideSkillTooltip();
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
            characterName.SetText(data.characterName);
            RefreshActionPoints();

            // 스킬 데이터 안전성 검사(? 연산자를 사용해 데이터가 부족해도 크래시 방지)
            skill[0].sprite = data.active != null && data.active.Length > 0 ? data.active[0]?.icon : null;
            skill[1].sprite = data.active != null && data.active.Length > 1 ? data.active[1]?.icon : null;
            skill[2].sprite = data.ultimateSkill?.icon;
            skill[3].sprite = data.normalSkill?.icon;
            skill[4].sprite = data.passive[0]?.icon;
            currentData = data;

            Summon();
            UpdateTurn();
            UpdateWave();
        }
    }


    public void OnClickUnit(int slotIndex)
    {
        if (SelectionManager.Instance == null)
            return;

        var characters = SelectionManager.Instance.unitOnStage;

        if (slotIndex < 0 || slotIndex >= characters.Count)
            return;

        CharacterBase target = characters[slotIndex];

        if (target == null || !target.isSpawned)
            return;

        // 캐릭터를 바꾸기 전에 스킬 조준과 하이라이트 해제
        if (UseSkill.Instance != null)
            UseSkill.Instance.ClearAllHighlights();

        // 스킬 사용 모드였다면 이동 모드로 복귀
        if (ModeManager.Instance != null &&
            ModeManager.Instance.CurrentMode == ModeManager.GameMode.UseSkill)
        {
            ModeManager.Instance.CurrentMode = ModeManager.GameMode.Movement;
        }

        // 해당 캐릭터 선택 후 초상화·스킬 정보 갱신
        SelectionManager.SelectCharacter(target);
        Refresh();
    }
    // Display the live character's resources, not shared ScriptableObject defaults.
    public void RefreshActionPoints()
    {
        CharacterBase character = SelectionManager.CharacterBase;
        if (AP != null) AP.SetText(character != null ? character.actionPoint.ToString() : "0");
        if (SP != null) SP.SetText(character != null ? character.steminaPoint.ToString() : "0");
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
        WaveManager waveManager = GameManager.Instance != null ? GameManager.Instance.Wave : null;
        BattleManager battle = BattleManager.Instance;
        SetCounter(currentwave, battle != null && battle.IsBattleActive && waveManager != null ? waveManager.CurrentWaveNumber : 0);
        UpdateEnemies();
    }

    public void UpdateEnemies()
    {
        BattleManager battle = BattleManager.Instance;
        SetCounter(currentenemy, battle != null && battle.IsBattleActive ? BattleManager.CountRemainingMonsters() : 0);
    }

    private static void SetCounter(TMPro.TextMeshProUGUI label, int value)
    {
        if (label == null) return;
        string text = value.ToString();
        if (label.text != text) label.SetText(text);
    }

    public SkillList GetSkill(int slotIndex)
    {
        if (currentData == null) return null;
        switch (slotIndex)
        {
            case 0:
            case 1: return currentData.active != null && slotIndex < currentData.active.Length ? currentData.active[slotIndex] : null;
            case 2: return currentData.ultimateSkill;
            case 3: return currentData.normalSkill;
            case 4: return currentData.passive != null && currentData.passive.Length > 0 ? currentData.passive[0] : null;
            default: return null;
        }
    }

    public void ShowSkillTooltip(int slotIndex)
    {
        SkillList selected = GetSkill(slotIndex);
        if (selected == null || skill == null || slotIndex < 0 || slotIndex >= skill.Length || skill[slotIndex] == null)
        {
            HideSkillTooltip();
            return;
        }
        RectTransform anchor = skill[slotIndex].transform as RectTransform;
        if (anchor == null || !(transform is RectTransform root)) return;
        if (skillTooltip == null)
            skillTooltip = UI_SkillTooltip.Create(root, characterName != null ? characterName.font : currentwave != null ? currentwave.font : null);
        skillTooltip.Show(selected, anchor);
    }

    public void HideSkillTooltip()
    {
        if (skillTooltip != null) skillTooltip.Hide();
    }

    public void OnClickPassiveSkill()
    {
        if (UseSkill.Instance != null) UseSkill.Instance.ClearAllHighlights();
        ShowSkillTooltip(4);
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
        if (UseSkill.Instance != null) UseSkill.Instance.UI_StartSkill1();
        else ShowSkillTooltip(0);
    }

    // 2번 스킬 버튼에 연결할 함수
    public void OnClickSkill2()
    {
        if (UseSkill.Instance != null) UseSkill.Instance.UI_StartSkill2();
        else ShowSkillTooltip(1);
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
                unit[i].sprite = character != null ? character.portrait : defaultImage;
            } 
 
        }
    }
}

