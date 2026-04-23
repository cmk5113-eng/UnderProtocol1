using System.Collections;
using UnityEngine;



public enum GameMode
{ None, Title, CharacterSelect, _Length
}
public class ModeManager : ManagerBase
{
    protected override IEnumerator OnConnected(GameManager newManager)
    {
        return null;
    }

    public GameMode CurrentMode { get; private set; }

    public void ChangeMode(GameMode wantMode)
    {

        if (CurrentMode == wantMode) return;

        CurrentMode = wantMode;
    }
    protected override void OnDisconnected()
    {

    }
}
