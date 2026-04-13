
using UnityEngine;

public class UIBase : MonoBehaviour
{
    public virtual void Registration(UIManager manager)
    { 
        
    }

    public virtual void Unregistration(UIManager manager) 
    {

    }
    public GameObject SetChild(GameObject newChild)
    {
        if (!newChild) return null;
        newChild.transform.SetParent(transform);
   
        return OnSetChild(newChild);
        
    }
    protected virtual GameObject OnSetChild(GameObject newChild)
    {
        return newChild;
    }
    public void UnsetChild(GameObject oldChild)
    {
        if (!oldChild) return;
        if(oldChild.transform.parent == transform)
        {
          oldChild.transform.SetParent(null);
        }
        OnUnsetChild(oldChild);

    }
    protected virtual void OnUnsetChild(GameObject oldChild)
    { 
    
    }
}
