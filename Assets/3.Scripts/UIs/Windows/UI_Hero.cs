using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_Hero : UI_ScreenBase
{
    [Header("Character Data List")]
    public List<CharacterData> characterList; // 변경

    [Header("UI References")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI jobText;
    public TextMeshProUGUI elementText;
    public Button nextButton;

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

        if (nameText) nameText.text = $"이름: {data.characterName}";
        if (jobText) jobText.text = $"직업: {data.job}";
        if (elementText) elementText.text = $"속성: {data.element}";
    }

    public CharacterData GetCurrentCharacter()
    {
        return characterList[currentIndex];
    }
}