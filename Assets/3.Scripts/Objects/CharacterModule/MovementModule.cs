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

        InputManager.OnMouseLeftButton -= OnLeftClickInput;
        InputManager.OnMouseLeftButton += OnLeftClickInput;

        // 1. 우클릭 이벤트 구독 추가
        InputManager.OnMouseRightButton -= OnRightClickInput;
        InputManager.OnMouseRightButton += OnRightClickInput;
    }

    public override void OnUnregistration(CharacterBase oldOwner)
    {
        base.OnUnregistration(oldOwner);

        GameManager.OnPhysicsCharacter -= MovementUpdate;
        InputManager.OnMouseLeftButton -= OnLeftClickInput;

        // 2. 우클릭 이벤트 구독 해제 추가
        InputManager.OnMouseRightButton -= OnRightClickInput;
    }

    // 3. 우클릭 입력 처리 함수 구현
    private void OnRightClickInput(bool isPressed, Vector2 screenPosition, Vector3 worldPosition)
    {
        if (!isPressed) return;

        // 게임 모드가 Movement(이동) 모드이고, 현재 하이라이트가 표시되어 있는 상태일 때만 실행
        if (ModeManager.Instance != null && ModeManager.Instance.CurrentMode == ModeManager.GameMode.Movement)
        {
            if (onHighLight)
            {
                ClearTileHighlight();
                Debug.Log("[MovementModule] 우클릭으로 타일 하이라이트를 해제했습니다.");
            }
        }
    }

    void OnDestroy()
    {
        GameManager.OnPhysicsCharacter -= MovementUpdate;
        InputManager.OnMouseLeftButton -= OnLeftClickInput;

        // OnDestroy 시에도 우클릭 이벤트 해제
        InputManager.OnMouseRightButton -= OnRightClickInput;
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

    public virtual float GetMoveSpeed() => 15.0f; // 즉시 이동 느낌을 위해 이동 속도를 높임
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

        float defaultTolerance = (this is MoveTileModule) ? 0.05f : 0.05f;
        float effectiveTolerance = Mathf.Max(targetTolerance, defaultTolerance);

        if (distance <= effectiveTolerance)
        {
            transform.position = targetDestination.Value;
            targetDestination = null;

            // 이동 완료 후 스태미나 차감
            if (Owner != null)
            {
                Owner.steminaPoint = Mathf.Max(0, Owner.steminaPoint - 1);
            }

            // 이동 완료 후 자식 클래스의 좌표 갱신 호출
            if (this is MoveTileModule moveTile)
            {
                moveTile.OnMoveComplete();
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

    public virtual bool IsOuterTile(Vector3Int tilePos)
    {
        if (this is MoveTileModule moveTile)
        {
            return moveTile.IsOuterTile(tilePos);
        }
        return false;
    }

    public void HandleMouseClick(Vector2 mouseWorldPos)
    {
        if (ModeManager.Instance == null || ModeManager.Instance.CurrentMode != ModeManager.GameMode.Movement) return;
        if (SelectionManager.CharacterBase == null || SelectionManager.CharacterBase != Owner) return;
        if (onHighLight != true) return;
        if (PlacementManager.Instance == null || PlacementManager.Instance.tilemap == null) return;

        Tilemap tilemap = PlacementManager.Instance.tilemap;
        Vector3Int targetCell = tilemap.WorldToCell(mouseWorldPos);
        targetCell.z = 0;

        if (this is MoveTileModule tileMoveModule)
        {
            // 이동 가능한 타일 목록 가져오기 (외곽 + 빈 타일만 필터링되어 전달됨)
            List<Vector3Int> validTiles = tileMoveModule.GetMovableTiles();

            bool isMovable = validTiles.Contains(targetCell);

            if (isMovable)
            {
                // 클릭한 타일 위치로 곧바로 이동
                tileMoveModule.MoveToTileDirect(targetCell);
                FinishMoveInput();
            }
            else
            {
                Debug.LogWarning($"[Movement Fail] Raw:{targetCell} 은 이동 불가능하거나 외곽/빈 타일이 아닙니다.");
            }
        }
        else
        {
            Vector3 targetWorldPos = new Vector3(mouseWorldPos.x, mouseWorldPos.y, Owner.transform.position.z);
            float distance = Vector2.Distance(Owner.transform.position, targetWorldPos);
            if (distance <= currentMoveRange)
            {
                MoveToDestination(targetWorldPos, 0.05f);
                FinishMoveInput();
            }
            else
            {
                Debug.LogWarning($"[Movement Fail] {targetWorldPos}는 이동 범위를 벗어났습니다.");
            }
        }
    }

    public void OnCharacterClicked()
    {
        Debug.Log($"[MoveTileModule] {gameObject.name} 캐릭터 선택 완료");
        if (TryGetComponent<CharacterBase>(out var character))
        {
            SelectionManager.SetSelectedCharacter(character);
            OnMovementModeChange(character);
            OnPressMoveButton();
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

        if (this is MoveTileModule moveTile)
        {
            // 이동력 기반 외곽+빈 타일 목록 가져오기
            List<Vector3Int> highlightTiles = moveTile.GetMovableTiles();
            Color highlightColor = new Color(0f, 0.5f, 1f, 0.5f);

            foreach (Vector3Int targetCell in highlightTiles)
            {
                if (tilemap.HasTile(targetCell))
                {
                    tilemap.SetTileFlags(targetCell, TileFlags.None);
                    tilemap.SetColor(targetCell, highlightColor);
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