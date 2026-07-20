using System.Collections;
using UnityEngine;





    // ... ���� �ڵ�

public class ModeManager : ManagerBase
{
    public enum GameMode
    {
        None, Title, Battle, CharacterSelect, Movement, UseSkill, EnemyTurn, _Length
    }

    public static ModeManager Instance { get;  set; }
private void Awake()
{
    // �̱��� �ʱ�ȭ
    if (Instance == null) Instance = this;
    else Destroy(gameObject);
}
public GameMode CurrentMode { get;  set; }
    protected override IEnumerator OnConnected(GameManager newManager)
    {
        return null;
    }


    public void ChangeMode(GameMode wantMode)
    {

        if (CurrentMode == wantMode) return;

        CurrentMode = wantMode;
    }
    protected override void OnDisconnected()
    {

    }
}
