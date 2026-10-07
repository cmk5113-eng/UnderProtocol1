using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static ModeManager;

public class BattleManager : ManagerBase
{



    // 💡 게임 모드 에뉴머레이션 (몬스터턴과 플레이어턴 포함)
    public enum TurnMode
    {
        PlayerTurn,
        MonsterTurn
    }

    public static float HP = 100;

    [Header("턴 및 웨이브 상태")]
    [SerializeField] private TurnMode currentTurnMode = TurnMode.PlayerTurn;
    [SerializeField] public static int currentTurn = 1;       // 현재 턴 변수
    // 현재 웨이브 변수

    [Header("캐릭터 리스트 관리")]
    // 씬에 배치된 플레이어와 몬스터들을 관리할 리스트
    [SerializeField] private List<CharacterBase> playerCharacters = new List<CharacterBase>();
    [SerializeField] private List<CharacterBase> monsterCharacters = new List<CharacterBase>();
    private Coroutine pendingTurnEnd;
    private int activeStageId = -1;
    private int lastCompletedPlayerTurn;
    private WaveSetter activeStage;
    public bool IsBattleActive { get; private set; }
    private readonly BattlePassiveSystem passiveSystem = new BattlePassiveSystem();
    private readonly BattleFieldEffectSystem fieldEffectSystem = new BattleFieldEffectSystem();
    [Header("패시브")]
    [SerializeField] private bool logPassiveActivations = true;
    public BattlePassiveSystem Passives => passiveSystem;
    public BattleFieldEffectSystem Fields => fieldEffectSystem;
    public bool IsPlayerTurn => IsBattleActive && currentTurnMode == TurnMode.PlayerTurn;
    public bool CanAcceptPlayerAction => IsPlayerTurn && !passiveSystem.IsResolving
        && !fieldEffectSystem.IsResolving && !HasMovingPlayers();

    public void BeginBattle(int stageId, WaveSetter stage)
    {
        ResetBattle();
        activeStageId = stageId;
        activeStage = stage;
        IsBattleActive = true;
        passiveSystem.BeginPlayerTurn(currentTurn);
    }

    public void CompleteBattle()
    {
        WaveManager wave = GameManager.Instance != null ? GameManager.Instance.Wave : null;
        if (!IsBattleActive || wave == null || wave.selectedWaves == null
            || wave.selectedWaves.Length == 0 || wave.currentWave == null
            || wave.currentWaveIndex < wave.selectedWaves.Length || HasRemainingMonsters())
            return;

        FinishBattle(true);
    }

    public void AbortBattle() => FinishBattle(false);
    private void FinishBattle(bool cleared)
    {
        if (!IsBattleActive) return;
        // UI registration happens after construction; resolve the screen when leaving battle.
        UIBase stageScreen = UIManager.ClaimGetUI(UIType.Stage);
        if (stageScreen != null)
        {
            stageScreen.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogError("UIManager에 등록된 Stage 화면이 없습니다.");
        }

        IsBattleActive = false;
        WaveSetter returnStage = activeStage;

        WaveManager wave = GameManager.Instance != null ? GameManager.Instance.Wave : null;
        if (cleared && ProgressManager.RecordStageClear(activeStageId,
            Mathf.Max(1, lastCompletedPlayerTurn), HP, wave != null && wave.selectedWaves != null
                ? wave.selectedWaves.Length : 0))
        {
            SaveManager save = GameManager.Instance != null ? GameManager.Instance.Save : null;
            if (save != null) save.Save(save.currentSlot);
        }

        PlacementController.RemoveAllObject();
        if (cleared && returnStage != null && returnStage.ShowPostBattleScenario())
        {
            Debug.Log("[Battle] 스테이지 클리어. 전투 후 시나리오를 표시합니다.");
        }
        else
        {
            if (returnStage != null) returnStage.ReturnToWorld();
            Debug.Log(cleared ? "[Battle] 스테이지 클리어. 월드로 복귀합니다."
                : "[Battle] 전투 종료. 월드로 복귀합니다.");
        }
    }

    public static bool HasRemainingMonsters()
    {
        foreach (MonsterBase monster in FindObjectsByType<MonsterBase>(
            FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            if (monster.currentHP > 0) return true;
        return false;
    }

    private static BattleManager instance;
    public static BattleManager Instance
    {
        get
        {
            // 💡 인스턴스가 비어있다면 씬 내부에서 직접 컴포넌트를 찾아와 세팅합니다.
            if (instance == null)
            {
                instance = FindFirstObjectByType<BattleManager>();

                // 유니티 구버전을 사용 중이시라면 아래 코드를 사용하세요.
                // instance = FindObjectOfType<BattleManager>();
            }
            return instance;
        }
    }
    
    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
        passiveSystem.PassiveActivated += LogPassiveActivation;
    }

    private void LogPassiveActivation(CharacterBase owner, PassiveSkill skill)
    {
        if (logPassiveActivations && owner != null && skill != null)
            Debug.Log($"[Passive] {owner.Data.characterName}: {skill.skillName}");
    }

    private void Start()
    {
        ResetBattle();
    }

    public void ResetBattle()
    {
        if (pendingTurnEnd != null)
        {
            StopCoroutine(pendingTurnEnd);
            pendingTurnEnd = null;
        }

        IsBattleActive = false;
        activeStageId = -1;
        activeStage = null;
        HP = 100;
        currentTurn = 1;
        lastCompletedPlayerTurn = 0;
        currentTurnMode = TurnMode.PlayerTurn;
        playerCharacters.Clear();
        monsterCharacters.Clear();
        passiveSystem.ResetBattle();
        fieldEffectSystem.ResetBattle();
    }

    public void NotifyAttackCompleted(CharacterBase actor, SkillList skill, SkillExecuteResult result,
        Vector3Int aim, ICollection<Vector3Int> attackArea)
    {
        if (!IsPlayerTurn || passiveSystem.IsResolving || PlacementManager.Instance == null) return;
        CharacterBase[] characters = FindObjectsByType<CharacterBase>(
            FindObjectsInactive.Exclude, FindObjectsSortMode.InstanceID);
        passiveSystem.OnAttackCompleted(actor, skill, result, aim, attackArea,
            characters, PlacementManager.Instance.tilemap);
        RefreshActionPointsUI();
    }

    public void NotifyMovementCompleted(CharacterBase actor, Vector3Int from, Vector3Int to)
    {
        if (!IsPlayerTurn || passiveSystem.IsResolving || PlacementManager.Instance == null) return;
        CharacterBase[] characters = FindObjectsByType<CharacterBase>(
            FindObjectsInactive.Exclude, FindObjectsSortMode.InstanceID);
        passiveSystem.OnMovementCompleted(actor, from, to, characters, PlacementManager.Instance.tilemap);
        RefreshActionPointsUI();
    }

    private static void RefreshActionPointsUI()
    {
        if (StageUIController.Instance != null) StageUIController.Instance.RefreshActionPoints();
    }

    private static bool HasMovingPlayers()
    {
        foreach (CharacterBase player in FindObjectsByType<CharacterBase>(
            FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (!BattlePassiveSystem.IsPlayer(player)) continue;
            MovementModule movement = player.GetComponent<MovementModule>();
            if (movement != null && movement.IsMoving) return true;
        }
        return false;
    }

    /// <summary>
    /// 플레이어 캐릭터들이 행동(AP 소비)할 때마다 호출하여 모두 0이 되었는지 체크하는 함수
    /// </summary>
    /// 
    public void FindMonsters()
    {
        monsterCharacters.Clear();

        CharacterBase[] characters = FindObjectsByType<CharacterBase>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        foreach (CharacterBase character in characters)
        {
            if (character.isEnemy)
            {
                monsterCharacters.Add(character);
            }
        }
    }
    public void CheckPlayerApAndTryEndTurn()
    {
        if (!CanAcceptPlayerAction) return;

        // The first turn starts after placement, so do not rely on the previous turn's roster.
        playerCharacters.Clear();
        foreach (CharacterBase player in FindObjectsByType<CharacterBase>(
            FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            if (BattlePassiveSystem.IsPlayer(player)) playerCharacters.Add(player);
        if (playerCharacters.Count == 0) return;

        bool allPlayersApZero = true;
        foreach (var player in playerCharacters)
        {
            // 캐릭터에 ap나 mobility 등의 행동력 변수가 있다고 가정 (ex: player.CurrentAp)
            // 여기서는 예시로 player.ap 속성을 체크합니다. 본인 프로젝트의 변수명에 맞게 수정하세요.
            if (player != null && player.actionPoint > 0)
            {
                allPlayersApZero = false;
                break;
            }
        }

        // 💡 모든 캐릭터의 AP가 0이 되면 턴 종료 실행
        if (allPlayersApZero)
        {
            Debug.Log("[Battle] 모든 플레이어의 AP가 0이 되어 자동으로 턴을 종료합니다.");
            EndTurn();
        }
    }

    /// <summary>
    /// 턴 종료 함수 (턴 종료 버튼에 이 함수를 바인딩하면 됩니다!)
    /// </summary>
    public void EndTurn()
    {
        if (CanAcceptPlayerAction)
        {
            Debug.Log($"[Battle] 플레이어 턴 {currentTurn} 종료.");
            // 중복 클릭을 막고, 같은 프레임에 처치한 몬스터의 삭제를 기다린다.
            currentTurnMode = TurnMode.MonsterTurn;
            if (ModeManager.Instance != null)
                ModeManager.Instance.CurrentMode = GameMode.EnemyTurn;

            if (UseSkill.Instance != null)
                UseSkill.Instance.ClearAllHighlights();

            pendingTurnEnd = StartCoroutine(FinishTurnAfterDestruction());
        }
    }

    private IEnumerator FinishTurnAfterDestruction()
    {
        yield return null;
        pendingTurnEnd = null;
        StartMonsterTurn();
    }

    /// <summary>
    /// 몬스터 턴 시작 처리
    /// </summary>
    public void StartMonsterTurn()
    {
        if (!IsBattleActive) return;
        currentTurnMode = TurnMode.MonsterTurn;

        // ModeManager가 있다면 연동 (필요 시 주석 해제)
        // if (ModeManager.Instance != null) ModeManager.Instance.CurrentMode = ModeManager.GameMode.MonsterTurn;

        // 💡 몬스터턴이 시작되면 몬스터턴 함수를 실행
        MonsterTurn();
    }


    /// <summary>
    /// 💡 몬스터턴 함수 (AI 행동을 순차적으로 처리하기 위해 코루틴 사용)
    /// </summary>
    public void MonsterTurn()
    {
        if (!IsBattleActive || currentTurnMode != TurnMode.MonsterTurn) return;
        lastCompletedPlayerTurn = currentTurn;
        Debug.Log("[Battle] 몬스터 턴 시작");

        CharacterBase[] characters = FindObjectsByType<CharacterBase>(
            FindObjectsInactive.Exclude, FindObjectsSortMode.InstanceID);
        if (PlacementManager.Instance != null)
        {
            passiveSystem.BeforeMonsterTurn(currentTurn, characters, PlacementManager.Instance.tilemap);
            fieldEffectSystem.BeforeMonsterTurn(currentTurn, characters, PlacementManager.Instance.tilemap,
                ExecuteSkill.Instance, passiveSystem);
        }


        if (monsterCharacters == null)
            return;

        MonsterBase[] monsters =
            UnityEngine.Object.FindObjectsByType<MonsterBase>(
                FindObjectsSortMode.None
            );

        foreach (MonsterBase monster in monsters)
        {
            if (monster == null)
                continue;

            if (monster.currentHP <= 0)
                continue;

            if (passiveSystem.ShouldSkipMonsterAction(monster) || fieldEffectSystem.ShouldSkipMonsterAction(monster))
                continue;

            int monsterDamage = fieldEffectSystem.MonsterDamage(monster, monster.currentHP);
            HP -= monsterDamage;

            Debug.Log($"{HP}");

            if (ScrollUI.Instance != null)
                ScrollUI.Instance.SubValue(monsterDamage * 0.01f);
        }

        // 모든 몬스터 처리 후 한 번만 판정
        bool defeated = ScrollUI.Instance != null && ScrollUI.Instance.HPscrollbar != null
            ? ScrollUI.Instance.HPscrollbar.value <= 0f : HP <= 0f;
        if (defeated)
        {
            FinishBattle(false);
            UIManager.ClaimPopUp("전투 종료", "게임오버", "확인");
            return;
        }
        EndMonsterTurn();
    }
    /// <summary>
    /// 몬스터 턴 종료 및 플레이어 턴 복귀
    /// </summary>
    public void EndMonsterTurn()
    {
        if (!IsBattleActive || currentTurnMode != TurnMode.MonsterTurn) return;

        Debug.Log($"[Battle] 몬스터 턴 {currentTurn} 종료.");

        // 💡 종료되면 플레이어턴으로 변경
        currentTurnMode = TurnMode.PlayerTurn;

        // 💡 현재 턴 변수 +1
        currentTurn++;

        // 새 웨이브는 기존 몬스터의 행동이 끝난 뒤 생성한다.
        // 따라서 새로 등장한 몬스터는 다음 플레이어 턴 전에 공격하지 않는다.
        if (!HasRemainingMonsters() && WaveLoader.Instance != null)
            WaveLoader.Instance.NextWave();

        // 마지막 웨이브가 끝났다면 시나리오/월드 전환 뒤 이동 모드/다음 턴을 다시 열지 않는다.
        if (!IsBattleActive) return;
        FindMonsters();
        StartPlayerTurn();
    }

    /// <summary>
    /// 플레이어 턴 시작 (AP 리셋 등)
    /// </summary>
    public void StartPlayerTurn()
    {
        if (!IsBattleActive) return;
        currentTurnMode = TurnMode.PlayerTurn;
        passiveSystem.BeginPlayerTurn(currentTurn);
        fieldEffectSystem.ClearTurnModifiers();

        if (ModeManager.Instance != null)
            ModeManager.Instance.CurrentMode = ModeManager.GameMode.Movement;

        // 턴 처리가 끝난 뒤 실제 배치된 플레이어의 행동력을 복구한다.
        playerCharacters.Clear();
        CharacterBase[] activeCharacters = FindObjectsByType<CharacterBase>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        foreach (CharacterBase player in activeCharacters)
        {
            if (!BattlePassiveSystem.IsPlayer(player))
                continue;

            playerCharacters.Add(player);
            player.actionPoint = Mathf.Max(1, player.maxAP);
            player.steminaPoint = player.maxStemina;
            passiveSystem.ApplyStartOfTurnBonuses(player);
            player.UpdateActionStateVisual();

            MovementModule moveModule = player.GetComponent<MovementModule>();
            if (moveModule != null)
                moveModule.StopMovement();
        }

        if (StageUIController.Instance != null)
        {
            StageUIController.Instance.UpdateTurn();
            StageUIController.Instance.UpdateWave();
            if (SelectionManager.Instance != null)
                StageUIController.Instance.resetunit();
            StageUIController.Instance.RefreshActionPoints();
        }
    }

    /// <summary>
    /// 💡 몬스터가 죽을 때마다 호출해 주어야 하는 함수
    /// </summary>
    public void OnMonsterDead()
    {
        // 사망 시에는 목록만 갱신하고, 다음 웨이브 판정은 턴 종료 때 한다.
        FindMonsters();
    }

    

    private void SpawnNextWaveMonsters()
    {
        // 💡 아까 공유해주신 ObjectManager를 활용한 예시 스폰 구조
        // GameObject newMonster = ObjectManager.CreateObject("Orc", new Vector3(2, 3, 0));
        // if(newMonster != null) monsterCharacters.Add(newMonster.GetComponent<CharacterBase>());
    }

    protected override IEnumerator OnConnected(GameManager newManager)
    {
        yield return null;
    }

    protected override void OnDisconnected()
    {
        ResetBattle();
    }
}
