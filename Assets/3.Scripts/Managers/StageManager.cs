using System.Collections;
using UnityEngine;

public class StageManager : ManagerBase
{
    public enum Stage { Stage_01, Stage_02 }
    public Stage currentStage;
    public StageManager Instance;

    protected override IEnumerator OnConnected(GameManager newManager)
    {
        Instance = this;
        return null;

    }

    protected override void OnDisconnected()
    {
     
    }

    

    public void InStage()
    { }
    public void OutStage()
    { }
}
