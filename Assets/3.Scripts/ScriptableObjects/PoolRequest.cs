using UnityEngine;

[CreateAssetMenu(fileName = "PoolRequest", menuName = "PoolRequest/DefaultPoolRequest")]
public class PoolRequest : ScriptableObject
{
    public PoolSetting[] settings;
}
[System.Serializable] // Serializable Serialize => Serial
public struct PoolSetting
{
    public string poolName;    // 이 풀링 정보를 어떤 이름으로 보고 싶은가?
    public GameObject target;  // 풀링할 대상 원거리 미니언
    public uint countInitial;   // 처음에 준비할 개수 60마리
    public uint countAdditional;// 부족하면 추가할 개수
}