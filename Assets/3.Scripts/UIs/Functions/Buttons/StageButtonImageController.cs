using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button), typeof(WaveSetter))]
public class StageButtonImageController : MonoBehaviour
{
    [SerializeField] private WaveSetter stage;
    [SerializeField] private Button stageButton;
    [SerializeField] private Image targetImage;
    [SerializeField] private Sprite unclearedSprite;
    [SerializeField] private Sprite clearedSprite;
    [SerializeField] private Sprite lockedSprite;
    [SerializeField] private Color unclearedColor = Color.white;
    [SerializeField] private Color clearedColor = new Color(0.5f, 1f, 0.7f, 1f);
    [SerializeField] private Color lockedColor = new Color(0.45f, 0.45f, 0.45f, 1f);

    private void Awake()
    {
        if (stage == null) stage = GetComponent<WaveSetter>();
        if (stageButton == null) stageButton = GetComponent<Button>();
        if (targetImage == null) targetImage = GetComponent<Image>();
        if (unclearedSprite == null && targetImage != null) unclearedSprite = targetImage.sprite;
    }

    private void OnEnable()
    {
        ProgressManager.OnProgressChanged += Refresh;
        Refresh();
    }

    private void OnDisable() => ProgressManager.OnProgressChanged -= Refresh;

    public void Refresh()
    {
        if (stage == null) return;
        bool unlocked = stage.CanEnterStage;
        if (stageButton != null) stageButton.interactable = unlocked;
        if (targetImage == null) return;

        bool cleared = ProgressManager.IsStageCleared(stage.StageId);
        // overrideSprite는 기존 하이라이트 애니메이션의 m_Sprite 변경에 덮어씌워지지 않는다.
        targetImage.overrideSprite = !unlocked ? lockedSprite
            : cleared ? clearedSprite : null;
        if (targetImage.overrideSprite == null && unclearedSprite != null)
            targetImage.sprite = unclearedSprite;
        targetImage.color = !unlocked ? lockedColor : cleared ? clearedColor : unclearedColor;
    }
}
