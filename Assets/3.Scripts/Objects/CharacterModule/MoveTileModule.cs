using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;


public enum MoveType { PlayerMove, EnemyMove, HealSkillMove, AttackSkillMove, OutlineSkillMove, FieldSkillMove }

public class MoveTileModule : MovementModule
{
    // ���� public tilemap ���� (Inspector�� ����Ǿ� ������ �켱 ���)
    public Tilemap tilemap;
    public int mobility = 3;    
    public MoveType MoveType;

    // ���� ��ġ
    private Queue<Vector3Int> pathQueue = new Queue<Vector3Int>();
    private Vector3Int currentTargetTile;

    // ������ Tilemap ���� ���� (Inspector�� ������ PlacementManager���� ������)
    Tilemap TM => tilemap != null ? tilemap : (PlacementManager.Instance != null ? PlacementManager.Instance.tilemap : null);

    // CurrentTile transform.position�� x,y�� ����ϵ� Ÿ�ϸ��� Z(���)�� ���缭 WorldToCell ȣ��
    public Vector3Int CurrentTile
    {
        get
        {
            var tm = TM;
            if (tm == null) return Vector3Int.zero;

            // [�ٽ� ����] 
            // 1. ĳ������ ���� ��ġ�� �����ɴϴ�.
            Vector3 pos = transform.position;

            // 2. Ÿ�ϸ��� Z���� ������ ����ϴ�. (��� ���� ����)
            pos.z = tm.transform.position.z;

            // 3. WorldToCell�� ��ȯ�մϴ�.
            Vector3Int cell = tm.WorldToCell(pos);

            // [�����] ���� ��ȯ�� ��ǥ�� ������ �´��� Ȯ��
            // Debug.Log($"[Pos Check] World: {transform.position} -> Cell: {cell}");

            return cell;
        }
    }
    
    // �ܺο��� ȣ�� �̵� ������ Ÿ�ϵ�
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

    // �� �޼��� �Է�(Vector2)���� �� ĭ �̵� �õ�
    // �Է��� ����(x �Ǵ� y)���� ��/��/��/�� ����. �̵� ���̸� ����(�� �Է´� �� Ÿ��)
    public bool TryStepByInput(Vector2 input)
    {
       
        // 1. �̵� ���̸� �Է��� �ƿ� ���� ���� (���� �̵� ����)
        if (targetDestination != null || pathQueue.Count > 0) return false;

        const float deadZone = 0.1f;
        if (input.sqrMagnitude < deadZone * deadZone) return false;

        // 2. ���⸸ ���� (�Է��� ����� ����)
        Vector3Int step = Vector3Int.zero;
        if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
            step = input.x > 0 ? Vector3Int.right : Vector3Int.left;
        else
            step = input.y > 0 ? Vector3Int.up : Vector3Int.down;

        // 3. ������ Ÿ�� ���
        Vector3Int nextTile = CurrentTile + step;

        // 4. �� �� �ִ� Ÿ������ �˻�
        if (!CanEnterTile(nextTile)) return false;

        // 5. Ÿ�ϸʿ��� �� �߽� ��ǥ ������ ������ ����
        var tm = TM;
        if (tm == null) return false;

        Vector3 worldTargetPos = tm.GetCellCenterWorld(nextTile);
        // �߿� ĳ���Ϳ� ���� Z ������� ������� ����(�� Y����) �̵��� ��Ȯ����
        worldTargetPos.z = transform.position.z;

        MoveToDestination(worldTargetPos, 0.01f);
   
        return true;
    }

    // ------------------------
    // �ٽ� Movement override
    // ------------------------
    //private bool isSnapping = false; // ��� ���� �÷���

    public override void PhysicsUpdate(float deltaTime)
    {
        var tm = TM;
        if (tm == null) return;

        // ���� �̵� ó��
        base.PhysicsUpdate(deltaTime);

        // ���� üũ
        if (targetDestination != null)
        {
            float dist = Vector3.Distance(transform.position, targetDestination.Value);

            if (dist <= targetTolerance)
            {
                // 이동 전 타일
                Vector3Int oldTile = CurrentTile;

                targetDestination = null;

                // 정확히 타일 중앙으로
                Vector3 center = tm.GetCellCenterWorld(currentTargetTile);
                center.z = transform.position.z;
                transform.position = center;

                // 이동 후 타일
                Vector3Int newTile = CurrentTile;

                if (TryGetComponent<CharacterBase>(out var character))
                {
                    PlacementManager.Instance.GetTileData(oldTile).Character = null;
                    PlacementManager.Instance.GetTileData(newTile).Character = character;
                }
            }
        }

        // ���� Ÿ�� �̵�
        if (targetDestination == null && pathQueue.Count > 0)
        {
            currentTargetTile = pathQueue.Dequeue();

            Vector3 worldPos = tm.GetCellCenterWorld(currentTargetTile);
            worldPos.z = transform.position.z;

            MoveToDestination(worldPos, 0.01f);
        }
        
    }

    // �� �߽� ��ǥ ���� (�ܺο��� ��� ����)
    public Vector3 GetCellCenterWorld(Vector3Int cell)
    {
        var tm = TM;
        if (tm == null) return transform.position;
        Vector3 p = tm.GetCellCenterWorld(cell);
        p.z = transform.position.z;
        return p;
    }

    // ------------------------
    // ����ư ��� (���� ����)
    // ------------------------

    private List<Vector3Int> FindPath(Vector3Int start, Vector3Int end)
    {
        List<Vector3Int> path = new List<Vector3Int>();
        Vector3Int current = start;

        // ���� ���� ������ ���� ���� ��ġ (�ִ� 100ĭ)
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

            // �� �α�: ���� ��� ĭ���� ��� �������� ��������, �׸��� �װ� �������� ���
           
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

    // public���� ����: PlacementManager�� tilemap�� ����Ͽ� �Ǵ�
    public bool CanEnterTile(Vector3Int tile)
    {
        var tm = TM;
        if (tm == null) return false;

        // 타일 존재 여부
        if (!tm.HasTile(tile))
        {
            Debug.LogWarning($"[CanEnter Fail] {tile}에 타일이 없습니다.");
            return false;
        }

        // TileData 가져오기
        TileData data = PlacementManager.Instance.GetTileData(tile);

        // 빈 타일인지 확인
        if (!data.isempty)
        {
            Debug.LogWarning($"[CanEnter Fail] {tile}에는 이미 캐릭터가 있습니다.");
            return false;
        }

        // 기존 MoveType 검사
        switch (MoveType)
        {
            case MoveType.PlayerMove:

                bool isOutline = IsOutline(tile);

                if (!isOutline)
                {
                    Debug.LogWarning($"[CanEnter Fail] {tile}는 외곽 타일입니다.");
                    return false;
                }

                break;
        }

        return true;
    }
    // ------------------------
    // Ÿ�� / ���� üũ (�ӽ�)
    // ------------------------

    bool IsField(Vector3Int tile)
    {
        // TODO: Tilemap ������ �����ϰų� �̸����� �Ǻ�
        return true;
    }

    bool IsOutline(Vector3Int tile)
    {
        // TODO: �ܰ� Ÿ�� �Ǻ� ����
        return true;
    }

    bool HasCharacter(Vector3Int tile)
    {
        // TODO: Dictionary<Vector3Int, CharacterBase>�� ���� ��õ
        return false;
    }
}

