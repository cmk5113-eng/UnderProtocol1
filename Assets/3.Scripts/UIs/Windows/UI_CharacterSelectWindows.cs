using System.Xml.Linq;
using TMPro;
using UnityEngine;

public class UI_CharacterSelectWindows : UI_ScreenBase
{

    private void OnEnable()
    {
    }

    public void Toggle() => gameObject.SetActive(!IsOpen);
    public static UI_CharacterSelectWindows Instance { get; private set; }
    public int currentCount = 0;
    public int maxCount = 12;
    [SerializeField] private TextMeshProUGUI currentText;
    [SerializeField] private TextMeshProUGUI maxText;
    public void ChangeModeToCharacterSelect()
    {
        ModeManager.Instance.CurrentMode = ModeManager.GameMode.CharacterSelect;
    }
    public void ChangeCurrentCharacter(GameObject selectedPrefab)
    {
       if(ModeManager.Instance.CurrentMode != ModeManager.GameMode.CharacterSelect)
        {
            return;
        }
        // 1. ���޹��� �������� ���� ĳ���ͷ� ���
        SelectionManager.selectedPrefab = selectedPrefab;

        int index = SelectionManager.selectedPrefab?.name switch
        {
            "Beak" => 0,
            "Choi" => 1,
            "Do" => 2,
            "Ha" => 3,
            "Jo" => 4,
            "Kang" => 5,
            "Lee" => 6,
            "Min" => 7,
            "Namgung" => 8,
            "Pyo" => 9,
            "Ryu" => 10,
            "Seo" => 11,
            _ => -1
        };

        if (index >= 0 && index < SelectionManager.Instance.characterBases.Length)
        {
            SelectionManager.characterBase = SelectionManager.Instance.characterBases[index];
        }
        if (index >= 0 && index < SelectionManager.Instance.characterDatas.Length)
        {
            SelectionManager.characterData = SelectionManager.Instance.characterDatas[index];
        }
        StageUIController.Instance.Refresh();

        // 3. UI ������Ʈ �� �ļ� �۾�
    }

    void Awake()
    {

        Instance = this;
        // �⺻���� null�̸� Error�� �� �� ������ �� ���̶� �־��ݴϴ�.
        //if (string.IsNullOrEmpty(currentcharacter)) currentcharacter = "";
    }

    // ��ư���� �� �Լ��� ȣ���ؼ� ���� �ٲߴϴ�.



    //// '����' ��ư�� ������ �� ����� �Լ�
    public void OnClickSpawn()
    {
        //if (string.IsNullOrEmpty(currentcharacter))
        {
            Debug.LogWarning("���� ĳ���� ��ư�� Ŭ���ؼ� �������ּ���!");
            return;
        }

        // ObjectManager���� ���� ������ ��� �̸����� ������ ��û�մϴ�.
        //ObjectManager.CreateObject(currentcharacter, spawnParent);
    }
    public void UpdateCountUI()
    {
        if (currentText != null) currentText.text = currentCount.ToString();
        if (maxText != null) maxText.text = maxCount.ToString();
    }

    public void AddCount()
    {
        if (currentCount < maxCount)
        {
            currentCount++;
            UpdateCountUI();
          
        }
    }

    /// <summary>
    /// 캐릭터가 해제(회수)되었을 때 호출하여 카운트를 1 내리는 함수
    /// </summary>
    public void RemoveCount()
    {
        if (currentCount > 0)
        {
            currentCount = 0;
            UpdateCountUI();
            Debug.Log($"[UI_Count] 캐릭터가 해제되어 카운트가 감소했습니다. ({currentCount} / {maxCount})");
        }
    }

}