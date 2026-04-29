using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PlacementManager : ManagerBase
{
    public static PlacementManager Instance;
    public Tilemap tilemap;         
    public static GameObject currentCharacter;

    void Awake()
    {
        // 씬에 하나뿐인 매니저로 사용하기 위한 간단한 싱글톤 설정
        Instance = this;
    }

    protected override IEnumerator OnConnected(GameManager newManager)
    {
        yield return null;
    }

    public bool PlaceCharacter(GameObject characterPrefab, Vector3Int cellPos)
    {
        if (characterPrefab == null) return false;

        // 1. 셀 좌표로 이름 생성
        string tileName = $"Unit_{cellPos.x}_{cellPos.y}";
        if (tilemap == null)
        {
            Debug.LogWarning("[PlacementManager] tilemap is null. PlaceCharacter aborted.");
            return false;
        }

        Vector3 spawnPos = tilemap.GetCellCenterWorld(cellPos);

        // 2. 같은 위치에 오브젝트 있는지 검사
        GameObject objectOnTile = GameObject.Find(tileName);
        if (objectOnTile != null)
        {
            UIManager.ClaimPopUp("경고", "이미 해당 위치에 유닛이 있습니다.", "확인");
            return false;
        }

        // 3. 기존 클론 정리 후 생성
        GameObject existingClone = GameObject.Find(characterPrefab.name);

        if (existingClone != null)
        {
            Debug.Log($"[이동] {characterPrefab.name} 위치 재스폰");
            ObjectManager.DestroyObject(existingClone);
        }
        GameObject newUnit = ObjectManager.CreateObject(characterPrefab, spawnPos);
        newUnit.name = tileName; // 타일 좌표로 이름 설정

        return true;
    }
    public void RemoveCharacter(GameObject characterPrefab)
    {
       
    }
    public void RemoveAllCharacter()
    { 
    
    }

    
      protected override void OnDisconnected()
    {

    }

}
