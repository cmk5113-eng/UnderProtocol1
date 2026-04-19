using UnityEngine;


public delegate void PoolEnqueueEvent(GameObject target);
public delegate void PoolDequeueEvent(GameObject target);

public class PooledObject : MonoBehaviour       
{
    public event PoolEnqueueEvent OnEnqueueEvent;    
    public event PoolDequeueEvent OnDequeueEvent;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void OnEnqueue()
    {
        if(OnEnqueueEvent != null) OnEnqueueEvent.Invoke(gameObject);
        else Destroy(gameObject);
    }

    // Update is called once per frame
    public void OnDequeue()
    {
        OnDequeueEvent?.Invoke(gameObject);
    }
}
