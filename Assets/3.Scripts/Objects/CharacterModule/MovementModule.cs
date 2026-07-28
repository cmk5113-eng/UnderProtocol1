using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MovementModule : CharacterModule, IRunnable
{
    public enum ID { Beak, Choi, Do, Ha, Jo, Kang, Lee, Min, Namgung, Pyo, Ryu, Seo };
    [SerializeField] private ID id;
    public static bool onHighLight = false;
    protected Vector3? targetDestination = null;
    protected Vector3? targetDirection = null;
    protected float targetTolerance;
    public bool IsMoving => targetDestination != null || targetDirection != null;

    public sealed override System.Type RegistrationType => typeof(MovementModule);

    private static int currentMoveRange;

    public override void OnRegistration(CharacterBase newOwner)
    {
        base.OnRegistration(newOwner);

        GameManager.OnPhysicsCharacter -= MovementUpdate;
        GameManager.OnPhysicsCharacter += MovementUpdate;

        // [수정] 마우스 클릭 이벤트 바인딩 복원
        InputManager.OnMouseLeftButton -= OnLeftClickInput;
        InputManager.OnMouseLeftButton += OnLeftClickInput;
    }

    public override void OnUnregistration(CharacterBase oldOwner)
    {
        base.OnUnregistration(oldOwner);

        GameManager.OnPhysicsCharacter -= MovementUpdate;
        InputManager.OnMouseLeftButton -= OnLeftClickInput;
    }

    private void OnLeftClickInput(bool isPressed, Vector2 screenPosition, Vector3 worldPosition)
    {
        if (!isPressed) return;
        HandleMouseClick(worldPosition);
    }

    public void MovementUpdate(float deltaTime)
    {
        Vector3 originPosition = transform.position;
        PhysicsUpdate(deltaTime);
        Vector3 positionDelta = transform.position - originPosition;

        if (Owner != null)
            Owner.MovementNotify(positionDelta);
    }

    public virtual void PhysicsUpdate(float deltaTime)
    {
        UpdateToDirection(deltaTime);
        UpdateToDestination(deltaTime);
    }

    public virtual float GetMoveSpeed() => 5.0f;
    public virtual float GetMoveSpeed(float deltaTime) => GetMoveSpeed() * deltaTime;

    public void Translate(Vector3 delta)
    {
        transform.position += delta;
    }

    public void UpdateToDirection(float deltaTime)
    {
        if (targetDirection is null) return;
        float speed = GetMoveSpeed(deltaTime);
        Translate(speed * targetDirection.Value);
    }

    public void UpdateToDestination(float deltaTime)
    {
        if (targetDestination is null) return;

        Vector3 currentMoveDirection = (targetDestination.Value - transform.position);
        float distance = currentMoveDirection.magnitude;

        float defaultTolerance = (this is MoveTileModule) ? 0.5f : 0.05f;
        float effectiveTolerance = Mathf.Max(targetTolerance, defaultTolerance);

        if (distance <= effectiveTolerance)
        {
            transform.position = targetDestination.Value;
            targetDestination = null;

            if (Owner != null)
            {
                Owner.steminaPoint = Mathf.Max(0, Owner.steminaPoint - 1);
            }
            return;
        }

        currentMoveDirection.Normalize();
        float speed = GetMoveSpeed(deltaTime);
        float resultMoveSpeed = Mathf.Min(speed, distance);
        Translate(resultMoveSpeed * currentMoveDirection);
    }

    public void MoveToDestination(Vector3 destination, float tolerance)
    {
        if (Owner != null && Owner.steminaPoint <= 0)
        {
            Debug.LogWarning($"[Movement] {Owner.Name}의 스태미나가 부족합니다. (Stamina: {Owner.steminaPoint})");
            return;
        }

        targetDirection = null;
        targetDestination = destination;
        targetTolerance = tolerance;
    }

    public void MoveToDirection(Vector3 direction)
    {
        if (Owner != null && Owner.steminaPoint <= 0)
        {
            Debug.LogWarning($"[Movement] {Owner.Name}의 스태미나가 부족합니다. (Stamina: {Owner.steminaPoint})");
            return;
        }

        targetDirection = direction.normalized;
        targetDestination = null;
    }

    public void StopMovement()
    {
        targetDirection = null;
        targetDestination = null;
    }

    void OnDestroy()
    {
        GameManager.OnPhysicsCharacter -= MovementUpdate;
        InputManager.OnMouseLeftButton -= OnLeftClickInput;
    }
   public void HandleMouseClick(Vector2 mouseWorldPos)
    {
        // 1. 이동 모드가 아니면 리턴
        if (ModeManager.Instance == null || ModeManager.Instance.CurrentMode != ModeManager.GameMode.Movement) return;

        // 2. 선택된 캐릭터가 없거나, '선택된 캐릭터'가 '나(this.Owner)'가 아니면 리턴
        if (SelectionManager.CharacterBase == null || SelectionManager.CharacterBase != Owner)
        {
            return;
        }

        if (onHighLight != true)
        {
            return;            
        }

        if (PlacementManager.Instance == null || PlacementManager.Instance.tilemap == null) return;
        Tilemap tilemap = PlacementManager.Instance.tilemap;

        Vector3Int targetCell = tilemap.WorldToCell(mouseWorldPos);
        targetCell.z = 0;

        if (this is MoveTileModule tileMoveModule)
        {
            List<Vector3Int> movableTiles = tileMoveModule.GetMovableTiles();
            bool isMovable = movableTiles.Exists(tile => tile.x == targetCell.x && tile.y == targetCell.y);

            if (isMovable)
            {
                // [이동 성공 시에만 이동 및 하이라이트 정리 실행]
                tileMoveModule.MoveToTile(targetCell);
                FinishMoveInput(); // ✅ 성공 시에만 호출해서 하이라이트/입력 종료
            }
            else
            {
                // [이동 실패 시] 
                // 다른 캐릭터 점유 타일이나 이동 불가능한 곳을 눌렀을 때는 
                // onHighLight를 끄지 않고 경고만 띄웁니다 (다음 클릭이 가능하도록).
                Debug.LogWarning($"[Movement Fail] {targetCell}은 이동 범위를 벗어났거나 이동 불가능합니다.");
            }
        }
        else
        {
            Vector3 targetWorldPos = new Vector3(mouseWorldPos.x, mouseWorldPos.y, Owner.transform.position.z);
            float distance = Vector2.Distance(Owner.transform.position, targetWorldPos);
            if (distance <= currentMoveRange)
            {
                MoveToDestination(targetWorldPos, 0.05f);
                FinishMoveInput(); // ✅ 성공 시에만 호출
            }
            else
            {
                Debug.LogWarning($"[Movement Fail] {targetWorldPos}는 이동 범위를 벗어났습니다.");
            }
        }
    }
    public void OnPressMoveButton()
    {
        currentMoveRange = Owner?.mobility ?? (this is MoveTileModule m ? m.mobility : 0);
        TileHighlight(transform.position, currentMoveRange);
    }

    public void OnMovementModeChange(CharacterBase character)
    {
        
        if (ModeManager.Instance != null)
        {
            ModeManager.Instance.CurrentMode = ModeManager.GameMode.Movement;
        }
        SelectionManager.SetSelectedCharacter(character);
        SelectionManager.CharacterData = character.Data;
        StageUIController.Instance.Refresh();

    }

    private void FinishMoveInput()
    {
        ClearTileHighlight();
    }

    public void TileHighlight(Vector3 centerPosition, int range)
    {
        
        if (ModeManager.Instance == null || ModeManager.Instance.CurrentMode != ModeManager.GameMode.Movement) return;
        if (PlacementManager.Instance == null || PlacementManager.Instance.tilemap == null) return;

        Tilemap tilemap = PlacementManager.Instance.tilemap;
        ClearTileHighlight();
        onHighLight = true;

        // [수정] MoveTileModule인 경우 실제 Pathfinding 기반 타일 범위만 정확히 하이라이트
        if (this is MoveTileModule moveTile)
        {
            List<Vector3Int> movableTiles = moveTile.GetMovableTiles();
            foreach (Vector3Int targetCell in movableTiles)
            {
                if (tilemap.HasTile(targetCell))
                {
                    tilemap.SetTileFlags(targetCell, TileFlags.None);
                    tilemap.SetColor(targetCell, new Color(0f, 0.5f, 1f, 0.5f));
                }
            }
        }
        else
        {
            Vector3Int centerCell = tilemap.WorldToCell(centerPosition);
            for (int x = -range; x <= range; x++)
            {
                for (int y = -range; y <= range; y++)
                {
                    if (Mathf.Abs(x) + Mathf.Abs(y) <= range)
                    {
                        Vector3Int targetCell = new Vector3Int(centerCell.x + x, centerCell.y + y, 0);
                        if (tilemap.HasTile(targetCell))
                        {
                            tilemap.SetTileFlags(targetCell, TileFlags.None);
                            tilemap.SetColor(targetCell, new Color(0f, 0.5f, 1f, 0.5f));
                        }
                    }
                }
            }
        }
    }

    public void ClearTileHighlight()
    {
        onHighLight = false;
        if (PlacementManager.Instance == null || PlacementManager.Instance.tilemap == null) return;
        Tilemap tilemap = PlacementManager.Instance.tilemap;

        foreach (Vector3Int pos in tilemap.cellBounds.allPositionsWithin)
        {
            if (tilemap.HasTile(pos))
            {
                tilemap.SetTileFlags(pos, TileFlags.None);
                tilemap.SetColor(pos, Color.white);
            }
        }
    }
}