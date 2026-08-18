using UnityEngine;
using UnityEngine.UI;

public class ScrollUI : MonoBehaviour
{
    public static ScrollUI Instance;
    [SerializeField] private Scrollbar HPscrollbar;
    [SerializeField] public Scrollbar GGscrollbar;


    void Start()

    {
        Instance = this;
        HPscrollbar.value = 1f;
        GGscrollbar.value = 0;
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
