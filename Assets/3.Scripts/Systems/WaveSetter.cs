using UnityEngine;

public class WaveSetter : MonoBehaviour 
{
    [SerializeField] public int index;
    public void SelectSkillByIndex()
    {
        if (index >= 0 && index < GameManager.Instance.Wave.StageWaveIndex.Count)
        {


            GameManager.Instance.Wave.selectedWaves = GameManager.Instance.Wave.StageWaveIndex[index];
            GameManager.Instance.Wave.currentWaveIndex = 0; // �������� ���� �� ���̺� �ε��� �ʱ�ȭ
            Debug.Log($"[{index + 1} ��������] ���� �Ϸ�! �� ���̺� ��: {GameManager.Instance.Wave.selectedWaves.Length}");
        }
        else
        {
            Debug.LogWarning($"�߸��� �������� �ε����Դϴ�: {index}");
        }
    }


}
