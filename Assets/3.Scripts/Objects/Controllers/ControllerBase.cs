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

    // ����: Tile ��� �̵��� ������ "�Է� ������ ���� ĭ"�� ��� �������� ����
    public void CommandMoveToDirection(Vector3 diraction)
    {
        if (Character == null) return;
        if (Character != SelectionManager.selectedPrefab) return;
        var movement = Character.GetModule<MovementModule>();
        var tileModule = movement as MoveTileModule;

        // 1. �Է� ���� �α� (Y�� ���� �������� Ȯ��)
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

            // 2. �� ���� �α�
            Debug.Log($"[Axis Check] absX: {absX}, absY: {absY}");

            if (absX >= absY) // �� ���� ������ X�� �����̶� ũ�� Y�� ���õ�
            {
                step = input2.x > 0 ? Vector3Int.right : Vector3Int.left;
                Debug.Log("[Final Step] X�� ����: " + step);
            }
            else
            {
                step = input2.y > 0 ? Vector3Int.up : Vector3Int.down;
                Debug.Log("[Final Step] Y�� ����: " + step);
            }

            Vector3Int nextTile = tileModule.CurrentTile + step;

            if (!tileModule.CanEnterTile(nextTile))
            {
                Debug.LogWarning($"[CanEnter Fail] Ÿ�� ���� �Ұ�: {nextTile}");
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
    // ���� Ű ���ε��� ����: TileMoveModule�� ���� �����ϴ� ���� �̵�
    public bool CommandStepByInput(Vector2 input)
    {
        if (Character == null) return false;

        // ���� ���õ� ĳ������ ���� �Է� ó�� (SelectionManager ����)
        if (Character == SelectionManager.selectedPrefab)
        {
            var tileModule = Character.GetModule<MoveTileModule>();
            if (tileModule != null)
            {
                return tileModule.TryStepByInput(input);
            }
        }

        // ���õ��� �ʾҰų� ����� ������ false ��ȯ (�� �κ��� ����Ǿ� �־����ϴ�)
        return false;
    }
}




