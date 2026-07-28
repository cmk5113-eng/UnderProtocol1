using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public enum MoveType { PlayerMove, EnemyMove, HealSkillMove, AttackSkillMove, OutlineSkillMove, FieldSkillMove }

public class MoveTileModule : MovementModule
{
    public Tilemap tilemap;
    public int mobility = 3;
    public MoveType MoveType;

    private Queue<Vector3Int> pathQueue = new Queue<Vector3Int>();
    private Vector3Int currentTargetTile;

    Tilemap TM => tilemap != null ? tilemap : (PlacementManager.Instance != null ? PlacementManager.Instance.tilemap : null);

    // [수정] 백킹 필드를 추가하여 런타임 이동 타일 갱신 지원
    private Vector3Int? cachedCurrentTile = null;

    public Vector3Int CurrentTile
    {
        get
        {
            if (cachedCurrentTile.HasValue)
                return cachedCurrentTile.Value;

            var tm = TM;
            if (tm == null) return Vector3Int.zero;

            Vector3 pos = transform.position;
            pos.z = tm.transform.position.z;
            return tm.WorldToCell(pos);
        }
        set
        {
            cachedCurrentTile = value;
        }
    }

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
        Debug.Log($"Start = {CurrentTile}");
        Debug.Log($"End = {targetTile}");
        var path = FindPath(CurrentTile, targetTile);

        Debug.Log(path == null ? "null" : $"count = {path.Count}");
        if (path == null || path.Count == 0) return;

        // 1. 기존 타일 점유 해제
        PlacementManager.Instance.SetTileEmpty(CurrentTile, true);

        // 2. 이동할 목표 타일 점유 설정
        PlacementManager.Instance.SetTileEmpty(targetTile, false);

        pathQueue.Clear();
        foreach (var tile in path)
            pathQueue.Enqueue(tile);

        // 3. 현재 위치 타일 좌표 갱신
        CurrentTile = targetTile;
    }

    public bool TryStepByInput(Vector2 input)
    {
        if (targetDestination != null || pathQueue.Count > 0) return false;

        const float deadZone = 0.1f;
        if (input.sqrMagnitude < deadZone * deadZone) return false;

        Vector3Int step = Vector3Int.zero;
        if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
            step = input.x > 0 ? Vector3Int.right : Vector3Int.left;
        else
            step = input.y > 0 ? Vector3Int.up : Vector3Int.down;

        Vector3Int nextTile = CurrentTile + step;

        if (!CanEnterTile(nextTile)) return false;

        var tm = TM;
        if (tm == null) return false;

        Vector3 worldTargetPos = tm.GetCellCenterWorld(nextTile);
        worldTargetPos.z = transform.position.z;

        MoveToDestination(worldTargetPos, 0.01f);
        return true;
    }

    public override void PhysicsUpdate(float deltaTime)
    {
        var tm = TM;
        if (tm == null) return;

        base.PhysicsUpdate(deltaTime);

        if (targetDestination != null)
        {
            float dist = Vector3.Distance(transform.position, targetDestination.Value);

            if (dist <= targetTolerance)
            {
                Vector3Int oldTile = CurrentTile;
                targetDestination = null;

                Vector3 center = tm.GetCellCenterWorld(currentTargetTile);
                center.z = transform.position.z;
                transform.position = center;

                // 캐시 초기화하여 실제 위치 재계산
                cachedCurrentTile = null;
                Vector3Int newTile = CurrentTile;

                if (TryGetComponent<CharacterBase>(out var character))
                {
                    var oldData = PlacementManager.Instance.GetTileData(oldTile);
                    if (oldData != null) oldData.Character = null;

                    var newData = PlacementManager.Instance.GetTileData(newTile);
                    if (newData != null) newData.Character = character;
                }
            }
        }

        if (targetDestination == null && pathQueue.Count > 0)
        {
            currentTargetTile = pathQueue.Dequeue();

            Vector3 worldPos = tm.GetCellCenterWorld(currentTargetTile);
            worldPos.z = transform.position.z;

            MoveToDestination(worldPos, 0.01f);
        }
    }

    public Vector3 GetCellCenterWorld(Vector3Int cell)
    {
        var tm = TM;
        if (tm == null) return transform.position;
        Vector3 p = tm.GetCellCenterWorld(cell);
        p.z = transform.position.z;
        return p;
    }

    private List<Vector3Int> FindPath(Vector3Int start, Vector3Int end)
    {
        Queue<Vector3Int> queue = new Queue<Vector3Int>();
        Dictionary<Vector3Int, Vector3Int> parent = new Dictionary<Vector3Int, Vector3Int>();
        HashSet<Vector3Int> visited = new HashSet<Vector3Int>();

        queue.Enqueue(start);
        visited.Add(start);

        while (queue.Count > 0)
        {
            Vector3Int current = queue.Dequeue();

            if (current == end)
                break;

            foreach (var next in GetNeighbors(current))
            {
                
                if (visited.Contains(next))
                    continue;

                if (!CanEnterTile(next) && next != end)
                    continue;

                visited.Add(next);
                parent[next] = current;
                queue.Enqueue(next);
            }
        }

        if (!visited.Contains(end))
            return null;

        List<Vector3Int> path = new List<Vector3Int>();

        Vector3Int p = end;

        while (p != start)
        {
            path.Add(p);
            p = parent[p];
        }

        path.Reverse();

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

    public bool CanEnterTile(Vector3Int tile)
    {
        TileData data = PlacementManager.Instance.GetTileData(tile);
        if (data == null) return false;
        return data.isempty;
    }

    // [수정] 캐릭터 클릭 시 이동 모드 전환 및 하이라이트 연동
    public void OnCharacterClicked()
    {
        Debug.Log($"[MoveTileModule] {gameObject.name} 캐릭터 선택 완료");

        if (TryGetComponent<CharacterBase>(out var character))
        {
            SelectionManager.SetSelectedCharacter(character);
            OnMovementModeChange(character);
            OnPressMoveButton(); // 하이라이트 호출
        }
    }
}