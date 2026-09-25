using UnityEngine;

public class WaveSetter : MonoBehaviour 
{
    [SerializeField] public int index;
    public void SelectSkillByIndex()
    {
        if (index >= 0 && index < GameManager.Instance.Wave.StageWaveIndex.Count)
        {


            GameManager.Instance.Wave.selectedWaves = GameManager.Instance.Wave.StageWaveIndex[index];
            GameManager.Instance.Wave.currentWave = null;
            GameManager.Instance.Wave.currentWaveIndex = 0; // �������� ���� �� ���̺� �ε��� �ʱ�ȭ
            Debug.Log($"[{index + 1} ��������] ���� �Ϸ�! �� ���̺� ��: {GameManager.Instance.Wave.selectedWaves.Length}");
            if (BattleManager.Instance != null)
                BattleManager.Instance.ResetBattle();

            // 진입 버튼에서 맵 설정과 웨이브 선택을 마친 즉시 첫 웨이브 실행.
            if (WaveLoader.Instance != null)
                WaveLoader.Instance.StartFirstWave();
        }
        else
        {
            Debug.LogWarning($"�߸��� �������� �ε����Դϴ�: {index}");
        }
    }


}
