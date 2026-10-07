using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class Dialog1 : MonoBehaviour, IPointerDownHandler
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI ScriptText_dialogue;
    [SerializeField] private TextMeshProUGUI ScriptText_dialoguename;
    [SerializeField] private Image ScriptImage_portrait;
    [Tooltip("미지정 시 현재 시나리오의 next 버튼을 사용합니다.")]
    [SerializeField] private Button skipButton;

    [Header("Dialogue Data")]
    [SerializeField] private string[] dialogue;
    [SerializeField] private string[] dialoguename;
    [SerializeField] private Sprite[] portraits;

    private int dialogue_count;
    private bool dialogueFinished;
    private Button.ButtonClickedEvent originalSkipClick;

    protected virtual void OnEnable()
    {
        dialogue_count = 0;
        dialogueFinished = false;
        ResolveSkipButton();
        if (skipButton != null) skipButton.gameObject.SetActive(false);
        ShowDialogue();
    }

    protected virtual void OnDisable()
    {
        RestoreSkipAction();
    }

    public void OnPointerDown(PointerEventData data)
    {
        if (dialogueFinished) return;

        dialogue_count++;
        if (dialogue == null || dialogue_count >= dialogue.Length)
        {
            CompleteDialogue();
            return;
        }

        ShowDialogue();
    }

    // 전투 후에는 기존 전투 시작 버튼 연결을 잠시 대체하고, 닫힐 때 복원한다.
    public void SetSkipAction(System.Action onSkip)
    {
        ResolveSkipButton();
        if (skipButton == null) return;

        RestoreSkipAction();
        if (onSkip == null) return;

        originalSkipClick = skipButton.onClick;
        skipButton.onClick = new Button.ButtonClickedEvent();
        skipButton.onClick.AddListener(() => onSkip());
    }

    private void RestoreSkipAction()
    {
        if (skipButton != null && originalSkipClick != null)
            skipButton.onClick = originalSkipClick;
        originalSkipClick = null;
    }

    private void ResolveSkipButton()
    {
        if (skipButton != null) return;
        Transform buttonTransform = transform.Find("next");
        if (buttonTransform == null && transform.parent != null)
            buttonTransform = transform.parent.Find("next");
        if (buttonTransform != null)
            skipButton = buttonTransform.GetComponent<Button>();
    }

    private void CompleteDialogue()
    {
        dialogueFinished = true;
        if (skipButton != null) skipButton.gameObject.SetActive(true);
    }

    private void ShowDialogue()
    {
        bool hasDialogue = dialogue != null && dialogue_count < dialogue.Length;
        if (ScriptText_dialogue != null)
            ScriptText_dialogue.text = hasDialogue ? dialogue[dialogue_count] : string.Empty;
        if (ScriptText_dialoguename != null)
            ScriptText_dialoguename.text = hasDialogue && dialoguename != null && dialogue_count < dialoguename.Length
                ? dialoguename[dialogue_count] : string.Empty;
        if (ScriptImage_portrait != null)
            ScriptImage_portrait.sprite = hasDialogue && portraits != null && dialogue_count < portraits.Length
                ? portraits[dialogue_count] : null;

        if (!hasDialogue) CompleteDialogue();
    }
}
