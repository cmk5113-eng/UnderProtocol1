using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

public class PlacementManager : ManagerBase
{
    public static PlacementManager Instance;
    public Tilemap tilemap;
    [SerializeField]public List<Tilemap> AllTileMap = new List<Tilemap>();
    public Dictionary<Vector3Int, TileData> tileDatas = new();
            
    void Awake()
    {
        // ���� �ϳ����� �Ŵ����� ����ϱ� ���� ������ �̱��� ����
        Instance = this;
    }
    public void SetTileMap()
    {
        for (int i = 0; i < AllTileMap.Count; i++)
        {
            Tilemap map = AllTileMap[i];

            if (map != null && map.gameObject.activeInHierarchy)
            {
                tilemap = AllTileMap[i];

                Debug.Log($"[PlacementManager] 현재 Tilemap Index : {i}, Name : {map.name}");

                return;
            }
        }

        Debug.LogWarning("[PlacementManager] 활성화된 Tilemap을 찾을 수 없습니다.");
    }
    public TileData GetTileData(Vector3Int cell)
    {
        if (!tileDatas.TryGetValue(cell, out TileData data))
        {
            data = new TileData();
            tileDatas[cell] = data;
        }

        return data;
    }
    public void SetTileEmpty(Vector3Int tile, bool isEmpty)
    {
        TileData data = GetTileData(tile);
        data.isempty = isEmpty;

        // [변수명 필요: PlacementManager 내부에서 TileData를 저장/관리하는 Dictionary나 배열 변수명]
        // 예시: [데이터저장변수명][tile] = data;
    }
    protected override IEnumerator OnConnected(GameManager newManager)
    {
        yield return null;
    }

    public bool PlaceCharacter(GameObject characterPrefab, Vector3Int cellPos)
    {
        if (characterPrefab == null) return false;

        // 1. �� ��ǥ�� �̸� ����
        string tileName = $"Unit_{cellPos.x}_{cellPos.y}";
        if (tilemap == null)
        {
            Debug.LogWarning("[PlacementManager] tilemap is null. PlaceCharacter aborted.");
            return false;
        }

        Vector3 spawnPos = tilemap.GetCellCenterWorld(cellPos);

        // 2. ���� ��ġ�� ������Ʈ �ִ��� �˻�
        GameObject objectOnTile = GameObject.Find(tileName);
        if (objectOnTile != null)
        {
            UIManager.ClaimPopUp("���", "�̹� �ش� ��ġ�� ������ �ֽ��ϴ�.", "Ȯ��");
            return false;
        }

        // 3. ���� Ŭ�� ���� �� ����
        GameObject existingClone = GameObject.Find(characterPrefab.name);

        if (existingClone != null)
        {
            Debug.Log($"[�̵�] {characterPrefab.name} ��ġ �罺��");
            ObjectManager.DestroyObject(existingClone);
        }
        GameObject newUnit = ObjectManager.CreateObject(characterPrefab, spawnPos);

        SelectionManager.CharacterBase.actionPoint = SelectionManager.CharacterBase.maxAP;
        SelectionManager.CharacterBase.steminaPoint = SelectionManager.CharacterBase.maxStemina;
        SelectionManager.CharacterBase.isSpawned = true;

        newUnit.name = tileName; // Ÿ�� ��ǥ�� �̸� ����

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