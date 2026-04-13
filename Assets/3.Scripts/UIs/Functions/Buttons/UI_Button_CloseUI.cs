using UnityEngine;

public class UI_Button_CloseUI : MonoBehaviour
{

    [SerializeField] UIType wantType;
    public void Close()
    {
        UIManager.ClaimCloseUI(wantType);
    }
}
