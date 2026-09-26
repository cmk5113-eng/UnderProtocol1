using UnityEngine;

public class LoadUI : MonoBehaviour
{
    public void LoadSlot1()
    {
        Debug.Log("유후! 로드성공");
        Debug.Log($"현재 진행도 : {ProgressManager.Progress}");
        GameManager.Instance.Save.Load(0);
    }

    public void LoadSlot2()
    {
        Debug.Log("유후! 로드성공");

        GameManager.Instance.Save.Load(1);
    }

    public void LoadSlot3()
    {
        Debug.Log("유후! 로드성공");

        GameManager.Instance.Save.Load(2);
    }
}
