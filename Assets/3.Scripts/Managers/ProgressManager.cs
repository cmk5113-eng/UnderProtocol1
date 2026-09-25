using System.Collections;
using UnityEngine;

public class ProgressManager : ManagerBase
{
    public static ProgressManager Instance;
    public static int Progress = 0;
    protected override IEnumerator OnConnected(GameManager newManager)
    {
        throw new System.NotImplementedException();
    }

    protected override void OnDisconnected()
    {
        throw new System.NotImplementedException();
    }

    private void Awake()
    {
        Instance = this;
    }
    
}
