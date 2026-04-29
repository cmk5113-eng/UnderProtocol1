using System.Collections.Generic; // 새로운 입력 시스템 네임스페이스 추가
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

public class PlacementController : UI_CharcterSelectWindows
{
    private Camera mainCamera;
    public GameObject currentObject;
    static List<GameObject> _objects = new List<GameObject>();
    int count => _objects.Count;
    int max = 4;
    public TextMeshProUGUI Current;
    public TextMeshProUGUI Max;


    private void RefreshUI()
    {
        if (Current != null) Current.text = count.ToString();
        if (Max != null) Max.text = max.ToString();
    }

    void Start()
    {
        mainCamera = Camera.main;
        if (PlacementManager.Instance.tilemap == null) PlacementManager.Instance.   tilemap = GameObject.FindObjectOfType<Tilemap>();
       
    }

    void Update()
    {
        RefreshUI();
        // Mouse.current.leftButton.wasPressedThisFrame으로 클릭 감지
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            // 마우스 현재 스크린 좌표 가져오기
            Vector2 mouseScreenPos = Mouse.current.position.ReadValue();

            // 월드 좌표로 변환
            Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, 0));
            mouseWorldPos.z = 1;
             
            // 타일맵 셀 좌표로 변환
            Vector3Int clickCellPos = PlacementManager.Instance.tilemap.WorldToCell(mouseWorldPos);
            Vector3Int origin = PlacementManager.Instance.tilemap.cellBounds.min;
            Vector3Int adjustedPos = clickCellPos - origin;

            if (ModeManager.Instance.CurrentMode == GameMode.CharacterSelect)
            {
                if (PlacementManager.Instance.tilemap.HasTile(clickCellPos))
                {
                    if (PlacementManager.currentCharacter != null)
                    {
                        // 1. 현재 선택된 캐릭터(currentCharacter)의 이름과 일치하는 오브젝트를 씬에서 검색
                        // 프리펩 생성 시 (Clone)이 붙는 규칙이라면 currentCharacter.name + "(Clone)"으로 수정
                        
                        GameObject target = GameObject.Find(PlacementManager.currentCharacter.name + "(Clone)");
                        if (count >= max)
                        {
                            UIManager.ClaimPopUp("띠딩", "인원 초과", "롸져");
                            PlacementManager.currentCharacter = null;
                            return;
                        }
                        // 2. 일치하는 이름의 오브젝트가 이미 있으면 삭제
                        if (target != null)
                        {
                            Debug.Log($"[Destroy] 기존에 존재하는 {target.name} 오브젝트를 삭제합니다.");
                            ObjectManager.DestroyObject(target);
                            _objects.Remove(target);
                           
                  
                        }

                        

                        Vector3 spawnPos = PlacementManager.Instance.tilemap.GetCellCenterWorld(clickCellPos);
                        GameObject obj = ObjectManager.CreateObject(PlacementManager.currentCharacter, spawnPos);
                        _objects.Add(obj);
                    
                        Debug.Log($"[Create] 타일 위치 {clickCellPos}에 {PlacementManager.currentCharacter}오브젝트 생성 완료");
                        PlacementManager.currentCharacter = null;
                    }
                    else
                    {
                        Debug.Log("캐릭터를선택하세요");
                    }
                }
            }

            else if (ModeManager.Instance.CurrentMode == GameMode.Battle)
            {
                Debug.Log("전투시작");
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