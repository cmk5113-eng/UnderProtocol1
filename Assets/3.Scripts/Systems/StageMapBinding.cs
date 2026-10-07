using UnityEngine;
using UnityEngine.Tilemaps;

public class StageMapBinding : MonoBehaviour
{
    [Tooltip("WaveManager 스테이지 목록의 0부터 시작하는 번호입니다. -1은 연결 해제 상태입니다.")]
    [Min(-1)] [SerializeField] private int stageIndex;
    [SerializeField] private Tilemap targetTilemap;
    [SerializeField] private StageMapData mapData;

    [Header("전투 후 시나리오")]
    [Tooltip("이 타일맵을 클리어한 뒤 표시할 시나리오 오브젝트입니다. 미지정 시 월드로 복귀합니다.")]
    [SerializeField] private GameObject postBattleScenario;

    public int StageIndex => stageIndex;
    public Tilemap TargetTilemap => targetTilemap != null ? targetTilemap : GetComponentInChildren<Tilemap>(true);
    public StageMapData MapData => mapData;
    public GameObject PostBattleScenario => postBattleScenario;

    public bool IsBoundTo(Tilemap tilemap)
    {
        if (tilemap == null) return false;
        // 공용 Grid의 미지정 Binding은 첫 번째 자식 맵의 연결로 해석하지 않는다.
        return targetTilemap != null ? targetTilemap == tilemap : GetComponent<Tilemap>() == tilemap;
    }

#if UNITY_EDITOR
    public void EditorSetTilemap(Tilemap value) => targetTilemap = value;
    public void EditorSetStageIndex(int value) => stageIndex = value;
#endif
}
