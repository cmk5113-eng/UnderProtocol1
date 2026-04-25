using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.InputSystem; // 새로운 입력 시스템 네임스페이스 추가

public class PlacementController : UI_CharcterSelectWindows
{
    public Tilemap tilemap;
    private Camera mainCamera;
    public GameObject currentObject;
    void Start()
    {
        mainCamera = Camera.main;
        if (tilemap == null) tilemap = GameObject.FindObjectOfType<Tilemap>();
  
    }

    void Update()
    {
        // Mouse.current.leftButton.wasPressedThisFrame으로 클릭 감지
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            // 마우스 현재 스크린 좌표 가져오기
            Vector2 mouseScreenPos = Mouse.current.position.ReadValue();

            // 월드 좌표로 변환
            Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, 0));
            mouseWorldPos.z = 1;
             
            // 타일맵 셀 좌표로 변환
            Vector3Int clickCellPos = tilemap.WorldToCell(mouseWorldPos);
            Vector3Int origin = tilemap.cellBounds.min;
            Vector3Int adjustedPos = clickCellPos - origin;

            if (ModeManager.Instance.CurrentMode == GameMode.CharacterSelect)
            {
                if (tilemap.HasTile(clickCellPos))
                {
                    if (PlacementManager.currentCharacter != null)
                    {
                        // 1. 현재 선택된 캐릭터(currentCharacter)의 이름과 일치하는 오브젝트를 씬에서 검색
                        // 프리펩 생성 시 (Clone)이 붙는 규칙이라면 currentCharacter.name + "(Clone)"으로 수정
                        GameObject target = GameObject.Find(PlacementManager.currentCharacter.name);

                        // 2. 일치하는 이름의 오브젝트가 이미 있으면 삭제
                        if (target != null)
                        {
                            Debug.Log($"[Destroy] 기존에 존재하는 {target.name} 오브젝트를 삭제합니다.");
                            ObjectManager.DestroyObject(target);
                        }
                        Vector3 spawnPos = tilemap.GetCellCenterWorld(clickCellPos);
                        ObjectManager.CreateObject(PlacementManager.currentCharacter, spawnPos);
                        Debug.Log($"[Create] 타일 위치 {clickCellPos}에 {PlacementManager.currentCharacter}오브젝트 생성 완료");
                        PlacementManager.currentCharacter = null;
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
}