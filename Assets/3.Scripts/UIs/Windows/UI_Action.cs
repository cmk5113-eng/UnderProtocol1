using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_Action : UI_ScreenBase  
{
    // selectedCharacter가 누구인가?
    //버튼1 => selectedcharacter의 스킬1
    //버튼2 => selectedcharacter의 스킬2
    //버튼3 => selectedcharacter의 스킬3
    //버튼4 => selectedcharacter의 스킬4
    [Header("UI References")]
    public GameObject skillPanel;      // 버튼들을 담고 있는 부모 패널
    public TextMeshProUGUI skillText1; // 버튼에 들어있는 텍스트 4개 (배열)
    public TextMeshProUGUI skillText2; // 버튼에 들어있는 텍스트 4개 (배열)
    public TextMeshProUGUI skillText3; // 버튼에 들어있는 텍스트 4개 (배열)
    public TextMeshProUGUI skillText4; // 버튼에 들어있는 텍스트 4개 (배열)
    
    
    public Button skillButton1;
    public Button skillButton2;
    public Button skillButton3;
    public Button skillButton4;


    public void showskills()
    {
        CharacterBase selected = SelectionManager.selectedCharacter;
        if (selected == null)
        {
            if (skillPanel.activeSelf) skillPanel.SetActive(false);
            return;
        }
        // 3. 캐릭터가 있으면 패널 켜기
        if (!skillPanel.activeSelf) skillPanel.SetActive(true);

        // 4. 각 스킬 데이터를 UI에 할당 (null 체크 포함)
        SetSkillSlot(selected.skill1, skillText1, skillButton1);
        SetSkillSlot(selected.skill2, skillText2, skillButton2);
        SetSkillSlot(selected.skill3, skillText3, skillButton3);
        SetSkillSlot(selected.skill4, skillText4, skillButton4);
    }

    // 반복되는 코드를 줄이기 위한 보조 함수
    private void SetSkillSlot(SkillData data, TextMeshProUGUI textMesh, Button button)
    {
        if (data != null)
        {
            textMesh.text = data.skillName; // 스킬 이름 표시
            button.interactable = true;    // 버튼 활성화
        }
        else
        {
            textMesh.text = "---";         // 스킬 없음 표시
            button.interactable = false;   // 버튼 비활성화
        }
    }
}