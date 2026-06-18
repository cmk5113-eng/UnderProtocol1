using System.Collections;
using UnityEngine;

public class SelectionManager : ManagerBase
{
   // public static SelectionManager Instance { get; private set; }

   //// public static SkillObject selectedSkill;
    public static CharacterBase selectedCharacter;
    protected override IEnumerator OnConnected(GameManager newManager)
    {
   //     // �̱��� ����
   //     Instance = this;    

   //     // �Է�/������Ʈ �̺�Ʈ ���� (�ߺ� ���� ���� ���� ���� ����)
   //     InputManager.OnMouseLeftButton -= HandleMouseLeft;
   //     InputManager.OnMouseLeftButton += HandleMouseLeft;

   //     InputManager.OnMouseMove -= HandleMouseMove;
   //     InputManager.OnMouseMove += HandleMouseMove;

   //     InputManager.OnConfirm -= HandleConfirm;
   //     InputManager.OnConfirm += HandleConfirm;

   //     GameManager.OnUpdateManager -= UpdateEvent;
   //     GameManager.OnUpdateManager += UpdateEvent;

      yield return null;
    }

    protected override void OnDisconnected()
    {
   //     // �̺�Ʈ ����
   //     InputManager.OnMouseLeftButton -= HandleMouseLeft;
   //     InputManager.OnMouseMove -= HandleMouseMove;
   //     InputManager.OnConfirm -= HandleConfirm;
   //     GameManager.OnUpdateManager -= UpdateEvent;

   //     if (Instance == this) Instance = null;
    }

   // // �ܺο��� SkillObject ���/����
   //public static void SetSelectedSkill(SkillObject skill)
   // //{
   // //    selectedSkill = skill;
   // //    Debug.Log($"SelectionManager: SetSelectedSkill -> {skill?.data?.name}");
   // //    // TODO: �ð�ȭ / Ŀ�� ���� ��
   // //}

   // //public static void ClearSelectedSkill()
   // //{
   // //    selectedSkill = null;
   // //    Debug.Log("SelectionManager: ClearSelectedSkill");
   // //    // TODO: �ð�ȭ ����
   // //}

    public static void SetSelectedCharacter(CharacterBase character)
    {
        selectedCharacter = character;
        Debug.Log($"SelectionManager: SetSelectedCharacter -> {character?.name}");
        // TODO: �ð�ȭ / Ŀ�� ���� ��
    }

    public static void ClearSelectedCharacter()
    {
       selectedCharacter = null;
        Debug.Log("SelectionManager: ClearSelectedSkill");
        // TODO: �ð�ȭ ����
    }


   // // ����: worldPosition(Vector3)���� �ٷ� ����
   // public void ProvideTarget(Vector3 worldPosition, GameObject target = null)
   // {
   //     if (selectedSkill == null) return;
   //     selectedSkill.ReceiveTarget(worldPosition, target);
   // }

   // // �߰�: screenPosition(Vector2)�� �޾� ī�޶�� ��ȯ �� ���� (ȣ���ڰ� Vector2�� ������ ���� �� �����ϰ� ���)
   // public void ProvideTarget(Vector2 screenPosition, GameObject target = null)
   // {
   //     Camera cam = GameManager.Instance?.Camera?.MainCamera ?? Camera.main;
   //     Vector3 world = Vector3.zero;
   //     if (cam != null)
   //     {
   //         // ScreenToWorldPoint�� z�� ī�޶� ��� �Ÿ��� ����ϹǷ� 2D�� �� z�� 0���� ����
   //         world = cam.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, cam.nearClipPlane));
   //         if (GameManager.is2D)
   //             world.z = 0f;
   //     }

   //     ProvideTarget(world, target);
   // }

   // // Ȯ�� �� ��ų ����
   // public void ConfirmSelection()
   // {
   //     if (selectedSkill == null) return;
   //     selectedSkill.Execute();
   // }

   // // ----- �Է� �̺�Ʈ �ڵ鷯 -----
   // // MouseButtonEvent(bool value, Vector2 screenPosition, Vector3 worldPosition)
   // void HandleMouseLeft(bool pressed, Vector2 screenPosition, Vector3 worldPosition)
   // {
   //     if (!pressed) return; // ������ ���� ó��
   //     if (selectedSkill == null) return;

   //     // Ŀ�� �Ʒ� ������Ʈ�� Ȯ���� ����
   //     GameObject under = GameManager.Instance?.Input?.GetGameObjectUnderCursor();

   //     // �����ϰ� �� �� �ϳ��� ����: worldPosition�� ��ȿ�ϸ� �װ�, �ƴ϶�� screenPosition �����ε� ���
   //     if (worldPosition != Vector3.zero)
   //         ProvideTarget(worldPosition, under);
   //     else
   //         ProvideTarget(screenPosition, under);
   // }

   // // MouseMoveEvent(Vector2 screenPosition, Vector3 worldPosition)
   // void HandleMouseMove(Vector2 screenPosition, Vector3 worldPosition)
   // {
   //     if (selectedSkill == null) return;

   //     GameObject under = GameManager.Instance?.Input?.GetGameObjectUnderCursor();

   //     if (worldPosition != Vector3.zero)
   //         selectedSkill.ReceiveTarget(worldPosition, under);
   //     else
   //         // ��� �̸���������� ȭ����ǥ ��� ��ȯ ���
   //         ProvideTarget(screenPosition, under);
   // }

   // // ButtonEvent(bool value)
   // void HandleConfirm(bool pressed)
   // {
   //     if (!pressed) return;
   //     ConfirmSelection();
   // }

   // // ������ ��� ������Ʈ �ʿ� �� ���
   // void UpdateEvent(float deltaTime)
   // {
   //     // ����� �� ����. ���� �� ���� ǥ��/Ÿ�̸� �� �ʿ��ϸ� �߰�.
   // }
}
