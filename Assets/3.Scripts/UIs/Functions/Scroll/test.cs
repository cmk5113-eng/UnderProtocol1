using UnityEngine;
using UnityEngine.UI;

public class Test : MonoBehaviour
{
        public static Test Instance;
        [SerializeField] private Scrollbar scrollbar;
        
        void Start()

        {
        Instance = this;
        scrollbar.value = 1f;
        }

        public void SubValue(float value)
        {
            scrollbar.value -= value;
        }
        public void PlusValue(float value)
        {
            scrollbar.value += value;
        }
    
}
