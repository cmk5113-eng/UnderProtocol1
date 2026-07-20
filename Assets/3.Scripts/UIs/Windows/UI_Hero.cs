    using System.Collections.Generic;
    using TMPro;
    using UnityEngine;
    using UnityEngine.UI;

    public class UI_Hero : UI_ScreenBase
    {
        [Header("Character Data List")]
        public List<CharacterData> characterList = new List<CharacterData>();

        public int currentIndex = 0;

        [Header("UI References")]
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI jobText;
        public TextMeshProUGUI elementText;
        public TextMeshProUGUI DialogText;
        public TextMeshProUGUI DiscriptText;

        public Image portrait;
        public Button nextButton;

        public Image[] ActiveSkill = new Image[2];
        public Image[] PassiveSkill = new Image[4];
        public Image[] UniqueSkill = new Image[4];

        private void Start()
        {
            if (nextButton != null)
                nextButton.onClick.AddListener(ShowNextCharacter);

            UpdateUI();
        }
    
        public void ShowNextCharacter()
        {
            if (characterList == null || characterList.Count == 0)
                return;

            currentIndex = (currentIndex + 1) % characterList.Count;

            UpdateUI();
        Debug.Log($"현재케릭터넘버 : {characterList.Count},{currentIndex}");
        
        }

        public CharacterData GetCurrentCharacter()
        {
            if (characterList == null || characterList.Count == 0)
                return null;

            return characterList[currentIndex];
        }

        public void RefreshUI()
        {
            UpdateUI();
        }

        private void UpdateUI()
        {
            CharacterData data = GetCurrentCharacter();

            if (data == null)
                return;

            if (nameText) nameText.text = data.characterName;
            if (jobText) jobText.text = data.job.ToString();
            if (elementText) elementText.text = data.element.ToString();
            if (portrait) portrait.sprite = data.Portrait;

            if (UniqueSkill[0]) UniqueSkill[0].sprite = data.staticpassive?.icon;
            if (UniqueSkill[1]) UniqueSkill[1].sprite = data.normalSkill?.icon;
            if (UniqueSkill[2]) UniqueSkill[2].sprite = data.linkSkill?.icon;
            if (UniqueSkill[3]) UniqueSkill[3].sprite = data.ultimateSkill?.icon;

            if (ActiveSkill[0]) ActiveSkill[0].sprite = data.active[0]?.icon;
            if (ActiveSkill[1]) ActiveSkill[1].sprite = data.active[1]?.icon;
            if (PassiveSkill[0]) PassiveSkill[0].sprite = data.passive[0]?.icon;

            if (PassiveSkill[1]) PassiveSkill[1].sprite = data.passive[1]?.icon;
            if (PassiveSkill[2]) PassiveSkill[2].sprite = data.passive[2]?.icon;
            if (PassiveSkill[3]) PassiveSkill[3].sprite = data.passive[3]?.icon;

        Debug.Log($"실제 UI 이미지 적용 확인: {data.characterName}의 {ActiveSkill[0].sprite?.name}");
    }
    }