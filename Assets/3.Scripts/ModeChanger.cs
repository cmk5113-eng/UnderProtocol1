using UnityEngine;

public class ModeChanger:ModeManager
{
    public void GoToTitle()
    {
       ChangeMode(GameMode.Title);
    }

    public void GoToCharacterSelect()
    {
        ChangeMode(GameMode.CharacterSelect);
    }
}
