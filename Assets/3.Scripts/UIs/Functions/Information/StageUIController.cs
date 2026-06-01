using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UI;

public class StageUIController : MonoBehaviour
{
    public static StageUIController Instance { get; private set; }
    
    [SerializeField] private Image portrait;
    [SerializeField] private Image skill1;
    [SerializeField] private Image skill2;
    [SerializeField] private Image skill3;
    [SerializeField] private Image unit1;
    [SerializeField] private Image unit2;
    [SerializeField] private Image unit3;
    [SerializeField] private Image unit4;
    [SerializeField] private TMPro.TextMeshProUGUI unit1name;
    [SerializeField] private TMPro.TextMeshProUGUI unit2name;
    [SerializeField] private TMPro.TextMeshProUGUI unit3name;
    [SerializeField] private TMPro.TextMeshProUGUI unit4name;


    private CharacterBase asCharacter;

    List<CharacterBase> unitOnStage = new List<CharacterBase>();

    private void Awake()
    {
        Instance = this;
    }
    public void Refresh()
    {
        if (PlacementManager.currentCharacter == null)
        {
            Debug.LogWarning("���� ĳ���Ͱ� �����ϴ�.");
            return;
        }

        asCharacter = PlacementManager.currentCharacter.GetComponent<CharacterBase>();

        if (asCharacter == null)
        {
            Debug.LogWarning("CharacterBase ������Ʈ�� �����ϴ�.");
            return;
        }


        portrait.sprite = asCharacter.portrait;
        skill1.sprite = asCharacter.skill1?.Image;
        skill2.sprite = asCharacter.skill2?.Image;
        skill3.sprite = asCharacter.skill3?.Image;
        Summon();
    }
    public void Summon()
    {

        
        unitOnStage.Add(asCharacter);
        resetunit();
    }
    public void UnSummon()
    {
        unitOnStage.Remove(asCharacter);
        resetunit() ;
    }
        public void resetunit()
    {
        // 1��° ���� (0�� �ε���)
        if (unitOnStage.Count > 0)
        {
            unit1.gameObject.SetActive(true);
            unit1.sprite = unitOnStage[0].portrait;
            unit1name.SetText(unitOnStage[0].Name);
        }
        else
        {
            unit1.gameObject.SetActive(false); // Ȥ�� �⺻ �̹��� ó��
            unit1name.SetText("");
        }

        // 2��° ���� (1�� �ε���)
        if (unitOnStage.Count > 1)
        {
            unit2.gameObject.SetActive(true);
            unit2.sprite = unitOnStage[1].portrait;
            unit2name.SetText(unitOnStage[1].Name);
        }
        else
        {
            unit2.gameObject.SetActive(false);
            unit2name.SetText("");
        }

        // 3��° ���� (2�� �ε���)
        if (unitOnStage.Count > 2)
        {
            unit3.gameObject.SetActive(true);
            unit3.sprite = unitOnStage[2].portrait;
            unit3name.SetText(unitOnStage[2].Name);
        }
        else
        {
            unit3.gameObject.SetActive(false);
            unit3name.SetText("");
        }

        // 4��° ���� (3�� �ε���)
        if (unitOnStage.Count > 3)
        {
            unit4.gameObject.SetActive(true);
            unit4.sprite = unitOnStage[3].portrait;
            unit4name.SetText(unitOnStage[3].Name);
        }
        else
        {
            unit4.gameObject.SetActive(false);
            unit4name.SetText("");
        }
    }
}

