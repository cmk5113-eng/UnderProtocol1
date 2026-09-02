using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MoveTileModule : MovementModule
{

    public static MoveTileModule Instance;
    public enum MoveType { PlayerMove, MonsterMove }
    public MoveType moveType = MoveType.PlayerMove;

    public int mobility = 3; // 이동력

    private Tilemap tilemap;
    public Tilemap TM
    {
        get
        {
            if (tilemap == null && PlacementManager.Instance != null)
                tilemap = PlacementManager.Instance.tilemap;
            return tilemap;
        }
    }

    public Vector3Int CurrentTile { get; private set; }
    private Vector3Int previousTile; // [추가] 이동 전 출발 타일 저장용

    private Vector3Int mapOrigin;

    private void Start()
    {
        InitializeMapOrigin();
        UpdateCurrentTile();
    }

    public void InitializeMapOrigin()
    {
        var tm = TM;
        if (tm == null) return;

        tm.CompressBounds();
        BoundsInt bounds = tm.cellBounds;
        mapOrigin = new Vector3Int(bounds.xMin, bounds.yMax - 1, 0);
    }

    public Vector2Int ConvertToCustomPosition(Vector3Int rawCell)
    {
        int customX = rawCell.x - mapOrigin.x + 1;
        int customY = mapOrigin.y - rawCell.y + 1;
        return new Vector2Int(customX, customY);
    }

    public override bool IsOuterTile(Vector3Int rawCell)
    {
        Vector2Int custom = ConvertToCustomPosition(rawCell);
        return (custom.x == 1 || custom.x == 10 || custom.y == 1 || custom.y == 10);
    }

    public void UpdateCurrentTile()
    {
        var tm = TM;
        if (tm != null)
        {
            CurrentTile = tm.WorldToCell(transform.position);
            CurrentTile = new Vector3Int(CurrentTile.x, CurrentTile.y, 0);
        }
    }

    /// <summary>
    /// 타일에 캐릭터나 장애물이 없는지 판별
    /// </summary>
    public bool CanEnterTile(Vector3Int tilePos)
    {
        if (PlacementManager.Instance == null) return true;

        TileData data = PlacementManager.Instance.GetTileData(tilePos);
        if (data == null) return false;

        return data.isempty;
    }

    /// <summary>
    /// 이동력 범위 내 타일 중 "외곽 타일"이면서 "비어 있는(점유 X)" 타일만 반환
    /// </summary>
    public List<Vector3Int> GetMovableTiles()
    {
        List<Vector3Int> movable = new List<Vector3Int>();
        UpdateCurrentTile();

        int range = Owner != null ? Owner.mobility : mobility;

        for (int dx = -range; dx <= range; dx++)
        {
            for (int dy = -range; dy <= range; dy++)
            {
                if (Mathf.Abs(dx) + Mathf.Abs(dy) > range) continue;

                Vector3Int targetCell = new Vector3Int(CurrentTile.x + dx, CurrentTile.y + dy, 0);

                if (TM == null || !TM.HasTile(targetCell)) continue;

                if (moveType == MoveType.PlayerMove && !IsOuterTile(targetCell)) continue;

                if (!CanEnterTile(targetCell)) continue;

                movable.Add(targetCell);
            }
        }

        return movable;
    }

    /// <summary>
    /// 클릭한 목적지 타일로 직접 이동
    /// </summary>
    public void MoveToTileDirect(Vector3Int targetCell)
    {
        var tm = TM;
        if (tm == null) return;

        // [핵심] 이동 시작 전 현재 위치(출발 타일)를 기억
        UpdateCurrentTile();
        previousTile = CurrentTile;

        Vector3 targetPos = tm.GetCellCenterWorld(targetCell);
        targetPos.z = transform.position.z;

        MoveToDestination(targetPos, 0.05f);
    }

    public void MoveToTile(Vector3Int targetCell)
    {
        MoveToTileDirect(targetCell);
    }

    /// <summary>
    /// 이동 완료 후 점유 타일 상태를 업데이트합니다.
    /// </summary>
    public void OnMoveComplete()
    {
        UpdateCurrentTile(); // 도착 타일 갱신

        if (PlacementManager.Instance != null)
        {
            // 1. 이전 타일 점유 해제 (isempty = true)
            TileData prevData = PlacementManager.Instance.GetTileData(previousTile);
            if (prevData != null)
            {
                prevData.isempty = true;
            }

            // 2. 새로운 도착 타일 점유 설정 (isempty = false)
            TileData currentData = PlacementManager.Instance.GetTileData(CurrentTile);
            if (currentData != null)
            {
                currentData.isempty = false;
            }

            Debug.Log($"[Tile Occupancy] 이전 타일:{previousTile}(빈 공간 처리) -> 현재 타일:{CurrentTile}(점유 처리)");
        }
    }

    public bool TryStepByInput(Vector2 input)
    {
        const float deadZone = 0.1f;
        if (input.sqrMagnitude < deadZone * deadZone) return false;

        Vector3Int step = Vector3Int.zero;
        if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
            step = input.x > 0 ? Vector3Int.right : Vector3Int.left;
        else
            step = input.y > 0 ? Vector3Int.up : Vector3Int.down;

        return TryStepByInput(step);
    }

    public void ClearCharacterPosition()
    {
        if (PlacementManager.Instance == null)
            return;

        UpdateCurrentTile();

        TileData data = PlacementManager.Instance.GetTileData(CurrentTile);

        if (data != null)
        {
            data.isempty = true;
            Debug.Log($"[Tile Occupancy] 캐릭터 삭제: {CurrentTile} → 빈 타일");
        }
    }


    public bool TryStepByInput(Vector3Int direction)
    {
        UpdateCurrentTile();
        Vector3Int targetCell = CurrentTile + direction;

        if (IsOuterTile(targetCell) && CanEnterTile(targetCell))
        {
            MoveToTileDirect(targetCell);
            return true;
        }

        return false;
    }
}