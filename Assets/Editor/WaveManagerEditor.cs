#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(WaveManager))]
public class WaveManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        WaveManager manager = (WaveManager)target;
        EditorGUILayout.LabelField($"맵에디터에 등록된 맵: {manager.Maps.Count}개", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("맵, 스테이지 버튼, 웨이브 순서와 몬스터 목록은 Stage Map Editor에서 설정합니다.", MessageType.Info);
        if (GUILayout.Button("Stage Map Editor 열기")) StageMapEditor.Open();
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            EditorGUILayout.LabelField($"현재 Wave: {manager.currentWaveIndex}");
    }
}
#endif

