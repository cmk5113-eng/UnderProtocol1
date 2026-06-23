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
    public Image[] ActiveSkill = new Image[3];
    public Image[] PassiveSkill = new Image[3];
    public Image[] UniqueSkill = new Image[4];



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

        if (nameText) nameText.text = $": {data.characterName}";
        if (jobText) jobText.text = $": {data.job}";
        if (elementText) elementText.text = $": {data.element}";
        if (portrait) portrait.sprite = data.Portrait;

        if (UniqueSkill[0]) UniqueSkill[0].sprite = data.staticpassive.PassiveIcon;
        if (UniqueSkill[1]) UniqueSkill[1].sprite = data.normalSkill.NormalIcon;
        if (UniqueSkill[2]) UniqueSkill[2].sprite = data.linkSkill.LinkIcon;
        if (UniqueSkill[3]) UniqueSkill[3].sprite = data.ultimateSkill.UltimateIcon;

        if (ActiveSkill[0]) ActiveSkill[0].sprite = data.active[0].Icon;
        if (ActiveSkill[1]) ActiveSkill[1].sprite = data.active[1].Icon;
        if (ActiveSkill[2]) ActiveSkill[2].sprite = data.pasive[0].PassiveIcon;
        if (PassiveSkill[0]) PassiveSkill[0].sprite = data.pasive[1].PassiveIcon;
        if (PassiveSkill[1]) PassiveSkill[1].sprite = data.pasive[2].PassiveIcon;
        if (PassiveSkill[2]) PassiveSkill[2].sprite = data.pasive[3].PassiveIcon;

}

    public CharacterData GetCurrentCharacter()
    {
        return characterList[currentIndex];
    }
}