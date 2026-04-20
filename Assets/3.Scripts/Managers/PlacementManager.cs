using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PlacementManager : ManagerBase
{
    public static PlacementManager Instance;
    public Tilemap tilemap;


    protected override IEnumerator OnConnected(GameManager newManager)
    {
        yield return null;
    }

        public bool PlaceCharacter(GameObject characterPrefab, Vector3Int cellPos)
    {
        if (characterPrefab == null) return false;

        // 1. 타일 좌표 이름 생성
        string tileName = $"Unit_{cellPos.x}_{cellPos.y}";
        Vector3 spawnPos = tilemap.GetCellCenterWorld(cellPos);

        // 2. 중복 타일 체크 (누구든 이미 그 자리에 있는가?)
        GameObject objectOnTile = GameObject.Find(tileName);
        if (objectOnTile != null)
        {
            UIManager.ClaimPopUp("알림", "이미 해당 위치에 캐릭터가 있습니다.", "확인");
            return false;
        }

        // 3. 동일 캐릭터 이동 체크 (동일한 이름의 프리펩이 이미 배치되어 있는가?)
        // 주의: Clone 이름을 고려하거나 생성 규칙에 맞춰 Find 작성
        GameObject existingClone = GameObject.Find(characterPrefab.name);

        if (existingClone != null)
        {
            Debug.Log($"[이동] {characterPrefab.name} 위치 변경");
            ObjectManager.DestroyObject(existingClone);
            // 이동은 인원수 변화가 없으므로 그대로 진행
        }
        GameObject newUnit = ObjectManager.CreateObject(characterPrefab, spawnPos);
        newUnit.name = tileName; // 타일 좌표로 이름 고정

        return true;

    }

    
      protected override void OnDisconnected()
    {

    }

}
