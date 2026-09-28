using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class Dialog2 : MonoBehaviour, IPointerDownHandler
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI ScriptText_dialogue;
    [SerializeField] private TextMeshProUGUI ScriptText_dialoguename;
    [SerializeField] private Image ScriptImage_portrait;

    [Header("Dialogue Data")]
    [SerializeField] private string[] dialogue;
    [SerializeField] private string[] dialoguename;
    [SerializeField] private Sprite[] portraits;

    private int dialogue_count = 0;

    private void Start()
    {
        ShowDialogue();
    }

    public void OnPointerDown(PointerEventData data)
    {
        dialogue_count++;

        // 마지막 대사 이후
        if (dialogue_count >= dialogue.Length)
        {
            UIManager.ClaimPopUp("대화종료", "끝났어요스킵하세요", "확인");

            dialogue_count = 0;

            // 필요하면 대화창 종료
            // gameObject.SetActive(false);

            return;
        }

        ShowDialogue();
    }

    private void ShowDialogue()
    {
        ScriptText_dialogue.text = dialogue[dialogue_count];

        ScriptText_dialoguename.text = dialoguename[dialogue_count];

        ScriptImage_portrait.sprite = portraits[dialogue_count];
    }
}