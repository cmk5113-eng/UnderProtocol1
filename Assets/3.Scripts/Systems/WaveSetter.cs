using UnityEngine;
using UnityEngine.Events;

public class WaveSetter : MonoBehaviour
{
    [SerializeField] public int index;
    [Tooltip("클리어 기록용 고유 번호. 웨이브 구성을 공유해도 서로 다른 스테이지는 다른 번호를 사용합니다.")]
    [SerializeField] private int stageId = -1;
    [SerializeField] private tempcontroller progressController;
    [SerializeField] private GameObject worldScreen;
    [SerializeField] private GameObject scenarioScreen;
    [Tooltip("진행도 검사 통과 후 실행할 기존 화면/맵 설정입니다. 버튼에는 SelectSkillByIndex만 연결합니다.")]
    [SerializeField] private UnityEvent onStageEntered = new UnityEvent();

    private GameObject battleMapRoot;

    public int StageId => stageId >= 0 ? stageId : index;
    public bool CanEnterStage => progressController != null && progressController.CanEnterStage(index);

    public void SelectSkillByIndex()
    {
        if (!CanEnterStage)
        {
            string message = progressController != null
                && progressController.TryGetRequiredProgress(index, out int required)
                ? $"진행도 {required} 이상이 필요합니다. (현재 {ProgressManager.Progress})"
                : "스테이지의 필요 진행도를 설정해주세요.";
            UIManager.ClaimPopUp("진입 불가", message, "확인");
            return;
        }

        WaveManager wave = GameManager.Instance != null ? GameManager.Instance.Wave : null;
        BattleManager battle = BattleManager.Instance;
        if (wave == null || battle == null || battle.IsBattleActive
            || index < 0 || index >= wave.StageWaveIndex.Count)
            return;

        WaveData[] waves = wave.StageWaveIndex[index];
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
