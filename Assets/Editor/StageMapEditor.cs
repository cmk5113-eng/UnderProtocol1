#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

public partial class StageMapEditor : EditorWindow
{
    private enum EditMode { Monster, Obstacle, FieldEffect, Erase }

    [SerializeField] private StageMapData stageMapData;
    [SerializeField] private Tilemap tilemap;
    [SerializeField] private int selectedWaveIndex;
    private bool scenePaintingEnabled;
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

    private void OnEnable()
    {
        minSize = new Vector2(430, 500);
        SceneView.duringSceneGui += OnSceneGUI;
        Undo.undoRedoPerformed += RefreshViews;
    }

    private void OnDisable()
    {
        EndPaintStroke();
        SceneView.duringSceneGui -= OnSceneGUI;
        Undo.undoRedoPerformed -= RefreshViews;
    }

    private void RefreshViews()
    {
        Repaint();
        SceneView.RepaintAll();
    }

    private void OnGUI()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            EditorGUILayout.HelpBox("몬스터 배치와 웨이브 목록은 Play Mode를 종료한 뒤 편집하세요.", MessageType.Info);
        using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode)) DrawEditorGUI();
    }

    private void DrawEditorGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);
        EditorGUILayout.LabelField("Stage Map Editor", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Tilemap과 Wave를 선택한 뒤 몬스터를 아래 타일판에 찍으세요. 좌클릭/드래그 배치, 우클릭/드래그 삭제. WaveData와 웨이브 목록(Scene)은 각각 저장합니다.", MessageType.Info);

        EditorGUI.BeginChangeCheck();
        tilemap = (Tilemap)EditorGUILayout.ObjectField("Tilemap", tilemap, typeof(Tilemap), true);
        if (EditorGUI.EndChangeCheck()) TryAutoFindStageMapData();

        if (GUILayout.Button("Hierarchy에서 선택한 Tilemap 가져오기") && Selection.activeGameObject != null)
        {
            Tilemap selected = Selection.activeGameObject.GetComponent<Tilemap>()
                ?? Selection.activeGameObject.GetComponentInChildren<Tilemap>(true);
            if (selected != null)
            {
                tilemap = selected;
                TryAutoFindStageMapData();
            }
        }

        stageMapData = (StageMapData)EditorGUILayout.ObjectField("Stage Map Data", stageMapData, typeof(StageMapData), false);

        EditorGUILayout.Space();
        DrawWaveSection();

        EditorGUILayout.Space();
        mode = (EditMode)GUILayout.Toolbar((int)mode, new[] { "Monster", "Obstacle", "Field Effect", "Erase" });
        scenePaintingEnabled = EditorGUILayout.Toggle("Scene View에서도 배치", scenePaintingEnabled);
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
        WaveManager manager = SceneWaveManager;
        if (manager == null)
        {
            EditorGUILayout.HelpBox("현재 Scene에서 WaveManager를 찾을 수 없습니다.", MessageType.Warning);
            return;
        }

        StageWaveEditorUtility.EnsureMigrated(manager);
        EditorGUILayout.LabelField($"전체 스테이지: {manager.StageCount}개 · Stage Index는 0부터 시작");
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Stage 추가"))
        {
            int addedIndex = StageWaveEditorUtility.AddStage(manager);
            StageMapBinding selectedBinding = CurrentBinding;
            if (selectedBinding != null && addedIndex >= 0)
            {
                Undo.RecordObject(selectedBinding, "Connect New Wave Stage");
                selectedBinding.EditorSetStageIndex(addedIndex);
                MarkSceneObjectDirty(selectedBinding);
                selectedWaveIndex = 0;
            }
            RefreshViews();
            GUIUtility.ExitGUI();
        }
        StageMapBinding currentBinding = CurrentBinding;
        using (new EditorGUI.DisabledScope(currentBinding == null || currentBinding.StageIndex < 0 || currentBinding.StageIndex >= manager.StageCount))
        {
            if (GUILayout.Button("현재 Stage 삭제"))
            {
                StageWaveEditorUtility.RemoveStage(manager, currentBinding.StageIndex);
                selectedWaveIndex = 0;
                RefreshViews();
                GUIUtility.ExitGUI();
            }
        }
        EditorGUILayout.EndHorizontal();
        if (GUILayout.Button("스테이지/웨이브 목록 저장 (Scene)")) SaveWaveList();
        if (tilemap == null)
        {
            EditorGUILayout.HelpBox("몬스터와 Wave를 편집하려면 Tilemap을 선택하세요.", MessageType.Warning);
            return;
        }

        StageMapBinding binding = CurrentBinding;
        if (binding == null)
        {
            EditorGUILayout.HelpBox("이 Tilemap에는 아직 StageMapBinding이 없습니다. 아래 버튼으로 연결 정보를 만든 뒤 Stage Index를 지정하세요.", MessageType.Warning);
            if (GUILayout.Button("이 Tilemap에 Stage Binding 추가"))
            {
                binding = Undo.AddComponent<StageMapBinding>(tilemap.gameObject);
                binding.EditorSetTilemap(tilemap);
                MarkSceneObjectDirty(binding);
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
            MarkSceneObjectDirty(binding);
            stageMapData = binding.MapData;
            RefreshViews();
        }

        if (binding.MapData != null) stageMapData = binding.MapData;
        int stageIndex = binding.StageIndex;

        SerializedObject managerSO = new SerializedObject(manager);
        if (stageIndex < 0 || stageIndex >= manager.StageCount)
        {
            EditorGUILayout.HelpBox(manager.StageCount == 0 ? "Stage 추가로 첫 스테이지를 만드세요." : $"Stage Index를 0~{manager.StageCount - 1} 사이로 지정하세요. -1은 연결 해제 상태입니다.", MessageType.Warning);
            return;
        }

        SerializedProperty waveArray = GetWaveArray(managerSO);
        if (waveArray == null) return;

        selectedWaveIndex = Mathf.Clamp(selectedWaveIndex, 0, Mathf.Max(0, waveArray.arraySize - 1));
        EditorGUILayout.LabelField($"연결: {tilemap.name} → Stage {stageIndex + 1}", EditorStyles.miniBoldLabel);
        EditorGUILayout.LabelField($"총 {waveArray.arraySize} Waves · 위에서 아래 순서로 실행");

        for (int i = 0; i < waveArray.arraySize; i++)
        {
            bool listChanged = false;
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Toggle(selectedWaveIndex == i, $"Wave {i + 1}", "Button", GUILayout.Width(75)))
            {
                if (selectedWaveIndex != i) RefreshViews();
                selectedWaveIndex = i;
            }

            EditorGUI.BeginChangeCheck();
            WaveData changed = (WaveData)EditorGUILayout.ObjectField(waveArray.GetArrayElementAtIndex(i).objectReferenceValue, typeof(WaveData), false);
            if (EditorGUI.EndChangeCheck())
            {
                waveArray.GetArrayElementAtIndex(i).objectReferenceValue = changed;
                ApplyWaveListChanges(managerSO);
            }
            using (new EditorGUI.DisabledScope(i == 0))
                if (GUILayout.Button("↑", GUILayout.Width(25))) { MoveWaveSlot(i, i - 1); listChanged = true; }
            using (new EditorGUI.DisabledScope(i == waveArray.arraySize - 1))
                if (GUILayout.Button("↓", GUILayout.Width(25))) { MoveWaveSlot(i, i + 1); listChanged = true; }
            if (GUILayout.Button("−", GUILayout.Width(25))) { RemoveWaveSlot(i); listChanged = true; }
            EditorGUILayout.EndHorizontal();
            if (listChanged) GUIUtility.ExitGUI();
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("빈 Wave 슬롯 추가")) { AddWaveSlot(null); GUIUtility.ExitGUI(); }
        if (GUILayout.Button("새 WaveData 추가")) { CreateWaveAsset(-1); GUIUtility.ExitGUI(); }
        EditorGUILayout.EndHorizontal();
        if (GUILayout.Button("웨이브 목록 저장 (Scene)")) SaveWaveList();
        EditorGUILayout.HelpBox("WaveData 칸에 기존 에셋을 넣으면 불러옵니다. ↑/↓ 순서 변경, − 목록에서 제거(에셋은 유지). 빈 슬롯은 전투 진입 전에 채워주세요.", MessageType.Info);
    }

    private SerializedProperty GetWaveArray(SerializedObject managerSO)
    {
        StageMapBinding binding = CurrentBinding;
        SerializedProperty stages = managerSO.FindProperty("stages");
        return stages != null && binding != null && binding.StageIndex >= 0 && binding.StageIndex < stages.arraySize
            ? stages.GetArrayElementAtIndex(binding.StageIndex).FindPropertyRelative("waves") : null;
    }

    private void ApplyWaveListChanges(SerializedObject managerSO)
    {
        if (managerSO.ApplyModifiedProperties()) MarkSceneObjectDirty((Component)managerSO.targetObject);
        RefreshViews();
    }

    private void AddWaveSlot(WaveData wave)
    {
        WaveManager manager = SceneWaveManager;
        if (manager == null) return;
        SerializedObject so = new SerializedObject(manager);
        SerializedProperty array = GetWaveArray(so);
        if (array == null) return;
        selectedWaveIndex = array.arraySize;
        array.arraySize++;
        // Unity는 배열을 늘리면 마지막 참조를 복사하므로 새 슬롯을 명시적으로 설정한다.
        array.GetArrayElementAtIndex(selectedWaveIndex).objectReferenceValue = wave;
        ApplyWaveListChanges(so);
    }

    private void RemoveWaveSlot(int index)
    {
        WaveManager manager = SceneWaveManager;
        if (manager == null) return;
        SerializedObject so = new SerializedObject(manager);
        SerializedProperty array = GetWaveArray(so);
        if (array == null || index < 0 || index >= array.arraySize) return;
        array.GetArrayElementAtIndex(index).objectReferenceValue = null;
        array.DeleteArrayElementAtIndex(index);
        if (selectedWaveIndex > index) selectedWaveIndex--;
        selectedWaveIndex = Mathf.Clamp(selectedWaveIndex, 0, Mathf.Max(0, array.arraySize - 1));
        ApplyWaveListChanges(so);
    }

    private void MoveWaveSlot(int from, int to)
    {
        WaveManager manager = SceneWaveManager;
        if (manager == null) return;
        SerializedObject so = new SerializedObject(manager);
        SerializedProperty array = GetWaveArray(so);
        if (array == null || from < 0 || to < 0 || from >= array.arraySize || to >= array.arraySize) return;
        array.MoveArrayElement(from, to);
        if (selectedWaveIndex == from) selectedWaveIndex = to;
        else if (from < selectedWaveIndex && to >= selectedWaveIndex) selectedWaveIndex--;
        else if (from > selectedWaveIndex && to <= selectedWaveIndex) selectedWaveIndex++;
        ApplyWaveListChanges(so);
    }

    private void CreateWaveAsset(int slot)
    {
        WaveManager manager = SceneWaveManager;
        if (manager == null || CurrentBinding == null) return;
        SerializedObject so = new SerializedObject(manager);
        SerializedProperty array = GetWaveArray(so);
        if (array == null || slot >= array.arraySize) return;
        string path = EditorUtility.SaveFilePanelInProject("WaveData 만들기", $"Stage{CurrentBinding.StageIndex + 1}_Wave{(slot < 0 ? array.arraySize : slot) + 1}", "asset", "몬스터 배치를 저장할 WaveData 경로를 선택하세요.");
        if (string.IsNullOrEmpty(path)) return;
        WaveData wave = CreateInstance<WaveData>();
        AssetDatabase.CreateAsset(wave, path);
        if (slot < 0) AddWaveSlot(wave);
        else
        {
            array.GetArrayElementAtIndex(slot).objectReferenceValue = wave;
            selectedWaveIndex = slot;
            ApplyWaveListChanges(so);
        }
        AssetDatabase.SaveAssetIfDirty(wave);
        EditorGUIUtility.PingObject(wave);
    }

    private static void MarkSceneObjectDirty(Component component)
    {
        EditorUtility.SetDirty(component);
        PrefabUtility.RecordPrefabInstancePropertyModifications(component);
        if (component.gameObject.scene.IsValid()) EditorSceneManager.MarkSceneDirty(component.gameObject.scene);
    }

    private void SaveWaveList()
    {
        WaveManager manager = SceneWaveManager;
        if (manager == null) return;
        var scene = manager.gameObject.scene;
        if (!scene.IsValid() || !EditorSceneManager.SaveScene(scene)) return;
        StageMapBinding binding = CurrentBinding;
        if (binding != null && binding.gameObject.scene != scene && !EditorSceneManager.SaveScene(binding.gameObject.scene)) return;
        ShowNotification(new GUIContent("웨이브 목록과 Stage Binding 저장 완료"));
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
        selectedWaveIndex = 0;
        selectedMonster = null;
        gridScroll = Vector2.zero;
        stageMapData = null;
        RefreshViews();
        if (tilemap == null) return;
        gridLayer = tilemap.cellBounds.zMin;
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
        if (!scenePaintingEnabled || EditorApplication.isPlayingOrWillChangePlaymode) return;

        Event e = Event.current;
        int control = GUIUtility.GetControlID(FocusType.Passive);
        if (e != null && e.type == EventType.Layout) HandleUtility.AddDefaultControl(control);
        if (e == null || e.alt || e.type != EventType.MouseDown || (e.button != 0 && e.button != 1)) return;
        if (HandleUtility.nearestControl != control) return;

        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        Plane plane = new Plane(tilemap.transform.forward, tilemap.GetCellCenterWorld(new Vector3Int(0, 0, gridLayer)));
        if (!plane.Raycast(ray, out float distance)) return;

        Vector3Int cell = tilemap.WorldToCell(ray.GetPoint(distance));
        cell.z = gridLayer;
        if (!tilemap.HasTile(cell)) return;

        bool changed = e.button == 1 || mode == EditMode.Erase ? EraseCell(cell) : PaintCell(cell);
        if (changed) e.Use();
        RefreshViews();
    }

    private bool PaintCell(Vector3Int cell)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || tilemap == null || !tilemap.HasTile(cell)) return false;
        if (mode == EditMode.Monster)
        {
            WaveData wave = CurrentWave;
            RefreshMonsterPalette();
            if (wave == null || GetMonsterProblem(selectedMonster) != null) return false;
            if (wave.monsters != null && wave.monsters.FindAll(x => x != null && x.position == cell).Count == 1
                && wave.monsters.Exists(x => x != null && x.position == cell && x.monsterID == selectedMonster.id)) return false;
            Undo.RecordObject(wave, "Place Stage Monster");
            if (wave.monsters == null) wave.monsters = new List<MonsterSpawnData>();
            wave.monsters.RemoveAll(x => x != null && x.position == cell);
            wave.monsters.Add(new MonsterSpawnData { monsterID = selectedMonster.id, position = cell });
            EditorUtility.SetDirty(wave);
        }
        else if (mode == EditMode.Obstacle)
        {
            if (stageMapData == null || obstaclePrefab == null) return false;
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
            if (stageMapData == null) return false;
            Undo.RecordObject(stageMapData, "Paint Stage Field Effect");
            if (stageMapData.fieldEffects == null) stageMapData.fieldEffects = new List<FieldEffectSpawnData>();
            stageMapData.fieldEffects.RemoveAll(x => x != null && x.position == cell);
            stageMapData.fieldEffects.Add(new FieldEffectSpawnData {
                position = cell, effectType = fieldEffectType, value = fieldEffectValue, duration = fieldEffectDuration
            });
            EditorUtility.SetDirty(stageMapData);
        }
        else return false;
        return true;
    }

    private bool EraseCell(Vector3Int cell)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || tilemap == null || !tilemap.HasTile(cell)) return false;
        bool changed = false;
        WaveData wave = CurrentWave;
        if ((mode == EditMode.Monster || mode == EditMode.Erase) && wave != null && wave.monsters != null
            && wave.monsters.Exists(x => x != null && x.position == cell))
        {
            Undo.RecordObject(wave, "Erase Stage Monster");
            wave.monsters.RemoveAll(x => x != null && x.position == cell);
            EditorUtility.SetDirty(wave);
            changed = true;
        }
        if (stageMapData == null) return changed;
        bool eraseObstacle = (mode == EditMode.Obstacle || mode == EditMode.Erase) && stageMapData.obstacles != null
            && stageMapData.obstacles.Exists(x => x != null && x.position == cell);
        bool eraseEffect = (mode == EditMode.FieldEffect || mode == EditMode.Erase) && stageMapData.fieldEffects != null
            && stageMapData.fieldEffects.Exists(x => x != null && x.position == cell);
        if (eraseObstacle || eraseEffect)
        {
            Undo.RecordObject(stageMapData, "Erase Stage Map Data");
            if (eraseObstacle) stageMapData.obstacles.RemoveAll(x => x != null && x.position == cell);
            if (eraseEffect) stageMapData.fieldEffects.RemoveAll(x => x != null && x.position == cell);
            EditorUtility.SetDirty(stageMapData);
            changed = true;
        }
        return changed;
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
        {
            RefreshMonsterPalette();
            foreach (var d in wave.monsters)
            {
                if (d == null) continue;
                MonsterData data = FindMonsterData(d.monsterID);
                DrawCell(d.position, new Color(1f,.2f,.25f,.35f), data != null ? $"{MonsterLabel(data)} ({d.monsterID})" : $"M:{d.monsterID}");
            }
        }
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
