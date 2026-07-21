using System;
using System.Collections.Generic;
using UnityEditor.U2D.Animation;
using UnityEngine;
using UnityEngine.Tilemaps;

public class UseSkill : MonoBehaviour
{
    public static UseSkill Instance { get; private set; }

    // 사정거리와 효과 범위를 시각적으로 구분할 색상 설정
    [Header("하이라이트 색상 설정")]
    [SerializeField] private Color castRangeColor = new Color(0f, 0.5f, 1f, 0.4f); // 반투명 푸른색
    [SerializeField] private Color aoeColor = new Color(1f, 0.2f, 0.2f, 0.5f);       // 반투명 붉은색

    private Tilemap tilemap;
    private ActiveSkill currentSkill;
    private CharacterBase caster;

    // 최적화를 위해 불이 켜진 타일들의 좌표만 기억하는 리스트
    private List<Vector3Int> castRangeTiles = new List<Vector3Int>();
    private List<Vector3Int> aoeTiles = new List<Vector3Int>();

    private Vector3Int lastMouseCell = new Vector3Int(-999, -999, -999);
    private bool isSkillTargetingActive = false;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // 기존 매니저에서 사용하던 타일맵 참조 가져오기
        if (PlacementManager.Instance != null)
        {
            tilemap = PlacementManager.Instance.tilemap;
        }
    }

    private void Update()
    {
        if (!isSkillTargetingActive || tilemap == null || currentSkill == null || caster == null) return;

        HandleRealtimeAoE();

        // 💡 마우스 좌클릭 시 독립된 신규 함수 실행
        if (Input.GetMouseButtonDown(0))
        {
            ExecuteSkillOnTarget();
        }

        // 마우스 우클릭 시 스킬 조준 취소
        if (Input.GetMouseButtonDown(1))
        {
            CancelTargeting();
        }
    }
    /// <summary>
    /// [외부 호출용] UI 스킬 버튼을 눌렀을 때 최초 1회 실행하는 함수
    /// </summary>
    public void StartSkillTargeting(ActiveSkill skill, CharacterBase skillCaster)
    {
        if (tilemap == null)
        {
            if (PlacementManager.Instance != null) tilemap = PlacementManager.Instance.tilemap;
            if (tilemap == null) return;
        }

        // 상태 초기화 및 데이터 할당
        ClearAllHighlights();
        currentSkill = skill;
        caster = skillCaster;
        isSkillTargetingActive = true;
        lastMouseCell = new Vector3Int(-999, -999, -999);

        // 1. 시전자의 위치를 기준으로 고정 '사정거리' 하이라이트 생성
        Vector3Int casterCell = tilemap.WorldToCell(SelectionManager.SelectedPrefab.transform.position);
        HighlightRange(casterCell, currentSkill.range, castRangeColor, castRangeTiles);
      
    }

    /// <summary>
    /// 마우스 움직임을 감지하여 실시간으로 AoE(효과범위)를 업데이트하는 로직
    /// </summary>
    private void HandleRealtimeAoE()
    {
        // 마우스 위치의 월드 좌표를 셀 좌표로 변환
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3Int currentMouseCell = tilemap.WorldToCell(mouseWorldPos);
        currentMouseCell.z = 0;

        // 마우스가 이전 프레임과 '다른 타일'로 이동했을 때만 갱신 (매 프레임 연산 방지 최적화)
        if (currentMouseCell != lastMouseCell)
        {
            lastMouseCell = currentMouseCell;

            // 기존 AoE 하이라이트만 지우기 (사정거리는 유지해야 하므로)
            ClearTileList(aoeTiles);

            // 마우스가 사정거리 하이라이트 '안에' 있을 때만 AoE 범위를 그려줌
            if (castRangeTiles.Contains(currentMouseCell))
            {
                // 2. 마우스 좌표를 중심점으로 스킬 자체의 효과범위(AoE) 하이라이트 생성
                HighlightRange(currentMouseCell, currentSkill.aoe, aoeColor, aoeTiles);
            }
        }
    }

    public void ExecuteSkillOnTarget()
    {
        // 1. 게임 모드가 UseSkill 일 때만 실행하는 조건문
        if (ModeManager.Instance == null || ModeManager.Instance.CurrentMode != ModeManager.GameMode.UseSkill)
        {
            return;
        }

        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3Int clickedCell = tilemap.WorldToCell(mouseWorldPos);
        clickedCell.z = tilemap.WorldToCell(SelectionManager.SelectedPrefab.transform.position).z;

        // 사정거리 외곽 클릭 시 차단 (선택사항 유지)
        if (!castRangeTiles.Contains(clickedCell))
        {
            Debug.Log("[Skill] 사정거리 밖을 클릭했습니다.");
            return;
        }

        // 시전자의 actionPoint가 0이면 실행 안 함
        if (caster.actionPoint == 0)
        {
            Debug.LogWarning($"[Skill] {caster.Name}의 ActionPoint가 0이라 스킬을 사용할 수 없습니다.");
            CancelTargeting();
            return;
        }

        // 2. 하이라이트를 먼저 종료 및 클리어 (ClearAllHighlights)
    

        // 3. AoE 범위에 저장되어 있던 타일들을 순회하며 오브젝트가 있는지 체크
        // (ClearAllHighlights를 실행했더라도 aoeTiles 리스트 변수 내부의 데이터는 아직 살아있으므로 이를 활용합니다)
        List<GameObject> enemiesToDestroy = new List<GameObject>();

        foreach (Vector3Int cellPos in aoeTiles)
        {
            Vector3 worldPos = tilemap.GetCellCenterWorld(cellPos);
            Collider2D hit = Physics2D.OverlapPoint(worldPos);

            if (hit != null)
            {
                // 부모나 자신에게서 CharacterBase가 있는지 가져옵니다.
                CharacterBase targetCharacter = hit.GetComponentInParent<CharacterBase>();

                // 4. Enemy 레이어나 태그 조건 체크
                if (targetCharacter != null && hit.CompareTag("Enemy"))
                {
                    enemiesToDestroy.Add(targetCharacter.gameObject);
                }
            }
        }
        DebugDetectedEnemiesCount(enemiesToDestroy);
        // 5. 검출된 Enemy들을 ObjectManager를 이용해 파괴 처리
        foreach (GameObject enemyObj in enemiesToDestroy)
        {
            if (enemyObj != null)
            {
                Debug.Log($"[Skill Action] 범위 안의 적 {enemyObj.name}을(를) 파괴합니다.");

                
                    ObjectManager.DestroyObject(enemyObj);
                
               }
        }

        // 6. 시전자의 actionPoint 소모 및 시각적 피드백 처리
        caster.actionPoint = 0;
        caster.UpdateActionStateVisual();

        // 7. 스킬 사용이 끝났으므로 게임 모드를 원래대로 복구
        if (ModeManager.Instance != null)
        {
            ModeManager.Instance.CurrentMode = ModeManager.GameMode.Movement;
        }

            ClearAllHighlights();
        Debug.Log($"[Skill Action] 스킬 실행 완료. {caster.Name}의 AP가 {caster.actionPoint}이 되었습니다.");
    }
    private void DebugDetectedEnemiesCount(List<GameObject> enemies)
    {
        foreach (Vector3Int cellPos in aoeTiles)
        {
            Vector3 worldPos = tilemap.GetCellCenterWorld(cellPos);

            Debug.Log($"Cell : {cellPos}");
            Debug.Log($"World : {worldPos}");

            Collider2D hit = Physics2D.OverlapPoint(worldPos);

            if (hit == null)
            {
                Debug.Log("Collider 없음");
                continue;
            }

            Debug.Log($"Hit : {hit.name}");

            CharacterBase targetCharacter = hit.GetComponentInParent<CharacterBase>();

            Debug.Log($"Character : {targetCharacter}");

            Debug.Log($"Tag : {hit.tag}");
        }
    }
    public void OnSkillModeChange()
    {
        ModeManager.Instance.CurrentMode = ModeManager.GameMode.UseSkill;
    }
    
    /// <summary>
    /// 특정 중심점을 기준으로 맨해튼 거리만큼 타일 색상을 바꾸고 리스트에 저장하는 공용 함수
    /// </summary>
    private void HighlightRange(Vector3Int centerCell, int radius, Color color, List<Vector3Int> saveList)
    {
        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                if (Mathf.Abs(x) + Mathf.Abs(y) <= radius)
                {
                    Vector3Int targetCell = new Vector3Int(centerCell.x + x, centerCell.y + y, centerCell.z);

                    if (tilemap.HasTile(targetCell))
                    {
                        tilemap.SetTileFlags(targetCell, TileFlags.None);
                        tilemap.SetColor(targetCell, color);
                        saveList.Add(targetCell);
                    }
                }
            }
        }
    }
    public void UI_StartSkill1()
    {
        Debug.Log("UI_StartSkill1() 호출됨.");

        Debug.Log($"현재 데이터{SelectionManager._characterData} 현재 프리펩{SelectionManager.SelectedPrefab} 현제 베이스{SelectionManager.CharacterBase}");

        //int index = Array.IndexOf(SelectionManager.Instance.characterDatas, SelectionManager._characterData);

        //if (index >= 0 && index < SelectionManager.Instance.characterBases.Length)
        //{
        //    SelectionManager.CharacterBase = SelectionManager.Instance.characterBases[index];
        //}
        //else
        //{
        //    SelectionManager.CharacterBase = null;
        //}

        // 💡 [수정] SelectionManager에 저장된 진짜 캐릭터를 시전자로 가져옵니다!
        CharacterBase currentCaster = SelectionManager.CharacterBase;

        if (StageUIController.Instance == null) return;
        CharacterData currentData = StageUIController.Instance.CurrentData;

        if (currentCaster != null && currentData != null && currentData.active != null && currentData.active.Length > 0)
        {
            ActiveSkill targetSkill = currentData.active[0]; // 1번 스킬
            if (targetSkill != null)
            {
                StartSkillTargeting(targetSkill, currentCaster);
            }
        }
    }

    /// <summary>
    /// [UI 버튼 OnClick 전용] 현재 선택된 캐릭터의 2번 스킬 하이라이트를 켭니다.
    /// </summary>
    public void UI_StartSkill2()
    {
        int index = Array.IndexOf(SelectionManager.Instance.characterDatas, SelectionManager._characterData);

        if (index >= 0 && index < SelectionManager.Instance.characterBases.Length)
        {
            SelectionManager.CharacterBase = SelectionManager.Instance.characterBases[index];
        }
        else
        {
            SelectionManager.CharacterBase = null;
        }

        // 💡 동일하게 적용
        CharacterBase currentCaster = SelectionManager.CharacterBase;

        if (StageUIController.Instance == null) return;
        CharacterData currentData = StageUIController.Instance.CurrentData;

        if (currentCaster != null && currentData != null && currentData.active != null && currentData.active.Length > 1)
        {
            ActiveSkill targetSkill = currentData.active[1]; // 2번 스킬
            if (targetSkill != null)
            {
                StartSkillTargeting(targetSkill, currentCaster);
            }
        }
    }

    /// <summary>
    /// [UI 버튼 OnClick 전용] 현재 선택된 캐릭터의 궁극기 하이라이트를 켭니다.
    /// </summary>
    /// <summary>
    /// 특정 타일 리스트의 색상을 원래대로(White) 돌려놓는 함수
    /// </summary>
    private void ClearTileList(List<Vector3Int> tileList)
    {
        foreach (Vector3Int pos in tileList)
        {
            if (tilemap.HasTile(pos))
            {
                tilemap.SetTileFlags(pos, TileFlags.None);

                // 만약 지우려는 타일이 사정거리 타일 리스트에도 포함되어 있다면 사정거리 색상으로 복구
                if (castRangeTiles.Contains(pos) && tileList == aoeTiles)
                {
                    tilemap.SetColor(pos, castRangeColor);
                }
                else
                {
                    tilemap.SetColor(pos, Color.white);
                }
            }
        }
        tileList.Clear();
    }

    /// <summary>
    /// 모든 하이라이트를 종료하고 초기화
    /// </summary>
    public void ClearAllHighlights()
    {
        isSkillTargetingActive = false;
        if (tilemap == null) return;

        ClearTileList(aoeTiles);
        ClearTileList(castRangeTiles);
    }

    private void CancelTargeting()
    {
        ClearAllHighlights();
        Debug.Log("스킬 조준이 취소되었습니다.");
    }
}
