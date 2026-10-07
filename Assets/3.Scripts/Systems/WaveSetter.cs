using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Tilemaps;

public class WaveSetter : MonoBehaviour
{
    [SerializeField] public int index;
    [Tooltip("맵에 StageMapBinding이 없을 때 별도로 지정한 웨이브 목록 번호를 사용합니다.")]
    [SerializeField] private bool overrideWaveStageIndex;
    [Tooltip("WaveManager 스테이지 목록의 번호입니다. -1은 연결 해제 상태입니다.")]
    [Min(-1)] [SerializeField] private int waveStageIndex;
    [Tooltip("클리어 기록용 고유 번호. 웨이브 구성을 공유해도 서로 다른 스테이지는 다른 번호를 사용합니다.")]
    [SerializeField] private int stageId = -1;
    // 이전 Scene/Prefab의 연결은 보존하되 입장 조건에서는 사용하지 않는다.
    [SerializeField, HideInInspector] private tempcontroller progressController;
    [SerializeField] private GameObject worldScreen;
    [SerializeField] private GameObject scenarioScreen;
    [Tooltip("진행도 검사 통과 후 실행할 기존 화면/맵 설정입니다. 버튼에는 SelectSkillByIndex만 연결합니다.")]
    [SerializeField] private UnityEvent onStageEntered = new UnityEvent();

    private GameObject battleMapRoot;
    private Tilemap battleTilemap;
    private GameObject postBattleScenario;

    public int StageId => stageId >= 0 ? stageId : index;
    public int WaveStageIndex => overrideWaveStageIndex ? waveStageIndex : index;
    public int RequiredProgress
    {
        get
        {
            StageButtonImageController button = GetComponent<StageButtonImageController>();
            return button != null ? button.RequiredProgress : 0;
        }
    }
    public bool CanEnterStage => ProgressManager.Progress >= RequiredProgress;

#if UNITY_EDITOR
    public void EditorSetWaveStageIndex(int value)
    {
        overrideWaveStageIndex = true;
        waveStageIndex = value;
    }
#endif

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
        if (wave == null || battle == null || battle.IsBattleActive || index < 0)
            return;

        StageButtonImageController button = GetComponent<StageButtonImageController>();
        if (button != null) button.ClearHover();
        PlacementController.RemoveAllObject();
        HideStageScenarios();
        onStageEntered.Invoke();

        // 종료 시 tilemap 참조를 초기화하므로 선택한 맵 루트를 미리 보관한다.
        var selectedTilemap = PlacementManager.Instance != null ? PlacementManager.Instance.tilemap : null;
        PlacementController selectedMap = selectedTilemap != null
            ? selectedTilemap.GetComponentInParent<PlacementController>(true) : null;
        battleMapRoot = selectedMap != null ? selectedMap.gameObject : null;
        battleTilemap = selectedTilemap;
        WaveLoader loader = WaveLoader.Instance;
        if (PlacementManager.Instance == null || PlacementManager.Instance.tilemap == null || loader == null)
        {
            Debug.LogError("[WaveSetter] 스테이지의 Tilemap 또는 WaveLoader가 없습니다.");
            CancelStageEntry();
            return;
        }

        // 입장 이벤트가 선택한 새 맵을 읽는다. 이전 맵/공용 Grid의 Binding을 재사용하지 않는다.
        int selectedStageIndex = WaveStageIndex;
        postBattleScenario = null;
        foreach (StageMapBinding binding in selectedTilemap.GetComponentsInParent<StageMapBinding>(true))
        {
            if (!binding.IsBoundTo(selectedTilemap)) continue;
            selectedStageIndex = binding.StageIndex;
            postBattleScenario = binding.PostBattleScenario;
            break;
        }
        if (selectedStageIndex < 0 || selectedStageIndex >= wave.StageCount)
        {
            Debug.LogWarning($"[WaveSetter] Stage Index {selectedStageIndex}가 WaveManager 범위를 벗어났거나 연결 해제 상태입니다.");
            CancelStageEntry();
            return;
        }

        WaveData[] waves = wave.GetStageWaves(selectedStageIndex);
        if (waves == null || waves.Length == 0 || System.Array.Exists(waves, item => item == null))
        {
            Debug.LogWarning($"[WaveSetter] {StageId} 스테이지의 웨이브 설정을 확인해주세요.");
            CancelStageEntry();
            return;
        }

        wave.selectedWaves = waves;
        wave.currentWave = null;
        wave.currentWaveIndex = 0;
        battle.BeginBattle(StageId, this);
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
        foreach (StageMapBinding binding in FindObjectsByType<StageMapBinding>(
            FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            GameObject scenario = binding.PostBattleScenario;
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
