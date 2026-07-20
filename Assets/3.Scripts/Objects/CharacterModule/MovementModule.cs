using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MovementModule : CharacterModule, IRunnable
{
    protected Vector3? targetDestination = null;
    protected Vector3? targetDirection = null;
    protected float targetTolerance;
    public bool IsMoving => targetDestination != null || targetDirection != null;

    public sealed override System.Type RegistrationType => typeof(MovementModule);

    public override void OnRegistration(CharacterBase newOwner)
    {
        base.OnRegistration(newOwner);

        // GameManager 물리 루프 등록
        GameManager.OnPhysicsCharacter -= MovementUpdate;
        GameManager.OnPhysicsCharacter += MovementUpdate;

        // 마우스 이벤트 등록
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

        if (ModeManager.Instance != null && ModeManager.Instance.CurrentMode == ModeManager.GameMode.Movement)
        {
            HandleMouseClick();
        }
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

        // 💡 [수정] 타일 이동 모듈인 경우 허용 오차를 대폭 늘려줍니다 (0.05f -> 0.5f)
        // 보통 2D 타일 크기가 1x1이므로, 0.5f 이내로 들어오면 도착한 것으로 판정하는 것이 안전합니다.
        float defaultTolerance = (this is MoveTileModule) ? 0.5f : 0.05f;
        float effectiveTolerance = Mathf.Max(targetTolerance, defaultTolerance);

        if (distance <= effectiveTolerance)
        {
            // 💡 오차가 나더라도 최종 위치는 정확히 목적지 타일 좌표로 강제 보정합니다.
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
        // 💡 [추가] 움직임 실행 전 steminaPoint 검사
        if (Owner != null && Owner.steminaPoint <= 0)
        {
            Debug.LogWarning($"[Movement] {Owner.Name}의 스태미나가 부족하여 이동할 수 없습니다. (Stamina: {Owner.steminaPoint})");
            return;
        }

        targetDirection = null;
        targetDestination = destination;
        targetTolerance = tolerance;
    }

    public void MoveToDirection(Vector3 direction)
    {
        // 💡 [추가] 움직임 실행 전 steminaPoint 검사
        if (Owner != null && Owner.steminaPoint <= 0)
        {
            Debug.LogWarning($"[Movement] {Owner.Name}의 스태미나가 부족하여 이동할 수 없습니다. (Stamina: {Owner.steminaPoint})");
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


    [Header("현재 선택된 캐릭터 정보")]
    [SerializeField] private CharacterBase selectedCharacter;
    private MovementModule selectedMovement;
    private int currentMoveRange;

    private bool isMoveInputActive = false;


    public void HandleMouseClick()
    {
        Debug.Log($"현재케릭터 : {SelectionManager.selectCharacter}");

        if (ModeManager.Instance == null || ModeManager.Instance.CurrentMode != ModeManager.GameMode.Movement)
        {
            return;
        }

        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 mousePos2D = new Vector2(mouseWorldPos.x, mouseWorldPos.y);

        // 1. 캐릭터 선택용 레이캐스트
        RaycastHit2D hit = Physics2D.Raycast(mousePos2D, Vector2.zero);
        if (hit.collider != null)
        {
            CharacterBase clickedCharacter = hit.collider.GetComponentInParent<CharacterBase>();
            if (clickedCharacter != null)
            {
                SelectCharacter(clickedCharacter);
                return;
            }
        }

        // 2. 캐릭터가 선택된 상태에서 빈 땅 클릭 시 타일 이동 처리
        if (isMoveInputActive && selectedCharacter != null && selectedMovement != null)
        {
            if (SelectionManager.Instance == null || PlacementManager.Instance.tilemap == null) return;
            Tilemap tilemap = PlacementManager.Instance.tilemap;

            Vector3Int targetCell = tilemap.WorldToCell(mouseWorldPos);
            targetCell.z = 0; // 비교용 Z축 통일

            if (selectedMovement is MoveTileModule tileMoveModule)
            {
                List<Vector3Int> movableTiles = tileMoveModule.GetMovableTiles();
                bool isMovable = movableTiles.Exists(tile => tile.x == targetCell.x && tile.y == targetCell.y);

                if (isMovable)
                {
                    // 자식 클래스의 MoveToTile 내부에서도 스태미나를 검사하는지 꼭 확인해보세요!
                    tileMoveModule.MoveToTile(targetCell);

                    ClearTileHighlight();
                    isMoveInputActive = false;
                    Debug.Log($"[Movement] {selectedCharacter.Name} 캐릭터가 {targetCell} 타일로 이동합니다.");
                }
                else
                {
                    Debug.LogWarning($"[Movement Fail] {targetCell}은 이동 범위를 벗어났습니다.");
                }
            }
            else
            {
                // 일반 그리드 미사용 시 이동
                Vector3 targetWorldPos = new Vector3(mouseWorldPos.x, mouseWorldPos.y, selectedCharacter.transform.position.z);
                float distance = Vector2.Distance(selectedCharacter.transform.position, targetWorldPos);
                if (distance <= currentMoveRange)
                {
                    selectedMovement.MoveToDestination(targetWorldPos, 0.05f);
                    ClearTileHighlight();
                    isMoveInputActive = false;
                }
            }
        }
    }

    private void SelectCharacter(CharacterBase character)
    {
        MovementModule targetMovement = character.GetComponent<MovementModule>();

        if (targetMovement != null && targetMovement.IsMoving)
        {
            Debug.LogWarning($"[Click Fail] {character.Name} 캐릭터가 아직 이동 중이라 선택할 수 없습니다.");
            return;
        }

        if (character.steminaPoint <= 0)
        {
            Debug.LogWarning($"{character.Name}은 스태미나가 없어 움직일 수 없습니다.");
            return;
        }

        selectedCharacter = character;
        selectedMovement = targetMovement;

        // 1. 전역 선택 매니저에 캐릭터 등록
        SelectionManager.SetSelectedCharacter(character);

        // 💡 [핵심 추가] UI 매니저를 깨워서 하단 스킬 아이콘과 초상화를 방금 선택한 캐릭터 정보로 그리도록 강제합니다.
        if (StageUIController.Instance != null)
        {
            // PlacementManager에도 방금 선택한 녀석의 프리팹/오브젝트 매칭을 위해 등록해 줍니다.
            PlacementManager.selectedCharacter = character.gameObject;
            StageUIController.Instance.Refresh();
        }

        Debug.Log($"{selectedCharacter.Name} 캐릭터를 선택하고 UI를 동기화했습니다.");
        OnPressMoveButton();
    }
    public void OnPressMoveButton()
    {
        if (selectedCharacter == null) return;

        isMoveInputActive = true;
        currentMoveRange = selectedCharacter.mobility;
        TileHighlight(selectedCharacter.transform.position, currentMoveRange);
    }

    public void OnMovementModeChange(CharacterBase character)
    {
        ModeManager.Instance.CurrentMode = ModeManager.GameMode.Movement;
        SelectionManager.SetSelectedCharacter(character);
    }

    public void TileHighlight(Vector3 centerPosition, int range)
    {
        if (ModeManager.Instance.CurrentMode is not ModeManager.GameMode.Movement) return;

        if (PlacementManager.Instance == null || PlacementManager.Instance.tilemap == null) return;
        Tilemap tilemap = PlacementManager.Instance.tilemap;

        ClearTileHighlight();
        Vector3Int centerCell = tilemap.WorldToCell(centerPosition);

        for (int x = -range; x <= range; x++)
        {
            for (int y = -range; y <= range; y++)
            {
                int manhattanDistance = Mathf.Abs(x) + Mathf.Abs(y);

                if (manhattanDistance <= range)
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

    private void ClearTileHighlight()
    {
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