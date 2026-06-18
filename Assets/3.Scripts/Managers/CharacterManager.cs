
using System.Collections.Generic;
using UnityEngine;

using System.Collections;


public class CharacterManager : ManagerBase
{
    protected override IEnumerator OnConnected(GameManager newManager)
    {
        yield return null;
    }

    protected override void OnDisconnected()
    {

    }
}
