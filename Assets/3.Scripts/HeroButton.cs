using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HeroButton : MonoBehaviour
{


    public CharacterData CurrentCharacter;
    private Button myButton;


    public void changeskill()
    {
        myButton.onClick.AddListener(UpdateUI);
    }
        public void UpdateUI()
    {

    }
}
