using UnityEngine;
using UnityEngine.Events;

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

        int selectedStageIndex = WaveStageIndex;
        var activeTilemap = PlacementManager.Instance != null ? PlacementManager.Instance.tilemap : null;
        StageMapBinding mapBinding = activeTilemap != null
            ? activeTilemap.GetComponentInParent<StageMapBinding>(true) : null;
        if (mapBinding != null) selectedStageIndex = mapBinding.StageIndex;

        if (selectedStageIndex < 0 || selectedStageIndex >= wave.StageCount)
        {
            Debug.LogWarning($"[WaveSetter] Stage Index {selectedStageIndex}가 WaveManager 범위를 벗어났거나 연결 해제 상태입니다.");
            return;
        }

        WaveData[] waves = wave.GetStageWaves(selectedStageIndex);
        if (waves == null || waves.Length == 0 || System.Array.Exists(waves, item => item == null))
        {
            Debug.LogWarning($"[WaveSetter] {StageId} 스테이지의 웨이브 설정을 확인해주세요.");
            return;
        }

        PlacementController.RemoveAllObject();
        onStageEntered.Invoke();

        // 종료 시 tilemap 참조를 초기화하므로 선택한 맵 루트를 미리 보관한다.
        var selectedTilemap = PlacementManager.Instance != null ? PlacementManager.Instance.tilemap : null;
        PlacementController selectedMap = selectedTilemap != null
            ? selectedTilemap.GetComponentInParent<PlacementController>(true) : null;
        battleMapRoot = selectedMap != null ? selectedMap.gameObject : null;
        WaveLoader loader = WaveLoader.Instance;
        if (PlacementManager.Instance == null || PlacementManager.Instance.tilemap == null || loader == null)
        {
            Debug.LogError("[WaveSetter] 스테이지의 Tilemap 또는 WaveLoader가 없습니다.");
            ReturnToWorld();
            return;
        }

        wave.selectedWaves = waves;
        wave.currentWave = null;
        wave.currentWaveIndex = 0;
        battle.BeginBattle(StageId, this);
        loader.StartFirstWave();
    }

    public void ReturnToWorld()
    {
        // 배치 화면/전투 화면과 맵은 서로 다른 프리팹에 있으므로 모두 닫는다.
        UIManager.ClaimCloseUI(UIType.CharacterSelect);
        UIManager.ClaimCloseUI(UIType.Stage);
        UIManager.ClaimCloseUI(UIType.Menu);
        if (battleMapRoot != null) battleMapRoot.SetActive(false);
        battleMapRoot = null;

        if (scenarioScreen != null) scenarioScreen.SetActive(false);
        if (worldScreen != null) worldScreen.SetActive(true);
        if (ModeManager.Instance != null)
            ModeManager.Instance.ChangeMode(ModeManager.GameMode.None);
    }
}
