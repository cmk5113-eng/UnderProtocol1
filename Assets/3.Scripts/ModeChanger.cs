
using UnityEngine;

public class ModeChanger : MonoBehaviour // ����� MonoBehaviour�� ����
{
    public void GoToTitle()
    {
        // ��¥ �Ŵ���(Instance)���� ��带 �ٲٶ�� ��ŵ�ϴ�.
        ModeManager.Instance.ChangeMode(ModeManager.GameMode.Title);
    }

    public void GoToCharacterSelect()
    {
        ModeManager.Instance.ChangeMode(ModeManager.GameMode.CharacterSelect);
    }

    public void GoToBattle()
    {
        ModeManager.Instance.ChangeMode(ModeManager.GameMode.Battle);
    }
}