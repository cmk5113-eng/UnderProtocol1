
using UnityEngine;

public class ModeChanger : MonoBehaviour // ����� MonoBehaviour�� ����
{
    public void GoToTitle()
    {
        // ��¥ �Ŵ���(Instance)���� ��带 �ٲٶ�� ��ŵ�ϴ�.
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