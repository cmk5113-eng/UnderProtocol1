using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Tilemaps;

public class WaveSetter : MonoBehaviour
{
    [SerializeField] private GameObject worldScreen;
    [SerializeField] private GameObject scenarioScreen;
    [Tooltip("진행도 검사 통과 후 실행할 화면 설정입니다. 맵/웨이브 연결은 Stage Map Editor에서 설정합니다.")]
    [SerializeField] private UnityEvent onStageEntered = new UnityEvent();

    private GameObject battleMapRoot;
    private Tilemap battleTilemap;
    private GameObject postBattleScenario;

    private StageMapEntry EditorMap => GameManager.Instance != null && GameManager.Instance.Wave != null
        ? GameManager.Instance.Wave.GetMap(this) : null;
    public int StageId => EditorMap != null ? EditorMap.GetStageId(this) : -1;
    public int RequiredProgress
    {
        get
        {
            StageButtonImageController button = GetComponent<StageButtonImageController>();
            return button != null ? button.RequiredProgress : 0;
        }
    }
    public bool CanEnterStage => ProgressManager.Progress >= RequiredProgress;

    public void SelectSkillByIndex()
    {
        if (!CanEnterStage)
        {
            string message = $"진행도 {RequiredProgress} 이상이 필요합니다. (현재 {ProgressManager.Progress})";
            UIManager.ClaimPopUp("진입 불가", message, "확인");
            return;
        }

        WaveManager wave = GameManager.Instance != null ? GameManager.Instance.Wave : null;
        BattleManager battle = BattleManager.Instance;
        if (wave == null || battle == null || battle.IsBattleActive)
            return;

        StageMapEntry settings = wave.GetMap(this);
        if (settings == null || settings.stageId < 0 || settings.waves == null || settings.waves.Length == 0
            || System.Array.Exists(settings.waves, item => item == null))
        {
            Debug.LogError("[WaveSetter] Stage Map Editor에서 이 버튼의 Tilemap과 웨이브를 설정해주세요.");
            return;
        }

        StageButtonImageController button = GetComponent<StageButtonImageController>();
        if (button != null) button.ClearHover();
        PlacementController.RemoveAllObject();
        HideStageScenarios();
        onStageEntered.Invoke();
        TileMapManager.SelectMap(settings.tilemap);

        // 종료 시 tilemap 참조를 초기화하므로 선택한 맵 루트를 미리 보관한다.
        var selectedTilemap = PlacementManager.Instance != null ? PlacementManager.Instance.tilemap : null;
        PlacementController selectedMap = selectedTilemap != null
            ? selectedTilemap.GetComponentInParent<PlacementController>(true) : null;
        battleMapRoot = selectedMap != null ? selectedMap.gameObject : null;
        battleTilemap = selectedTilemap;
        WaveLoader loader = WaveLoader.Instance ?? wave.gameObject.AddComponent<WaveLoader>();
        if (PlacementManager.Instance == null || PlacementManager.Instance.tilemap == null)
        {
            Debug.LogError("[WaveSetter] 맵에디터의 Tilemap과 PlacementManager를 확인해주세요.");
            CancelStageEntry();
            return;
        }

        if (selectedTilemap != settings.tilemap || !wave.SelectMap(selectedTilemap))
        {
            CancelStageEntry();
            return;
        }
        postBattleScenario = settings.postBattleScenario;
        battle.BeginBattle(settings.GetStageId(this), this);
        if (settings.mapData != null)
        {
            StageMapLoader mapLoader = selectedTilemap.GetComponent<StageMapLoader>()
                ?? selectedTilemap.gameObject.AddComponent<StageMapLoader>();
            if (!mapLoader.LoadMapData()) { CancelStageEntry(); return; }
        }
        loader.StartFirstWave();
    }

    private void CancelStageEntry()
    {
        PlacementController.RemoveAllObject();
        ReturnToWorld();
    }

    private void HideStageScenarios()
    {
        if (scenarioScreen == null) return;
        WaveManager manager = GameManager.Instance != null ? GameManager.Instance.Wave : null;
        if (manager != null) foreach (StageMapEntry map in manager.Maps)
        {
            GameObject scenario = map != null ? map.postBattleScenario : null;
            if (scenario != null && scenario != scenarioScreen
                && scenario.transform.IsChildOf(scenarioScreen.transform))
                scenario.SetActive(false);
        }
        // 기존 UnityEvent의 시나리오 연결을 사용해 배경/공통 UI는 유지하고 대화만 닫는다.
        foreach (WaveSetter stage in FindObjectsByType<WaveSetter>(
            FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (stage.scenarioScreen != scenarioScreen) continue;
            for (int i = 0; i < stage.onStageEntered.GetPersistentEventCount(); i++)
            {
                if (stage.onStageEntered.GetPersistentMethodName(i) != nameof(GameObject.SetActive)) continue;
                GameObject scenario = stage.onStageEntered.GetPersistentTarget(i) as GameObject;
                if (scenario != null && scenario != scenarioScreen
                    && scenario.transform.IsChildOf(scenarioScreen.transform))
                    scenario.SetActive(false);
            }
        }
    }

    public bool ShowPostBattleScenario()
    {
        if (postBattleScenario == null) return false;

        CloseBattleView();
        HideStageScenarios();
        postBattleScenario.SetActive(false);

        if (worldScreen != null) worldScreen.SetActive(false);
        if (scenarioScreen != null) scenarioScreen.SetActive(true);
        postBattleScenario.SetActive(true);
        return true;
    }

    private void CloseBattleView()
    {
        // 배치 화면/전투 화면과 맵은 서로 다른 프리팹에 있으므로 모두 닫는다.
        UIManager.ClaimCloseUI(UIType.CharacterSelect);
        UIManager.ClaimCloseUI(UIType.Stage);
        UIManager.ClaimCloseUI(UIType.Menu);
        if (battleTilemap != null) battleTilemap.gameObject.SetActive(false);
        battleTilemap = null;
        if (battleMapRoot != null) battleMapRoot.SetActive(false);
        battleMapRoot = null;

        if (ModeManager.Instance != null)
            ModeManager.Instance.ChangeMode(ModeManager.GameMode.None);
    }

    public void ReturnToWorld()
    {
        CloseBattleView();
        if (postBattleScenario != null) postBattleScenario.SetActive(false);
        postBattleScenario = null;
        HideStageScenarios();
        if (scenarioScreen != null) scenarioScreen.SetActive(false);
        if (worldScreen != null) worldScreen.SetActive(true);
    }
}

