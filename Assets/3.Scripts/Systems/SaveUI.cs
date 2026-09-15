using UnityEngine;

public class SaveUI : MonoBehaviour
{
    public void SaveSlot1()
    {
        Debug.Log("유후! 세이브성공");
        GameManager.Instance.Save.Save(0);
    }

    public void SaveSlot2()
    {
        Debug.Log("유후! 세이브성공");
        GameManager.Instance.Save.Save(1);
    }

    public void SaveSlot3()
    {
        Debug.Log("유후! 세이브성공");
        GameManager.Instance.Save.Save(2);
    }

}
