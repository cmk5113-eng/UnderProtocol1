using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;
using static HeroButton;

public class SkillLoadButton : MonoBehaviour
{
    public enum SkillGroupType {
        Active1,
        Active2,
        Passive1,
        Passive2,
        Passive3,
        Passive4
    }

    [Header("설정")]
    [SerializeField] public SkillGroupType targetSkillGroup;
   
    
    private Button myButton;

   
    public void UpdateUI()
    { }

    private void Start()
    {
        myButton = GetComponent<Button>();
        if (myButton != null)
        {
            myButton.onClick.AddListener(OnButtonClicked);
        }
    }

    private void OnButtonClicked()
    {
        // 💡 버튼은 인벤토리를 직접 몰라도 됩니다! 
        // 매니저에게 "이 타입의 스킬 리스트 좀 열고 데이터 채워줘"라고 요청합니다.
        if (CanvasManager.Instance != null)
        {
            CanvasManager.Instance.OpenSkillListWithData(targetSkillGroup);
        }
        else
        {
            Debug.LogError("CanvasManager.Instance를 찾을 수 없습니다. 씬에 CanvasManager가 있나요?");
        }
    }

}