#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public partial class StageMapEditor
{
    [SerializeField] private MonsterData selectedMonster;
    [SerializeField] private int gridCellSize = 48;
    [SerializeField] private int gridLayer;
    private Vector2 gridScroll;
    private bool showSpawnList;
    private int paintUndoGroup = -1;
    private readonly List<MonsterData> monsterPalette = new List<MonsterData>();

    private WaveLoader FindMapWaveLoader()
    {
        if (tilemap == null) return null;
        // WaveLoader.Instance와 같은 우선순위로 이 맵의 실제 생성 목록을 사용한다.
        WaveLoader loader = tilemap.GetComponentInParent<WaveLoader>(true);
        if (loader != null) return loader;
        PlacementController map = tilemap.GetComponentInParent<PlacementController>(true);
        if (map != null)
        {
            loader = map.GetComponentInChildren<WaveLoader>(true);
            if (loader != null) return loader;
        }
        return Object.FindFirstObjectByType<WaveLoader>(FindObjectsInactive.Include);
    }

    private void RefreshMonsterPalette()
    {
        monsterPalette.Clear();
        WaveLoader loader = FindMapWaveLoader();
        if (loader == null) return;
        SerializedProperty list = new SerializedObject(loader).FindProperty("monsterDatas");
        if (list == null) return;
        for (int i = 0; i < list.arraySize; i++)
        {
            MonsterData data = list.GetArrayElementAtIndex(i).objectReferenceValue as MonsterData;
            if (data != null) monsterPalette.Add(data);
        }
        if (selectedMonster == null)
            selectedMonster = monsterPalette.Find(data => GetMonsterProblem(data) == null);
    }

    private MonsterData FindMonsterData(int id) => monsterPalette.Find(data => data.id == id);
    private static string MonsterLabel(MonsterData data) => string.IsNullOrEmpty(data.monsterName) ? data.name : data.monsterName;

    private string GetMonsterProblem(MonsterData data)
    {
        if (data == null) return "배치할 몬스터를 선택하세요.";
        if (!monsterPalette.Contains(data)) return "선택한 MonsterData를 이 맵의 WaveLoader > Monster Datas에 등록하세요.";
        if (monsterPalette.FindAll(item => item.id == data.id).Count != 1)
            return $"Monster ID {data.id}가 중복 등록되어 있습니다. WaveLoader의 ID를 고유하게 설정하세요.";
        if (data.prefab == null || data.prefab.GetComponent<MonsterBase>() == null)
            return "MonsterData의 Prefab과 프리팹 루트의 MonsterBase를 설정하세요.";
        return null;
    }

    private static Texture2D MonsterThumbnail(MonsterData data)
    {
        if (data == null || data.prefab == null) return null;
        SpriteRenderer renderer = data.prefab.GetComponentInChildren<SpriteRenderer>(true);
        Object asset = renderer != null && renderer.sprite != null ? (Object)renderer.sprite : data.prefab;
        return AssetPreview.GetAssetPreview(asset) ?? AssetPreview.GetMiniThumbnail(asset);
    }

    private void DrawMonsterSettings()
    {
        RefreshMonsterPalette();
        WaveData wave = CurrentWave;
        EditorGUILayout.LabelField($"편집 중: Wave {selectedWaveIndex + 1} · {(wave != null ? wave.name : "빈 슬롯")}", EditorStyles.boldLabel);
        if (wave == null)
        {
            EditorGUILayout.HelpBox("위 목록에 WaveData를 넣거나 새로 만들어 몬스터 배치를 시작하세요.", MessageType.Warning);
            WaveData[] waves = CurrentStageWaves;
            if (waves != null && selectedWaveIndex < waves.Length && GUILayout.Button("이 슬롯에 WaveData 생성"))
                CreateWaveAsset(selectedWaveIndex);
            return;
        }

        using (new EditorGUI.DisabledScope(true))
            EditorGUILayout.ObjectField("몬스터 목록 출처", FindMapWaveLoader(), typeof(WaveLoader), true);

        if (monsterPalette.Count == 0)
            EditorGUILayout.HelpBox("이 맵의 WaveLoader > Monster Datas에 MonsterData를 등록하세요. 등록된 몬스터가 여기에 표시됩니다.", MessageType.Warning);

        int columns = Mathf.Max(1, Mathf.FloorToInt((position.width - 40) / 105f));
        for (int i = 0; i < monsterPalette.Count; i++)
        {
            if (i % columns == 0) EditorGUILayout.BeginHorizontal();
            MonsterData data = monsterPalette[i];
            string problem = GetMonsterProblem(data);
            using (new EditorGUI.DisabledScope(problem != null))
            {
                GUIContent content = new GUIContent($"{MonsterLabel(data)}\nID {data.id}", MonsterThumbnail(data), problem ?? $"{MonsterLabel(data)} 배치");
                if (GUILayout.Toggle(selectedMonster == data, content, "Button", GUILayout.Width(100), GUILayout.Height(70)))
                    selectedMonster = data;
            }
            if (i % columns == columns - 1 || i == monsterPalette.Count - 1) EditorGUILayout.EndHorizontal();
        }

        selectedMonster = (MonsterData)EditorGUILayout.ObjectField("배치할 몬스터", selectedMonster, typeof(MonsterData), false);
        string selectionProblem = GetMonsterProblem(selectedMonster);
        if (selectionProblem != null) EditorGUILayout.HelpBox(selectionProblem, MessageType.Warning);
        else EditorGUILayout.LabelField($"{MonsterLabel(selectedMonster)} · ID {selectedMonster.id} · 좌클릭 배치 / 우클릭 삭제");

        DrawMonsterGrid(wave);

        string waveProblem = GetWaveProblem(wave);
        if (waveProblem != null) EditorGUILayout.HelpBox(waveProblem, MessageType.Error);
        using (new EditorGUI.DisabledScope(waveProblem != null))
            if (GUILayout.Button("현재 WaveData 저장")) SaveCurrentWave();
        EditorGUILayout.LabelField(EditorUtility.IsDirty(wave) ? "배치 변경됨 · WaveData 저장 필요" : "WaveData 저장됨", EditorStyles.miniLabel);

        showSpawnList = EditorGUILayout.Foldout(showSpawnList, "배치 데이터 직접 수정", true);
        if (showSpawnList)
        {
            SerializedObject so = new SerializedObject(wave);
            EditorGUILayout.PropertyField(so.FindProperty("monsters"), new GUIContent("현재 Wave 몬스터"), true);
            if (so.ApplyModifiedProperties())
            {
                EditorUtility.SetDirty(wave);
                RefreshViews();
            }
        }
    }

    private string GetWaveProblem(WaveData wave)
    {
        if (wave == null || tilemap == null) return "Tilemap과 WaveData를 선택하세요.";
        if (wave.monsters == null) return null;
        var occupied = new HashSet<Vector3Int>();
        foreach (MonsterSpawnData spawn in wave.monsters)
        {
            if (spawn == null) return "빈 몬스터 항목을 제거하세요.";
            if (!tilemap.HasTile(spawn.position)) return $"{spawn.position}에는 타일이 없습니다. 배치를 수정하세요.";
            if (!occupied.Add(spawn.position)) return $"{spawn.position}에 몬스터가 중복 배치되어 있습니다.";
            string problem = GetMonsterProblem(FindMonsterData(spawn.monsterID));
            if (problem != null) return $"ID {spawn.monsterID} · {problem}";
        }
        return null;
    }

    private void SaveCurrentWave()
    {
        RefreshMonsterPalette();
        WaveData wave = CurrentWave;
        string problem = GetWaveProblem(wave);
        if (problem != null) { ShowNotification(new GUIContent(problem)); return; }
        if (wave.monsters == null)
        {
            Undo.RecordObject(wave, "Initialize Wave Monsters");
            wave.monsters = new List<MonsterSpawnData>();
            EditorUtility.SetDirty(wave);
        }
        AssetDatabase.SaveAssetIfDirty(wave);
        ShowNotification(new GUIContent($"{wave.name} 저장 완료"));
    }

    private void DrawMonsterGrid(WaveData wave)
    {
        if (tilemap == null) return;
        BoundsInt bounds = tilemap.cellBounds;
        if (bounds.size.x == 0 || bounds.size.y == 0 || bounds.size.z == 0)
        {
            EditorGUILayout.HelpBox("선택한 Tilemap에 타일이 없습니다.", MessageType.Info);
            return;
        }
        gridCellSize = EditorGUILayout.IntSlider("타일판 확대", gridCellSize, 32, 80);
        if (bounds.size.z > 1) gridLayer = EditorGUILayout.IntSlider("Z 레이어", gridLayer, bounds.zMin, bounds.zMax - 1);
        else gridLayer = bounds.zMin;
        EditorGUILayout.LabelField("몬스터 배치 타일판 · 같은 칸에 다시 찍으면 교체됩니다.", EditorStyles.miniLabel);

        float height = Mathf.Min(420, bounds.size.y * gridCellSize + 42);
        Rect viewport = GUILayoutUtility.GetRect(1, height, GUILayout.ExpandWidth(true));
        Rect gridRect = new Rect(32, 24, bounds.size.x * gridCellSize, bounds.size.y * gridCellSize);
        Rect contentRect = new Rect(0, 0, gridRect.xMax + 4, gridRect.yMax + 4);
        int control = GUIUtility.GetControlID(FocusType.Passive);
        Event e = Event.current;
        // 뷰포트 밖의 클릭으로 스크롤 안의 보이지 않는 칸을 수정하지 않는다.
        bool pointerInViewport = e != null && viewport.Contains(e.mousePosition);
        gridScroll = GUI.BeginScrollView(viewport, gridScroll, contentRect);

        var spawns = new Dictionary<Vector3Int, MonsterSpawnData>();
        if (wave.monsters != null)
            foreach (MonsterSpawnData spawn in wave.monsters) if (spawn != null) spawns[spawn.position] = spawn;

        // 보이는 범위만 그려 넓은/희소 Tilemap에서도 창이 멈추지 않는다.
        int firstColumn = Mathf.Clamp(Mathf.FloorToInt((gridScroll.x - gridRect.x) / gridCellSize), 0, bounds.size.x - 1);
        int lastColumn = Mathf.Clamp(Mathf.CeilToInt((gridScroll.x + viewport.width - gridRect.x) / gridCellSize), 0, bounds.size.x - 1);
        int firstRow = Mathf.Clamp(Mathf.FloorToInt((gridScroll.y - gridRect.y) / gridCellSize), 0, bounds.size.y - 1);
        int lastRow = Mathf.Clamp(Mathf.CeilToInt((gridScroll.y + viewport.height - gridRect.y) / gridCellSize), 0, bounds.size.y - 1);
        for (int column = firstColumn; column <= lastColumn; column++)
            GUI.Label(new Rect(gridRect.x + column * gridCellSize, 2, gridCellSize, 20), (bounds.xMin + column).ToString(), EditorStyles.miniLabel);
        for (int row = firstRow; row <= lastRow; row++)
        {
            int y = bounds.yMax - 1 - row;
            GUI.Label(new Rect(0, gridRect.y + row * gridCellSize, 30, gridCellSize), y.ToString(), EditorStyles.miniLabel);
            for (int column = firstColumn; column <= lastColumn; column++)
            {
                Vector3Int cell = new Vector3Int(bounds.xMin + column, y, gridLayer);
                Rect rect = new Rect(gridRect.x + column * gridCellSize, gridRect.y + row * gridCellSize, gridCellSize - 1, gridCellSize - 1);
                bool hasTile = tilemap.HasTile(cell);
                EditorGUI.DrawRect(rect, hasTile ? new Color(.18f, .22f, .25f) : new Color(.08f, .08f, .08f));
                if (!hasTile) continue;
                DrawTileSprite(rect, tilemap.GetSprite(cell));
                if (spawns.TryGetValue(cell, out MonsterSpawnData spawn))
                {
                    EditorGUI.DrawRect(rect, new Color(1, .15f, .18f, .5f));
                    MonsterData data = FindMonsterData(spawn.monsterID);
                    Texture2D thumbnail = MonsterThumbnail(data);
                    if (thumbnail != null) GUI.DrawTexture(rect, thumbnail, ScaleMode.ScaleToFit);
                    GUI.Label(rect, new GUIContent(spawn.monsterID.ToString(), $"{(data != null ? MonsterLabel(data) : "미등록 몬스터")} · {cell}"), EditorStyles.whiteMiniLabel);
                }
                else GUI.Label(rect, new GUIContent("", cell.ToString()));
            }
        }

        if (GUI.enabled && pointerInViewport && e != null && !e.alt && (e.type == EventType.MouseDown || e.type == EventType.MouseDrag)
            && (e.button == 0 || e.button == 1) && (e.type == EventType.MouseDown || GUIUtility.hotControl == control)
            && TryGetGridCell(e.mousePosition, gridRect, bounds, gridLayer, gridCellSize, out Vector3Int target) && tilemap.HasTile(target))
        {
            if (e.type == EventType.MouseDown)
            {
                GUIUtility.hotControl = control;
                BeginPaintStroke();
            }
            if (e.button == 1) EraseCell(target);
            else PaintCell(target);
            e.Use();
            RefreshViews();
        }
        if (e != null && e.type == EventType.MouseUp && GUIUtility.hotControl == control)
        {
            GUIUtility.hotControl = 0;
            EndPaintStroke();
            e.Use();
        }
        GUI.EndScrollView();
    }

    private static bool TryGetGridCell(Vector2 point, Rect grid, BoundsInt bounds, int layer, int cellSize, out Vector3Int cell)
    {
        cell = default;
        if (cellSize <= 0 || !grid.Contains(point)) return false;
        cell = new Vector3Int(bounds.xMin + Mathf.FloorToInt((point.x - grid.x) / cellSize),
            bounds.yMax - 1 - Mathf.FloorToInt((point.y - grid.y) / cellSize), layer);
        return bounds.Contains(cell);
    }

    private static void DrawTileSprite(Rect rect, Sprite sprite)
    {
        if (sprite == null || sprite.texture == null) return;
        Rect source = sprite.rect;
        GUI.DrawTextureWithTexCoords(rect, sprite.texture, new Rect(source.x / sprite.texture.width, source.y / sprite.texture.height,
            source.width / sprite.texture.width, source.height / sprite.texture.height));
    }

    private void BeginPaintStroke()
    {
        Undo.IncrementCurrentGroup();
        paintUndoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Paint Wave Monsters");
    }

    private void EndPaintStroke()
    {
        if (paintUndoGroup >= 0) Undo.CollapseUndoOperations(paintUndoGroup);
        paintUndoGroup = -1;
    }
}
#endif
