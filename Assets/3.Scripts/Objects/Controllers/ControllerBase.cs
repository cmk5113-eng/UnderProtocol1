using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using System.Collections.Generic;
using UnityEngine.Tilemaps;



public class ControllerBase : MonoBehaviour, IFunctionable
{
    CharacterBase _character;
    public CharacterBase Character => _character;


    public void RegistrationFunctions()
    { 
        Possess(GetComponent<CharacterBase>());
    
    }
    public void UnregistrationFunctions()
    {
        
    }

    protected virtual void OnPossess(CharacterBase newcontroller)
    {
    }

    public void Possess(CharacterBase target)
    {
        if (!target) return;
        ControllerBase result = target.Possessed(this);
        if (result == this) 
        {
            _character = target;
            OnPossess(target);
        }
        
    }


    protected virtual void OnUnpossess(CharacterBase oldcontroller) { }
    public void Unpossess()
    {
        if (Character)
        {
            if (Character.Unpossessed(this))
            { 
             OnUnpossess(Character);
            }
        }
        _character = null;
    }

    // 수정: Tile 기반 이동이 있으면 "입력 방향의 다음 칸"을 즉시 목적지로 설정
    public void CommandMoveToDirection(Vector3 diraction)
    {
        if (Character == null) return;
        if (Character != SelectionManager.selectedCharacter) return;
        var movement = Character.GetModule<MovementModule>();
        var tileModule = movement as MoveTileModule;

        // 1. 입력 원본 로그 (Y축 값이 들어오는지 확인)
        Debug.Log($"[Input Raw] X: {diraction.x}, Y: {diraction.y}");

        Vector2 input2 = new Vector2(diraction.x, diraction.y);
        const float deadZone = 0.1f;
        if (input2.sqrMagnitude < deadZone * deadZone) return;

        if (tileModule != null)
        {
            if (movement != null && movement.IsMoving) return;

            Vector3Int step = Vector3Int.zero;
            float absX = Mathf.Abs(input2.x);
            float absY = Mathf.Abs(input2.y);

            // 2. 축 판정 로그
            Debug.Log($"[Axis Check] absX: {absX}, absY: {absY}");

            if (absX >= absY) // 이 조건 때문에 X가 조금이라도 크면 Y는 무시됨
            {
                step = input2.x > 0 ? Vector3Int.right : Vector3Int.left;
                Debug.Log("[Final Step] X축 결정: " + step);
            }
            else
            {
                step = input2.y > 0 ? Vector3Int.up : Vector3Int.down;
                Debug.Log("[Final Step] Y축 결정: " + step);
            }

            Vector3Int nextTile = tileModule.CurrentTile + step;

            if (!tileModule.CanEnterTile(nextTile))
            {
                Debug.LogWarning($"[CanEnter Fail] 타일 진입 불가: {nextTile}");
                return;
            }

            tileModule.MoveToTile(nextTile);
            return;
        }

        if (Character && Character.GetModule<MovementModule>() is IRunnable target)
            target.MoveToDirection(diraction);
    }
    public void CommandMoveToDestination(Vector3 destination, float tolerance)
    {

        if (Character&&Character.GetModule<MovementModule>() is IRunnable target) target.MoveToDestination(destination, tolerance);
    }
    public void CommandStop() 
    {
        if (Character&&Character.GetModule<MovementModule>() is IRunnable target) target.StopMovement();                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                  }
    // 기존 키 바인딩용 보조: TileMoveModule에 직접 위임하는 단위 이동
    public bool CommandStepByInput(Vector2 input)
    {
        if (Character == null) return false;

        // 현재 선택된 캐릭터일 때만 입력 처리 (SelectionManager 연동)
        if (Character == SelectionManager.selectedCharacter)
        {
            var tileModule = Character.GetModule<MoveTileModule>();
            if (tileModule != null)
            {
                return tileModule.TryStepByInput(input);
            }
        }

        // 선택되지 않았거나 모듈이 없으면 false 반환 (이 부분이 누락되어 있었습니다)
        return false;
    }
}




