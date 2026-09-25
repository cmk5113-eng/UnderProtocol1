using TMPro;
using UnityEngine;

public class tempcontroller : MonoBehaviour
{
    public static tempcontroller Instance;

    [SerializeField] private TextMeshProUGUI progressText;

    [SerializeField] private int[] stage1Progress;
    



    private void Awake()
    {
        Instance = this;
    }

    // Start에서 UpdateGoldUI()를 바로 호출하지 않고 
    // SaveManager가 Load()할 때 호출되도록 넘겨줍니다.

    public void addGold()
    {
        ProgressManager.Progress++;
        UpdateProgressUI();
    }

    public void UpdateProgressUI()
    {
        if (progressText != null)
        {
            progressText.text = ProgressManager.Progress.ToString();
        }
    }
}