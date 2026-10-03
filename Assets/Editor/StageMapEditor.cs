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
    private int selectedWaveIndex;
    private int monsterId;
    private GameObject obstaclePrefab;
    private int obstacleRotation;
    private bool obstacleBlocksMovement = true;
    private bool obstacleBlocksAttack;
    private int obstacleHitPoint;
    private StageFieldEffectType fieldEffectType = StageFieldEffectType.Fire;
    private int fieldEffectValue = 1;
    private int fieldEffectDuration = 3;
    private EditMode mode = EditMode.Monster;
    private Vector2 scroll;

    private WaveManager SceneWaveManager => Object.FindFirstObjectByType<WaveManager>(FindObjectsInactive.Include);

    private StageMapBinding CurrentBinding => FindBinding(tilemap);

    private WaveData[] CurrentStageWaves
    {
        get
        {
            WaveManager manager = SceneWaveManager;
            StageMapBinding binding = CurrentBinding;
            return manager != null && binding != null ? manager.GetStageWaves(binding.StageIndex) : null;
        }
    }

    private WaveData CurrentWave
    {
        get
        {
            WaveData[] waves = CurrentStageWaves;
            if (waves == null || selectedWaveIndex < 0 || selectedWaveIndex >= waves.Length) return null;
            return waves[selectedWaveIndex];
        }
    }

    [MenuItem("Tools/Stage Map Editor")]
    public static void Open() => GetWindow<StageMapEditor>("Stage Map Editor");

    private void OnEnable() => SceneView.duringSceneGui += OnSceneGUI;
    private void OnDisable() => SceneView.duringSceneGui -= OnSceneGUI;

    private void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);
        EditorGUILayout.LabelField("Stage Map Editor", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Tilemap을 중심으로 이 맵에서 사용할 웨이브/장애물/필드 이펙트를 편집합니다. Scene View 좌클릭 배치, 우클릭 삭제.", MessageType.Info);

        EditorGUI.BeginChangeCheck();
        tilemap = (Tilemap)EditorGUILayout.ObjectField("Tilemap", tilemap, typeof(Tilemap), true);
        if (EditorGUI.EndChangeCheck()) TryAutoFindStageMapData();

        stageMapData = (StageMapData)EditorGUILayout.ObjectField("Stage Map Data", stageMapData, typeof(StageMapData), false);

        EditorGUILayout.Space();
        DrawWaveSection();

        EditorGUILayout.Space();
        mode = (EditMode)GUILayout.Toolbar((int)mode, new[] { "Monster", "Obstacle", "Field Effect", "Erase" });
        EditorGUILayout.Space();

        switch (mode)
        {
            case EditMode.Monster:
                DrawMonsterSettings();
                break;
            case EditMode.Obstacle:
                DrawObstacleSettings();
                break;
            case EditMode.FieldEffect:
                DrawFieldEffectSettings();
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

    private void DrawWaveSection()
    {
        EditorGUILayout.LabelField("이 Tilemap의 기존 Waves", EditorStyles.boldLabel);
        if (tilemap == null)
        {
            EditorGUILayout.HelpBox("먼저 Tilemap을 선택하세요.", MessageType.Warning);
            return;
        }

        WaveManager manager = SceneWaveManager;
        if (manager == null)
        {
            EditorGUILayout.HelpBox("현재 Scene에서 WaveManager를 찾을 수 없습니다.", MessageType.Warning);
            return;
        }

        StageMapBinding binding = CurrentBinding;
        if (binding == null)
        {
            EditorGUILayout.HelpBox("이 Tilemap에는 아직 StageMapBinding이 없습니다. 아래 버튼으로 연결 정보를 만든 뒤 Stage Index를 지정하세요.", MessageType.Warning);
            if (GUILayout.Button("이 Tilemap에 Stage Binding 추가"))
            {
                GameObject target = tilemap.transform.parent != null ? tilemap.transform.parent.gameObject : tilemap.gameObject;
                binding = Undo.AddComponent<StageMapBinding>(target);
                binding.EditorSetTilemap(tilemap);
                EditorUtility.SetDirty(binding);
            }
            return;
        }

        SerializedObject bindingSO = new SerializedObject(binding);
        SerializedProperty stageIndexProp = bindingSO.FindProperty("stageIndex");
        SerializedProperty mapDataProp = bindingSO.FindProperty("mapData");
        EditorGUILayout.PropertyField(stageIndexProp, new GUIContent("Stage Index"));
        EditorGUILayout.PropertyField(mapDataProp, new GUIContent("Stage Map Data"));
        if (bindingSO.ApplyModifiedProperties())
        {
            EditorUtility.SetDirty(binding);
            stageMapData = binding.MapData;
        }

        if (binding.MapData != null) stageMapData = binding.MapData;
        int stageIndex = Mathf.Max(0, binding.StageIndex);

        SerializedObject managerSO = new SerializedObject(manager);
        string[] stageProperties = { "stage1Waves", "stage2Waves", "stage3Waves", "stage4Waves", "stage5Waves" };
        if (stageIndex >= stageProperties.Length)
        {
            EditorGUILayout.HelpBox($"Stage Index {stageIndex}에 대응하는 WaveManager 배열이 없습니다.", MessageType.Error);
            return;
        }

        SerializedProperty waveArray = managerSO.FindProperty(stageProperties[stageIndex]);
        if (waveArray == null) return;

        WaveData[] waves = new WaveData[waveArray.arraySize];
        for (int i = 0; i < waveArray.arraySize; i++)
            waves[i] = waveArray.GetArrayElementAtIndex(i).objectReferenceValue as WaveData;

        selectedWaveIndex = Mathf.Clamp(selectedWaveIndex, 0, Mathf.Max(0, waves.Length - 1));
        EditorGUILayout.LabelField($"연결: {tilemap.name} → Stage {stageIndex + 1}", EditorStyles.miniBoldLabel);

        for (int i = 0; i < waves.Length; i++)
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Toggle(selectedWaveIndex == i, $"Wave {i + 1}", "Button", GUILayout.Width(75)))
                selectedWaveIndex = i;

            EditorGUI.BeginChangeCheck();
            WaveData changed = (WaveData)EditorGUILayout.ObjectField(waves[i], typeof(WaveData), false);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(manager, "Change Existing Stage Wave");
                waveArray.GetArrayElementAtIndex(i).objectReferenceValue = changed;
                managerSO.ApplyModifiedProperties();
                EditorUtility.SetDirty(manager);
            }
            EditorGUILayout.EndHorizontal();
        }
    }

    private StageMapBinding FindBinding(Tilemap target)
    {
        if (target == null) return null;
        StageMapBinding binding = target.GetComponent<StageMapBinding>();
        if (binding != null) return binding;
        binding = target.GetComponentInParent<StageMapBinding>(true);
        if (binding != null && binding.TargetTilemap == target) return binding;
        foreach (StageMapBinding candidate in Object.FindObjectsByType<StageMapBinding>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (candidate.TargetTilemap == target) return candidate;
        return null;
    }

    private void DrawMonsterSettings()
    {
        WaveData wave = CurrentWave;
        EditorGUILayout.LabelField($"현재 Wave: {(wave != null ? (selectedWaveIndex + 1).ToString() : "없음")}");
        if (wave == null)
        {
            EditorGUILayout.HelpBox("위 Wave 목록에서 편집할 WaveData를 선택하세요.", MessageType.Warning);
            return;
        }
        monsterId = EditorGUILayout.IntField("Monster ID", monsterId);

        SerializedObject so = new SerializedObject(wave);
        SerializedProperty monsters = so.FindProperty("monsters");
        EditorGUILayout.PropertyField(monsters, new GUIContent("현재 Wave 몬스터"), true);
        if (so.ApplyModifiedProperties()) EditorUtility.SetDirty(wave);
    }

    private void DrawObstacleSettings()
    {
        obstaclePrefab = (GameObject)EditorGUILayout.ObjectField("Obstacle Prefab", obstaclePrefab, typeof(GameObject), false);
        obstacleRotation = EditorGUILayout.IntSlider("Rotation (90°)", obstacleRotation, 0, 3);
        obstacleBlocksMovement = EditorGUILayout.Toggle("Blocks Movement", obstacleBlocksMovement);
        obstacleBlocksAttack = EditorGUILayout.Toggle("Blocks Attack", obstacleBlocksAttack);
        obstacleHitPoint = Mathf.Max(0, EditorGUILayout.IntField("HP (0 = indestructible)", obstacleHitPoint));

        if (stageMapData != null)
        {
            SerializedObject so = new SerializedObject(stageMapData);
            EditorGUILayout.PropertyField(so.FindProperty("obstacles"), new GUIContent("배치된 장애물"), true);
            if (so.ApplyModifiedProperties()) EditorUtility.SetDirty(stageMapData);
        }
    }

    private void DrawFieldEffectSettings()
    {
        fieldEffectType = (StageFieldEffectType)EditorGUILayout.EnumPopup("Effect Type", fieldEffectType);
        fieldEffectValue = Mathf.Max(0, EditorGUILayout.IntField("Value", fieldEffectValue));
        fieldEffectDuration = Mathf.Max(0, EditorGUILayout.IntField("Duration", fieldEffectDuration));
        if (stageMapData != null)
        {
            SerializedObject so = new SerializedObject(stageMapData);
            EditorGUILayout.PropertyField(so.FindProperty("fieldEffects"), new GUIContent("배치된 필드 이펙트"), true);
            if (so.ApplyModifiedProperties()) EditorUtility.SetDirty(stageMapData);
        }
    }

    private void TryAutoFindStageMapData()
    {
        if (tilemap == null) return;
        selectedWaveIndex = 0;
        StageMapBinding binding = FindBinding(tilemap);
        if (binding != null && binding.MapData != null)
        {
            stageMapData = binding.MapData;
            return;
        }
        StageMapLoader loader = tilemap.GetComponentInParent<StageMapLoader>(true);
        if (loader != null && loader.MapData != null) stageMapData = loader.MapData;
    }

    private void DrawCounts()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("현재 데이터", EditorStyles.boldLabel);
        int monsters = CurrentWave != null && CurrentWave.monsters != null ? CurrentWave.monsters.Count : 0;
        int obstacles = stageMapData != null && stageMapData.obstacles != null ? stageMapData.obstacles.Count : 0;
        int effects = stageMapData != null && stageMapData.fieldEffects != null ? stageMapData.fieldEffects.Count : 0;
        EditorGUILayout.LabelField($"Wave {selectedWaveIndex + 1} Monsters: {monsters}");
        EditorGUILayout.LabelField($"Obstacles: {obstacles}");
        EditorGUILayout.LabelField($"Field Effects: {effects}");
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (tilemap == null) return;
        DrawPreview();

        Event e = Event.current;
        if (e == null || e.alt || e.type != EventType.MouseDown || (e.button != 0 && e.button != 1)) return;

        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        Plane plane = new Plane(tilemap.transform.forward, tilemap.transform.position);
        if (!plane.Raycast(ray, out float distance)) return;

        Vector3Int cell = tilemap.WorldToCell(ray.GetPoint(distance));
        if (!tilemap.HasTile(cell)) return;

        if (e.button == 1 || mode == EditMode.Erase) EraseCell(cell);
        else PaintCell(cell);

        e.Use();
        Repaint();
        SceneView.RepaintAll();
    }

    private void PaintCell(Vector3Int cell)
    {
        if (mode == EditMode.Monster)
        {
            WaveData wave = CurrentWave;
            if (wave == null) return;
            Undo.RecordObject(wave, "Place Stage Monster");
            if (wave.monsters == null) wave.monsters = new List<MonsterSpawnData>();
            wave.monsters.RemoveAll(x => x != null && x.position == cell);
            wave.monsters.Add(new MonsterSpawnData { monsterID = monsterId, position = cell });
            EditorUtility.SetDirty(wave);
        }
        else if (mode == EditMode.Obstacle)
        {
            if (stageMapData == null || obstaclePrefab == null) return;
            Undo.RecordObject(stageMapData, "Place Stage Obstacle");
            if (stageMapData.obstacles == null) stageMapData.obstacles = new List<ObstacleSpawnData>();
            stageMapData.obstacles.RemoveAll(x => x != null && x.position == cell);
            stageMapData.obstacles.Add(new ObstacleSpawnData {
                prefab = obstaclePrefab, position = cell, rotationQuarterTurns = obstacleRotation,
                blocksMovement = obstacleBlocksMovement, blocksAttack = obstacleBlocksAttack, hitPoint = obstacleHitPoint
            });
            EditorUtility.SetDirty(stageMapData);
        }
        else if (mode == EditMode.FieldEffect)
        {
            if (stageMapData == null) return;
            Undo.RecordObject(stageMapData, "Paint Stage Field Effect");
            if (stageMapData.fieldEffects == null) stageMapData.fieldEffects = new List<FieldEffectSpawnData>();
            stageMapData.fieldEffects.RemoveAll(x => x != null && x.position == cell);
            stageMapData.fieldEffects.Add(new FieldEffectSpawnData {
                position = cell, effectType = fieldEffectType, value = fieldEffectValue, duration = fieldEffectDuration
            });
            EditorUtility.SetDirty(stageMapData);
        }
    }

    private void EraseCell(Vector3Int cell)
    {
        WaveData wave = CurrentWave;
        if (wave != null && wave.monsters != null)
        {
            Undo.RecordObject(wave, "Erase Stage Monster");
            if (wave.monsters.RemoveAll(x => x != null && x.position == cell) > 0) EditorUtility.SetDirty(wave);
        }
        if (stageMapData == null) return;
        Undo.RecordObject(stageMapData, "Erase Stage Map Data");
        bool changed = false;
        if (stageMapData.obstacles != null) changed |= stageMapData.obstacles.RemoveAll(x => x != null && x.position == cell) > 0;
        if (stageMapData.fieldEffects != null) changed |= stageMapData.fieldEffects.RemoveAll(x => x != null && x.position == cell) > 0;
        if (changed) EditorUtility.SetDirty(stageMapData);
    }

    private void DrawPreview()
    {
        if (stageMapData != null)
        {
            if (stageMapData.fieldEffects != null)
                foreach (var d in stageMapData.fieldEffects) if (d != null) DrawCell(d.position, new Color(1f,.55f,.1f,.35f), d.effectType.ToString());
            if (stageMapData.obstacles != null)
                foreach (var d in stageMapData.obstacles) if (d != null) DrawCell(d.position, new Color(.25f,.65f,1f,.35f), d.prefab != null ? d.prefab.name : "Obstacle");
        }
        WaveData wave = CurrentWave;
        if (wave != null && wave.monsters != null)
            foreach (var d in wave.monsters) if (d != null) DrawCell(d.position, new Color(1f,.2f,.25f,.35f), $"M:{d.monsterID}");
    }

    private void DrawCell(Vector3Int cell, Color color, string label)
    {
        Vector3 center = tilemap.GetCellCenterWorld(cell);
        Vector3 size = Vector3.Scale(tilemap.layoutGrid.cellSize, tilemap.transform.lossyScale);
        float radius = Mathf.Max(.15f, Mathf.Min(Mathf.Abs(size.x), Mathf.Abs(size.y)) * .38f);
        Handles.color = color;
        Handles.DrawSolidDisc(center, tilemap.transform.forward, radius);
        Handles.color = Color.white;
        Handles.Label(center, label);
    }
}
#endif
