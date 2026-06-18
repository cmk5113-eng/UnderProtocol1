//using System;
//using UnityEngine;


//public class SkillObject : MonoBehaviour
//{
//    CharacterBase owner;
//    public SkillData data { get; private set; }

//    bool isTargeting = false;
//    Vector3 selectedPosition;
//    GameObject selectedTarget;

//    public void Init(CharacterBase owner, SkillData data)
//    {
//        this.owner = owner;
//        this.data = data;

//        // SelectionManager�� ��� -> �÷��̾� �Է�(Ÿ��/��� ����)�� �ް� ��
//        SelectionManager.SetSelectedSkill(this);

//        EnterTargeting();
//    }

//    void EnterTargeting()
//    {
//        isTargeting = true;
//        // TODO: ���� ǥ��, Ÿ�� ǥ�� UI �� �ð��� �ǵ�� ó��
//        Debug.Log($"Skill '{data.skillName}' entered targeting mode.");
//    }

//    // �ܺο��� ���� ������ ���� (Ÿ��/��� ���� �� ȣ��)
//    public void ReceiveTarget(Vector3 worldPos, GameObject target = null)
//    {
//        selectedPosition = worldPos;
//        selectedTarget = target;
//        Debug.Log($"Target received for skill '{data.skillName}' at {worldPos}" + (target != null ? $", target:{target.name}" : ""));
//    }

//    // Ȯ��(Enter �Ǵ� Ȯ�� ��ư) �� ȣ��
//    public void Execute()
//    {
//        if (!isTargeting)
//        {
//            Debug.LogWarning("Execute called while not targeting");
//            return;
//        }

//        // ������ ������ ���� ����: selectedTarget�� ���� ApplyDamage �Ǵ� TakeDamage �޼��尡 ������ ȣ��
//        if (selectedTarget != null)
//        {
//            var targetCharacter = selectedTarget.GetComponent<CharacterBase>();
//            if (targetCharacter != null)
//            {
//                // ������ ApplyDamage/TakeDamage ȣ�� �õ� (����ü�� ���� ���缭 ����)
//                var method = targetCharacter.GetType().GetMethod("ApplyDamage");
//                if (method != null)
//                {
//                    method.Invoke(targetCharacter, new object[] { data.damage });
//                }
//                else
//                {
//                    method = targetCharacter.GetType().GetMethod("TakeDamage");
//                    if (method != null)
//                        method.Invoke(targetCharacter, new object[] { data.damage });
//                    else
//                        Debug.LogWarning("Target has no ApplyDamage/TakeDamage method. Implement damage application.");
//                }
//            }
//            else
//            {
//                Debug.Log($"No CharacterBase on selected target '{selectedTarget.name}', applying generic effect.");
//                // ����/����Ʈ�� �����Ϸ��� ���⿡ ����
//            }
//        }
//        else
//        {
//            // ����� ���� ��ų(����/��� ���)�� ó��
//            Debug.Log($"Execute skill '{data.skillName}' at position {selectedPosition}");
//            // ����Ʈ ����, ���� �� ��� �˻� �� ������ ���� �� ����
//        }

//        // ����Ʈ/�ִϸ��̼� ���� (prefab�� �ִϸ��̼�/��ƼŬ�� ���� ��)
//        // TODO: �ʿ� �� �߰� ����

//        EndSkill();
//    }

//    void EndSkill()
//    {
//        isTargeting = false;
//        // SelectionManager ���� ����
//        if (SelectionManager.selectedSkill != null)
//            SelectionManager.ClearSelectedSkill();

//        // ���� ó��: ������Ʈ �ı� �Ǵ� Ǯ������ ��ȯ
//        Destroy(gameObject);
//    }


//}