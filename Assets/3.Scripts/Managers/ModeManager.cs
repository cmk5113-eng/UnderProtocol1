using System.Collections;
using UnityEngine;



public enum GameMode
{ None, Title, Battle, CharacterSelect, _Length
}


    // ... 기존 코드

public class ModeManager : ManagerBase
{
public static ModeManager Instance { get; private set; }
private void Awake()
{
    // 싱글톤 초기화
    if (Instance == null) Instance = this;
    else Destroy(gameObject);
}
public GameMode CurrentMode { get; private set; }
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
