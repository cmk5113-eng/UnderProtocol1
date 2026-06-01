using System.Collections.Generic; // ���ο� �Է� �ý��� ���ӽ����̽� �߰�
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TextCore.Text;
using UnityEngine.Tilemaps;

public class PlacementController : UI_CharacterSelectWindows
{
    private Camera mainCamera;
    static List<GameObject> _objects = new List<GameObject>();
    int count => _objects.Count;
    int max = 12 ;
    public TextMeshProUGUI Current;
    public TextMeshProUGUI Max;
    public static GameObject CurrentSkill;

        
    private void RefreshUI()
    {
        if (Current != null) Current.text = count.ToString();
        if (Max != null) Max.text = max.ToString();
    }

    void Start()
    {
        mainCamera = Camera.main;
        if (PlacementManager.Instance.tilemap == null) PlacementManager.Instance.   tilemap = GameObject.FindGameObjectWithTag("MainTile")?.GetComponent<Tilemap>();
       
    }

    void Update()
    {
        RefreshUI();
        // Mouse.current.leftButton.wasPressedThisFrame���� Ŭ�� ����
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            // ���콺 ���� ��ũ�� ��ǥ ��������
            Vector2 mouseScreenPos = Mouse.current.position.ReadValue();

            // ���� ��ǥ�� ��ȯ
            Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, 0));
            mouseWorldPos.z = 1;
             
            // Ÿ�ϸ� �� ��ǥ�� ��ȯ
            Vector3Int clickCellPos = PlacementManager.Instance.tilemap.WorldToCell(mouseWorldPos);
            Vector3Int origin = PlacementManager.Instance.tilemap.cellBounds.min;
            Vector3Int adjustedPos = clickCellPos - origin;

            if (ModeManager.Instance.CurrentMode == GameMode.CharacterSelect)
            {
                if (PlacementManager.Instance.tilemap.HasTile(clickCellPos))
                {
                    if (PlacementManager.currentCharacter != null)
                    {
                        // 1. ���� ���õ� ĳ����(currentCharacter)�� �̸��� ��ġ�ϴ� ������Ʈ�� ������ �˻�
                        // ������ ���� �� (Clone)�� �ٴ� ��Ģ�̶�� currentCharacter.name + "(Clone)"���� ����
                        
                        GameObject target = GameObject.Find(PlacementManager.currentCharacter.name + "(Clone)");
                        if (count >= max)
                        {
                            UIManager.ClaimPopUp("���", "�ο� �ʰ�", "����");
                            PlacementManager.currentCharacter = null;
                            return;
                        }
                        // 2. ��ġ�ϴ� �̸��� ������Ʈ�� �̹� ������ ����
                        if (target != null)
                        {
                            Debug.Log($"[Destroy] ������ �����ϴ� {target.name} ������Ʈ�� �����մϴ�.");
                            ObjectManager.DestroyObject(target);
                            _objects.Remove(target);
                           
                  
                        }

                        

                        Vector3 spawnPos = PlacementManager.Instance.tilemap.GetCellCenterWorld(clickCellPos);
                        StageUIController.Instance.Refresh();
                        GameObject obj = ObjectManager.CreateObject(PlacementManager.currentCharacter, spawnPos);
                        _objects.Add(obj);

                        SelectionManager.ClearSelectedCharacter();// ������ ������Ʈ���� CharacterBase ������Ʈ�� ������ ���� ����
                        
                        if (obj.TryGetComponent<CharacterBase>(out var character))
                        {
                            SelectionManager.SetSelectedCharacter(character);
                        Debug.Log(SelectionManager.selectedCharacter);

                        }
                        else
                        {
                            Debug.LogWarning($"{obj.name}�� CharacterBase ������Ʈ�� �����ϴ�!");
                        }


                        Debug.Log($"[Create] Ÿ�� ��ġ {clickCellPos}�� {PlacementManager.currentCharacter}������Ʈ ���� �Ϸ�");
                        PlacementManager.currentCharacter = null;
                    }
                    else
                    {
                        Debug.Log("ĳ���͸������ϼ���");
                    }
                }
            }

            else if (ModeManager.Instance.CurrentMode == GameMode.Battle)
            {
                Debug.Log("��������");
            }
        }
        
    }

    public static void RemoveAllObject()
    { 
  
            for(int i = _objects.Count - 1; i >= 0; i--)
        {
           ObjectManager.DestroyObject(_objects[i]);
        }
        _objects.Clear();
    }
}