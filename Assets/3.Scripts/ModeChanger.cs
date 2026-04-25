
using UnityEngine;

public class ModeChanger : MonoBehaviour // 상속을 MonoBehaviour로 변경
{
    public void GoToTitle()
    {
        // 진짜 매니저(Instance)에게 모드를 바꾸라고 시킵니다.
        ModeManager.Instance.ChangeMode(GameMode.Title);
    }

    public void GoToCharacterSelect()
    {
        ModeManager.Instance.ChangeMode(GameMode.CharacterSelect);
    }

    public void GoToBattle()
    {
        ModeManager.Instance.ChangeMode(GameMode.Battle);
    }
}