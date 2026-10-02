using UnityEngine;

public enum PassiveTrigger
{
    OwnerAttackCompleted,
    AdjacentAllyMoved,
    AdjacentAllyAttackCompleted,
    OppositeAllyAttackCompleted
}

public enum PassiveEffect
{
    None,
    AttackFront,
    PlantBomb,
    AttackNearestToActor,
    RestoreActorMovement,
    ApplyRandomBurn,
    RestoreMovementPerKill,
    ExtraActionAtCorner,
    AttackNearestToOwner,
    ApplyRandomStun,
    AttackSurvivors
}

[CreateAssetMenu(fileName = "Skill", menuName = "PassiveSkill")]
public class PassiveSkill : SkillList
{
    [Header("패시브 발동 조건 / 효과")]
    public PassiveTrigger trigger;
    [Tooltip("None은 기존 미구현 패시브 데이터와의 호환을 위해 아무 효과도 실행하지 않습니다.")]
    public PassiveEffect passiveEffect = PassiveEffect.None;
    [Min(0), Tooltip("0이면 제한 없음. 태산의 모서리 추가 행동은 1로 설정합니다.")]
    public int maxActivationsPerTurn;

    [Header("패시브 수치")]
    [Min(0)] public int passiveDamage = 1;
    [Min(0), Tooltip("현재 이동 횟수(steminaPoint)를 회복합니다. 기본 이동 거리(mobility)는 바꾸지 않습니다.")]
    public int movementPoints = 1;
    [Min(1)] public int extraActions = 1;
    [Min(1), Tooltip("폭탄/화상이 피해를 주는 적 턴 수 또는 스턴으로 건너뛸 적 턴 수")]
    public int durationTurns = 1;
    [Min(0), Tooltip("폭탄의 맨해튼 거리 반경. 0은 설치된 한 칸입니다.")]
    public int bombRadius;

    // 기존 코드에서 사용하는 API를 유지합니다. 전투 발동은 BattlePassiveSystem이 담당합니다.
    public virtual bool IsUsable(CharacterBase from, CharacterBase to) => true;
    public virtual void Onuse(CharacterBase from, CharacterBase to)
    { }
    public virtual bool IsUsable(CharacterBase from, Vector3 position) => true;
    public virtual void OnUse(CharacterBase from, Vector3 position)
    { }

}
