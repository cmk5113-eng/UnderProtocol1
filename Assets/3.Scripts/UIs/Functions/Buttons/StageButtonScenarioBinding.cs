using UnityEngine;
using UnityEngine.UI;

/// <summary>Assigns the clear scenario for the stage started by this button's WaveSetter.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Button), typeof(WaveSetter))]
public class StageButtonScenarioBinding : MonoBehaviour
{
    [Header("스테이지 클리어 후 시나리오")]
    [Tooltip("대사와 next/스킵 버튼을 포함한 시나리오 루트를 지정합니다. " +
        "이 컴포넌트가 있으면 맵에디터의 전투 후 시나리오보다 우선합니다. 비워 두면 월드로 복귀합니다.")]
    [SerializeField] private GameObject postBattleScenario;

    public GameObject PostBattleScenario => postBattleScenario;
}
