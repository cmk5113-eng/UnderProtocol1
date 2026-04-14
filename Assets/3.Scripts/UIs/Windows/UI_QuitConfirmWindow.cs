using Unity.VisualScripting;
using UnityEngine;

public class UI_QuitConfirmWindow : OpenableUIBase

{
    public void Confirm()
    { 
        GameManager.QuitGame();
    }
    public void Cancel()
    {
        UIManager.ClaimCloseUI(UIType.GameQuit);
    }

}