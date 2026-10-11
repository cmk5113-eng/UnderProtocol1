using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class StageMapLoader : MonoBehaviour
{
    private readonly List<GameObject> spawnedObstacles = new List<GameObject>();
    private readonly HashSet<Vector3Int> blockedCells = new HashSet<Vector3Int>();
    private Tilemap loadedMap;

    public bool LoadMapData()
    {
        ClearRuntimeObjects();
        WaveManager manager = GameManager.Instance != null ? GameManager.Instance.Wave : null;
        Tilemap map = PlacementManager.Instance != null ? PlacementManager.Instance.tilemap : null;
        StageMapEntry settings = manager != null ? manager.GetMap(map) : null;
        if (settings == null || manager.ActiveMap != settings) return false;
        StageMapData mapData = settings.mapData;
        if (mapData == null) return true;

        var positions = new HashSet<Vector3Int>();
        if (mapData.obstacles != null) foreach (ObstacleSpawnData data in mapData.obstacles)
        {
            if (data == null || data.prefab == null || !map.HasTile(data.position)
                || !positions.Add(data.position)
                || (data.blocksMovement && !PlacementManager.Instance.GetTileData(data.position).isempty))
            {
                Debug.LogError("[StageMapLoader] 맵에디터의 장애물 위치와 프리팹을 확인해주세요.");
                return false;
            }
        }

        loadedMap = map;
        if (mapData.obstacles != null) foreach (ObstacleSpawnData data in mapData.obstacles)
        {
            GameObject obj = Instantiate(data.prefab, map.GetCellCenterWorld(data.position),
                Quaternion.Euler(0f, 0f, -90f * data.rotationQuarterTurns), map.transform);
            spawnedObstacles.Add(obj);
            if (data.blocksMovement)
            {
                blockedCells.Add(data.position);
                PlacementManager.Instance.GetTileData(data.position).isempty = false;
            }
        }

        if (mapData.fieldEffects != null) foreach (FieldEffectSpawnData effect in mapData.fieldEffects)
        {
            if (effect == null) continue;
            SkillTileFieldEffectType type = RuntimeEffect(effect.effectType);
            if (type != SkillTileFieldEffectType.None)
                BattleManager.Instance.Fields.Apply(map, effect.position, null, type,
                    effect.value, effect.duration, Vector3Int.zero);
        }
        return true;
    }

    private static SkillTileFieldEffectType RuntimeEffect(StageFieldEffectType type)
    {
        switch (type)
        {
            case StageFieldEffectType.Fire: return SkillTileFieldEffectType.Fire;
            case StageFieldEffectType.Ice: return SkillTileFieldEffectType.Ice;
            case StageFieldEffectType.Electric: return SkillTileFieldEffectType.Electric;
            case StageFieldEffectType.Wind: return SkillTileFieldEffectType.Wind;
            case StageFieldEffectType.Earth: return SkillTileFieldEffectType.Earth;
            case StageFieldEffectType.Dark: return SkillTileFieldEffectType.Dark;
            default: return SkillTileFieldEffectType.None;
        }
    }

    public void ClearRuntimeObjects()
    {
        foreach (GameObject obj in spawnedObstacles)
        {
            if (obj == null) continue;
            obj.SetActive(false);
            Destroy(obj);
        }
        spawnedObstacles.Clear();
        if (PlacementManager.Instance != null && PlacementManager.Instance.tilemap == loadedMap)
            foreach (Vector3Int cell in blockedCells)
            {
                TileData data = PlacementManager.Instance.GetTileData(cell);
                if (data.Character == null) data.isempty = true;
            }
        blockedCells.Clear();
        loadedMap = null;
    }
}

