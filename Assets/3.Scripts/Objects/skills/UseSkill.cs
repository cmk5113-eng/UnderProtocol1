using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class UseSkill : MonoBehaviour
{
    public static UseSkill Instance { get; private set; }

    [Header("하이라이트 색상 설정")]
    [SerializeField] private Color castRangeColor = new Color(0f, 0.5f, 1f, 0.4f); // 반투명 푸른색
    [SerializeField] private Color aoeColor = new Color(1f, 0.2f, 0.2f, 0.5f);       // 반투명 붉은색

    private Tilemap tilemap;
    private SkillList currentSkill;

    private CharacterBase caster;

    private List<Vector3Int> castRangeTiles = new List<Vector3Int>();
    private List<Vector3Int> aoeTiles = new List<Vector3Int>();

    private Vector3Int lastMouseCell = new Vector3Int(-999, -999, -999);
    private bool isSkillTargetingActive = false;
    private int patternRotation = 0;
    private int CurrentSkillRange =>
    currentSkill != null ? currentSkill.range : 0;

    private int CurrentSkillAoe =>
        currentSkill != null ? currentSkill.aoe : 0;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {   
    }

    private void Update()
    {
        if (ModeManager.Instance == null || ModeManager.Instance.CurrentMode != ModeManager.GameMode.UseSkill) return;

        if (!isSkillTargetingActive || tilemap == null || (currentSkill == null ) || caster == null) return;

        HandlePatternRotation();
        HandleRealtimeAoE();

        if (Input.GetMouseButtonDown(0))
        {
            ExecuteSkillOnTarget();
        }

        if (Input.GetMouseButtonDown(1))
        {
            CancelTargeting();
        }
    }




    /// <summary>
    /// [궁극기 전용] 스킬 조준 시작
    /// </summary>
    public void StartSkillTargeting(SkillList skill, CharacterBase skillCaster)
    {
        if (skill == null || skillCaster == null) return;
        BattleManager battle = BattleManager.Instance;
        if (battle != null && battle.IsBattleActive && !battle.CanAcceptPlayerAction) return;
        if (ModeManager.Instance != null &&
            ModeManager.Instance.CurrentMode != ModeManager.GameMode.UseSkill)
        {
            ModeManager.Instance.CurrentMode = ModeManager.GameMode.UseSkill;
        }

        // 스킬 사용을 시작할 때마다 현재 Tilemap을 다시 가져온다.
        if (PlacementManager.Instance == null)
            return;

        tilemap = PlacementManager.Instance.tilemap;

        if (tilemap == null)
        {
            Debug.LogWarning("[Skill] 현재 Tilemap이 없습니다.");
            return;
        }

        ClearAllHighlights();

        currentSkill = skill;
        caster = skillCaster;
        patternRotation = 0;
        isSkillTargetingActive = true;
        lastMouseCell = new Vector3Int(-999, -999, -999);

        Vector3Int casterCell =
            tilemap.WorldToCell(skillCaster.transform.position);

        casterCell.z = 0;

        if (currentSkill.HasRangePattern)
        {
            HighlightPattern(
                casterCell,
                currentSkill.rangePattern,
                patternRotation,
                castRangeColor,
                castRangeTiles
            );
        }
        else
        {
            HighlightRange(
                casterCell,
                CurrentSkillRange,
                castRangeColor,
                castRangeTiles
            );
        }
    }
     
    

    private void HandleRealtimeAoE()
    {

        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3Int currentMouseCell = tilemap.WorldToCell(mouseWorldPos);
        currentMouseCell.z = 0;

        if (currentMouseCell != lastMouseCell)
        {
            lastMouseCell = currentMouseCell;

            ClearTileList(aoeTiles);

            if (castRangeTiles.Contains(currentMouseCell))
            {
                if (currentSkill.HasRoePattern)
                {
                    HighlightRoePattern(
                        currentMouseCell,
                        currentSkill.roePattern,
                        patternRotation,
                        aoeColor,
                        aoeTiles
                    );
                }
                else
                {
                    int targetAoe = CurrentSkillAoe;

                    if (targetAoe > 0)
                    {
                        HighlightRange(
                            currentMouseCell,
                            targetAoe,
                            aoeColor,
                            aoeTiles
                        );
                    }
                }
            }
        }
    }

    public void ClearRealtimeAoE()
    {
        if (tilemap == null) return;

        ClearTileList(aoeTiles);

        foreach (Vector3Int castPos in castRangeTiles)
        {
            if (tilemap.HasTile(castPos))
            {
                tilemap.SetTileFlags(castPos, TileFlags.None);
                tilemap.SetColor(castPos, castRangeColor);
            }
        }

        lastMouseCell = new Vector3Int(-999, -999, -999);
    }

    public void ExecuteSkillOnTarget()
    {
        if (!isSkillTargetingActive || currentSkill == null) return;
        BattleManager battle = BattleManager.Instance;
        if (battle != null && battle.IsBattleActive && !battle.CanAcceptPlayerAction) return;
        if (ModeManager.Instance == null ||
            ModeManager.Instance.CurrentMode != ModeManager.GameMode.UseSkill)
        {
            Debug.Log("[Skill] 현재 스킬 사용 모드가 아닙니다.");
            return;
        }

        if (tilemap == null)
        {
            Debug.LogWarning("[Skill] Tilemap이 없습니다.");
            return;
        }

        if (caster == null)
        {
            Debug.LogWarning("[Skill] 캐스터가 없습니다.");
            CancelTargeting();
            return;
        }

        // 마우스 위치 → 타일 좌표
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3Int clickedCell = tilemap.WorldToCell(mouseWorldPos);

        // 하이라이트와 동일하게 Z = 0
        clickedCell.z = 0;

        // 사정거리 확인
        if (!castRangeTiles.Contains(clickedCell))
        {
            //Debug.Log("[Skill] 사정거리 밖을 클릭했습니다.");
            return;
        }

        // AP 확인
        if (caster.actionPoint <= 0)
        {
            //Debug.LogWarning(
            //    $"[Skill] ActionPoint가 부족합니다. 현재 AP = {caster.actionPoint}"
            //);

            CancelTargeting();
            return;
        }

        List<CharacterBase> targets = new List<CharacterBase>();
        List<SkillTargetHit> patternHits = new List<SkillTargetHit>();
        var attackCells = new List<Vector3Int>(aoeTiles);
        // Legacy single-target skills do not paint an AOE, but still hit the clicked cell.
        if (!currentSkill.HasRoePattern && CurrentSkillAoe <= 0 && tilemap.HasTile(clickedCell))
            attackCells.Add(clickedCell);

        foreach (Vector3Int cellPos in attackCells)
        {
            Vector3 worldPos = tilemap.GetCellCenterWorld(cellPos);
            Collider2D hit = Physics2D.OverlapPoint(worldPos);

            if (hit == null)
                continue;

            CharacterBase targetCharacter =
                hit.GetComponentInParent<CharacterBase>();

            if (targetCharacter == null ||
                !targetCharacter.isEnemy ||
                targets.Contains(targetCharacter))
            {
                continue;
            }

            targets.Add(targetCharacter);

            if (currentSkill.HasRoePattern)
            {
                Vector2Int rotatedPosition = new Vector2Int(
                    cellPos.x - clickedCell.x,
                    cellPos.y - clickedCell.y
                );

                Vector2Int localPosition =
                    RotateOffset(rotatedPosition, -patternRotation);

                SkillPatternTile patternTile =
                    currentSkill.roePattern.Find(
                        tile => tile.position == localPosition
                    );

                if (patternTile != null)
                {
                    patternHits.Add(new SkillTargetHit(
                        targetCharacter,
                        patternTile
                    ));
                }
            }
        }

        SkillExecuteResult result = null;

        if (ExecuteSkill.Instance != null)
        {
            if (currentSkill.HasRoePattern)
            {
                result = ExecuteSkill.Instance.ExecutePattern(
                    caster,
                    currentSkill,
                    clickedCell,
                    patternHits,
                    patternRotation
                );
            }
            else
            {
                result = ExecuteSkill.Instance.Execute(
                    caster,
                    currentSkill,
                    targets
                );
            }
        }
        else
        {
            Debug.LogWarning("[Skill] ExecuteSkill.Instance is missing.");
            return;
        }

        if (result != null && ScrollUI.Instance != null)
        {
            ScrollUI.Instance.PlusGaugevalue(0.02f * result.hitCount);
        }

        // Spend the action BEFORE passives can grant another one.
        CharacterBase actionActor = caster;
        SkillList actionSkill = currentSkill;
        actionActor.actionPoint = Mathf.Max(0, actionActor.actionPoint - 1);

        // 이동 모드로 전환
        if (ModeManager.Instance != null)
        {
            ModeManager.Instance.CurrentMode =
                ModeManager.GameMode.Movement;
        }

        ClearAllHighlights();

        if (battle != null)
            battle.NotifyAttackCompleted(actionActor, actionSkill, result, clickedCell, attackCells);
        actionActor.UpdateActionStateVisual();
        // Keep the existing explicit EndTurn flow: movement bonuses can still be used after attacking.
    }

    private void DebugDetectedEnemiesCount(List<GameObject> enemies)
    {
        foreach (Vector3Int cellPos in aoeTiles)
        {
            Vector3 worldPos = tilemap.GetCellCenterWorld(cellPos);
            Collider2D hit = Physics2D.OverlapPoint(worldPos);

            if (hit == null)
            {
                Debug.Log("Collider 없음");
                continue;
            }

            CharacterBase targetCharacter = hit.GetComponentInParent<CharacterBase>();
        }
    }

    public void OnSkillModeChange()
    {
        if (ModeManager.Instance != null)
        {
            ModeManager.Instance.CurrentMode = ModeManager.GameMode.UseSkill;
        }
    }

    private void HandlePatternRotation()
    {
        if (currentSkill == null ||
            !currentSkill.canRotate ||
            (!currentSkill.HasRangePattern &&
             !currentSkill.HasRoePattern))
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            RotatePatternClockwise();
        }
    }

    public void RotatePatternClockwise()
    {
        if (!isSkillTargetingActive ||
            currentSkill == null ||
            !currentSkill.canRotate)
        {
            return;
        }

        patternRotation = (patternRotation + 1) % 4;
        RefreshRotatedPatterns();

        Debug.Log(
            $"[Skill] Pattern rotation: {patternRotation * 90} degrees"
        );
    }

    private void RefreshRotatedPatterns()
    {
        if (tilemap == null || caster == null || currentSkill == null)
            return;

        ClearTileList(aoeTiles);
        ClearTileList(castRangeTiles);

        Vector3Int casterCell =
            tilemap.WorldToCell(caster.transform.position);
        casterCell.z = 0;

        if (currentSkill.HasRangePattern)
        {
            HighlightPattern(
                casterCell,
                currentSkill.rangePattern,
                patternRotation,
                castRangeColor,
                castRangeTiles
            );
        }
        else
        {
            HighlightRange(
                casterCell,
                CurrentSkillRange,
                castRangeColor,
                castRangeTiles
            );
        }

        lastMouseCell = new Vector3Int(-999, -999, -999);
    }

    private Vector2Int RotateOffset(Vector2Int position, int rotation)
    {
        rotation = ((rotation % 4) + 4) % 4;

        switch (rotation)
        {
            case 1:
                return new Vector2Int(-position.y, position.x);
            case 2:
                return new Vector2Int(-position.x, -position.y);
            case 3:
                return new Vector2Int(position.y, -position.x);
            default:
                return position;
        }
    }


    private void HighlightPattern(
        Vector3Int centerCell,
        List<Vector2Int> pattern,
        int rotation,
        Color color,
        List<Vector3Int> saveList)
    {
        if (pattern == null)
            return;

        foreach (Vector2Int offset in pattern)
        {
            Vector2Int rotatedOffset = RotateOffset(offset, rotation);

            Vector3Int targetCell = new Vector3Int(
                centerCell.x + rotatedOffset.x,
                centerCell.y + rotatedOffset.y,
                0
            );

            HighlightCell(targetCell, color, saveList);
        }
    }

    private void HighlightRoePattern(
        Vector3Int pivotCell,
        List<SkillPatternTile> pattern,
        int rotation,
        Color color,
        List<Vector3Int> saveList)
    {
        if (pattern == null)
            return;

        foreach (SkillPatternTile tile in pattern)
        {
            if (tile == null)
                continue;

            Vector2Int rotatedOffset =
                RotateOffset(tile.position, rotation);

            Vector3Int targetCell = new Vector3Int(
                pivotCell.x + rotatedOffset.x,
                pivotCell.y + rotatedOffset.y,
                0
            );

            HighlightCell(targetCell, color, saveList);
        }
    }

    private void HighlightCell(
        Vector3Int cell,
        Color color,
        List<Vector3Int> saveList)
    {
        if (!tilemap.HasTile(cell) || saveList.Contains(cell))
            return;

        tilemap.SetTileFlags(cell, TileFlags.None);
        tilemap.SetColor(cell, color);
        saveList.Add(cell);
    }


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
    public void UI_StartNormalSkill()
    {

        Debug.Log($"현재타일맵{tilemap}");
        // 💡 1. 이전 모든 조준 및 하이라이트 강제 완전 종료
        ClearAllHighlights();

        CharacterBase currentCaster = SelectionManager.CharacterBase;

        if (currentCaster.isSpawned == false)
        {
            Debug.Log("캐릭터를소환해주세요");
            return;
        }
        if (StageUIController.Instance == null) return;
        CharacterData currentData = StageUIController.Instance.CurrentData;

        if (currentCaster != null && currentData != null && currentData.normalSkill != null)
        {
            NormalSkill targetSkill = currentData.normalSkill;
            if (targetSkill != null)
            {
                StartSkillTargeting(targetSkill, currentCaster);
            }
        }
    }


    /// <summary>
    /// [UI 버튼 OnClick 전용] 1번 스킬 실행
    /// </summary>
    public void UI_StartSkill1()
    {

        Debug.Log($"현재타일맵{tilemap}");
        // 💡 1. 이전 모든 조준 및 하이라이트 강제 완전 종료
        ClearAllHighlights();
        
        CharacterBase currentCaster = SelectionManager.CharacterBase;

        if (currentCaster.isSpawned == false)
        {
            Debug.Log("캐릭터를소환해주세요");
            return;
        }
        if (StageUIController.Instance == null) return;
        CharacterData currentData = StageUIController.Instance.CurrentData;

        if (currentCaster != null && currentData != null && currentData.active != null && currentData.active.Length > 0)
        {
            ActiveSkill targetSkill = currentData.active[0];
            if (targetSkill != null)
            {
                StartSkillTargeting(targetSkill, currentCaster);
            }
        }
    }

    /// <summary>
    /// [UI 버튼 OnClick 전용] 2번 스킬 실행
    /// </summary>
    public void UI_StartSkill2()
    {
        // 💡 1. 이전 모든 조준 및 하이라이트 강제 완전 종료
        ClearAllHighlights();

        CharacterBase currentCaster = SelectionManager.CharacterBase;

        if (currentCaster.isSpawned == false)
        {
            Debug.Log("캐릭터를소환해주세요");
            return;
        }
        if (StageUIController.Instance == null) return;
        CharacterData currentData = StageUIController.Instance.CurrentData;

        if (currentCaster != null && currentData != null && currentData.active != null && currentData.active.Length > 0)
        {
            ActiveSkill targetSkill = currentData.active[0];
            if (targetSkill != null)
            {
                StartSkillTargeting(targetSkill, currentCaster);
            }
        }
    }

    /// <summary>
    /// [UI 버튼 OnClick 전용] 궁극기 실행
    /// </summary>
    public void UI_Ultimate()
    {
        // 💡 1. 이전 모든 조준 및 하이라이트 강제 완전 종료
        ClearAllHighlights();

        // 💡 2. 게이지 부족 시 차단
        if (ScrollUI.Instance == null || ScrollUI.Instance.GGscrollbar.value < 1.0f)
        {
            Debug.Log("궁극기 게이지가 부족합니다.");
            return;
        }


        // 💡 3. UI_StartSkill1과 동일하게 캐스터 및 데이터 참조
        CharacterBase currentCaster = SelectionManager.CharacterBase;
        if (StageUIController.Instance == null) return;
        CharacterData currentData = StageUIController.Instance.CurrentData;

        if (currentCaster != null && currentData != null && currentData.ultimateSkill != null)
        {
            UltimateSkill targetSkill = currentData.ultimateSkill;
            if (targetSkill != null)
            {
                StartSkillTargeting(targetSkill, currentCaster);
            }
        }
        ScrollUI.Instance.SubGaugeValue(1f);
    }

    private void ClearTileList(List<Vector3Int> tileList)
    {
        foreach (Vector3Int pos in tileList)
        {
            if (tilemap.HasTile(pos))
            {
                tilemap.SetTileFlags(pos, TileFlags.None);

                if (castRangeTiles.Contains(pos) && tileList == aoeTiles)
                {
                    tilemap.SetColor(pos, castRangeColor);
                }
                else
                {
                    tilemap.SetColor(pos, BattleFieldEffectSystem.TileColor(tilemap, pos));
                }
            }
        }
        tileList.Clear();
    }

    public void ClearAllHighlights()
    {
        isSkillTargetingActive = false;
        patternRotation = 0;

        // 💡 참조 비우기
        currentSkill = null;
        caster = null;

        lastMouseCell = new Vector3Int(-999, -999, -999);

        if (tilemap == null && PlacementManager.Instance != null)
        {
            tilemap = PlacementManager.Instance.tilemap;
        }

        if (tilemap != null)
        {
            ClearTileList(aoeTiles);
            ClearTileList(castRangeTiles);
            tilemap.RefreshAllTiles();
        }

        aoeTiles.Clear();
        castRangeTiles.Clear();
    }

    private void CancelTargeting()
    {
        ClearAllHighlights();
        Debug.Log("스킬 조준이 취소되었습니다.");
    }
}
