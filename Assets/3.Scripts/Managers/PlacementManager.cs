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

    private void Awake()
    {
        Instance = this;

        if (tilemap != null)
            InitializeMapOrigin();
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
        if (!tileDatas.TryGetValue(cell, out TileData data) || data == null)
        {
            data = new TileData();

            data.Type = IsOuterTile(cell)
                ? TileData.tiletype.outside
                : TileData.tiletype.inside;

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

        // 1. 타일 좌표 이름 생성
        string tileName = $"Unit_{cellPos.x}_{cellPos.y}";

        if (tilemap == null)
        { 
            return false;
        }

        Vector3 spawnPos = tilemap.GetCellCenterWorld(cellPos);

        // 2. 해당 위치에 오브젝트가 있는지 검사
        GameObject objectOnTile = GameObject.Find(tileName);

        if (objectOnTile != null)
        { 
            return false;
        }

        // 3. 기존에 배치된 같은 캐릭터가 있다면 제거
        GameObject existingClone = GameObject.Find(characterPrefab.name);

        if (existingClone != null)
        { 

            ObjectManager.DestroyObject(existingClone);
        }

        // 4. 해당 타일의 TileData 가져오기
        TileData tileData = PlacementManager.Instance.GetTileData(cellPos);

        // 5. 타일 조건 검사
        if (!tileData.isempty)
        { 
            return false;
        }

        if (tileData.Type != TileData.tiletype.outside)
        { 
            return false;
        }

        // 6. 유닛 생성
        GameObject newUnit = ObjectManager.CreateObject(characterPrefab, spawnPos);

        if (newUnit == null)
        { 
            return false;
        }

        // 7. CharacterBase 연결
        CharacterBase character = newUnit.GetComponent<CharacterBase>();

        if (character == null)
        { 
            ObjectManager.DestroyObject(newUnit);
            return false;
        }

        // 8. TileData에 캐릭터 등록
        tileData.Character = character;

        // 9. 캐릭터 상태 초기화
        character.actionPoint = character.maxAP;
        character.steminaPoint = character.maxStemina;
        character.isSpawned = true;

        // 10. 타일 좌표를 이용해서 이름 지정
        newUnit.name = tileName;

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
    public bool IsOuterTile(Vector3Int rawCell)
    {
        // 여기서는 맵의 원점 기준으로 판단해야 함
        Vector2Int custom = ConvertToCustomPosition(rawCell);

        return custom.x == 1 ||
               custom.x == 10 ||
               custom.y == 1 ||
               custom.y == 10;
    }
    private Vector3Int mapOrigin;

    public void InitializeMapOrigin()
    {
        if (tilemap == null)
            return;

        tilemap.CompressBounds();

        BoundsInt bounds = tilemap.cellBounds;

        mapOrigin = new Vector3Int(
            bounds.xMin,
            bounds.yMax - 1,
            0
        );
    }

    public Vector2Int ConvertToCustomPosition(Vector3Int rawCell)
    {
        int customX = rawCell.x - mapOrigin.x + 1;
        int customY = mapOrigin.y - rawCell.y + 1;

        return new Vector2Int(customX, customY);
    }
}