using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.InputSystem; // 새로운 입력 시스템 네임스페이스 추가

public class SpawnCharactertoMap : UI_CharcterSelectWindows
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
     
            if (tilemap.HasTile(clickCellPos))
            {
                if (currentCharacter is not null)
                {
                    Vector3 spawnPos = tilemap.GetCellCenterWorld(clickCellPos);
                    ObjectManager.CreateObject(currentCharacter, spawnPos);
                    Debug.Log($"[Create] 타일 위치 {clickCellPos}에 {currentCharacter}오브젝트 생성 완료");
                    currentCharacter = null;
                    
                }
                else
                {
                    Debug.Log("캐릭터를선택하세요");

                }
                }
            }
        
    }
}