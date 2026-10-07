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

    protected virtual void OnEnable()
    {
        dialogue_count = 0;
        dialogueFinished = false;
        ResolveSkipButton();
        ShowDialogue();
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
        // 버튼을 실제로 눌렀을 때와 같은 인스펙터 연결을 한 번 실행한다.
        if (skipButton != null) skipButton.onClick.Invoke();
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
    }
}
