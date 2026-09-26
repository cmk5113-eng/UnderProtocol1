using TMPro;
using UnityEngine;

public class tempcontroller : MonoBehaviour
{
    public static tempcontroller Instance;

    [SerializeField] private TextMeshProUGUI progressText;
    [Tooltip("0번은 첫 스테이지의 필요 진행도입니다. 값 이상일 때 입장할 수 있습니다.")]
    [SerializeField] private int[] stage1Progress = { 0, 1, 2, 3, 4 };

    private void Awake() => Instance = this;

    private void OnEnable()
    {
        ProgressManager.OnProgressChanged += UpdateProgressUI;
        UpdateProgressUI();
    }

    private void OnDisable()
    {
        ProgressManager.OnProgressChanged -= UpdateProgressUI;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public bool TryGetRequiredProgress(int stageIndex, out int requiredProgress)
    {
        requiredProgress = 0;
        if (stage1Progress == null || stageIndex < 0 || stageIndex >= stage1Progress.Length)
            return false;

        requiredProgress = Mathf.Max(0, stage1Progress[stageIndex]);
        return true;
    }

    public bool CanEnterStage(int stageIndex)
    {
        return TryGetRequiredProgress(stageIndex, out int requiredProgress)
            && ProgressManager.Progress >= requiredProgress;
    }

    // 기존 씬의 테스트 버튼 연결을 유지한다.
    public void addGold() => ProgressManager.Progress++;

    public void UpdateProgressUI()
    {
        if (progressText != null)
            progressText.text = ProgressManager.Progress.ToString();
    }
}
