using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_Hero : UI_ScreenBase
{
    [Header("Character Data List")]
    public List<CharacterData> characterList; // ����

    [Header("UI References")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI jobText;
    public TextMeshProUGUI elementText;
    public TextMeshProUGUI DialogText;
    public TextMeshProUGUI DiscriptText;
    public Image portrait;
    public Button nextButton;
    public Button[] ActiveButton = new Button[3];
    public Button[] PassiveButton = new Button[3];
    public Sprite[] UniqueSkill = new Sprite[4];



    private int currentIndex = 0;

    void Start()
    {
        if (nextButton != null)
            nextButton.onClick.AddListener(ShowNextCharacter);

        UpdateUI();
    }

    public void ShowNextCharacter()
    {
        if (characterList.Count == 0) return;

        currentIndex = (currentIndex + 1) % characterList.Count;
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (characterList.Count <= currentIndex) return;

        var data = characterList[currentIndex];

        if (nameText) nameText.text = $"�̸�: {data.characterName}";
        if (jobText) jobText.text = $"����: {data.job}";
        if (elementText) elementText.text = $"�Ӽ�: {data.element}";
        if (portrait) portrait.sprite = data.Portrait;
        

    }

    public CharacterData GetCurrentCharacter()
    {
        return characterList[currentIndex];
    }
}