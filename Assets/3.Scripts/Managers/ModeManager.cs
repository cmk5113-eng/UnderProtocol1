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

     protected UIBase ChangeMode(GameMode wantMode)
    {
        UIBase result = ChangeMode(wantMode);
        return result;
        
    }
    protected override void OnDisconnected()
    {

    }
}
