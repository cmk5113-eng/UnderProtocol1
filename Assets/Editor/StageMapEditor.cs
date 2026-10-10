#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

public partial class StageMapEditor : EditorWindow
{
    private enum EditMode { Monster, Obstacle, FieldEffect, Erase }

    private StageMapData stageMapData => CurrentMap != null ? CurrentMap.mapData : null;
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

    private WaveManager SceneWaveManager
    {
        get
        {
            WaveManager[] managers = Object.FindObjectsByType<WaveManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            WaveManager found = null;
            foreach (WaveManager manager in managers)
            {
                if (tilemap != null && manager.GetMap(tilemap) == null
                    && manager.gameObject.scene != tilemap.gameObject.scene) continue;
                if (found != null) return null;
                found = manager;
            }
            return found;
        }
    }

    private StageMapEntry CurrentMap => SceneWaveManager != null ? SceneWaveManager.GetMap(tilemap) : null;

    private WaveData[] CurrentStageWaves
    {
        get
        {
            WaveManager manager = SceneWaveManager;
            return manager != null && CurrentMap != null ? CurrentMap.waves : null;
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
        EditorGUILayout.HelpBox("이 창에서 Tilemap, 입장 버튼, 웨이브 순서와 몬스터 목록을 설정하세요. 좌클릭/드래그 배치, 우클릭/드래그 삭제. WaveData 배치와 맵 설정(Scene)은 각각 저장합니다.", MessageType.Info);

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
        EditorGUILayout.LabelField("이 Tilemap의 맵 설정", EditorStyles.boldLabel);
        WaveManager manager = SceneWaveManager;
        if (manager == null)
        {
            EditorGUILayout.HelpBox("선택한 Tilemap의 Scene에는 WaveManager가 하나 있어야 합니다.", MessageType.Warning);
            return;
        }

        EditorGUILayout.LabelField($"맵에디터에 등록된 맵: {manager.Maps.Count}개");
        if (tilemap == null)
        {
            EditorGUILayout.HelpBox("Tilemap을 선택하세요.", MessageType.Warning);
            return;
        }
        if (CurrentMap == null)
        {
            EditorGUILayout.HelpBox("이 Tilemap은 맵에디터에 등록되지 않았습니다.", MessageType.Warning);
            if (GUILayout.Button("이 Tilemap 등록")) { RegisterCurrentMap(); GUIUtility.ExitGUI(); }
            return;
        }

        SerializedObject managerSO = new SerializedObject(manager);
        SerializedProperty mapProperty = GetMapProperty(managerSO);
        if (mapProperty == null) return;
        EditorGUILayout.PropertyField(mapProperty.FindPropertyRelative("stageId"), new GUIContent("클리어 기록 ID"));
        EditorGUILayout.PropertyField(mapProperty.FindPropertyRelative("stageButtons"), new GUIContent("입장 버튼 (Clear ID -1: 맵 ID)"), true);
        EditorGUILayout.PropertyField(mapProperty.FindPropertyRelative("mapData"), new GUIContent("Stage Map Data"));
        EditorGUILayout.PropertyField(mapProperty.FindPropertyRelative("postBattleScenario"), new GUIContent("전투 후 시나리오"));
        EditorGUILayout.PropertyField(mapProperty.FindPropertyRelative("monsterDatas"), new GUIContent("등록된 몬스터"), true);
        if (managerSO.ApplyModifiedProperties()) { MarkSceneObjectDirty(manager); RefreshViews(); }

        if (CurrentMap.stageId < 0)
            EditorGUILayout.HelpBox("클리어 기록 ID는 0 이상이어야 합니다.", MessageType.Error);
        foreach (StageMapEntry map in manager.Maps)
            if (map != CurrentMap && map != null && map.stageId == CurrentMap.stageId)
                EditorGUILayout.HelpBox("다른 맵과 클리어 기록 ID가 같습니다. 고유한 ID를 지정하세요.", MessageType.Warning);
        if (CurrentMap.stageButtons == null || CurrentMap.stageButtons.Count == 0)
            EditorGUILayout.HelpBox("이 맵을 여는 WaveSetter 버튼을 등록하세요.", MessageType.Warning);
        else foreach (StageMapButton button in CurrentMap.stageButtons)
            if (button == null || button.button == null || manager.GetMap(button.button) != CurrentMap)
                EditorGUILayout.HelpBox("빈 버튼을 제거하고, 한 버튼을 여러 맵에 등록하지 마세요.", MessageType.Error);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Stage Map Data 생성")) CreateMapDataAsset();
        if (GUILayout.Button("이 맵 설정 삭제")) { RemoveCurrentMap(); GUIUtility.ExitGUI(); }
        EditorGUILayout.EndHorizontal();
        if (stageMapData != null && GUILayout.Button("장애물/필드 데이터 저장"))
            AssetDatabase.SaveAssetIfDirty(stageMapData);

        SerializedProperty waveArray = GetWaveArray(managerSO);
        if (waveArray == null) return;

        selectedWaveIndex = Mathf.Clamp(selectedWaveIndex, 0, Mathf.Max(0, waveArray.arraySize - 1));
        EditorGUILayout.LabelField($"맵: {tilemap.name} · 클리어 기록 ID {CurrentMap.stageId}", EditorStyles.miniBoldLabel);
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
        if (GUILayout.Button("맵 설정 저장 (Scene)")) SaveWaveList();
        EditorGUILayout.HelpBox("WaveData 칸에 기존 에셋을 넣으면 불러옵니다. ↑/↓ 순서 변경, − 목록에서 제거(에셋은 유지). 빈 슬롯은 전투 진입 전에 채워주세요.", MessageType.Info);
    }

    private SerializedProperty GetWaveArray(SerializedObject managerSO)
    {
        return GetMapProperty(managerSO)?.FindPropertyRelative("waves");
    }

    private SerializedProperty GetMapProperty(SerializedObject managerSO)
    {
        SerializedProperty maps = managerSO.FindProperty("maps");
        if (maps == null || tilemap == null) return null;
        for (int i = 0; i < maps.arraySize; i++)
            if (maps.GetArrayElementAtIndex(i).FindPropertyRelative("tilemap").objectReferenceValue == tilemap)
                return maps.GetArrayElementAtIndex(i);
        return null;
    }

    private bool RegisterCurrentMap()
    {
        WaveManager manager = SceneWaveManager;
        if (manager == null || tilemap == null || EditorApplication.isPlayingOrWillChangePlaymode || CurrentMap != null) return false;
        Undo.RecordObject(manager, "Register Map in Stage Map Editor");
        if (!manager.EditorAddMap(tilemap)) return false;
        MarkSceneObjectDirty(manager);
        selectedWaveIndex = 0;
        RefreshViews();
        return true;
    }

    private bool RemoveCurrentMap()
    {
        WaveManager manager = SceneWaveManager;
        if (manager == null || EditorApplication.isPlayingOrWillChangePlaymode || CurrentMap == null) return false;
        Undo.RecordObject(manager, "Remove Map from Stage Map Editor");
        if (!manager.EditorRemoveMap(tilemap)) return false;
        MarkSceneObjectDirty(manager);
        selectedWaveIndex = 0;
        RefreshViews();
        return true;
    }

    private void CreateMapDataAsset()
    {
        if (CurrentMap == null) return;
        string path = EditorUtility.SaveFilePanelInProject("Stage Map Data 만들기", $"Stage{CurrentMap.stageId + 1}_Map", "asset", "장애물/필드 배치를 저장할 경로를 선택하세요.");
        if (string.IsNullOrEmpty(path)) return;
        StageMapData data = CreateInstance<StageMapData>();
        AssetDatabase.CreateAsset(data, path);
        SerializedObject so = new SerializedObject(SceneWaveManager);
        GetMapProperty(so).FindPropertyRelative("mapData").objectReferenceValue = data;
        ApplyWaveListChanges(so);
        AssetDatabase.SaveAssetIfDirty(data);
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
        if (manager == null || CurrentMap == null) return;
        SerializedObject so = new SerializedObject(manager);
        SerializedProperty array = GetWaveArray(so);
        if (array == null || slot >= array.arraySize) return;
        string path = EditorUtility.SaveFilePanelInProject("WaveData 만들기", $"Stage{CurrentMap.stageId + 1}_Wave{(slot < 0 ? array.arraySize : slot) + 1}", "asset", "몬스터 배치를 저장할 WaveData 경로를 선택하세요.");
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
        if (tilemap != null && tilemap.gameObject.scene != scene && !EditorSceneManager.SaveScene(tilemap.gameObject.scene)) return;
        ShowNotification(new GUIContent("맵에디터 설정 저장 완료"));
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
        RefreshViews();
        if (tilemap == null) return;
        gridLayer = tilemap.cellBounds.zMin;
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
            var replacing = wave.monsters != null ? wave.monsters.FindAll(spawn => SpawnCovers(spawn, cell)) : new List<MonsterSpawnData>();
            if (replacing.Count == 1 && replacing[0].monsterID == selectedMonster.id) return false;
            foreach (Vector3Int occupied in selectedMonster.GetOccupiedCells(cell))
            {
                if (!tilemap.HasTile(occupied) || HasBlockingObstacle(occupied))
                {
                    ShowNotification(new GUIContent("몬스터의 모든 점유 칸에 타일이 있어야 하며 장애물과 겹칠 수 없습니다."));
                    return false;
                }
                if (wave.monsters != null && wave.monsters.Exists(spawn => !replacing.Contains(spawn) && SpawnCovers(spawn, occupied)))
                {
                    ShowNotification(new GUIContent("다른 몬스터의 점유 영역과 겹칩니다."));
                    return false;
                }
            }
            Undo.RecordObject(wave, "Place Stage Monster");
            if (wave.monsters == null) wave.monsters = new List<MonsterSpawnData>();
            wave.monsters.RemoveAll(spawn => replacing.Contains(spawn));
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
        if (mode == EditMode.Monster || mode == EditMode.Erase) RefreshMonsterPalette();
        if ((mode == EditMode.Monster || mode == EditMode.Erase) && wave != null && wave.monsters != null
            && wave.monsters.Exists(spawn => SpawnCovers(spawn, cell)))
        {
            Undo.RecordObject(wave, "Erase Stage Monster");
            wave.monsters.RemoveAll(spawn => SpawnCovers(spawn, cell));
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
                foreach (Vector3Int cell in SpawnCells(d))
                    DrawCell(cell, new Color(1f,.2f,.25f,.35f), cell == d.position
                        ? data != null ? $"{MonsterLabel(data)} ({d.monsterID})" : $"M:{d.monsterID}" : "");
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

