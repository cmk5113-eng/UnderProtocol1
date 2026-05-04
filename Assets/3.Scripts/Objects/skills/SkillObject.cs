using System;
using UnityEngine;


public class SkillObject : MonoBehaviour
{
    CharacterBase owner;
    public SkillData data { get; private set; }

    bool isTargeting = false;
    Vector3 selectedPosition;
    GameObject selectedTarget;

    public void Init(CharacterBase owner, SkillData data)
    {
        this.owner = owner;
        this.data = data;

        // SelectionManager에 등록 -> 플레이어 입력(타일/대상 선택)을 받게 함
        SelectionManager.SetSelectedSkill(this);

        EnterTargeting();
    }

    void EnterTargeting()
    {
        isTargeting = true;
        // TODO: 범위 표시, 타겟 표시 UI 등 시각적 피드백 처리
        Debug.Log($"Skill '{data.skillName}' entered targeting mode.");
    }

    // 외부에서 선택 정보를 전달 (타일/대상 선택 시 호출)
    public void ReceiveTarget(Vector3 worldPos, GameObject target = null)
    {
        selectedPosition = worldPos;
        selectedTarget = target;
        Debug.Log($"Target received for skill '{data.skillName}' at {worldPos}" + (target != null ? $", target:{target.name}" : ""));
    }

    // 확인(Enter 또는 확인 버튼) 시 호출
    public void Execute()
    {
        if (!isTargeting)
        {
            Debug.LogWarning("Execute called while not targeting");
            return;
        }

        // 간단한 데미지 적용 예제: selectedTarget에 대해 ApplyDamage 또는 TakeDamage 메서드가 있으면 호출
        if (selectedTarget != null)
        {
            var targetCharacter = selectedTarget.GetComponent<CharacterBase>();
            if (targetCharacter != null)
            {
                // 가능한 ApplyDamage/TakeDamage 호출 시도 (구현체에 따라 맞춰서 변경)
                var method = targetCharacter.GetType().GetMethod("ApplyDamage");
                if (method != null)
                {
                    method.Invoke(targetCharacter, new object[] { data.damage });
                }
                else
                {
                    method = targetCharacter.GetType().GetMethod("TakeDamage");
                    if (method != null)
                        method.Invoke(targetCharacter, new object[] { data.damage });
                    else
                        Debug.LogWarning("Target has no ApplyDamage/TakeDamage method. Implement damage application.");
                }
            }
            else
            {
                Debug.Log($"No CharacterBase on selected target '{selectedTarget.name}', applying generic effect.");
                // 범위/이펙트만 적용하려면 여기에 구현
            }
        }
        else
        {
            // 대상이 없는 스킬(범위/장소 기반)의 처리
            Debug.Log($"Execute skill '{data.skillName}' at position {selectedPosition}");
            // 이펙트 생성, 범위 내 대상 검색 및 데미지 적용 등 구현
        }

        // 이펙트/애니메이션 실행 (prefab에 애니메이션/파티클이 있을 것)
        // TODO: 필요 시 추가 연출

        EndSkill();
    }

    void EndSkill()
    {
        isTargeting = false;
        // SelectionManager 선택 해제
        if (SelectionManager.selectedSkill != null)
            SelectionManager.ClearSelectedSkill();

        // 종료 처리: 오브젝트 파괴 또는 풀링으로 반환
        Destroy(gameObject);
    }


}