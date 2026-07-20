using UnityEngine;

public class UI_LogInWindows : MonoBehaviour
{
    public TMPro.TMP_InputField Nicknameinput;
    public void MakeUserData()
    {
      DBManager.ClaimMakeUserData(Nicknameinput.text);
    }
}
