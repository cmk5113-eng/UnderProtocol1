using UnityEngine;

public class UI_Button_ToggleUI : MonoBehaviour
{

    [SerializeField] UIType wantType;
    public void Toggle()
    {
        UIManager.ClaimToggleUI(wantType);
    }
}
