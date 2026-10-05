using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button), typeof(WaveSetter))]
public class StageButtonImageController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
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
    [Tooltip("이 값 이상일 때 스테이지 버튼을 활성화하고 전투 진입을 허용합니다. 0이면 진행도 제한이 없습니다.")]
    [SerializeField, Min(0)] private int requredProgress;
    // 기존 직렬화 이름을 유지해 Scene/Prefab에 입력한 값을 그대로 사용한다.
    public int RequiredProgress => Mathf.Max(0, requredProgress);

    private Transform hoverCircle;
    private Transform hoverBox;

    private void Awake()
    {
        if (stage == null) stage = GetComponent<WaveSetter>();
        if (stageButton == null) stageButton = GetComponent<Button>();
        if (targetImage == null) targetImage = GetComponent<Image>();
        if (unclearedSprite == null && targetImage != null) unclearedSprite = targetImage.sprite;
        hoverCircle = transform.Find("Circle");
        hoverBox = transform.Find("Box");

        if (stageButton != null && stageButton.transition == Selectable.Transition.Animation)
        {
            // 클릭/키보드 선택은 Hover가 아니다. 기존 Normal/Highlighted 컨트롤러를 사용한다.
            AnimationTriggers triggers = stageButton.animationTriggers;
            triggers.pressedTrigger = triggers.normalTrigger;
            triggers.selectedTrigger = triggers.normalTrigger;
            triggers.disabledTrigger = triggers.normalTrigger;
        }
    }

    private void OnEnable()
    {
        ProgressManager.OnProgressChanged += Refresh;
        ClearHover();
        Refresh();
    }

    private void OnDisable()
    {
        ProgressManager.OnProgressChanged -= Refresh;
        ClearHover();
    }

    public void OnPointerEnter(PointerEventData eventData) => ClearSelection();

    public void OnPointerExit(PointerEventData eventData) => ClearHover();

    private void ClearSelection()
    {
        EventSystem events = EventSystem.current;
        if (events != null && !events.alreadySelecting && events.currentSelectedGameObject == gameObject)
            events.SetSelectedGameObject(null);
    }

    public void ClearHover()
    {
        ClearSelection();
        Animator animator = stageButton != null ? stageButton.animator : null;
        if (animator != null && animator.isActiveAndEnabled && animator.runtimeAnimatorController != null)
        {
            string normal = stageButton.animationTriggers.normalTrigger;
            bool hasNormal = false;
            foreach (AnimatorControllerParameter parameter in animator.parameters)
            {
                if (parameter.type != AnimatorControllerParameterType.Trigger) continue;
                animator.ResetTrigger(parameter.nameHash);
                if (parameter.name == normal) hasNormal = true;
            }
            if (hasNormal)
            {
                animator.SetTrigger(normal);
                animator.Update(0f);
            }
        }
         
        if (hoverBox != null) hoverBox.gameObject.SetActive(false);
    }

    public void Refresh()
    {
        if (stage == null) return;
        bool unlocked = stage.CanEnterStage;
        if (stageButton != null) stageButton.interactable = unlocked;
        if (!unlocked) ClearHover();
        if (targetImage == null) return;

        bool cleared = ProgressManager.IsStageCleared(stage.StageId);
        // overrideSprite는 기존 하이라이트 애니메이션의 m_Sprite 변경에 덮어씌워지지 않는다.
        targetImage.overrideSprite = !unlocked ? lockedSprite
            : cleared ? clearedSprite : null;
        if (targetImage.overrideSprite == null && unclearedSprite != null)
            targetImage.sprite = unclearedSprite;
        targetImage.color = !unlocked ? lockedColor : cleared ? clearedColor : unclearedColor;
    }

    public void LockedStage()
    {
        targetImage.sprite = lockedSprite;
    }    

}
