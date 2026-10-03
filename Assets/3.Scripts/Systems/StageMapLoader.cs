using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class StageMapLoader : MonoBehaviour
{
    [SerializeField] private StageMapData mapData;
    [SerializeField] private Tilemap targetTilemap;
    [SerializeField] private Transform obstacleRoot;

    private readonly List<GameObject> spawnedObstacles = new List<GameObject>();

    public StageMapData MapData => mapData;

    public void LoadMapData()
    {
        ClearRuntimeObjects();
        if (mapData == null) return;

        Tilemap map = targetTilemap != null
            ? targetTilemap
            : PlacementManager.Instance != null ? PlacementManager.Instance.tilemap : null;
        if (map == null) return;

        if (mapData.obstacles != null)
        {
            foreach (ObstacleSpawnData data in mapData.obstacles)
            {
                if (data == null || data.prefab == null || !map.HasTile(data.position)) continue;
                Vector3 position = map.GetCellCenterWorld(data.position);
                Quaternion rotation = Quaternion.Euler(0f, 0f, -90f * data.rotationQuarterTurns);
                GameObject obj = Instantiate(data.prefab, position, rotation, obstacleRoot);
                spawnedObstacles.Add(obj);

                if (PlacementManager.Instance != null)
                    PlacementManager.Instance.GetTileData(data.position).isempty = false;
            }
        }

        // 필드 이펙트는 데이터와 미리보기까지 준비한다.
        // 실제 전투 효과 적용은 각 효과 시스템이 정해지면 mapData.fieldEffects를 소비하면 된다.
    }

    public void ClearRuntimeObjects()
    {
        foreach (GameObject obj in spawnedObstacles)
            if (obj != null) Destroy(obj);
        spawnedObstacles.Clear();
    }
}
