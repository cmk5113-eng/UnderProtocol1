using UnityEngine;

public class Monster : MonoBehaviour
{
    // [ 외부 노출을 막기 위해 기존 필드를 private으로 변경 (또는 인펙터 노출용 SerializeField) ]
    public int hp = 100;
    public int atk = 1;
    public bool isDead = false;
    public bool isActive = false;

    // -------------------------------------------------------------
    // [ 다른 스크립트에서 접근할 C# 프로퍼티 (Property) ]
    // -------------------------------------------------------------

    // 1. 체력 프로퍼티 (체력이 0 이하가 되면 자동으로 사망 처리)
    public int HP
    {
        get => hp;
        set
        {
            if (isDead) return; // 이미 죽은 몬스터는 체력 연산 무시

            hp = value;
            if (hp <= 0)
            {
                hp = 0;
                IsDead = true; // 사망 프로퍼티 호출
            }
        }
    }

    // 2. 공격력 프로퍼티
    public int Atk
    {
        get => atk;
        set => atk = value;
    }

    // 3. 사망 여부 프로퍼티 (읽기는 자유롭게, 수정은 내부 조건이나 특정 상황에서만)
    public bool IsDead
    {
        get => isDead;
        set
        {
            isDead = value;
            if (isDead)
            {
                Debug.Log($"{gameObject.name}이(가) 사망했습니다.");
                // 여기에 사망 애니메이션 재생이나 그리드에서 제외하는 로직을 추가하면 좋습니다.
            }
        }
    }

    // 4. 활성화 여부 프로퍼티 (SRPG에서 행동 가능 상태 등을 제어)
    public bool IsActive
    {
        get => isActive;
        set => isActive = value;
    }
}