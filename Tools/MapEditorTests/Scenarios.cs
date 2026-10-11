using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

internal static class Scenarios
{
    private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    private static FieldInfo Field(object target, string field)
    {
        for (Type type = target.GetType(); type != null; type = type.BaseType)
        {
            FieldInfo found = type.GetField(field, Private | BindingFlags.Public | BindingFlags.DeclaredOnly);
            if (found != null) return found;
        }
        throw new MissingFieldException(target.GetType().Name, field);
    }
    private static void Set(object target, string field, object value) => Field(target, field).SetValue(target, value);
    private static T Get<T>(object target, string field) => (T)Field(target, field).GetValue(target);
    private static object Call(object target, string method, params object[] args) => target.GetType().GetMethod(method, Private).Invoke(target, args);
    private static WaveData Current(StageMapEditor editor) => (WaveData)typeof(StageMapEditor).GetProperty("CurrentWave", Private).GetValue(editor);
    private static void Mode(StageMapEditor editor, string mode) => Set(editor, "mode", Enum.Parse(typeof(StageMapEditor).GetNestedType("EditMode", BindingFlags.NonPublic), mode));
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static void RegisterButton(WaveSetter setter, int clearId)
    {
        StageMapEntry map = GameManager.Instance.Wave.Maps[0];
        map.stageButtons.Add(new StageMapButton { button = setter, clearId = clearId });
    }

    private static void SetStageWaves(WaveManager manager, int index, WaveData[] waves)
    {
        while (manager.Maps.Count <= index) manager.EditorAddMap(new GameObject("map " + manager.Maps.Count).AddComponent<Tilemap>());
        manager.Maps[index].waves = waves;
    }

    private sealed class Fixture
    {
        public StageMapEditor editor;
        public WaveManager manager;
        public WaveLoader loader;
        public StageMapEntry entry => manager.GetMap(map);
        public Tilemap map;
        public WaveData first, second;
        public StageMapData terrain;
        public MonsterData a, b;
    }

    private static MonsterData Monster(int id, string name)
    {
        var prefab = new GameObject(name); prefab.AddComponent<MonsterBase>();
        return new MonsterData { id = id, monsterName = name, prefab = prefab };
    }

    private static Fixture NewFixture()
    {
        GameObject.All.Clear(); AssetDatabase.Assets.Clear(); AssetDatabase.Saved.Clear(); Undo.Records.Clear();
        EditorUtility.Dirty.Clear(); EditorUtility.NextAssetPath = null; EditorSceneManager.Dirty.Clear(); EditorSceneManager.Saved.Clear();
        EditorApplication.isPlayingOrWillChangePlaymode = false; MonsterBase._monsters.Clear(); GUIUtility.hotControl = 0; Event.current = null;
        ProgressManager.ResetForTest(); UIManager.LastPopUpMessage = null;
        EventSystem.current = null;
        var f = new Fixture { editor = new StageMapEditor(), first = new WaveData { name = "first" }, second = new WaveData { name = "second" }, terrain = new StageMapData() };
        f.manager = new GameObject("manager").AddComponent<WaveManager>();
        var root = new GameObject("inactive map"); root.activeSelf = false; root.AddComponent<PlacementController>();
        f.loader = root.AddComponent<WaveLoader>();
        var tiles = new GameObject("tilemap") { parent = root };
        f.map = tiles.AddComponent<Tilemap>(); f.manager.EditorAddMap(f.map);
        SetStageWaves(f.manager, 0, new[] { f.first, f.second });
        SetStageWaves(f.manager, 1, Array.Empty<WaveData>());
        f.entry.mapData = f.terrain;
        for (int x = -2; x <= 0; x++) for (int y = -1; y <= 1; y++) f.map.Tiles.Add(new Vector3Int(x, y));
        f.a = Monster(11, "A"); f.b = Monster(22, "B");
        Set(f.entry, "monsterDatas", new List<MonsterData> { f.a, f.b });
        Set(f.editor, "tilemap", f.map); Set(f.editor, "selectedMonster", f.a);
        GameManager.Instance = new GameObject("game manager").AddComponent<GameManager>(); GameManager.Instance.Wave = f.manager;
        BattleManager.Instance = new GameObject("battle").AddComponent<BattleManager>();
        PlacementManager.Instance = new GameObject("placement").AddComponent<PlacementManager>(); PlacementManager.Instance.tilemap = f.map;
        f.manager.SelectMap(f.map);
        return f;
    }

    private static void EditModeReadsSerializedWaves()
    {
        Fixture f = NewFixture();
        Check(f.manager.Maps.Count == 2 && Current(f.editor) == f.first, "editor did not read its registered map");
        Check(f.manager.GetMap((Tilemap)null) == null && f.manager.GetMap(new GameObject("unregistered").AddComponent<Tilemap>()) == null, "unregistered map resolved to another map");
        f.entry.waves = new[] { f.second };
        Check(Current(f.editor) == f.second, "editor ignored saved map waves");
    }

    private static void PaintReplacesOneCellAndIsolatesWaves()
    {
        Fixture f = NewFixture(); var cell = new Vector3Int(-2, 1);
        Check((bool)Call(f.editor, "PaintCell", cell), "valid paint failed");
        int undoCount = Undo.Records.Count;
        Check(!(bool)Call(f.editor, "PaintCell", cell) && Undo.Records.Count == undoCount, "same paint recorded a duplicate");
        Set(f.editor, "selectedMonster", f.b); Call(f.editor, "PaintCell", cell);
        Check(f.first.monsters.Count == 1 && f.first.monsters[0].monsterID == 22 && f.first.monsters[0].position == cell, "cell replacement failed");
        Set(f.editor, "selectedWaveIndex", 1); Call(f.editor, "PaintCell", cell);
        Check(f.second.monsters.Count == 1 && f.first.monsters.Count == 1, "another wave was changed");
        Check(!(bool)Call(f.editor, "PaintCell", new Vector3Int(999, 0)), "paint outside tilemap accepted");
    }

    private static void ErasingRespectsTerrainLayers()
    {
        Fixture f = NewFixture(); var cell = new Vector3Int(0, 0); Call(f.editor, "PaintCell", cell);
        f.terrain.obstacles.Add(new ObstacleSpawnData { position = cell }); f.terrain.fieldEffects.Add(new FieldEffectSpawnData { position = cell });
        Call(f.editor, "EraseCell", cell);
        Check(f.first.monsters.Count == 0 && f.terrain.obstacles.Count == 1 && f.terrain.fieldEffects.Count == 1, "monster erase removed terrain");
        Mode(f.editor, "Obstacle"); Call(f.editor, "EraseCell", cell);
        Check(f.terrain.obstacles.Count == 0 && f.terrain.fieldEffects.Count == 1, "obstacle erase removed field effect");
        Mode(f.editor, "Monster"); Call(f.editor, "PaintCell", cell);
        Mode(f.editor, "Erase"); Call(f.editor, "EraseCell", cell);
        Check(f.first.monsters.Count == 0 && f.terrain.fieldEffects.Count == 0, "all-layer erase failed");
    }

    private static void ListAdditionRemovalAndUndo()
    {
        Fixture f = NewFixture(); var otherStage = new[] { new WaveData() }; SetStageWaves(f.manager, 1, otherStage);
        Call(f.editor, "AddWaveSlot", new object[] { null });
        Check(f.manager.Maps[0].waves.Length == 3 && Current(f.editor) == null, "new slot copied the previous reference");
        Check(f.manager.Maps[1].waves.SequenceEqual(otherStage), "editing one stage changed another");
        Call(f.editor, "RemoveWaveSlot", 0);
        Check(f.manager.Maps[0].waves.Length == 2 && f.manager.Maps[0].waves[0] == f.second, "non-null slot was cleared instead of removed");
        Check(Get<int>(f.editor, "selectedWaveIndex") == 1, "selection was not adjusted");
        Undo.PerformUndo(); Check(f.manager.Maps[0].waves.Length == 3 && f.manager.Maps[0].waves[0] == f.first, "list undo failed");
        Check(EditorSceneManager.Dirty.Count > 0, "list changes did not mark scene dirty");
    }

    private static void ReorderingPreservesSelectedAsset()
    {
        for (int selected = 0; selected < 3; selected++)
            for (int from = 0; from < 3; from++) for (int to = 0; to < 3; to++)
            {
                Fixture f = NewFixture(); var third = new WaveData(); var original = new[] { f.first, f.second, third };
                SetStageWaves(f.manager, 0, original); Set(f.editor, "selectedWaveIndex", selected);
                WaveData active = Current(f.editor); Call(f.editor, "MoveWaveSlot", from, to);
                Check(Current(f.editor) == active, $"selection changed for move {from}->{to} at {selected}");
                Check(f.manager.Maps[0].waves[to] == original[from], "wave execution order did not change");
            }
    }

    private static void InvalidMonsterDataCannotBePainted()
    {
        Fixture f = NewFixture(); var cell = new Vector3Int(0, 0);
        Set(f.editor, "selectedMonster", Monster(99, "unregistered"));
        Check(!(bool)Call(f.editor, "PaintCell", cell), "unregistered monster painted");
        Set(f.editor, "selectedMonster", f.a); Set(f.entry, "monsterDatas", new List<MonsterData> { f.a, Monster(11, "duplicate") });
        Check(!(bool)Call(f.editor, "PaintCell", cell), "duplicate ID painted");
        Set(f.entry, "monsterDatas", new List<MonsterData> { f.a }); f.a.prefab = new GameObject("no MonsterBase");
        Check(!(bool)Call(f.editor, "PaintCell", cell), "invalid prefab painted");
        Check(f.first.monsters.Count == 0, "rejected paint changed data");
    }

    private static void SaveTargetsSelectedAssetAndRejectsInvalidSpawns()
    {
        Fixture f = NewFixture(); Call(f.editor, "PaintCell", new Vector3Int(0, 0)); EditorUtility.SetDirty(f.second);
        Call(f.editor, "SaveCurrentWave");
        Check(AssetDatabase.Saved.SequenceEqual(new[] { f.first }) && !EditorUtility.IsDirty(f.first) && EditorUtility.IsDirty(f.second), "save affected unrelated wave");
        f.first.monsters.Add(new MonsterSpawnData { monsterID = 11, position = new Vector3Int(999, 0) });
        Call(f.editor, "SaveCurrentWave"); Check(AssetDatabase.Saved.Count == 1, "invalid position was saved");
        f.first.monsters.Clear(); f.first.monsters.Add(new MonsterSpawnData { monsterID = 123, position = new Vector3Int(0, 0) });
        Call(f.editor, "SaveCurrentWave"); Check(AssetDatabase.Saved.Count == 1, "unknown ID was saved");
        f.first.monsters = null; Call(f.editor, "SaveCurrentWave");
        Check(f.first.monsters != null && f.first.monsters.Count == 0, "empty legacy wave was not initialized");
    }

    private static void NewWaveCreationAndCancellation()
    {
        Fixture f = NewFixture(); Call(f.editor, "CreateWaveAsset", -1);
        Check(f.manager.Maps[0].waves.Length == 2 && AssetDatabase.Assets.Count == 0, "cancel created a wave or slot");
        EditorUtility.NextAssetPath = "Assets/TestWave.asset"; Call(f.editor, "CreateWaveAsset", -1);
        WaveData created = Current(f.editor);
        Check(created != f.first && created != f.second && created.monsters != null && f.manager.Maps[0].waves.Length == 3, "new wave not created and linked");
        Check(AssetDatabase.Assets["Assets/TestWave.asset"] == created, "new wave was not stored at selected path");
        Call(f.editor, "RemoveWaveSlot", 2); Check(AssetDatabase.Assets.ContainsKey("Assets/TestWave.asset"), "removing a slot deleted its asset");
        Call(f.editor, "AddWaveSlot", new object[] { null }); EditorUtility.NextAssetPath = "Assets/SlotWave.asset"; Call(f.editor, "CreateWaveAsset", 2);
        Check(f.manager.Maps[0].waves.Length == 3 && Current(f.editor) == AssetDatabase.Assets["Assets/SlotWave.asset"], "empty slot creation appended extra slot");
    }

    private static void WaveListSaveIncludesTilemapScene()
    {
        Fixture f = NewFixture(); f.map.gameObject.scene = new UnityEngine.SceneManagement.Scene(2); Call(f.editor, "SaveWaveList");
        Check(EditorSceneManager.Saved.Select(scene => scene.id).SequenceEqual(new[] { 1, 2 }), "scene containing the editor configuration was not saved");
        EditorSceneManager.Saved.Clear(); f.map.gameObject.scene = f.manager.gameObject.scene; Call(f.editor, "SaveWaveList");
        Check(EditorSceneManager.Saved.Count == 1, "same scene saved twice");
    }

    private static void PaintedWaveLoadsThroughRealWaveLoader()
    {
        Fixture f = NewFixture(); Call(f.editor, "PaintCell", new Vector3Int(-2, 1));
        Set(f.editor, "selectedMonster", f.b); Call(f.editor, "PaintCell", new Vector3Int(0, -1)); Call(f.editor, "SaveCurrentWave");
        var connected = (IEnumerator)Call(f.manager, "OnConnected", GameManager.Instance); while (connected.MoveNext()) { }
        Check(f.manager.Maps.Select(map => map.waves).ToList()[0] == f.manager.Maps[0].waves, "runtime list differs from editor's serialized list");
        f.manager.currentWave = Current(f.editor); Check(f.loader.LoadWave(), "runtime rejected the editor-authored wave");
        Check(MonsterBase._monsters.Count == 2 && MonsterBase._monsters[0].name == "A clone" && MonsterBase._monsters[1].name == "B clone", "runtime instantiated wrong monster IDs");
        Check(MonsterBase._monsters[0].transform.position.x == -1.5f && MonsterBase._monsters[0].transform.position.y == 1.5f, "runtime lost tile coordinates");
    }

    private static void SpawnedMonstersUseDataHPInsteadOfPrefabHP()
    {
        Fixture f = NewFixture();
        f.a.hp = 5;
        f.b.hp = 3;
        MonsterBase prefabA = f.a.prefab.GetComponent<MonsterBase>();
        MonsterBase prefabB = f.b.prefab.GetComponent<MonsterBase>();
        prefabA.MaxHP = prefabA.currentHP = 1;
        prefabB.MaxHP = 99;
        prefabB.currentHP = 77;
        f.first.monsters.Add(new MonsterSpawnData { monsterID = f.a.id, position = new Vector3Int(-2, 1) });
        f.first.monsters.Add(new MonsterSpawnData { monsterID = f.b.id, position = new Vector3Int(0, -1) });
        f.first.monsters.Add(new MonsterSpawnData { monsterID = f.a.id, position = new Vector3Int(-1, 0) });
        f.manager.currentWave = f.first;
        Check(f.loader.LoadWave(), "HP regression wave failed to load");
        Check(MonsterBase._monsters.Count == 3, "wrong HP regression spawn count");
        MonsterBase first = MonsterBase._monsters[0].GetComponent<MonsterBase>();
        MonsterBase second = MonsterBase._monsters[1].GetComponent<MonsterBase>();
        MonsterBase third = MonsterBase._monsters[2].GetComponent<MonsterBase>();
        Check(first.MaxHP == 5 && first.currentHP == 5, "data HP did not replace the 1-HP prefab");
        Check(second.MaxHP == 3 && second.currentHP == 3, "another monster ID inherited stale prefab HP");
        first.currentHP = 2;
        Check(third.MaxHP == 5 && third.currentHP == 5 && f.a.hp == 5, "same-ID spawns shared current HP");
        Check(prefabA.currentHP == 1 && prefabB.currentHP == 77, "spawn initialization changed a prefab");

        f.a.hp = 8;
        f.second.monsters.Add(new MonsterSpawnData { monsterID = f.a.id, position = new Vector3Int(0, 0) });
        f.manager.currentWave = f.second;
        Check(f.loader.LoadWave(), "subsequent HP regression wave failed to load");
        MonsterBase next = MonsterBase._monsters[3].GetComponent<MonsterBase>();
        Check(next.MaxHP == 8 && next.currentHP == 8, "new spawn did not use the updated data HP");
        Check(first.currentHP == 2 && third.currentHP == 5, "new spawn reset an existing monster's HP");
    }

    private static void GridCoordinatesAndDragPainting()
    {
        Fixture f = NewFixture(); MethodInfo hit = typeof(StageMapEditor).GetMethod("TryGetGridCell", BindingFlags.Static | BindingFlags.NonPublic);
        var rect = new Rect(32, 24, 144, 144);
        object[] args = { new Vector2(33, 25), rect, f.map.cellBounds, 0, 48, null };
        Check((bool)hit.Invoke(null, args) && (Vector3Int)args[5] == new Vector3Int(-2, 1), "top-left grid coordinates wrong");
        args[0] = new Vector2(175, 167); Check((bool)hit.Invoke(null, args) && (Vector3Int)args[5] == new Vector3Int(0, -1), "bottom-right grid coordinates wrong");
        args[0] = new Vector2(176, 168); Check(!(bool)hit.Invoke(null, args), "outer boundary accepted");
        Event.current = new Event { type = EventType.MouseDown, button = 0, mousePosition = new Vector2(33, 25) }; Call(f.editor, "DrawMonsterGrid", f.first);
        Check(f.first.monsters.Count == 1 && GUIUtility.hotControl == 42, "grid click did not paint/capture");
        Event.current = new Event { type = EventType.MouseDrag, button = 0, mousePosition = new Vector2(129, 25) }; Call(f.editor, "DrawMonsterGrid", f.first);
        Check(f.first.monsters.Count == 2, "drag did not paint next tile");
        Event.current = new Event { type = EventType.MouseUp, button = 0 }; Call(f.editor, "DrawMonsterGrid", f.first);
        Check(GUIUtility.hotControl == 0 && Get<int>(f.editor, "paintUndoGroup") == -1, "drag capture not released");
        Event.current = new Event { type = EventType.MouseDown, button = 1, mousePosition = new Vector2(33, 25) }; Call(f.editor, "DrawMonsterGrid", f.first);
        Check(f.first.monsters.Count == 1 && f.first.monsters[0].position == new Vector3Int(0, 1), "right-click removed wrong tile");
        Event.current = new Event { type = EventType.MouseUp, button = 1 }; Call(f.editor, "DrawMonsterGrid", f.first);
        f.first.monsters.Clear(); f.map.cellBounds = new BoundsInt(-2, -10, 0, 3, 20, 1);
        Event.current = new Event { type = EventType.MouseDown, button = 0, mousePosition = new Vector2(33, 480) }; Call(f.editor, "DrawMonsterGrid", f.first);
        Check(f.first.monsters.Count == 0 && GUIUtility.hotControl == 0, "click below viewport painted an invisible cell");
    }

    private static void SwitchingTilemapsResolvesNewStageAndPalette()
    {
        Fixture f = NewFixture(); Set(f.editor, "selectedWaveIndex", 1);
        var nextRoot = new GameObject("next map"); nextRoot.AddComponent<PlacementController>();
        nextRoot.AddComponent<WaveLoader>(); var nextMonster = Monster(33, "C");
        var nextMap = new GameObject("next tilemap") { parent = nextRoot }.AddComponent<Tilemap>();
        nextMap.Tiles.Add(new Vector3Int(0, 0)); f.manager.EditorAddMap(nextMap);
        var nextWave = new WaveData(); StageMapEntry entry = f.manager.GetMap(nextMap);
        entry.waves = new[] { nextWave }; entry.monsterDatas.Add(nextMonster);
        Set(f.editor, "tilemap", nextMap); Call(f.editor, "TryAutoFindStageMapData");
        Check(Current(f.editor) == nextWave && entry.mapData == null, "previous map data leaked into next map");
        Check((bool)Call(f.editor, "PaintCell", new Vector3Int(0, 0)) && nextWave.monsters[0].monsterID == 33, "next map used previous monster catalog");
        Check(f.first.monsters.Count == 0 && f.second.monsters.Count == 0, "switching maps changed prior waves");
    }

    private static void PlayModeGuardsAndPaintUndo()
    {
        Fixture f = NewFixture(); var cell = new Vector3Int(0, 0); Call(f.editor, "PaintCell", cell); Undo.PerformUndo();
        Check(f.first.monsters.Count == 0, "paint undo failed");
        EditorApplication.isPlayingOrWillChangePlaymode = true;
        Check(!(bool)Call(f.editor, "PaintCell", cell) && !(bool)Call(f.editor, "EraseCell", cell), "play mode changed authoring data");
    }

    private static void OnlyRegisteredMapsAreUsed()
    {
        Fixture f = NewFixture();
        var unknown = new GameObject("same name") { parent = f.map.gameObject.parent }.AddComponent<Tilemap>();
        Check(!f.manager.SelectMap(unknown) && f.manager.selectedWaves == null, "unregistered map fell back to another map");
        var setter = new GameObject("unregistered button").AddComponent<WaveSetter>();
        Get<UnityEngine.Events.UnityEvent>(setter, "onStageEntered").AddListener(() => TileMapManager.SelectMap(f.map));
        setter.SelectSkillByIndex();
        Check(!BattleManager.Instance.IsBattleActive && f.manager.selectedWaves == null, "button entered via an event fallback");
        Check(new GameObject("fresh manager").AddComponent<WaveManager>().Maps.Count == 0, "new manager created legacy stages");
    }

    private static void ThirtyStagesCanBeAuthoredAndLoaded()
    {
        Fixture f = NewFixture();
        while (f.manager.Maps.Count < 30)
        {
            Set(f.editor, "tilemap", new GameObject("tilemap").AddComponent<Tilemap>());
            Check((bool)Call(f.editor, "RegisterCurrentMap"), "map registration failed");
        }
        Tilemap last = f.manager.Maps[29].tilemap; last.Tiles.Add(new Vector3Int(0, 0));
        f.manager.Maps[29].monsterDatas.Add(f.a);
        var wave = new WaveData(); Call(f.editor, "AddWaveSlot", wave); Call(f.editor, "PaintCell", new Vector3Int(0, 0));
        Check(Current(f.editor) == wave && f.manager.Maps[28].waves.Length == 0, "map wave list shared another map");
        var setter = new GameObject("stage button").AddComponent<WaveSetter>();
        f.manager.Maps[29].stageButtons.Add(new StageMapButton { button = setter, clearId = 100 });
        setter.SelectSkillByIndex();
        Check(BattleManager.Instance.IsBattleActive && BattleManager.Instance.LastStageId == 100, "button clear ID was not read from map editor data");
        Check(f.manager.currentWave == wave && MonsterBase._monsters.Count == 1 && PlacementManager.Instance.tilemap == last, "map 30 did not load the authored wave");
    }

    private static void DuplicateConnectionsAreRejected()
    {
        Fixture f = NewFixture(); var setter = new GameObject("button").AddComponent<WaveSetter>(); RegisterButton(setter, 17);
        f.manager.Maps[1].stageButtons.Add(new StageMapButton { button = setter });
        Check(f.manager.GetMap(setter) == null, "duplicate button chose a map arbitrarily");
        setter.SelectSkillByIndex(); Check(!BattleManager.Instance.IsBattleActive, "duplicate button entered battle");
        f.manager.Maps[1].stageButtons.Clear(); f.manager.Maps[1].tilemap = f.map;
        Check(f.manager.GetMap(f.map) == null && f.manager.GetMap(setter) == null, "duplicate tilemap chose a record arbitrarily");
    }

    private static void MapRemovalPreservesOtherConnectionsAndClearIdsWithUndo()
    {
        Fixture f = NewFixture(); SetStageWaves(f.manager, 2, new[] { new WaveData() });
        StageMapEntry later = f.manager.Maps[2]; Tilemap removed = f.manager.Maps[1].tilemap;
        later.stageId = 77; var button = new GameObject("later button").AddComponent<WaveSetter>();
        later.stageButtons.Add(new StageMapButton { button = button, clearId = 99 });
        Set(f.editor, "tilemap", removed); Check((bool)Call(f.editor, "RemoveCurrentMap"), "map removal failed");
        Check(f.manager.Maps.Count == 2 && f.manager.GetMap(button).tilemap == later.tilemap && button.StageId == 99, "removal shifted a connection or clear ID");
        Undo.PerformUndo();
        Check(f.manager.Maps.Count == 3 && f.manager.GetMap(removed) != null && button.StageId == 99, "undo did not restore the removed map");
    }

    private static void RemovedMapsRejectBattleEntryAndCanBeRegisteredAgain()
    {
        Fixture f = NewFixture(); var setter = new GameObject("button").AddComponent<WaveSetter>(); RegisterButton(setter, 0);
        Check((bool)Call(f.editor, "RemoveCurrentMap"), "map removal failed"); f.manager.ResetSelection();
        setter.SelectSkillByIndex(); Check(!BattleManager.Instance.IsBattleActive && f.manager.selectedWaves == null, "removed map entered another map's waves");
        Check((bool)Call(f.editor, "RegisterCurrentMap"), "map could not be registered again");
        SelectMapOnEntry(setter, f.map); Call(f.editor, "AddWaveSlot", new WaveData());
        setter.SelectSkillByIndex(); Check(BattleManager.Instance.IsBattleActive, "registered map could not enter battle");
    }

    private static void MapRegistrationGuardsPlayModeAndNullTargets()
    {
        Fixture f = NewFixture(); int count = f.manager.Maps.Count;
        EditorApplication.isPlayingOrWillChangePlaymode = true;
        Check(!(bool)Call(f.editor, "RegisterCurrentMap") && !(bool)Call(f.editor, "RemoveCurrentMap") && f.manager.Maps.Count == count, "play mode changed map records");
        EditorApplication.isPlayingOrWillChangePlaymode = false; Set(f.editor, "tilemap", null);
        Check(!(bool)Call(f.editor, "RegisterCurrentMap") && !(bool)Call(f.editor, "RemoveCurrentMap"), "null tilemap accepted");
    }

    private static StageButtonImageController StageButton(WaveSetter setter, int required)
    {
        if (setter.GetComponent<Button>() == null) setter.gameObject.AddComponent<Button>();
        setter.gameObject.AddComponent<Image>().sprite = new Sprite { name = "uncleared" };
        StageButtonImageController controller = setter.gameObject.AddComponent<StageButtonImageController>();
        Set(controller, "requredProgress", required);
        Set(controller, "lockedSprite", new Sprite { name = "locked" });
        Set(controller, "clearedSprite", new Sprite { name = "cleared" });
        Call(controller, "Awake"); Call(controller, "OnEnable");
        return controller;
    }

    private static void RequiredProgressControlsButtonAndEntryAtTheBoundary()
    {
        NewFixture();
        var setter = new GameObject("stage 30").AddComponent<WaveSetter>(); RegisterButton(setter, 29);
        var legacy = new GameObject("legacy progress").AddComponent<tempcontroller>(); legacy.allow = false;
        StageButtonImageController controller = StageButton(setter, 100);
        Button button = setter.GetComponent<Button>(); Image image = setter.GetComponent<Image>();
        foreach (int progress in new[] { 0, 99, 100, 101, 99 })
        {
            ProgressManager.Progress = progress;
            bool expected = progress >= 100;
            Check(setter.RequiredProgress == 100 && setter.CanEnterStage == expected, "require boundary still depended on tempcontroller or stage index");
            Check(button.interactable == expected, "progress change did not refresh button interaction");
            Check(image.overrideSprite == (expected ? null : Get<Sprite>(controller, "lockedSprite")), "button sprite did not follow entry permission");
            Check(image.color.Equals(expected ? Color.white : new Color(.45f, .45f, .45f, 1)), "button color did not follow entry permission");
        }
        Call(controller, "OnDisable");
    }

    private static void DirectStageEntryChecksRequireWithoutTempcontroller()
    {
        Fixture f = NewFixture(); f.entry.stageId = 29;
        Call(f.editor, "PaintCell", new Vector3Int(0, 0));
        var setter = new GameObject("stage 30 button").AddComponent<WaveSetter>(); RegisterButton(setter, 29);
        StageButtonImageController controller = StageButton(setter, 100);
        f.manager.ResetSelection(); int entered = 0; Get<UnityEngine.Events.UnityEvent>(setter, "onStageEntered").AddListener(() => entered++);
        SelectMapOnEntry(setter, f.map);
        ProgressManager.Progress = 99; setter.SelectSkillByIndex();
        Check(!BattleManager.Instance.IsBattleActive && entered == 0 && f.manager.selectedWaves == null, "direct invocation bypassed require check");
        Check(UIManager.LastPopUpMessage.Contains("100") && UIManager.LastPopUpMessage.Contains("99"), "locked popup did not use button requirement and actual progress");
        ProgressManager.Progress = 100; setter.SelectSkillByIndex();
        Check(BattleManager.Instance.IsBattleActive && BattleManager.Instance.LastStageId == 29 && entered == 1, "stage 30 was still locked by absent tempcontroller or its five-slot array");
        Check(f.manager.currentWave == f.first && MonsterBase._monsters.Count == 1, "unlocked stage did not load its configured wave");
        Call(controller, "OnDisable");
    }

    private static void EachStageButtonKeepsItsOwnRequirement()
    {
        NewFixture();
        var first = new GameObject("first button").AddComponent<WaveSetter>(); RegisterButton(first, 7);
        var second = new GameObject("second button").AddComponent<WaveSetter>(); RegisterButton(second, 7);
        StageButtonImageController firstUI = StageButton(first, 0), secondUI = StageButton(second, 100);
        ProgressManager.Progress = 50;
        Check(first.CanEnterStage && first.GetComponent<Button>().interactable, "zero requirement was not unlocked");
        Check(!second.CanEnterStage && !second.GetComponent<Button>().interactable, "requirements leaked between buttons sharing an index");
        ProgressManager.Progress = 100;
        Check(first.CanEnterStage && second.CanEnterStage && second.GetComponent<Button>().interactable, "equal requirement did not unlock independent button");
        Call(firstUI, "OnDisable"); Call(secondUI, "OnDisable");
    }

    private static void ClearedAppearanceDoesNotBypassRequireAndRefreshesAfterLoad()
    {
        NewFixture();
        var setter = new GameObject("cleared stage").AddComponent<WaveSetter>(); RegisterButton(setter, 12);
        StageButtonImageController controller = StageButton(setter, 100);
        Image image = setter.GetComponent<Image>(); Button button = setter.GetComponent<Button>();
        ProgressManager.ClearedStages.Add(setter.StageId); ProgressManager.Progress = 99;
        Check(!button.interactable && image.overrideSprite == Get<Sprite>(controller, "lockedSprite"), "clear record bypassed requirement");
        ProgressManager.Progress = 100;
        Check(button.interactable && image.overrideSprite == Get<Sprite>(controller, "clearedSprite") && image.color.Equals(new Color(.5f, 1, .7f, 1)), "unlocked clear record did not use cleared appearance");
        ProgressManager.ClearedStages.Clear(); ProgressManager.Progress = 100;
        Check(button.interactable && image.overrideSprite == null && image.sprite == Get<Sprite>(controller, "unclearedSprite"), "new save slot kept another slot's cleared sprite");
        ProgressManager.Progress = 0;
        Check(!button.interactable && image.overrideSprite == Get<Sprite>(controller, "lockedSprite"), "lower-progress slot did not relock button");
        Call(controller, "OnDisable");
    }

    private static void DefaultAndNegativeRequirementsNeedNoLegacyProgressConnection()
    {
        NewFixture();
        var setter = new GameObject("legacy setter without button art").AddComponent<WaveSetter>(); RegisterButton(setter, 29);
        Check(setter.RequiredProgress == 0 && setter.CanEnterStage, "setter without image controller still required a tempcontroller connection");
        Button button = setter.gameObject.AddComponent<Button>();
        StageButtonImageController controller = setter.gameObject.AddComponent<StageButtonImageController>();
        Set(controller, "requredProgress", -10); Call(controller, "Awake"); Call(controller, "OnEnable");
        Check(controller.RequiredProgress == 0 && setter.CanEnterStage && button.interactable, "negative requirement or absent image broke entry permission");
        Set(controller, "requredProgress", 5); controller.Refresh();
        Check(!setter.CanEnterStage && !button.interactable, "button without an image bypassed positive requirement");
        Call(controller, "OnDisable");
    }

    private static void SelectMapOnEntry(WaveSetter setter, Tilemap map)
    {
        WaveManager manager = GameManager.Instance.Wave;
        foreach (StageMapEntry entry in manager.Maps)
            entry.stageButtons.RemoveAll(item => item.button == setter);
        manager.GetMap(map).stageButtons.Add(new StageMapButton { button = setter });
    }

    private static StageButtonImageController AnimatedStageButton(out WaveSetter setter, out Animator animator)
    {
        setter = new GameObject("animated stage").AddComponent<WaveSetter>();
        Button button = setter.gameObject.AddComponent<Button>(); button.transition = Selectable.Transition.Animation;
        button.animationTriggers.highlightedTrigger = "HighLighted";
        animator = setter.gameObject.AddComponent<Animator>();
        animator.parameters = new[] {
            new AnimatorControllerParameter { name = "HighLighted", type = AnimatorControllerParameterType.Trigger },
            new AnimatorControllerParameter { name = "Normal", type = AnimatorControllerParameterType.Trigger } };
        new GameObject("Circle") { parent = setter.gameObject };
        new GameObject("Box") { parent = setter.gameObject };
        return StageButton(setter, 0);
    }

    private static void ShowHover(WaveSetter setter, Animator animator)
    {
        setter.transform.Find("Circle").gameObject.SetActive(true);
        setter.transform.Find("Box").gameObject.SetActive(true);
        animator.SetTrigger("HighLighted");
    }

    private static bool HoverCleared(WaveSetter setter) => setter.transform.Find("Circle").gameObject.activeSelf
        && !setter.transform.Find("Box").gameObject.activeSelf;

    private static void ClickedButtonClearsHoverAndSelection()
    {
        NewFixture();
        StageButtonImageController ui = AnimatedStageButton(out WaveSetter setter, out Animator animator);
        Button button = setter.GetComponent<Button>();
        Check(button.animationTriggers.pressedTrigger == "Normal" && button.animationTriggers.selectedTrigger == "Normal"
            && button.animationTriggers.disabledTrigger == "Normal", "unsupported click/selection triggers can leave Hover running");
        EventSystem.current = new EventSystem { currentSelectedGameObject = setter.gameObject };
        ShowHover(setter, animator);
        ui.OnPointerExit(new PointerEventData());
        Check(EventSystem.current.currentSelectedGameObject == null && HoverCleared(setter), "clicked button stayed selected, kept Hover box, or hid its normal icon after pointer exit");
        Check(animator.LastAppliedTrigger == "Normal" && animator.PendingTriggers.Count == 0, "pending Hover trigger restarted after cleanup");
        EventSystem.current.currentSelectedGameObject = setter.gameObject;
        ui.OnPointerEnter(new PointerEventData());
        Check(EventSystem.current.currentSelectedGameObject == null, "old selection prevented Hover on pointer re-entry");
        var other = new GameObject("new selected control"); EventSystem.current.currentSelectedGameObject = other;
        ui.OnPointerExit(new PointerEventData());
        Check(EventSystem.current.currentSelectedGameObject == other, "hover cleanup cleared another control's selection");
        Call(ui, "OnDisable");
    }

    private static void LockedAndReenabledButtonsDoNotRestoreHover()
    {
        NewFixture();
        StageButtonImageController ui = AnimatedStageButton(out WaveSetter setter, out Animator animator);
        ShowHover(setter, animator); setter.gameObject.SetActive(false); Call(ui, "OnDisable");
        Check(HoverCleared(setter), "closing the world kept Hover box or hid the normal icon");
        setter.gameObject.SetActive(true); Call(ui, "OnEnable");
        Check(HoverCleared(setter) && animator.LastAppliedTrigger == "Normal", "reopening the world restored the old Hover");
        ShowHover(setter, animator); Set(ui, "requredProgress", 100); ProgressManager.Progress = 0;
        Check(!setter.GetComponent<Button>().interactable && HoverCleared(setter), "locking a hovered button left its effects visible");
        EventSystem.current = new EventSystem { currentSelectedGameObject = setter.gameObject, alreadySelecting = true };
        ui.ClearHover();
        Check(EventSystem.current.currentSelectedGameObject == setter.gameObject && HoverCleared(setter), "cleanup re-entered EventSystem selection");
        Call(ui, "OnDisable");
    }

    private static void ConfigureStageScreens(WaveSetter setter, Tilemap map, GameObject world, GameObject screen, GameObject scenario)
    {
        Set(setter, "worldScreen", world); Set(setter, "scenarioScreen", screen);
        var entered = Get<UnityEngine.Events.UnityEvent>(setter, "onStageEntered");
        entered.AddPersistentListener(screen, nameof(GameObject.SetActive), () => screen.SetActive(true));
        entered.AddPersistentListener(scenario, nameof(GameObject.SetActive), () => scenario.SetActive(true));
        SelectMapOnEntry(setter, map);
        entered.AddListener(() => { map.gameObject.parent.SetActive(true); world.SetActive(false); });
    }

    private static void ReenteringStagesClosesPreviousScenarioAndTilemap()
    {
        Fixture f = NewFixture(); Call(f.editor, "PaintCell", new Vector3Int(0, 0));
        f.second.monsters.Add(new MonsterSpawnData { monsterID = f.b.id, position = new Vector3Int(-1, 0) });
        SetStageWaves(f.manager, 1, new[] { f.second });
        var otherMap = new GameObject("second tilemap") { parent = f.map.gameObject.parent, activeSelf = false }.AddComponent<Tilemap>();
        otherMap.Tiles.UnionWith(f.map.Tiles);
        f.manager.Maps[1].tilemap = otherMap; f.manager.Maps[1].monsterDatas.Add(f.b);
        var mapDecoration = new GameObject("map decoration") { parent = f.map.gameObject.parent };
        var world = new GameObject("world"); var screen = new GameObject("scenario screen") { activeSelf = false };
        var firstScenario = new GameObject("first scenario") { parent = screen, activeSelf = false };
        var secondScenario = new GameObject("second scenario") { parent = screen, activeSelf = false };
        var background = new GameObject("shared background") { parent = screen };
        var first = new GameObject("first stage") { parent = world }.AddComponent<WaveSetter>(); RegisterButton(first, 0);
        var second = new GameObject("second stage") { parent = world }.AddComponent<WaveSetter>(); RegisterButton(second, 1);
        ConfigureStageScreens(first, f.map, world, screen, firstScenario);
        ConfigureStageScreens(second, otherMap, world, screen, secondScenario);
        first.SelectSkillByIndex();
        Check(f.manager.currentWave == f.first && firstScenario.activeInHierarchy && !secondScenario.activeSelf, "first entry did not select its content");
        GameObject firstMonster = MonsterBase._monsters.Single();
        PlacementController.RemoveAllObject(); first.ReturnToWorld();
        Check(!firstScenario.activeSelf && !f.map.gameObject.activeSelf && !firstMonster.activeSelf && world.activeSelf, "leaving battle kept old child content active");
        // 부모가 닫힌 상태에 이전 activeSelf/Tilemap 참조가 남아 있어도 새 입장을 격리한다.
        firstScenario.SetActive(true); f.map.gameObject.SetActive(true); PlacementManager.Instance.tilemap = f.map;
        second.SelectSkillByIndex();
        Check(f.manager.currentWave == f.second && BattleManager.Instance.LastStageId == 1, "entry read the previous map's wave editor configuration");
        Check(secondScenario.activeInHierarchy && !firstScenario.activeSelf, "new scenario re-enabled the previous scenario");
        Check(otherMap.gameObject.activeInHierarchy && !f.map.gameObject.activeSelf && PlacementManager.Instance.OriginMap == otherMap, "new map kept an old tilemap or origin");
        Check(MonsterBase._monsters.Count == 1 && MonsterBase._monsters[0].name == "B clone", "new stage spawned previous stage monsters");
        Check(background.activeSelf && mapDecoration.activeSelf, "stage cleanup disabled shared decorations");
        PlacementController.RemoveAllObject(); second.ReturnToWorld();
        Check(!secondScenario.activeSelf && !otherMap.gameObject.activeSelf && !screen.activeSelf && world.activeSelf, "second return did not close selected content");
    }

    private static void ScreenEventsCannotOverrideEditorMap()
    {
        Fixture f = NewFixture(); Call(f.editor, "PaintCell", new Vector3Int(0, 0));
        Tilemap otherMap = new GameObject("unselected map") { parent = f.map.gameObject.parent }.AddComponent<Tilemap>();
        var setter = new GameObject("button").AddComponent<WaveSetter>(); RegisterButton(setter, 0);
        Get<UnityEngine.Events.UnityEvent>(setter, "onStageEntered").AddListener(() => TileMapManager.SelectMap(otherMap));
        setter.SelectSkillByIndex();
        Check(BattleManager.Instance.IsBattleActive && f.manager.currentWave == f.first && PlacementManager.Instance.tilemap == f.map, "screen event overrode map editor configuration");
        Check(!otherMap.gameObject.activeSelf && f.map.gameObject.activeSelf, "unselected sibling stayed enabled");
    }

    private static void UnconfiguredButtonDoesNotRunEntryEvents()
    {
        Fixture f = NewFixture(); f.manager.ResetSelection();
        var setter = new GameObject("invalid stage").AddComponent<WaveSetter>(); int entered = 0;
        Get<UnityEngine.Events.UnityEvent>(setter, "onStageEntered").AddListener(() => entered++);
        setter.SelectSkillByIndex();
        Check(entered == 0 && !BattleManager.Instance.IsBattleActive && f.manager.selectedWaves == null, "unconfigured button ran screen events or entered battle");
    }

    private static Dialog1 AddDialogue(GameObject scenario, Type type, string[] lines)
    {
        var child = new GameObject("scenario") { parent = scenario };
        var dialogue = (Dialog1)typeof(GameObject).GetMethod(nameof(GameObject.AddComponent)).MakeGenericMethod(type).Invoke(child, null);
        Set(dialogue, "dialogue", lines);
        Set(dialogue, "ScriptText_dialogue", child.AddComponent<TMPro.TextMeshProUGUI>());
        Set(dialogue, "ScriptText_dialoguename", new GameObject("speaker") { parent = scenario }.AddComponent<TMPro.TextMeshProUGUI>());
        Set(dialogue, "ScriptImage_portrait", child.AddComponent<Image>());
        return dialogue;
    }

    private static void DialogueCompletionInvokesExistingSkipOnce()
    {
        foreach (Type type in new[] { typeof(Dialog1), typeof(Dialog2), typeof(Dialog3), typeof(Dialog4), typeof(Dialog5) })
        {
            NewFixture();
            var scenario = new GameObject("story");
            var skip = new GameObject(type == typeof(Dialog1) ? "assigned skip" : "next") { parent = scenario }.AddComponent<Button>();
            bool visible = type != typeof(Dialog2);
            skip.gameObject.SetActive(visible); skip.interactable = false;
            var calls = new List<string>();
            skip.onClick.AddPersistentListener(scenario, "FirstAction", () => calls.Add("first"));
            skip.onClick.AddPersistentListener(scenario, "SecondAction", () => calls.Add("second"));
            skip.onClick.AddListener(() => calls.Add("runtime"));
            Button.ButtonClickedEvent original = skip.onClick;
            Dialog1 dialogue = AddDialogue(scenario, type, new[] { "first", "last" });
            if (type == typeof(Dialog1)) Set(dialogue, "skipButton", skip);
            Set(dialogue, "dialoguename", new[] { "speaker" });
            Set(dialogue, "portraits", Array.Empty<Sprite>());
            Call(dialogue, "OnEnable");
            Check(calls.Count == 0 && Get<TMPro.TextMeshProUGUI>(dialogue, "ScriptText_dialogue").text == "first", type.Name + " invoked skip before dialogue finished");
            Check(skip.gameObject.activeSelf == visible && !skip.interactable && skip.onClick == original, "opening dialogue changed the authored skip button");
            dialogue.OnPointerDown(new PointerEventData());
            Check(calls.Count == 0 && Get<TMPro.TextMeshProUGUI>(dialogue, "ScriptText_dialogue").text == "last", type.Name + " did not advance to the final line before invoking skip");
            Check(Get<TMPro.TextMeshProUGUI>(dialogue, "ScriptText_dialoguename").text == "", "short speaker data must be tolerated");
            dialogue.OnPointerDown(new PointerEventData()); dialogue.OnPointerDown(new PointerEventData());
            Check(calls.SequenceEqual(new[] { "first", "second", "runtime" }), type.Name + " did not invoke all existing skip callbacks in order exactly once");
            Check(skip.onClick == original && skip.gameObject.activeSelf == visible && !skip.interactable, "completion replaced the skip event or changed button visibility");
            Check(Get<TMPro.TextMeshProUGUI>(dialogue, "ScriptText_dialogue").text == "last", "finished dialogue looped");
            Check(UIManager.LastPopUpMessage == null, "dialogue completion still opened a popup");
        }
    }

    private static void DialogueReopensFromStartAndEmptyDataCanExit()
    {
        NewFixture(); var scenario = new GameObject("story");
        var skip = new GameObject("next") { parent = scenario }.AddComponent<Button>();
        int calls = 0; skip.onClick.AddPersistentListener(scenario, "Finish", () => calls++);
        Button.ButtonClickedEvent original = skip.onClick;
        Dialog1 dialogue = AddDialogue(scenario, typeof(Dialog5), new[] { "first", "last" });
        Call(dialogue, "OnEnable"); dialogue.OnPointerDown(new PointerEventData()); dialogue.OnPointerDown(new PointerEventData());
        Check(calls == 1, "first completion did not invoke skip once");
        scenario.SetActive(false); scenario.SetActive(true); Call(dialogue, "OnEnable");
        Check(calls == 1 && Get<TMPro.TextMeshProUGUI>(dialogue, "ScriptText_dialogue").text == "first", "reopening a scenario did not reset dialogue or invoked skip early");
        dialogue.OnPointerDown(new PointerEventData()); dialogue.OnPointerDown(new PointerEventData());
        Check(calls == 2 && skip.onClick == original, "reopened scenario lost or duplicated its skip callbacks");
        foreach (string[] empty in new[] { Array.Empty<string>(), null })
        {
            int before = calls;
            Set(dialogue, "dialogue", empty); Call(dialogue, "OnEnable");
            Check(calls == before && Get<TMPro.TextMeshProUGUI>(dialogue, "ScriptText_dialogue").text == "", "empty dialogue changed screens during activation or indexed missing data");
            dialogue.OnPointerDown(new PointerEventData()); dialogue.OnPointerDown(new PointerEventData());
            Check(calls == before + 1 && skip.onClick == original, "empty dialogue did not invoke its existing skip callbacks once");
        }
    }

    private static void PostBattleScenarioUsesExistingSkipAndConfiguredDestination()
    {
        Fixture f = NewFixture();
        var world = new GameObject("world"); var screen = new GameObject("scenario screen") { activeSelf = false };
        var before = new GameObject("before") { parent = screen, activeSelf = false };
        var after = new GameObject("after") { parent = screen, activeSelf = false };
        var skip = new GameObject("next") { parent = after }.AddComponent<Button>();
        Dialog1 dialogue = AddDialogue(after, typeof(Dialog2), new[] { "after battle" });
        f.entry.postBattleScenario = after;
        var otherMap = new GameObject("other map").AddComponent<Tilemap>();
        f.manager.EditorAddMap(otherMap); StageMapEntry otherEntry = f.manager.GetMap(otherMap);
        var stale = new GameObject("previous post scenario") { parent = screen };
        otherEntry.postBattleScenario = stale;
        var setter = new GameObject("stage") { parent = world }.AddComponent<WaveSetter>(); RegisterButton(setter, 0);
        int completed = 0;
        skip.onClick.AddPersistentListener(after, "CompleteStory", () => completed++);
        skip.onClick.AddPersistentListener(setter, nameof(WaveSetter.ReturnToWorld), setter.ReturnToWorld);
        Button.ButtonClickedEvent original = skip.onClick;
        ConfigureStageScreens(setter, f.map, world, screen, before);
        setter.SelectSkillByIndex();
        Check(!after.activeSelf && !stale.activeSelf, "stage entry left a post scenario visible");
        PlacementController.RemoveAllObject();
        Check(PlacementManager.Instance.tilemap == null, "test did not clear the live map reference");
        Check(setter.ShowPostBattleScenario(), "post scenario was lost during battle cleanup");
        Call(dialogue, "OnEnable");
        Check(after.activeInHierarchy && !before.activeSelf && !stale.activeSelf && !world.activeSelf, "post scenario opened alongside old content or world");
        Check(!f.map.gameObject.activeSelf && !f.map.gameObject.parent.activeSelf && !BattleManager.Instance.IsBattleActive, "post scenario kept battle view or state open");
        Check(completed == 0 && skip.onClick == original && skip.gameObject.activeSelf, "post scenario changed its existing skip button");
        dialogue.OnPointerDown(new PointerEventData()); dialogue.OnPointerDown(new PointerEventData());
        Check(world.activeSelf && !screen.activeSelf && !after.activeSelf && completed == 1, "post completion did not automatically run the configured world return callbacks once");
        Check(skip.onClick == original, "post completion replaced its existing skip event");
        setter.SelectSkillByIndex(); PlacementController.RemoveAllObject();
        Check(setter.ShowPostBattleScenario(), "replayed stage lost its configured post scenario");
        Call(dialogue, "OnEnable"); dialogue.OnPointerDown(new PointerEventData());
        Check(world.activeSelf && completed == 2 && skip.onClick == original, "replayed post completion lost or duplicated authored callbacks");

        var destination = new GameObject("configured destination") { activeSelf = false };
        var configured = new Button.ButtonClickedEvent();
        configured.AddPersistentListener(destination, nameof(GameObject.SetActive), () => destination.SetActive(true));
        configured.AddPersistentListener(after, nameof(GameObject.SetActive), () => after.SetActive(false));
        configured.AddPersistentListener(screen, nameof(GameObject.SetActive), () => screen.SetActive(false));
        skip.onClick = configured;
        setter.SelectSkillByIndex(); PlacementController.RemoveAllObject();
        Check(setter.ShowPostBattleScenario(), "stage lost its post scenario after a skip callback edit");
        Call(dialogue, "OnEnable"); dialogue.OnPointerDown(new PointerEventData());
        Check(destination.activeSelf && !world.activeSelf && !screen.activeSelf && !after.activeSelf && completed == 2,
            "post completion ignored the configured destination and forced a world return");
        Check(skip.onClick == configured, "post scenario replaced newly configured skip callbacks");
    }

    private static void MissingPostBattleScenarioKeepsWorldFallback()
    {
        Fixture f = NewFixture();
        var world = new GameObject("world"); var screen = new GameObject("scenario screen") { activeSelf = false };
        var before = new GameObject("before") { parent = screen, activeSelf = false };
        var setter = new GameObject("stage") { parent = world }.AddComponent<WaveSetter>(); RegisterButton(setter, 0);
        ConfigureStageScreens(setter, f.map, world, screen, before); setter.SelectSkillByIndex();
        PlacementController.RemoveAllObject();
        Check(!setter.ShowPostBattleScenario(), "unconfigured stage claimed a post scenario"); setter.ReturnToWorld();
        Check(world.activeSelf && !screen.activeSelf && !before.activeSelf && !f.map.gameObject.activeSelf, "world fallback left stage content active");
    }

    private static void LargeMonsterPaintEraseAndUndoUseTheWholeBody()
    {
        Fixture f = NewFixture(); f.a.footprintSize = new Vector2Int(2, 2);
        var anchor = new Vector3Int(-2, -1); var corner = new Vector3Int(-1, 0);
        Check((bool)Call(f.editor, "PaintCell", anchor), "2x2 monster could not be painted at a valid anchor");
        Check(f.first.monsters.Count == 1 && f.first.monsters[0].position == anchor, "large monster was serialized as multiple units");
        Check(Call(f.editor, "GetWaveProblem", f.first) == null, "valid footprint failed save validation");
        int undoCount = Undo.Records.Count;
        Check(!(bool)Call(f.editor, "PaintCell", corner) && Undo.Records.Count == undoCount,
            "dragging over the same body moved or duplicated the monster");
        Check((bool)Call(f.editor, "EraseCell", corner) && f.first.monsters.Count == 0,
            "erasing an occupied corner did not remove the whole monster");
        Undo.PerformUndo();
        Check(f.first.monsters.Count == 1 && f.first.monsters[0].position == anchor, "undo did not restore a single large monster");
    }

    private static void LargeMonsterPaintingRejectsEdgesHolesAndOtherUnits()
    {
        Fixture f = NewFixture();
        var small = new Vector3Int(-1, 0);
        Set(f.editor, "selectedMonster", f.b); Call(f.editor, "PaintCell", small);
        Set(f.editor, "selectedMonster", f.a); f.a.footprintSize = new Vector2Int(2, 2);
        int undoCount = Undo.Records.Count;
        Check(!(bool)Call(f.editor, "PaintCell", new Vector3Int(-2, -1)), "overlap with another monster was accepted");
        Check(!(bool)Call(f.editor, "PaintCell", new Vector3Int(0, 1)), "body outside the map was accepted");
        f.map.Tiles.Remove(new Vector3Int(0, 0));
        Check(!(bool)Call(f.editor, "PaintCell", small), "non-anchor hole was accepted while replacing a unit");
        Check(f.first.monsters.Count == 1 && f.first.monsters[0].monsterID == f.b.id && Undo.Records.Count == undoCount,
            "rejected placement deleted an existing monster or recorded undo");
    }

    private static void LargeMonsterSaveValidationChecksEveryCoveredCell()
    {
        Fixture f = NewFixture(); f.a.footprintSize = new Vector2Int(2, 2);
        var anchor = new Vector3Int(-2, -1); var corner = new Vector3Int(-1, 0);
        Call(f.editor, "PaintCell", anchor);
        f.first.monsters.Add(new MonsterSpawnData { monsterID = f.b.id, position = corner });
        Call(f.editor, "SaveCurrentWave"); Check(AssetDatabase.Saved.Count == 0, "overlapping bodies were saved");
        f.first.monsters.RemoveAt(1); f.map.Tiles.Remove(corner);
        Call(f.editor, "SaveCurrentWave"); Check(AssetDatabase.Saved.Count == 0, "body covering a missing tile was saved");
        f.map.Tiles.Add(corner);
        f.terrain.obstacles.Add(new ObstacleSpawnData { position = corner, blocksMovement = true });
        Call(f.editor, "SaveCurrentWave"); Check(AssetDatabase.Saved.Count == 0, "body covering a blocking obstacle was saved");
        f.terrain.obstacles[0].blocksMovement = false;
        Call(f.editor, "SaveCurrentWave"); Check(AssetDatabase.Saved.Count == 1, "nonblocking terrain prevented a valid save");
    }

    private static void WaveLoaderRegistersOneLargeUnitAtEveryBodyCell()
    {
        Fixture f = NewFixture(); f.a.footprintSize = new Vector2Int(2, 2); f.a.hp = 8;
        var anchor = new Vector3Int(-2, -1);
        Call(f.editor, "PaintCell", anchor); f.manager.currentWave = f.first;
        Check(f.loader.LoadWave(), "valid authored 2x2 wave did not load");
        Check(MonsterBase._monsters.Count == 1, "2x2 wave created more than one monster");
        MonsterBase monster = MonsterBase._monsters[0].GetComponent<MonsterBase>();
        foreach (Vector3Int cell in f.a.GetOccupiedCells(anchor))
            Check(PlacementManager.Instance.GetTileData(cell).Character == monster, "a body cell was not registered to the shared unit");
        Check(monster.currentHP == 8 && monster.transform.position.x == -1 && monster.transform.position.y == 0,
            "large unit did not use authored HP or the center of its whole body");
        Check(monster.GetAnchorCell(f.map) == anchor && f.manager.currentWaveIndex == 0,
            "LoadWave changed the stored anchor or advanced the wave before success handling");
        monster.ReleaseOccupancy();
        foreach (Vector3Int cell in f.a.GetOccupiedCells(anchor))
            Check(PlacementManager.Instance.GetTileData(cell).isempty, "large unit cleanup left an occupied tile");
    }

    private static void InvalidLargeWaveCannotPartiallySpawnOrAdvance()
    {
        Fixture f = NewFixture(); f.a.footprintSize = new Vector2Int(2, 2);
        f.first.monsters.Add(new MonsterSpawnData { monsterID = f.b.id, position = new Vector3Int(-2, -1) });
        f.first.monsters.Add(new MonsterSpawnData { monsterID = f.a.id, position = new Vector3Int(0, 0) });
        f.manager.currentWave = f.first; f.entry.waves = new[] { f.first };
        Check(!f.loader.LoadWave() && MonsterBase._monsters.Count == 0, "invalid second body left a partial wave");
        BattleManager.Instance.IsBattleActive = true; f.loader.NextWave();
        Check(f.manager.currentWaveIndex == 0 && MonsterBase._monsters.Count == 0, "failed load advanced the wave or spawned a partial unit");
        f.first.monsters[1].position = new Vector3Int(-2, -1);
        Check(!f.loader.LoadWave() && MonsterBase._monsters.Count == 0, "overlapping spawn rectangles were accepted");
    }

    private static void WaveLoaderRejectsNonAnchorOccupancyBeforeCreatingUnits()
    {
        Fixture f = NewFixture(); f.a.footprintSize = new Vector2Int(2, 2);
        var anchor = new Vector3Int(-2, -1); var corner = new Vector3Int(-1, 0);
        f.first.monsters.Add(new MonsterSpawnData { monsterID = f.a.id, position = anchor }); f.manager.currentWave = f.first;
        TileData blocked = PlacementManager.Instance.GetTileData(corner); blocked.isempty = false;
        Check(!f.loader.LoadWave() && MonsterBase._monsters.Count == 0 && !blocked.isempty,
            "body covering a blocker was spawned or changed the blocker");
        var other = new GameObject("already placed").AddComponent<MonsterBase>(); blocked.Character = other;
        Check(!f.loader.LoadWave() && MonsterBase._monsters.Count == 0 && blocked.Character == other,
            "body covering another unit was spawned or changed that unit");
        blocked.Character = null;
        Check(f.loader.LoadWave() && MonsterBase._monsters.Count == 1, "a corrected footprint could not be loaded");
    }

    private static void WaveLoaderInitializesCellShieldsSeparatelyForEachSpawn()
    {
        Fixture f = NewFixture(); f.a.footprintSize = new Vector2Int(2, 1); f.a.hasShield = true; f.a.hp = 8;
        var first = new Vector3Int(-2, -1); var second = new Vector3Int(-2, 1);
        f.first.monsters.Add(new MonsterSpawnData { monsterID = f.a.id, position = first });
        f.first.monsters.Add(new MonsterSpawnData { monsterID = f.a.id, position = second }); f.manager.currentWave = f.first;
        Check(f.loader.LoadWave() && MonsterBase._monsters.Count == 2, "shielded wave did not create both units");
        MonsterBase a = MonsterBase._monsters[0].GetComponent<MonsterBase>();
        MonsterBase b = MonsterBase._monsters[1].GetComponent<MonsterBase>();
        Check(a.ShieldCount == 2 && b.ShieldCount == 2 && a.HasCellShield(f.map, first) && b.HasCellShield(f.map, second),
            "WaveLoader did not initialize one shield for every authored body cell");
        a.TakeDamageAtCell(f.map, first, 3);
        Check(a.currentHP == 8 && a.ShieldCount == 1 && b.ShieldCount == 2 && f.a.hasShield,
            "a shield hit leaked to HP, shared data, or another spawn");
        a.TakeDamageAtCell(f.map, first, 3);
        Check(a.currentHP == 5 && b.currentHP == 8, "exposed cell did not damage only its own unit");
    }

    private static void CatalogRegistrationControlsRuntimeSpawnsAndRetry()
    {
        Fixture f = NewFixture(); Call(f.editor, "PaintCell", new Vector3Int(0, 0));
        f.entry.waves = new[] { f.first }; f.entry.monsterDatas.Clear(); BattleManager.Instance.IsBattleActive = true;
        f.loader.NextWave();
        Check(f.manager.currentWaveIndex == 0 && MonsterBase._monsters.Count == 0, "missing registration advanced or partially spawned the wave");
        f.entry.monsterDatas.Add(f.a); f.loader.NextWave();
        Check(f.manager.currentWaveIndex == 1 && MonsterBase._monsters.Single().name == "A clone", "registering the missing monster in editor data did not fix loading");
    }

    private static void DuplicateCatalogFailsBeforeSpawningOrAdvancing()
    {
        Fixture f = NewFixture(); Call(f.editor, "PaintCell", new Vector3Int(0, 0));
        f.entry.monsterDatas.Add(Monster(f.a.id, "duplicate")); BattleManager.Instance.IsBattleActive = true;
        f.loader.NextWave();
        Check(f.manager.currentWaveIndex == 0 && MonsterBase._monsters.Count == 0, "ambiguous ID chose a prefab or advanced the wave");
    }

    private static void MapSwitchUsesItsOwnCatalogAndRejectsStaleSelection()
    {
        Fixture f = NewFixture(); var next = new GameObject("next map").AddComponent<Tilemap>(); next.Tiles.Add(new Vector3Int(0, 0));
        f.manager.EditorAddMap(next); StageMapEntry config = f.manager.GetMap(next);
        var wave = new WaveData(); wave.monsters.Add(new MonsterSpawnData { monsterID = f.a.id, position = new Vector3Int(0, 0) });
        config.waves = new[] { wave }; config.monsterDatas.Add(Monster(f.a.id, "next catalog"));
        PlacementManager.Instance.tilemap = next; f.manager.currentWave = wave;
        Check(!f.loader.LoadWave() && MonsterBase._monsters.Count == 0, "stale map selection loaded another map's wave");
        f.manager.SelectMap(next); f.manager.currentWave = f.first;
        Check(!f.loader.LoadWave(), "foreign wave bypassed the selected map's authored list");
        f.manager.currentWave = wave;
        Check(f.loader.LoadWave() && MonsterBase._monsters.Single().name == "next catalog clone", "loader used the prior map's same-ID prefab");
    }

    private static void SharedMapButtonsKeepTheirOwnClearIds()
    {
        Fixture f = NewFixture(); var first = new GameObject("first").AddComponent<WaveSetter>(); var second = new GameObject("second").AddComponent<WaveSetter>();
        RegisterButton(first, 3); RegisterButton(second, 27);
        Check(first.StageId == 3 && second.StageId == 27, "shared map collapsed distinct clear records");
        first.SelectSkillByIndex(); Check(BattleManager.Instance.LastStageId == 3, "first clear ID changed at entry");
        PlacementController.RemoveAllObject(); second.SelectSkillByIndex();
        Check(BattleManager.Instance.LastStageId == 27, "second clear ID changed at entry");
        var fresh = new GameObject("fresh map").AddComponent<Tilemap>(); f.manager.EditorAddMap(fresh);
        Check(f.manager.GetMap(fresh).stageId > 27, "new map reused a button's clear ID");
    }

    private static void EditorMapDataLoadsAtEntryAndCleansUpOnExit()
    {
        Fixture f = NewFixture(); Call(f.editor, "PaintCell", new Vector3Int(-2, 1));
        var blocked = new Vector3Int(0, 0); var passable = new Vector3Int(-2, -1);
        f.terrain.obstacles.Add(new ObstacleSpawnData { prefab = new GameObject("blocking obstacle"), position = blocked });
        f.terrain.obstacles.Add(new ObstacleSpawnData { prefab = new GameObject("decoration"), position = passable, blocksMovement = false });
        f.terrain.fieldEffects.Add(new FieldEffectSpawnData { position = new Vector3Int(-1, 0), effectType = StageFieldEffectType.Ice, value = 2, duration = 3 });
        f.terrain.fieldEffects.Add(new FieldEffectSpawnData { position = new Vector3Int(-1, -1), effectType = StageFieldEffectType.Earth, value = 1, duration = 4 });
        var setter = new GameObject("button").AddComponent<WaveSetter>(); RegisterButton(setter, 0); setter.SelectSkillByIndex();
        Check(BattleManager.Instance.IsBattleActive && !PlacementManager.Instance.GetTileData(blocked).isempty && PlacementManager.Instance.GetTileData(passable).isempty, "editor obstacles did not load with their movement setting");
        Check(BattleManager.Instance.Fields.Applied.Count == 2
            && BattleManager.Instance.Fields.Applied.Any(field => field.type == SkillTileFieldEffectType.Ice)
            && BattleManager.Instance.Fields.Applied.Any(field => field.type == SkillTileFieldEffectType.Earth), "editor ice/earth fields were not passed to battle effects");
        Check((int)StageFieldEffectType.Dark == 5 && (int)StageFieldEffectType.Custom == 6 && (int)StageFieldEffectType.Earth == 7,
            "adding earth changed existing serialized field values");
        var clones = GameObject.All.Where(obj => obj.name == "blocking obstacle clone" || obj.name == "decoration clone").ToArray();
        Check(clones.Length == 2 && clones.All(obj => obj.parent == f.map.gameObject), "map objects loaded under another map");
        PlacementController.RemoveAllObject();
        Check(clones.All(obj => !obj.activeSelf) && f.manager.ActiveMap == null && f.manager.selectedWaves == null, "battle exit kept map objects or wave selection");
    }

    private static void InvalidMapDataDoesNotLeavePartialBattleObjects()
    {
        Fixture f = NewFixture(); var cell = new Vector3Int(0, 0);
        f.terrain.obstacles.Add(new ObstacleSpawnData { prefab = new GameObject("obstacle"), position = cell });
        f.terrain.obstacles.Add(new ObstacleSpawnData { prefab = new GameObject("other obstacle"), position = cell });
        var setter = new GameObject("button").AddComponent<WaveSetter>(); RegisterButton(setter, 0); setter.SelectSkillByIndex();
        Check(!BattleManager.Instance.IsBattleActive && MonsterBase._monsters.Count == 0 && f.manager.ActiveMap == null, "invalid terrain left battle state or spawned monsters");
        Check(!GameObject.All.Any(obj => obj.name == "obstacle clone"), "invalid terrain left a partially loaded obstacle");
    }

    private static void WaveOrdinalsFollowSuccessfulLoadsAndRepeatedAssets()
    {
        Fixture f = NewFixture();
        f.entry.waves = new[] { f.first, f.second, f.first };
        BattleManager.Instance.IsBattleActive = true;
        Check(f.manager.CurrentWaveNumber == 0, "unloaded wave has an ordinal");
        f.loader.NextWave(); Check(f.manager.CurrentWaveNumber == 1, "first empty wave did not display 1");
        f.loader.NextWave(); Check(f.manager.CurrentWaveNumber == 2, "second wave did not display 2");
        f.loader.NextWave(); Check(f.manager.CurrentWaveNumber == 3, "reused wave asset displayed its first position");
        f.manager.ResetSelection(); Check(f.manager.CurrentWaveNumber == 0, "exit kept the previous ordinal");
        f.manager.SelectMap(f.map);
        f.first.monsters.Add(new MonsterSpawnData { monsterID = 99999, position = new Vector3Int(0, 0) });
        f.loader.NextWave(); Check(f.manager.CurrentWaveNumber == 0, "failed spawn advanced the displayed ordinal");
    }

    private static void ExpandedAuthoredCatalogLoadsAndRetainsEveryMonsterId()
    {
        Fixture f = NewFixture();
        f.entry.monsterDatas.Clear();
        foreach (string path in System.IO.Directory.GetFiles("Assets/1.Datas/Original/ScriptableObjects/Enemy", "*.asset"))
        {
            string source = System.IO.File.ReadAllText(path);
            int id = int.Parse(System.Text.RegularExpressions.Regex.Match(source, @"(?m)^  id: (\d+)").Groups[1].Value);
            f.entry.monsterDatas.Add(Monster(id, System.IO.Path.GetFileNameWithoutExtension(path)));
        }
        f.first.monsters.Add(new MonsterSpawnData { monsterID = 0, position = new Vector3Int(-2,1) });
        BattleManager.Instance.BeginBattle(0,null);f.loader.NextWave();
        Check(f.manager.CurrentWaveNumber == 1 && MonsterBase._monsters.Count == 1,
            "expanded authored catalog prevented the first wave from spawning");
        Check(f.entry.monsterDatas.Count == 15 && f.entry.monsterDatas.Select(data => data.id).Distinct().Count() == 15,
            "copied monster assets retained colliding IDs");
        Check(Enumerable.Range(0,14).All(id => f.entry.monsterDatas.Any(data => data.id == id))
            && f.entry.monsterDatas.Any(data => data.id == 1000), "expanded monster IDs were lost or silently replaced");
    }

    public static int Main()
    {
        Action[] tests = { EditModeReadsSerializedWaves, PaintReplacesOneCellAndIsolatesWaves, ErasingRespectsTerrainLayers,
            ListAdditionRemovalAndUndo, ReorderingPreservesSelectedAsset, InvalidMonsterDataCannotBePainted,
            SaveTargetsSelectedAssetAndRejectsInvalidSpawns, NewWaveCreationAndCancellation, WaveListSaveIncludesTilemapScene,
            PaintedWaveLoadsThroughRealWaveLoader, SpawnedMonstersUseDataHPInsteadOfPrefabHP,
            GridCoordinatesAndDragPainting, SwitchingTilemapsResolvesNewStageAndPalette, PlayModeGuardsAndPaintUndo,
            OnlyRegisteredMapsAreUsed, ThirtyStagesCanBeAuthoredAndLoaded,
            DuplicateConnectionsAreRejected,
            MapRemovalPreservesOtherConnectionsAndClearIdsWithUndo, RemovedMapsRejectBattleEntryAndCanBeRegisteredAgain,
            MapRegistrationGuardsPlayModeAndNullTargets,
            RequiredProgressControlsButtonAndEntryAtTheBoundary, DirectStageEntryChecksRequireWithoutTempcontroller,
            EachStageButtonKeepsItsOwnRequirement, ClearedAppearanceDoesNotBypassRequireAndRefreshesAfterLoad,
            DefaultAndNegativeRequirementsNeedNoLegacyProgressConnection,
            ClickedButtonClearsHoverAndSelection, LockedAndReenabledButtonsDoNotRestoreHover,
            ReenteringStagesClosesPreviousScenarioAndTilemap, ScreenEventsCannotOverrideEditorMap,
            UnconfiguredButtonDoesNotRunEntryEvents,
            DialogueCompletionInvokesExistingSkipOnce, DialogueReopensFromStartAndEmptyDataCanExit,
            PostBattleScenarioUsesExistingSkipAndConfiguredDestination, MissingPostBattleScenarioKeepsWorldFallback,
            LargeMonsterPaintEraseAndUndoUseTheWholeBody, LargeMonsterPaintingRejectsEdgesHolesAndOtherUnits,
            LargeMonsterSaveValidationChecksEveryCoveredCell, WaveLoaderRegistersOneLargeUnitAtEveryBodyCell,
            InvalidLargeWaveCannotPartiallySpawnOrAdvance, WaveLoaderRejectsNonAnchorOccupancyBeforeCreatingUnits,
            WaveLoaderInitializesCellShieldsSeparatelyForEachSpawn,
            CatalogRegistrationControlsRuntimeSpawnsAndRetry, DuplicateCatalogFailsBeforeSpawningOrAdvancing,
            MapSwitchUsesItsOwnCatalogAndRejectsStaleSelection, SharedMapButtonsKeepTheirOwnClearIds,
            EditorMapDataLoadsAtEntryAndCleansUpOnExit, InvalidMapDataDoesNotLeavePartialBattleObjects,
            WaveOrdinalsFollowSuccessfulLoadsAndRepeatedAssets, ExpandedAuthoredCatalogLoadsAndRetainsEveryMonsterId };
        foreach (Action test in tests) { test(); Console.WriteLine("PASS " + test.Method.Name); }
        Console.WriteLine($"{tests.Length} map-editor workflow scenarios passed (API doubles; Unity verification still required).");
        return 0;
    }
}

