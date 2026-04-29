using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;


public enum MoveType { PlayerMove, EnemyMove, HealSkillMove, AttackSkillMove, OutlineSkillMove, FieldSkillMove }

public class MoveTileModule : MovementModule
{
    // 기존 public tilemap 유지 (Inspector에 연결되어 있으면 우선 사용)
    public Tilemap tilemap;
    public int mobility = 3;    
    public MoveType MoveType;

    // 현재 위치
    private Queue<Vector3Int> pathQueue = new Queue<Vector3Int>();
    private Vector3Int currentTargetTile;

    // 안전한 Tilemap 참조 헬퍼 (Inspector에 없으면 PlacementManager에서 가져옴)
    Tilemap TM => tilemap != null ? tilemap : (PlacementManager.Instance != null ? PlacementManager.Instance.tilemap : null);

    // CurrentTile transform.position의 x,y를 사용하되 타일맵의 Z(평면)를 맞춰서 WorldToCell 호출
    public Vector3Int CurrentTile
    {
        get
        {
            var tm = TM;
            if (tm == null) return Vector3Int.zero;

            // [핵심 보정] 
            // 1. 캐릭터의 현재 위치를 가져옵니다.
            Vector3 pos = transform.position;

            // 2. 타일맵의 Z평면과 강제로 맞춥니다. (계산 오차 방지)
            pos.z = tm.transform.position.z;

            // 3. WorldToCell로 변환합니다.
            Vector3Int cell = tm.WorldToCell(pos);

            // [디버그] 현재 변환된 좌표가 실제와 맞는지 확인
            // Debug.Log($"[Pos Check] World: {transform.position} -> Cell: {cell}");

            return cell;
        }
    }

    // 외부에서 호출 이동 가능한 타일들
    public List<Vector3Int> GetMovableTiles()
    {
        List<Vector3Int> result = new List<Vector3Int>();
        Vector3Int start = CurrentTile;

        Queue<(Vector3Int pos, int cost)> queue = new Queue<(Vector3Int, int)>();
        HashSet<Vector3Int> visited = new HashSet<Vector3Int>();

        queue.Enqueue((start, 0));
        visited.Add(start);

        while (queue.Count > 0)
        {
            var (pos, cost) = queue.Dequeue();

            if (cost > mobility) continue;

            if (pos != start)
                result.Add(pos);

            foreach (var next in GetNeighbors(pos))
            {
                if (visited.Contains(next)) continue;

                if (!CanEnterTile(next)) continue;

                visited.Add(next);
                queue.Enqueue((next, cost + 1));
            }
        }

        return result;
    }

    public void MoveToTile(Vector3Int targetTile)
    {


        var path = FindPath(CurrentTile, targetTile);

        pathQueue.Clear();

        foreach (var tile in path)
            pathQueue.Enqueue(tile);
    }

    // 새 메서드 입력(Vector2)으로 한 칸 이동 시도
    // 입력의 주축(x 또는 y)으로 상/하/좌/우 결정. 이동 중이면 무시(한 입력당 한 타일)
    public bool TryStepByInput(Vector2 input)
    {
       
        // 1. 이동 중이면 입력을 아예 받지 않음 (연속 이동 방지)
        if (targetDestination != null || pathQueue.Count > 0) return false;

        const float deadZone = 0.1f;
        if (input.sqrMagnitude < deadZone * deadZone) return false;

        // 2. 방향만 추출 (입력의 세기는 무시)
        Vector3Int step = Vector3Int.zero;
        if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
            step = input.x > 0 ? Vector3Int.right : Vector3Int.left;
        else
            step = input.y > 0 ? Vector3Int.up : Vector3Int.down;

        // 3. 목적지 타일 계산
        Vector3Int nextTile = CurrentTile + step;

        // 4. 갈 수 있는 타일인지 검사
        if (!CanEnterTile(nextTile)) return false;

        // 5. 타일맵에서 셀 중심 좌표 가져와 목적지 설정
        var tm = TM;
        if (tm == null) return false;

        Vector3 worldTargetPos = tm.GetCellCenterWorld(nextTile);
        // 중요 캐릭터와 같은 Z 평면으로 맞춰줘야 수직(예 Y방향) 이동이 정확해짐
        worldTargetPos.z = transform.position.z;

        MoveToDestination(worldTargetPos, 0.01f);
   
        return true;
    }

    // ------------------------
    // 핵심 Movement override
    // ------------------------
    private bool isSnapping = false; // 재귀 방지 플래그

    public override void PhysicsUpdate(float deltaTime)
    {

        var tm = TM;
        if (tm == null) return;

        // 현재 이동 처리
        base.PhysicsUpdate(deltaTime);

        // 도착 체크
        if (targetDestination != null)
        {
            float dist = Vector3.Distance(transform.position, targetDestination.Value);

            if (dist <= targetTolerance)
            {
                targetDestination = null;
            }
        }

        // 다음 타일 이동
        if (targetDestination == null && pathQueue.Count > 0)
        {
            currentTargetTile = pathQueue.Dequeue();

            Vector3 worldPos = tm.GetCellCenterWorld(currentTargetTile);
            worldPos.z = transform.position.z;

            MoveToDestination(worldPos, 0.01f);
        }
    }

    // 셀 중심 좌표 제공 (외부에서 사용 가능)
    public Vector3 GetCellCenterWorld(Vector3Int cell)
    {
        var tm = TM;
        if (tm == null) return transform.position;
        Vector3 p = tm.GetCellCenterWorld(cell);
        p.z = transform.position.z;
        return p;
    }

    // ------------------------
    // 맨해튼 경로 (간단 버전)
    // ------------------------

    private List<Vector3Int> FindPath(Vector3Int start, Vector3Int end)
    {
        Debug.Log($"[FindPath] 시작: {start} -> 목적지: {end}");
        List<Vector3Int> path = new List<Vector3Int>();
        Vector3Int current = start;

        // 무한 루프 방지를 위한 안전 장치 (최대 100칸)
        int safetyBreak = 0;

        while (current != end && safetyBreak < 100)
        {
            safetyBreak++;
            Vector3Int delta = end - current;
            Vector3Int step;

            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            {
                step = new Vector3Int(delta.x > 0 ? 1 : -1, 0, 0);
            }
            else
            {
                step = new Vector3Int(0, delta.y > 0 ? 1 : -1, 0);
            }

            Vector3Int next = current + step;
            bool canEnter = CanEnterTile(next);

            // 상세 로그: 현재 어느 칸에서 어느 방향으로 가려는지, 그리고 그게 가능한지 출력
           
            if (!canEnter)
            {
                break;
            }

            current = next;
            path.Add(current);
        }

        return path;
    }
    List<Vector3Int> GetNeighbors(Vector3Int pos)
    {
        return new List<Vector3Int>
        {
            pos + Vector3Int.up,
            pos + Vector3Int.down,
            pos + Vector3Int.left,
            pos + Vector3Int.right
        };
    }

    // public으로 노출: PlacementManager의 tilemap을 사용하여 판단
    public bool CanEnterTile(Vector3Int tile)
    {
        var tm = TM;
        if (tm == null) return false;

        // 타일 데이터 존재 확인
        if (!tm.HasTile(tile))
        {
            Debug.LogWarning($"[CanEnter Fail] {tile} 좌표에 타일 에셋이 없습니다! (HasTile == false)");
            return false;
        }

        // MoveType별 규칙 확인
        switch (MoveType)
        {
            case MoveType.PlayerMove:
                bool isOutline = IsOutline(tile);
                bool hasChar = HasCharacter(tile);
                if (!isOutline || hasChar)
                {
                    Debug.LogWarning($"[CanEnter Fail] {tile} 판정결과 -> Outline인가?: {isOutline}, 캐릭터있는가?: {hasChar}");
                    return false;
                }
                break;
                // 다른 케이스들도 동일하게 로그 추가...
        }

        return true;
    }
    // ------------------------
    // 타일 / 점유 체크 (임시)
    // ------------------------

    bool IsField(Vector3Int tile)
    {
        // TODO: Tilemap 종류로 구분하거나 이름으로 판별
        return true;
    }

    bool IsOutline(Vector3Int tile)
    {
        // TODO: 외곽 타일 판별 로직
        return true;
    }

    bool HasCharacter(Vector3Int tile)
    {
        // TODO: Dictionary<Vector3Int, CharacterBase>로 관리 추천
        return false;
    }
}

