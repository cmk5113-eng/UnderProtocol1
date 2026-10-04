using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

internal static class Scenarios
{
    private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    private static void Set(object target, string field, object value) => target.GetType().GetField(field, Private | BindingFlags.Public).SetValue(target, value);
    private static T Get<T>(object target, string field) => (T)target.GetType().GetField(field, Private | BindingFlags.Public).GetValue(target);
    private static object Call(object target, string method, params object[] args) => target.GetType().GetMethod(method, Private).Invoke(target, args);
    private static WaveData Current(StageMapEditor editor) => (WaveData)typeof(StageMapEditor).GetProperty("CurrentWave", Private).GetValue(editor);
    private static void Mode(StageMapEditor editor, string mode) => Set(editor, "mode", Enum.Parse(typeof(StageMapEditor).GetNestedType("EditMode", BindingFlags.NonPublic), mode));
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static void SetStageWaves(WaveManager manager, int index, WaveData[] waves)
    {
        while (manager.StageCount <= index) manager.AddStage();
        Get<List<StageWaveData>>(manager, "stages")[index].waves = waves;
    }

    private sealed class Fixture
    {
        public StageMapEditor editor;
        public WaveManager manager;
        public WaveLoader loader;
        public StageMapBinding binding;
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
        var f = new Fixture { editor = new StageMapEditor(), first = new WaveData { name = "first" }, second = new WaveData { name = "second" }, terrain = new StageMapData() };
        f.manager = new GameObject("manager").AddComponent<WaveManager>();
        SetStageWaves(f.manager, 0, new[] { f.first, f.second });
        SetStageWaves(f.manager, 1, Array.Empty<WaveData>());
        var root = new GameObject("inactive map"); root.activeSelf = false; root.AddComponent<PlacementController>();
        f.loader = root.AddComponent<WaveLoader>();
        var tiles = new GameObject("tilemap") { parent = root };
        f.map = tiles.AddComponent<Tilemap>(); f.binding = tiles.AddComponent<StageMapBinding>(); f.binding.EditorSetTilemap(f.map);
        Set(f.binding, "mapData", f.terrain);
        for (int x = -2; x <= 0; x++) for (int y = -1; y <= 1; y++) f.map.Tiles.Add(new Vector3Int(x, y));
        f.a = Monster(11, "A"); f.b = Monster(22, "B");
        Set(f.loader, "monsterDatas", new List<MonsterData> { f.a, f.b });
        Set(f.editor, "tilemap", f.map); Set(f.editor, "stageMapData", f.terrain); Set(f.editor, "selectedMonster", f.a);
        GameManager.Instance = new GameObject("game manager").AddComponent<GameManager>(); GameManager.Instance.Wave = f.manager;
        BattleManager.Instance = new GameObject("battle").AddComponent<BattleManager>();
        PlacementManager.Instance = new GameObject("placement").AddComponent<PlacementManager>(); PlacementManager.Instance.tilemap = f.map;
        return f;
    }

    private static void EditModeReadsSerializedWaves()
    {
        Fixture f = NewFixture();
        Check(f.manager.StageWaveIndex.Count == 2, "compatibility view must read the same authoring list in edit mode");
        Check(Current(f.editor) == f.first, "edit mode cannot read the selected serialized wave");
        Check(f.manager.GetStageWaves(-1) == null && f.manager.GetStageWaves(5) == null, "invalid stage accepted");
        Set(f.binding, "stageIndex", 1); SetStageWaves(f.manager, 1, new[] { f.second });
        Check(Current(f.editor) == f.second, "Stage Index did not change waves");
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
        Check(f.manager.GetStageWaves(0).Length == 3 && Current(f.editor) == null, "new slot copied the previous reference");
        Check(f.manager.GetStageWaves(1).SequenceEqual(otherStage), "editing one stage changed another");
        Call(f.editor, "RemoveWaveSlot", 0);
        Check(f.manager.GetStageWaves(0).Length == 2 && f.manager.GetStageWaves(0)[0] == f.second, "non-null slot was cleared instead of removed");
        Check(Get<int>(f.editor, "selectedWaveIndex") == 1, "selection was not adjusted");
        Undo.PerformUndo(); Check(f.manager.GetStageWaves(0).Length == 3 && f.manager.GetStageWaves(0)[0] == f.first, "list undo failed");
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
                Check(f.manager.GetStageWaves(0)[to] == original[from], "wave execution order did not change");
            }
    }

    private static void InvalidMonsterDataCannotBePainted()
    {
        Fixture f = NewFixture(); var cell = new Vector3Int(0, 0);
        Set(f.editor, "selectedMonster", Monster(99, "unregistered"));
        Check(!(bool)Call(f.editor, "PaintCell", cell), "unregistered monster painted");
        Set(f.editor, "selectedMonster", f.a); Set(f.loader, "monsterDatas", new List<MonsterData> { f.a, Monster(11, "duplicate") });
        Check(!(bool)Call(f.editor, "PaintCell", cell), "duplicate ID painted");
        Set(f.loader, "monsterDatas", new List<MonsterData> { f.a }); f.a.prefab = new GameObject("no MonsterBase");
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
        Check(f.manager.GetStageWaves(0).Length == 2 && AssetDatabase.Assets.Count == 0, "cancel created a wave or slot");
        EditorUtility.NextAssetPath = "Assets/TestWave.asset"; Call(f.editor, "CreateWaveAsset", -1);
        WaveData created = Current(f.editor);
        Check(created != f.first && created != f.second && created.monsters != null && f.manager.GetStageWaves(0).Length == 3, "new wave not created and linked");
        Check(AssetDatabase.Assets["Assets/TestWave.asset"] == created, "new wave was not stored at selected path");
        Call(f.editor, "RemoveWaveSlot", 2); Check(AssetDatabase.Assets.ContainsKey("Assets/TestWave.asset"), "removing a slot deleted its asset");
        Call(f.editor, "AddWaveSlot", new object[] { null }); EditorUtility.NextAssetPath = "Assets/SlotWave.asset"; Call(f.editor, "CreateWaveAsset", 2);
        Check(f.manager.GetStageWaves(0).Length == 3 && Current(f.editor) == AssetDatabase.Assets["Assets/SlotWave.asset"], "empty slot creation appended extra slot");
    }

    private static void WaveListSaveIncludesBindingScene()
    {
        Fixture f = NewFixture(); f.binding.gameObject.scene = new UnityEngine.SceneManagement.Scene(2); Call(f.editor, "SaveWaveList");
        Check(EditorSceneManager.Saved.Select(scene => scene.id).SequenceEqual(new[] { 1, 2 }), "scene containing the binding was not saved");
        EditorSceneManager.Saved.Clear(); f.binding.gameObject.scene = f.manager.gameObject.scene; Call(f.editor, "SaveWaveList");
        Check(EditorSceneManager.Saved.Count == 1, "same scene saved twice");
    }

    private static void PaintedWaveLoadsThroughRealWaveLoader()
    {
        Fixture f = NewFixture(); Call(f.editor, "PaintCell", new Vector3Int(-2, 1));
        Set(f.editor, "selectedMonster", f.b); Call(f.editor, "PaintCell", new Vector3Int(0, -1)); Call(f.editor, "SaveCurrentWave");
        var connected = (IEnumerator)Call(f.manager, "OnConnected", GameManager.Instance); while (connected.MoveNext()) { }
        Check(f.manager.StageWaveIndex[0] == f.manager.GetStageWaves(0), "runtime list differs from editor's serialized list");
        f.manager.currentWave = Current(f.editor); Check(f.loader.LoadWave(), "runtime rejected the editor-authored wave");
        Check(MonsterBase._monsters.Count == 2 && MonsterBase._monsters[0].name == "A clone" && MonsterBase._monsters[1].name == "B clone", "runtime instantiated wrong monster IDs");
        Check(MonsterBase._monsters[0].transform.position.x == -1.5f && MonsterBase._monsters[0].transform.position.y == 1.5f, "runtime lost tile coordinates");
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
        var nextLoader = nextRoot.AddComponent<WaveLoader>(); var nextMonster = Monster(33, "C");
        Set(nextLoader, "monsterDatas", new List<MonsterData> { nextMonster });
        var nextObject = new GameObject("next tilemap") { parent = nextRoot };
        var nextMap = nextObject.AddComponent<Tilemap>(); nextMap.Tiles.Add(new Vector3Int(0, 0));
        var nextBinding = nextObject.AddComponent<StageMapBinding>(); nextBinding.EditorSetTilemap(nextMap); Set(nextBinding, "stageIndex", 1);
        var nextWave = new WaveData(); SetStageWaves(f.manager, 1, new[] { nextWave });
        Set(f.editor, "tilemap", nextMap); Call(f.editor, "TryAutoFindStageMapData");
        Check(Current(f.editor) == nextWave && Get<StageMapData>(f.editor, "stageMapData") == null, "previous map data leaked into next map");
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

    private static void LegacyMigrationPreservesAllFiveStagesAndStaysEmptyAfterDeletion()
    {
        NewFixture(); var legacyManager = new GameObject("legacy manager").AddComponent<WaveManager>();
        var originals = new WaveData[5][];
        for (int i = 0; i < originals.Length; i++)
        {
            originals[i] = new[] { new WaveData { name = "legacy" + i }, new WaveData() };
            Set(legacyManager, "stage" + (i + 1) + "Waves", originals[i]);
        }
        Check(legacyManager.MigrateLegacyStages() && legacyManager.StageCount == 5, "legacy stages were not migrated");
        for (int i = 0; i < originals.Length; i++) Check(legacyManager.GetStageWaves(i).SequenceEqual(originals[i]), "migration lost a legacy wave reference");
        Check(!legacyManager.MigrateLegacyStages(), "migration ran a second time");
        while (legacyManager.StageCount > 0) legacyManager.RemoveStage(0);
        Check(legacyManager.StageCount == 0 && !legacyManager.MigrateLegacyStages(), "deleting all stages resurrected legacy data");
        var fresh = new GameObject("fresh manager").AddComponent<WaveManager>();
        Check(fresh.StageCount == 0, "new component still creates five hardcoded stages");
    }

    private static void ThirtyStagesCanBeAuthoredAndLoaded()
    {
        Fixture f = NewFixture();
        while (f.manager.StageCount < 30) Check(StageWaveEditorUtility.AddStage(f.manager) == f.manager.StageCount - 1, "append returned wrong index");
        Check(f.manager.StageCount == 30 && f.manager.StageWaveIndex.Count == 30, "stage list stopped at five");
        f.binding.EditorSetStageIndex(29); var wave = new WaveData(); Call(f.editor, "AddWaveSlot", wave);
        Check(Current(f.editor) == wave && f.manager.GetStageWaves(29).Length == 1 && f.manager.GetStageWaves(28).Length == 0, "new stage shared another stage's wave list");
        Call(f.editor, "PaintCell", new Vector3Int(0, 0)); Call(f.editor, "SaveCurrentWave");
        var setter = new GameObject("stage button").AddComponent<WaveSetter>(); setter.index = 100;
        Set(setter, "progressController", new GameObject("progress").AddComponent<tempcontroller>());
        setter.SelectSkillByIndex();
        Check(BattleManager.Instance.IsBattleActive && BattleManager.Instance.LastStageId == 100, "battle entry still used fixed stage count or changed clear ID");
        Check(f.manager.currentWave == wave && MonsterBase._monsters.Count == 1, "Stage Index 29 did not load the authored wave");
    }

    private static void NewComponentResetDoesNotLookLikeLegacyAfterArrayNormalization()
    {
        NewFixture(); var fresh = new GameObject("reset manager").AddComponent<WaveManager>();
        Call(fresh, "Reset");
        for (int i = 1; i <= 5; i++) Set(fresh, "stage" + i + "Waves", Array.Empty<WaveData>());
        Check(fresh.StageCount == 0 && !fresh.NeedsStageMigration, "new component's empty legacy fields created five stages");
        StageWaveEditorUtility.AddStage(fresh); Check(fresh.StageCount == 1, "new component could not author stages after reset");
    }

    private static void StageRemovalRemapsBindingsAndPreservesProgressIdsWithUndo()
    {
        Fixture f = NewFixture(); var middle = new WaveData(); var later = new WaveData();
        SetStageWaves(f.manager, 1, new[] { middle }); SetStageWaves(f.manager, 2, new[] { later });
        var removedBinding = new GameObject("removed map").AddComponent<StageMapBinding>(); removedBinding.EditorSetStageIndex(1);
        var laterBinding = new GameObject("later map").AddComponent<StageMapBinding>(); laterBinding.EditorSetStageIndex(2);
        var removedSetter = new GameObject("removed button").AddComponent<WaveSetter>(); removedSetter.index = 1;
        var laterSetter = new GameObject("later button").AddComponent<WaveSetter>(); laterSetter.index = 2;
        Check(StageWaveEditorUtility.RemoveStage(f.manager, 1), "stage removal failed");
        Check(f.manager.StageCount == 2 && f.manager.GetStageWaves(1)[0] == later && f.binding.StageIndex == 0, "wrong stage was removed");
        Check(removedBinding.StageIndex == -1 && removedSetter.WaveStageIndex == -1, "deleted stage references were not disconnected");
        Check(laterBinding.StageIndex == 1 && laterSetter.WaveStageIndex == 1 && laterSetter.index == 2 && laterSetter.StageId == 2, "later references changed clear/progress IDs");
        Check(middle != null && later != null, "removing stage destroyed wave assets");
        Undo.PerformUndo();
        Check(f.manager.StageCount == 3 && f.manager.GetStageWaves(1)[0] == middle && removedBinding.StageIndex == 1 && laterBinding.StageIndex == 2, "stage delete undo did not restore manager and bindings together");
        Check(removedSetter.WaveStageIndex == 1 && laterSetter.WaveStageIndex == 2, "stage delete undo did not restore button references");
    }

    private static void DetachedStagesRejectBattleEntryAndCanBeReconnected()
    {
        Fixture f = NewFixture(); var setter = new GameObject("button").AddComponent<WaveSetter>(); setter.index = 0;
        Set(setter, "progressController", new GameObject("progress").AddComponent<tempcontroller>());
        StageWaveEditorUtility.RemoveStage(f.manager, 0);
        Check(f.binding.StageIndex == -1 && setter.WaveStageIndex == -1, "removed stage was rebound implicitly");
        setter.SelectSkillByIndex(); Check(!BattleManager.Instance.IsBattleActive, "detached map entered a different stage");
        int index = StageWaveEditorUtility.AddStage(f.manager); f.binding.EditorSetStageIndex(index);
        Call(f.editor, "AddWaveSlot", new WaveData());
        setter.SelectSkillByIndex(); Check(BattleManager.Instance.IsBattleActive, "map could not reconnect to new stage");
    }

    private static void DynamicStageEditingGuardsInvalidIndexesAndPlayMode()
    {
        Fixture f = NewFixture(); int count = f.manager.StageCount;
        Check(!StageWaveEditorUtility.RemoveStage(f.manager, -1) && !StageWaveEditorUtility.RemoveStage(f.manager, count), "invalid stage deletion accepted");
        EditorApplication.isPlayingOrWillChangePlaymode = true;
        Check(StageWaveEditorUtility.AddStage(f.manager) == -1 && !StageWaveEditorUtility.RemoveStage(f.manager, 0) && f.manager.StageCount == count, "play mode changed stage collection");
        EditorApplication.isPlayingOrWillChangePlaymode = false;
        Check(!f.manager.SetStageWave(99, 0, new WaveData()), "invalid stage wave update accepted");
    }

    private static StageButtonImageController StageButton(WaveSetter setter, int required)
    {
        setter.gameObject.AddComponent<Button>();
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
        var setter = new GameObject("stage 30").AddComponent<WaveSetter>(); setter.index = 29;
        var legacy = new GameObject("legacy progress").AddComponent<tempcontroller>(); legacy.allow = false;
        Set(setter, "progressController", legacy);
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
        Fixture f = NewFixture(); SetStageWaves(f.manager, 29, new[] { f.first }); f.binding.EditorSetStageIndex(29);
        Call(f.editor, "PaintCell", new Vector3Int(0, 0));
        var setter = new GameObject("stage 30 button").AddComponent<WaveSetter>(); setter.index = 29;
        StageButtonImageController controller = StageButton(setter, 100);
        int entered = 0; Get<UnityEngine.Events.UnityEvent>(setter, "onStageEntered").AddListener(() => entered++);
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
        var first = new GameObject("first button").AddComponent<WaveSetter>(); first.index = 7;
        var second = new GameObject("second button").AddComponent<WaveSetter>(); second.index = 7;
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
        var setter = new GameObject("cleared stage").AddComponent<WaveSetter>(); setter.index = 12;
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
        var setter = new GameObject("legacy setter without button art").AddComponent<WaveSetter>(); setter.index = 29;
        Check(setter.RequiredProgress == 0 && setter.CanEnterStage, "setter without image controller still required a tempcontroller connection");
        Button button = setter.gameObject.AddComponent<Button>();
        StageButtonImageController controller = setter.gameObject.AddComponent<StageButtonImageController>();
        Set(controller, "requredProgress", -10); Call(controller, "Awake"); Call(controller, "OnEnable");
        Check(controller.RequiredProgress == 0 && setter.CanEnterStage && button.interactable, "negative requirement or absent image broke entry permission");
        Set(controller, "requredProgress", 5); controller.Refresh();
        Check(!setter.CanEnterStage && !button.interactable, "button without an image bypassed positive requirement");
        Call(controller, "OnDisable");
    }

    public static int Main()
    {
        Action[] tests = { EditModeReadsSerializedWaves, PaintReplacesOneCellAndIsolatesWaves, ErasingRespectsTerrainLayers,
            ListAdditionRemovalAndUndo, ReorderingPreservesSelectedAsset, InvalidMonsterDataCannotBePainted,
            SaveTargetsSelectedAssetAndRejectsInvalidSpawns, NewWaveCreationAndCancellation, WaveListSaveIncludesBindingScene,
            PaintedWaveLoadsThroughRealWaveLoader, GridCoordinatesAndDragPainting, SwitchingTilemapsResolvesNewStageAndPalette, PlayModeGuardsAndPaintUndo,
            LegacyMigrationPreservesAllFiveStagesAndStaysEmptyAfterDeletion, ThirtyStagesCanBeAuthoredAndLoaded,
            NewComponentResetDoesNotLookLikeLegacyAfterArrayNormalization,
            StageRemovalRemapsBindingsAndPreservesProgressIdsWithUndo, DetachedStagesRejectBattleEntryAndCanBeReconnected,
            DynamicStageEditingGuardsInvalidIndexesAndPlayMode,
            RequiredProgressControlsButtonAndEntryAtTheBoundary, DirectStageEntryChecksRequireWithoutTempcontroller,
            EachStageButtonKeepsItsOwnRequirement, ClearedAppearanceDoesNotBypassRequireAndRefreshesAfterLoad,
            DefaultAndNegativeRequirementsNeedNoLegacyProgressConnection };
        foreach (Action test in tests) { test(); Console.WriteLine("PASS " + test.Method.Name); }
        Console.WriteLine($"{tests.Length} map-editor workflow scenarios passed (API doubles; Unity verification still required).");
        return 0;
    }
}
