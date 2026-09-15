using TMPro;
using UnityEngine;

public class tempcontroller : MonoBehaviour
{
    public static tempcontroller Instance;

    [SerializeField] private TextMeshProUGUI goldtext;

    public int currentGold = 0;

    private void Awake()
    {
        Instance = this;
    }

    // Start에서 UpdateGoldUI()를 바로 호출하지 않고 
    // SaveManager가 Load()할 때 호출되도록 넘겨줍니다.

    public void addGold()
    {
        currentGold++;
        UpdateGoldUI();
    }

    public void UpdateGoldUI()
    {
        if (goldtext != null)
        {
            goldtext.text = currentGold.ToString();
        }
    }
}