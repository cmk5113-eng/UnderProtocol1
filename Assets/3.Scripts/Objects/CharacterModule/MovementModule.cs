using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MovementModule : CharacterModule, IRunnable
{
    public enum ID { Beak, Choi, Do, Ha, Jo, Kang, Lee, Min, Namgung, Pyo, Ryu, Seo };
    [SerializeField] private ID id; // 인스펙터에서 각 캐릭터에 맞게 지정

    protected Vector3? targetDestination = null;
    protected Vector3? targetDirection = null;
    protected float targetTolerance;
    public bool IsMoving => targetDestination != null || targetDirection != null;

    public sealed override System.Type RegistrationType => typeof(MovementModule);

    private static int currentMoveRange;
    private static bool isMoveInputActive = false;

    public override void OnRegistration(CharacterBase newOwner)
    {
        base.OnRegistration(newOwner);

        // GameManager 물리 루프 등록
        GameManager.OnPhysicsCharacter -= MovementUpdate;
        GameManager.OnPhysicsCharacter += MovementUpdate;

        //// 마우스 이벤트 등록
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
            Debug.LogWarning($"[Movement] {Owner.Name}의 스태미나가 부족하여 이동할 수 없습니다. (Stamina: {Owner.steminaPoint})");
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


    public void HandleMouseClick(Vector2 mouseWorldPos)
    {
        if (ModeManager.Instance == null || ModeManager.Instance.CurrentMode != ModeManager.GameMode.Movement || SelectionManager.SelectedPrefab != gameObject)
        {
            return;
        }

        //Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        //Vector2 mousePos2D = new Vector2(mouseWorldPos.x, mouseWorldPos.y);

        //// 1. 캐릭터 선택용 레이캐스트
        //RaycastHit2D hit = Physics2D.Raycast(mousePos2D, Vector2.zero);
        //if (hit.collider != null)
        //{
        //    // 💡 [핵심] 씬의 모든 오브젝트가 동시 반응하지 않고, "마우스에 부딪힌 나 자신(gameObject)"일 때만 실행하도록 검사합니다!
        //    if (hit.collider.gameObject == this.gameObject)
        //    {
        //        // Enum id를 int로 바로 형변환 (switch문 불필요)
        //        int index = (int)id;

        //        if (SelectionManager.Instance != null)
        //        {
        //            if (index >= 0 && index < SelectionManager.Instance.characterBases.Length)
        //                SelectionManager.characterBase = SelectionManager.Instance.characterBases[index];

        //            if (index >= 0 && index < SelectionManager.Instance.characterPrefabs.Length)
        //                SelectionManager.selectedPrefab = SelectionManager.Instance.characterPrefabs[index];

        //            if (index >= 0 && index < SelectionManager.Instance.characterDatas.Length)
        //                SelectionManager.characterData = SelectionManager.Instance.characterDatas[index];
        //        }

        //        if (StageUIController.Instance != null)
        //        {
        //            StageUIController.Instance.Refresh();
        //        }

        //        CharacterBase clickedCharacter = GetComponent<CharacterBase>();
        //        if (clickedCharacter != null)
        //        {
        //            SelectCharacter(clickedCharacter);
        //        }
        //        return;
        //    }
        //}
        //StageUIController.Instance.Refresh();
        // 2. 캐릭터가 선택된 상태에서 빈 땅 클릭 시 타일 이동 처리
        if (isMoveInputActive)
        {
            if (SelectionManager.Instance == null || PlacementManager.Instance == null || PlacementManager.Instance.tilemap == null) return;
            Tilemap tilemap = PlacementManager.Instance.tilemap;

            Vector3Int targetCell = tilemap.WorldToCell(mouseWorldPos);
            targetCell.z = 0;

            if (this is MoveTileModule tileMoveModule)
            {
                List<Vector3Int> movableTiles = tileMoveModule.GetMovableTiles();
                bool isMovable = movableTiles.Exists(tile => tile.x == targetCell.x && tile.y == targetCell.y);

                if (isMovable)
                {
                    tileMoveModule.MoveToTile(targetCell);

                    ClearTileHighlight();
                    isMoveInputActive = false;
                    Debug.Log($"[Movement] {Owner.Name} 캐릭터가 {targetCell} 타일로 이동합니다.");
                }
                else
                {
                    Debug.LogWarning($"[Movement Fail] {targetCell}은 이동 범위를 벗어났습니다.");
                }
            }
            else
            {
                // 일반 그리드 미사용 시 이동
                Vector3 targetWorldPos = new Vector3(mouseWorldPos.x, mouseWorldPos.y, Owner.transform.position.z);
                float distance = Vector2.Distance(Owner.transform.position, targetWorldPos);
                if (distance <= currentMoveRange)
                {
                    MoveToDestination(targetWorldPos, 0.05f);
                    ClearTileHighlight();
                    isMoveInputActive = false;
                }

            }
            ModeManager.Instance.ChangeMode(ModeManager.GameMode.None);
        }
    }

    //private void SelectCharacter(CharacterBase character)
    //{
    //    MovementModule targetMovement = character.GetComponent<MovementModule>();

    //    if (targetMovement != null && targetMovement.IsMoving)
    //    {
    //        Debug.LogWarning($"[Click Fail] {character.Name} 캐릭터가 아직 이동 중이라 선택할 수 없습니다.");
    //        return;
    //    }

    //    if (character.steminaPoint <= 0)
    //    {
    //        Debug.LogWarning($"{character.Name}은 스태미나가 없어 움직일 수 없습니다.");
    //        return;
    //    }

    //    selectedCharacter = character;
    //    selectedMovement = targetMovement;

    //    // 전역 선택 매니저에 캐릭터 등록
    //    SelectionManager.SetSelectedCharacter(character);

    //    if (StageUIController.Instance != null)
    //    {
    //        SelectionManager.selectedPrefab = character.gameObject;
    //        StageUIController.Instance.Refresh();
    //    }

    //    Debug.Log($"{selectedCharacter.Name} 캐릭터를 선택하고 UI를 동기화했습니다.");
    //    OnPressMoveButton();
    //}

    public void OnPressMoveButton()
    {
        isMoveInputActive = true;
        currentMoveRange = Owner?.mobility ?? 0;
        TileHighlight(transform.position, currentMoveRange);
    }

    public void OnMovementModeChange(CharacterBase character)
    {
        if (ModeManager.Instance != null)
        {
            ModeManager.Instance.CurrentMode = ModeManager.GameMode.Movement;
        }
        SelectionManager.SetSelectedCharacter(character);
    }

    public void TileHighlight(Vector3 centerPosition, int range)
    {

        if (ModeManager.Instance == null || ModeManager.Instance.CurrentMode is not ModeManager.GameMode.Movement) return;

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

    public void ClearTileHighlight()    
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
