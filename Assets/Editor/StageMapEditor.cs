#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

public class StageMapEditor : EditorWindow
{
    private enum EditMode { Monster, Obstacle, FieldEffect, Erase }

    private StageMapData stageMapData;
    private Tilemap tilemap;
    private WaveData waveData;
    private int waveIndex;
    private int monsterId;
    private GameObject obstaclePrefab;
    private int obstacleRotation;
    private StageFieldEffectType fieldEffectType = StageFieldEffectType.Fire;
    private int fieldEffectValue = 1;
    private int fieldEffectDuration = 3;
    private EditMode mode = EditMode.Monster;
    private Vector2 scroll;

    [MenuItem("Tools/Stage Map Editor")]
    public static void Open() => GetWindow<StageMapEditor>("Stage Map Editor");

    private void OnEnable() => SceneView.duringSceneGui += OnSceneGUI;
    private void OnDisable() => SceneView.duringSceneGui -= OnSceneGUI;

    private void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);
        EditorGUILayout.LabelField("Stage Map Editor", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Scene View에서 타일을 클릭해 데이터를 배치합니다. 우클릭은 현재 모드와 관계없이 해당 셀의 데이터를 삭제합니다.", MessageType.Info);

        stageMapData = (StageMapData)EditorGUILayout.ObjectField("Stage Map Data", stageMapData, typeof(StageMapData), false);
        tilemap = (Tilemap)EditorGUILayout.ObjectField("Tilemap", tilemap, typeof(Tilemap), true);

        EditorGUILayout.Space();
        mode = (EditMode)GUILayout.Toolbar((int)mode, new[] { "Monster", "Obstacle", "Field Effect", "Erase" });
        EditorGUILayout.Space();

        switch (mode)
        {
            case EditMode.Monster:
                waveData = (WaveData)EditorGUILayout.ObjectField("Wave Data", waveData, typeof(WaveData), false);
                waveIndex = EditorGUILayout.IntField("Wave 표시 번호", waveIndex);
                monsterId = EditorGUILayout.IntField("Monster ID", monsterId);
                EditorGUILayout.HelpBox("몬스터는 기존 WaveData.monsters에 직접 저장됩니다.", MessageType.None);
                break;
            case EditMode.Obstacle:
                obstaclePrefab = (GameObject)EditorGUILayout.ObjectField("Obstacle Prefab", obstaclePrefab, typeof(GameObject), false);
                obstacleRotation = EditorGUILayout.IntSlider("Rotation (90°)", obstacleRotation, 0, 3);
                break;
            case EditMode.FieldEffect:
                fieldEffectType = (StageFieldEffectType)EditorGUILayout.EnumPopup("Effect Type", fieldEffectType);
                fieldEffectValue = EditorGUILayout.IntField("Value", fieldEffectValue);
                fieldEffectDuration = EditorGUILayout.IntField("Duration", fieldEffectDuration);
                break;
        }

        EditorGUILayout.Space();
        using (new EditorGUI.DisabledScope(tilemap == null))
        {
            if (GUILayout.Button("Frame Tilemap") && tilemap != null)
            {
                Selection.activeGameObject = tilemap.gameObject;
                SceneView.lastActiveSceneView?.FrameSelected();
            }
        }

        DrawCounts();
        EditorGUILayout.EndScrollView();
    }

    private void DrawCounts()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("현재 데이터", EditorStyles.boldLabel);
        int monsters = waveData != null && waveData.monsters != null ? waveData.monsters.Count : 0;
        int obstacles = stageMapData != null && stageMapData.obstacles != null ? stageMapData.obstacles.Count : 0;
        int effects = stageMapData != null && stageMapData.fieldEffects != null ? stageMapData.fieldEffects.Count : 0;
        EditorGUILayout.LabelField($"Wave {waveIndex} Monsters: {monsters}");
        EditorGUILayout.LabelField($"Obstacles: {obstacles}");
        EditorGUILayout.LabelField($"Field Effects: {effects}");
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (tilemap == null) return;

        DrawPreview();

        Event e = Event.current;
        if (e == null || e.alt) return;
        if (e.type != EventType.MouseDown || (e.button != 0 && e.button != 1)) return;

        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        Plane plane = new Plane(tilemap.transform.forward, tilemap.transform.position);
        if (!plane.Raycast(ray, out float distance)) return;

        Vector3 world = ray.GetPoint(distance);
        Vector3Int cell = tilemap.WorldToCell(world);
        if (!tilemap.HasTile(cell)) return;

        if (e.button == 1 || mode == EditMode.Erase)
            EraseCell(cell);
        else
            PaintCell(cell);

        e.Use();
        Repaint();
        SceneView.RepaintAll();
    }

    private void PaintCell(Vector3Int cell)
    {
        switch (mode)
        {
            case EditMode.Monster:
                if (waveData == null) return;
                Undo.RecordObject(waveData, "Place Stage Monster");
                if (waveData.monsters == null) waveData.monsters = new List<MonsterSpawnData>();
                waveData.monsters.RemoveAll(x => x != null && x.position == cell);
                waveData.monsters.Add(new MonsterSpawnData { monsterID = monsterId, position = cell });
                EditorUtility.SetDirty(waveData);
                break;

            case EditMode.Obstacle:
                if (stageMapData == null || obstaclePrefab == null) return;
                Undo.RecordObject(stageMapData, "Place Stage Obstacle");
                if (stageMapData.obstacles == null) stageMapData.obstacles = new List<ObstacleSpawnData>();
                stageMapData.obstacles.RemoveAll(x => x != null && x.position == cell);
                stageMapData.obstacles.Add(new ObstacleSpawnData
                {
                    prefab = obstaclePrefab,
                    position = cell,
                    rotationQuarterTurns = obstacleRotation
                });
                EditorUtility.SetDirty(stageMapData);
                break;

            case EditMode.FieldEffect:
                if (stageMapData == null) return;
                Undo.RecordObject(stageMapData, "Paint Stage Field Effect");
                if (stageMapData.fieldEffects == null) stageMapData.fieldEffects = new List<FieldEffectSpawnData>();
                stageMapData.fieldEffects.RemoveAll(x => x != null && x.position == cell);
                stageMapData.fieldEffects.Add(new FieldEffectSpawnData
                {
                    position = cell,
                    effectType = fieldEffectType,
                    value = Mathf.Max(0, fieldEffectValue),
                    duration = Mathf.Max(0, fieldEffectDuration)
                });
                EditorUtility.SetDirty(stageMapData);
                break;
        }
    }

    private void EraseCell(Vector3Int cell)
    {
        if (waveData != null && waveData.monsters != null)
        {
            Undo.RecordObject(waveData, "Erase Stage Monster");
            if (waveData.monsters.RemoveAll(x => x != null && x.position == cell) > 0)
                EditorUtility.SetDirty(waveData);
        }

        if (stageMapData != null)
        {
            Undo.RecordObject(stageMapData, "Erase Stage Map Data");
            bool changed = false;
            if (stageMapData.obstacles != null)
                changed |= stageMapData.obstacles.RemoveAll(x => x != null && x.position == cell) > 0;
            if (stageMapData.fieldEffects != null)
                changed |= stageMapData.fieldEffects.RemoveAll(x => x != null && x.position == cell) > 0;
            if (changed) EditorUtility.SetDirty(stageMapData);
        }
    }

    private void DrawPreview()
    {
        if (stageMapData != null)
        {
            if (stageMapData.fieldEffects != null)
                foreach (var data in stageMapData.fieldEffects)
                    if (data != null) DrawCell(data.position, new Color(1f, 0.55f, 0.1f, 0.35f), data.effectType.ToString());

            if (stageMapData.obstacles != null)
                foreach (var data in stageMapData.obstacles)
                    if (data != null) DrawCell(data.position, new Color(0.25f, 0.65f, 1f, 0.35f), data.prefab != null ? data.prefab.name : "Obstacle");
        }

        if (waveData != null && waveData.monsters != null)
            foreach (var data in waveData.monsters)
                if (data != null) DrawCell(data.position, new Color(1f, 0.2f, 0.25f, 0.35f), $"M:{data.monsterID}");
    }

    private void DrawCell(Vector3Int cell, Color color, string label)
    {
        Vector3 center = tilemap.GetCellCenterWorld(cell);
        Vector3 size = Vector3.Scale(tilemap.layoutGrid.cellSize, tilemap.transform.lossyScale);
        float radius = Mathf.Max(0.15f, Mathf.Min(Mathf.Abs(size.x), Mathf.Abs(size.y)) * 0.38f);
        Handles.color = color;
        Handles.DrawSolidDisc(center, tilemap.transform.forward, radius);
        Handles.color = Color.white;
        Handles.Label(center, label);
    }
}
#endif
