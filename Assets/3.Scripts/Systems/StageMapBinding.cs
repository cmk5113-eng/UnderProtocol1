using UnityEngine;
using UnityEngine.Tilemaps;

public class StageMapBinding : MonoBehaviour
{
    [Tooltip("WaveManager의 stage1Waves=0, stage2Waves=1 ... 에 대응합니다.")]
    [Min(0)] [SerializeField] private int stageIndex;
    [SerializeField] private Tilemap targetTilemap;
    [SerializeField] private StageMapData mapData;

    public int StageIndex => stageIndex;
    public Tilemap TargetTilemap => targetTilemap != null ? targetTilemap : GetComponentInChildren<Tilemap>(true);
    public StageMapData MapData => mapData;

#if UNITY_EDITOR
    public void EditorSetTilemap(Tilemap value) => targetTilemap = value;
#endif
}
