#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[CustomEditor(typeof(WaveManager))]
public class WaveManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        WaveManager manager = (WaveManager)target;
        using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
        {
            StageWaveEditorUtility.EnsureMigrated(manager);
            serializedObject.Update();
            EditorGUILayout.LabelField($"스테이지 목록 · {manager.StageCount}개", EditorStyles.boldLabel);
            SerializedProperty stages = serializedObject.FindProperty("stages");
            for (int i = 0; i < stages.arraySize; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Stage {i + 1} · Stage Index {i}", EditorStyles.miniBoldLabel);
                bool remove = GUILayout.Button("Stage 삭제", GUILayout.Width(90));
                EditorGUILayout.EndHorizontal();
                if (remove) { StageWaveEditorUtility.RemoveStage(manager, i); GUIUtility.ExitGUI(); }
                EditorGUILayout.PropertyField(stages.GetArrayElementAtIndex(i).FindPropertyRelative("waves"), new GUIContent("Waves"), true);
                EditorGUILayout.Space();
            }
            if (GUILayout.Button("Stage 추가")) { StageWaveEditorUtility.AddStage(manager); GUIUtility.ExitGUI(); }
            if (serializedObject.ApplyModifiedProperties()) StageWaveEditorUtility.MarkChanged(manager);
        }
        EditorGUILayout.Space();
        DrawPropertiesExcluding(serializedObject, "m_Script", "stages");
        serializedObject.ApplyModifiedProperties();
    }
}

// Inspector와 맵 에디터가 같은 추가/삭제 및 인덱스 보정 경로를 사용한다.
public static class StageWaveEditorUtility
{
    public static void EnsureMigrated(WaveManager manager)
    {
        if (!manager.NeedsStageMigration) return;
        Undo.RecordObject(manager, "Migrate Legacy Stage Waves");
        if (manager.MigrateLegacyStages()) MarkChanged(manager);
    }

    public static int AddStage(WaveManager manager)
    {
        if (manager == null || EditorApplication.isPlayingOrWillChangePlaymode) return -1;
        EnsureMigrated(manager);
        Undo.RecordObject(manager, "Add Wave Stage");
        int index = manager.AddStage();
        MarkChanged(manager);
        SceneView.RepaintAll();
        return index;
    }

    public static bool RemoveStage(WaveManager manager, int index)
    {
        if (manager == null || EditorApplication.isPlayingOrWillChangePlaymode) return false;
        EnsureMigrated(manager);
        if (index < 0 || index >= manager.StageCount) return false;
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Remove Wave Stage");
        Undo.RecordObject(manager, "Remove Wave Stage");
        manager.RemoveStage(index);
        MarkChanged(manager);

        if (manager.gameObject.scene.IsValid())
        {
            bool singleManager = Object.FindObjectsByType<WaveManager>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length == 1;
            foreach (StageMapBinding binding in Object.FindObjectsByType<StageMapBinding>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if ((!singleManager && binding.gameObject.scene != manager.gameObject.scene) || binding.StageIndex < index) continue;
                Undo.RecordObject(binding, "Remap Stage Binding");
                binding.EditorSetStageIndex(binding.StageIndex == index ? -1 : binding.StageIndex - 1);
                MarkChanged(binding);
            }
            foreach (WaveSetter setter in Object.FindObjectsByType<WaveSetter>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if ((!singleManager && setter.gameObject.scene != manager.gameObject.scene) || setter.WaveStageIndex < index) continue;
                Undo.RecordObject(setter, "Remap Wave Setter");
                setter.EditorSetWaveStageIndex(setter.WaveStageIndex == index ? -1 : setter.WaveStageIndex - 1);
                // index/stageId는 진행도 검사와 클리어 기록용이므로 변경하지 않는다.
                MarkChanged(setter);
            }
        }
        Undo.CollapseUndoOperations(group);
        SceneView.RepaintAll();
        return true;
    }

    public static void MarkChanged(Component component)
    {
        EditorUtility.SetDirty(component);
        PrefabUtility.RecordPrefabInstancePropertyModifications(component);
        if (component.gameObject.scene.IsValid()) EditorSceneManager.MarkSceneDirty(component.gameObject.scene);
    }
}
#endif
