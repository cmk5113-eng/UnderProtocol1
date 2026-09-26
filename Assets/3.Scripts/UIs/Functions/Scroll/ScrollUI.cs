using UnityEngine;
using UnityEngine.UI;

public class ScrollUI : MonoBehaviour
{
    public static ScrollUI Instance;
    [SerializeField] public Scrollbar HPscrollbar;
    [SerializeField] public Scrollbar GGscrollbar;


    void Awake()

    {
        Instance = this;
        ResetValue();
    }

    public void ResetValue()
    { 
        if (HPscrollbar != null) HPscrollbar.value = 1f;
        if (GGscrollbar != null) GGscrollbar.value = 0;
    }


    public void SubValue(float value)
    {
        HPscrollbar.value -= value;
    }
    public void PlusValue(float value)
    {
        HPscrollbar.value += value;
    }

    public void PlusGaugevalue(float value)
    { 
        GGscrollbar.value += value;
    }
    public void SubGaugeValue(float value)
    {
        GGscrollbar.value -= value;

    }
    
}
