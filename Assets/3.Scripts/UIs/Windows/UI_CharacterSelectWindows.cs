using TMPro;
using UnityEngine;

public class UI_CharacterSelectWindows : UI_ScreenBase
{
    // ������ ũ�⳪ �߿䵵 ������ ��ġ (���� ������Ʈ -> �⺻ �ڷ���)
 
    // UI �ؽ�Ʈ�� ������Ʈ�ϴ� ���� �Լ�

    private void OnEnable()
    {
        // â�� ���� �� �����ϰ� ���� �ʱ�ȭ ����
        // ��: �Ŵ������� ���� ī��Ʈ ���� �����ͼ� ����
        // ������ �ϴ� �ӽ÷� 0, 12�� �־ ������ ���Կ�.

    }
  

    // 1. �ܼ��� ī��Ʈ�� 1 �ø��� ���� �� ȣ��
   

    // 3. �ʱ�ȭ �� ���
   

    public void Toggle() => gameObject.SetActive(!IsOpen);




    public static UI_CharacterSelectWindows Instance { get; private set; }

    // �������� �����Ͻ� '���� ĳ����' ���� (�̸����� ����)
 
    public void ChangeCurrentCharacter(GameObject selectedPrefab)
    {
        // 1. ���޹��� �������� ���� ĳ���ͷ� ���
        PlacementManager.currentCharacter = selectedPrefab;
        

        // 2. ����� ��ϵǾ����� Ȯ�� �α�
        if (PlacementManager.currentCharacter != null)
        {
            Debug.Log($"���õ� ĳ���Ͱ� '{PlacementManager.currentCharacter.name}'(��)�� ��ϵǾ����ϴ�.");
        }

        // 3. UI ������Ʈ �� �ļ� �۾�
    }

    //[Header("��ȯ ��ġ ����")]
    //[SerializeField] private Transform spawnParent;

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
}