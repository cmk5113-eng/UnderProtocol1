using UnityEngine;

public abstract 
    class ManagerBase : MonoBehaviour
{
    GameManager _connectedManager;
    public void Connect(GameManager newManager)
    { 
        if(_connectedManager !=null) Disconnect();
     _connectedManager = newManager;
        OnConnected(newManager);
    }
    public void Disconnect()
    { 
         _connectedManager = null;
        OnDisConnected();
    }
    protected abstract void OnConnected(GameManager newManager);
    
    protected abstract void OnDisConnected();
    
}
