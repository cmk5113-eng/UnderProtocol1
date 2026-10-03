// Minimal API doubles for compiling the real editor and exercising data mutations.
// They do not emulate Unity rendering, import, prefab serialization, or native scene IO.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UObject = UnityEngine.Object;

namespace UnityEngine
{
    public class Object
    {
        public string name;
        public static T[] FindObjectsByType<T>(FindObjectsInactive inactive, FindObjectsSortMode sort) where T : Object
            => GameObject.All.Where(g => inactive == FindObjectsInactive.Include || g.activeSelf).SelectMany(g => g.Components).OfType<T>().ToArray();
        public static T FindFirstObjectByType<T>(FindObjectsInactive inactive) where T : Object
            => FindObjectsByType<T>(inactive, FindObjectsSortMode.None).FirstOrDefault();
        public static GameObject Instantiate(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            var copy = new GameObject(prefab.name + " clone");
            copy.transform.position = position;
            if (prefab.GetComponent<MonsterBase>() != null) copy.AddComponent<MonsterBase>();
            return copy;
        }
    }
    public class Component : Object
    {
        public GameObject gameObject;
        public Transform transform => gameObject.transform;
        public T GetComponent<T>() where T : Component => gameObject.GetComponent<T>();
        public T GetComponentInParent<T>(bool includeInactive = false) where T : Component
        {
            for (GameObject current = gameObject; current != null; current = current.parent)
                if (current.GetComponent<T>() is T component) return component;
            return null;
        }
        public T GetComponentInChildren<T>(bool includeInactive = false) where T : Component
        {
            T local = GetComponent<T>();
            if (local != null) return local;
            foreach (GameObject child in GameObject.All.Where(g => g.parent == gameObject))
            {
                T found = child.transform.GetComponentInChildren<T>(includeInactive);
                if (found != null) return found;
            }
            return null;
        }
    }
    public class MonoBehaviour : Component { }
    public class GameObject : Object
    {
        public static readonly List<GameObject> All = new List<GameObject>();
        public readonly List<Component> Components = new List<Component>();
        public GameObject parent;
        public bool activeSelf = true;
        public bool activeInHierarchy => activeSelf && (parent == null || parent.activeInHierarchy);
        public SceneManagement.Scene scene = new SceneManagement.Scene(1);
        public readonly Transform transform;
        public GameObject(string name = "Object") { this.name = name; transform = AddComponent<Transform>(); All.Add(this); }
        public T AddComponent<T>() where T : Component, new() { var value = new T { gameObject = this, name = name }; Components.Add(value); return value; }
        public T GetComponent<T>() where T : Component => Components.OfType<T>().FirstOrDefault();
        public T GetComponentInChildren<T>(bool include = false) where T : Component => transform.GetComponentInChildren<T>(include);
        public void SetActive(bool value) => activeSelf = value;
    }
    public class Transform : Component { public Vector3 position; public Vector3 forward = new Vector3(0, 0, 1); public Vector3 lossyScale = new Vector3(1, 1, 1); public Vector3 localScale = new Vector3(1, 1, 1); }
    public class ScriptableObject : Object { public static T CreateInstance<T>() where T : ScriptableObject, new() => new T(); }
    public class Texture : Object { public int width = 64, height = 64; }
    public class Texture2D : Texture { }
    public class Sprite : Object { public Texture2D texture; public Rect rect; }
    public class SpriteRenderer : Component { public Sprite sprite; }
    public class Grid : Component { public Vector3 cellSize = new Vector3(1, 1, 0); public Vector3 cellGap; }
    public struct Vector2 { public float x, y; public Vector2(float x, float y) { this.x = x; this.y = y; } public static Vector2 zero => default; }
    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public static Vector3 Scale(Vector3 a, Vector3 b) => new Vector3(a.x * b.x, a.y * b.y, a.z * b.z);
        public string ToString(string format) => $"({x}, {y}, {z})";
    }
    public struct Vector3Int : IEquatable<Vector3Int>
    {
        public int x, y, z;
        public Vector3Int(int x, int y, int z = 0) { this.x = x; this.y = y; this.z = z; }
        public bool Equals(Vector3Int value) => x == value.x && y == value.y && z == value.z;
        public override bool Equals(object value) => value is Vector3Int cell && Equals(cell);
        public override int GetHashCode() => HashCode.Combine(x, y, z);
        public static bool operator ==(Vector3Int a, Vector3Int b) => a.Equals(b);
        public static bool operator !=(Vector3Int a, Vector3Int b) => !a.Equals(b);
        public override string ToString() => $"({x}, {y}, {z})";
    }
    public struct Rect
    {
        public float x, y, width, height;
        public float xMax => x + width; public float yMax => y + height;
        public Rect(float x, float y, float width, float height) { this.x = x; this.y = y; this.width = width; this.height = height; }
        public bool Contains(Vector2 point) => point.x >= x && point.x < xMax && point.y >= y && point.y < yMax;
    }
    public struct BoundsInt
    {
        public Vector3Int position, size;
        public int xMin => position.x; public int yMin => position.y; public int zMin => position.z;
        public int xMax => xMin + size.x; public int yMax => yMin + size.y; public int zMax => zMin + size.z;
        public BoundsInt(int x, int y, int z, int width, int height, int depth) { position = new Vector3Int(x, y, z); size = new Vector3Int(width, height, depth); }
        public bool Contains(Vector3Int cell) => cell.x >= xMin && cell.x < xMax && cell.y >= yMin && cell.y < yMax && cell.z >= zMin && cell.z < zMax;
    }
    public struct Color { public Color(float r, float g, float b, float a = 1) { } public static Color white => default; }
    public struct Quaternion { public static Quaternion identity => default; }
    public struct Ray { public Vector3 GetPoint(float distance) => default; }
    public struct Plane { public Plane(Vector3 normal, Vector3 point) { } public bool Raycast(Ray ray, out float distance) { distance = 0; return false; } }
    public static class Mathf
    {
        public static int Clamp(int x, int min, int max) => Math.Clamp(x, min, max);
        public static int Max(int a, int b) => Math.Max(a, b);
        public static float Max(float a, float b) => Math.Max(a, b);
        public static float Min(float a, float b) => Math.Min(a, b);
        public static float Abs(float x) => Math.Abs(x);
        public static int FloorToInt(float x) => (int)Math.Floor(x);
        public static int CeilToInt(float x) => (int)Math.Ceiling(x);
    }
    public enum FindObjectsInactive { Exclude, Include } public enum FindObjectsSortMode { None }
    public enum EventType { Layout, Repaint, MouseDown, MouseDrag, MouseUp, Used }
    public class Event { public static Event current; public EventType type; public bool alt; public int button; public Vector2 mousePosition; public void Use() => type = EventType.Used; }
    public enum FocusType { Passive } public enum ScaleMode { ScaleToFit }
    public class GUIContent { public GUIContent(string text) { } public GUIContent(string text, string tooltip) { } public GUIContent(string text, Texture image, string tooltip) { } }
    public class GUIStyle { }
    public class GUILayoutOption { }
    public static class GUIUtility { public static int hotControl; public static int GetControlID(FocusType type) => 42; public static void ExitGUI() => throw new InvalidOperationException("ExitGUI"); }
    public static class GUI
    {
        public static bool enabled = true;
        public static Vector2 BeginScrollView(Rect view, Vector2 scroll, Rect content) => scroll;
        public static void EndScrollView() { }
        public static void Label(Rect rect, string text, GUIStyle style) { }
        public static void Label(Rect rect, GUIContent content, GUIStyle style = null) { }
        public static void DrawTexture(Rect rect, Texture texture, ScaleMode mode) { }
        public static void DrawTextureWithTexCoords(Rect rect, Texture texture, Rect uv) { }
    }
    public static class GUILayout
    {
        public static bool Button(string text, params GUILayoutOption[] options) => false;
        public static bool Toggle(bool value, string text, string style, params GUILayoutOption[] options) => value;
        public static bool Toggle(bool value, GUIContent text, string style, params GUILayoutOption[] options) => value;
        public static int Toolbar(int value, string[] items) => value;
        public static GUILayoutOption Width(float value) => new GUILayoutOption();
        public static GUILayoutOption Height(float value) => new GUILayoutOption();
        public static GUILayoutOption ExpandWidth(bool value) => new GUILayoutOption();
    }
    public static class GUILayoutUtility { public static Rect GetRect(float width, float height, params GUILayoutOption[] options) => new Rect(0, 0, 400, height); }
    public static class Debug { public static void Log(object text) { } public static void LogError(object text) { } public static void LogWarning(object text) { } }
    public class SerializeField : Attribute { }
    public class HideInInspector : Attribute { }
    public class Tooltip : Attribute { public Tooltip(string text) { } }
    public class Min : Attribute { public Min(float value) { } }
    public class RangeAttribute : Attribute { public RangeAttribute(float min, float max) { } }
    public class Header : Attribute { public Header(string text) { } }
    public class CreateAssetMenuAttribute : Attribute { public string menuName; }
}
namespace UnityEngine.SceneManagement
{
    public struct Scene : IEquatable<Scene>
    {
        public int id; public Scene(int id) { this.id = id; } public bool IsValid() => id > 0;
        public bool Equals(Scene scene) => id == scene.id;
        public override bool Equals(object value) => value is Scene scene && Equals(scene);
        public override int GetHashCode() => id;
        public static bool operator ==(Scene a, Scene b) => a.Equals(b); public static bool operator !=(Scene a, Scene b) => !a.Equals(b);
    }
}
namespace UnityEngine.Tilemaps
{
    public class Tilemap : UnityEngine.Component
    {
        public readonly HashSet<UnityEngine.Vector3Int> Tiles = new HashSet<UnityEngine.Vector3Int>();
        public UnityEngine.BoundsInt cellBounds = new UnityEngine.BoundsInt(-2, -1, 0, 3, 3, 1);
        public UnityEngine.Grid layoutGrid = new UnityEngine.GameObject().AddComponent<UnityEngine.Grid>();
        public bool HasTile(UnityEngine.Vector3Int cell) => Tiles.Contains(cell);
        public UnityEngine.Sprite GetSprite(UnityEngine.Vector3Int cell) => null;
        public UnityEngine.Vector3 GetCellCenterWorld(UnityEngine.Vector3Int cell) => new UnityEngine.Vector3(cell.x + .5f, cell.y + .5f, cell.z);
        public UnityEngine.Vector3Int WorldToCell(UnityEngine.Vector3 point) => new UnityEngine.Vector3Int((int)point.x, (int)point.y, (int)point.z);
    }
}
namespace UnityEditor
{
    using UnityEngine;
    public class EditorWindow : ScriptableObject { public Vector2 minSize; public Rect position = new Rect(0, 0, 500, 900); public static T GetWindow<T>(string title) where T : new() => new T(); public void Repaint() { } public void ShowNotification(GUIContent value) { } }
    public class MenuItem : Attribute { public MenuItem(string name) { } }
    public class CustomEditor : Attribute { public CustomEditor(Type type) { } }
    public class Editor : ScriptableObject
    {
        public UObject target; public SerializedObject serializedObject;
        public virtual void OnInspectorGUI() { }
        public static void DrawPropertiesExcluding(SerializedObject so, params string[] excluded) { }
    }
    public enum MessageType { Info, Warning, Error }
    public class SceneView { public static event Action<SceneView> duringSceneGui; public static SceneView lastActiveSceneView; public static void RepaintAll() { } public void FrameSelected() { } }
    public static class Selection { public static GameObject activeGameObject; }
    public static class EditorApplication { public static bool isPlayingOrWillChangePlaymode; }
    public static class EditorStyles { public static GUIStyle boldLabel = new GUIStyle(), miniBoldLabel = new GUIStyle(), miniLabel = new GUIStyle(), whiteMiniLabel = new GUIStyle(); }
    public static class EditorGUI
    {
        public sealed class DisabledScope : IDisposable { private readonly bool previous; public DisabledScope(bool disabled) { previous = GUI.enabled; GUI.enabled &= !disabled; } public void Dispose() => GUI.enabled = previous; }
        public static void BeginChangeCheck() { } public static bool EndChangeCheck() => false;
        public static void DrawRect(Rect rect, Color color) { }
    }
    public static class EditorGUILayout
    {
        public static Vector2 BeginScrollView(Vector2 value) => value; public static void EndScrollView() { }
        public static void BeginHorizontal() { } public static void EndHorizontal() { } public static void Space() { }
        public static void LabelField(string label, GUIStyle style = null) { } public static void HelpBox(string label, MessageType type) { }
        public static UObject ObjectField(string label, UObject value, Type type, bool scene) => value;
        public static UObject ObjectField(UObject value, Type type, bool scene) => value;
        public static void PropertyField(SerializedProperty property, GUIContent label, bool children = false) { }
        public static int IntSlider(string label, int value, int min, int max) => value;
        public static int IntField(string label, int value) => value; public static bool Toggle(string label, bool value) => value;
        public static Enum EnumPopup(string label, Enum value) => value; public static bool Foldout(bool value, string label, bool toggle) => value;
    }
    public static class EditorUtility
    {
        public static readonly HashSet<UObject> Dirty = new HashSet<UObject>(); public static string NextAssetPath;
        public static void SetDirty(UObject value) => Dirty.Add(value); public static bool IsDirty(UObject value) => Dirty.Contains(value);
        public static string SaveFilePanelInProject(string title, string file, string extension, string message) => NextAssetPath;
    }
    public static class PrefabUtility { public static void RecordPrefabInstancePropertyModifications(UObject value) { } }
    public static class EditorGUIUtility { public static void PingObject(UObject value) { } }
    public static class AssetPreview { public static Texture2D GetAssetPreview(UObject value) => null; public static Texture2D GetMiniThumbnail(UObject value) => null; }
    public static class AssetDatabase
    {
        public static readonly Dictionary<string, UObject> Assets = new Dictionary<string, UObject>(); public static readonly List<UObject> Saved = new List<UObject>();
        public static void CreateAsset(UObject asset, string path) { asset.name = System.IO.Path.GetFileNameWithoutExtension(path); Assets.Add(path, asset); }
        public static void SaveAssetIfDirty(UObject asset) { Saved.Add(asset); EditorUtility.Dirty.Remove(asset); }
    }
    public static class Undo
    {
        public static event Action undoRedoPerformed; public static readonly Stack<Action> Records = new Stack<Action>();
        private static int group; private static readonly Dictionary<int, int> starts = new Dictionary<int, int>();
        public static void RecordObject(UObject target, string name)
        {
            var fields = target.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var snapshot = fields.Select(f => (field: f, value: Copy(f.GetValue(target)))).ToArray();
            Records.Push(() => { foreach (var entry in snapshot) entry.field.SetValue(target, entry.value); });
        }
        private static object Copy(object value)
        {
            if (value is Array array) return array.Clone();
            if (value is IList list)
            {
                var copy = (IList)Activator.CreateInstance(value.GetType());
                foreach (object item in list)
                    copy.Add(item is MonsterSpawnData spawn ? new MonsterSpawnData { monsterID = spawn.monsterID, position = spawn.position }
                        : item is StageWaveData stage ? new StageWaveData { waves = stage.waves == null ? null : (WaveData[])stage.waves.Clone() } : item);
                return copy;
            }
            return value;
        }
        public static void PerformUndo() { Records.Pop()(); undoRedoPerformed?.Invoke(); }
        public static T AddComponent<T>(GameObject gameObject) where T : Component, new() => gameObject.AddComponent<T>();
        public static void IncrementCurrentGroup() { group++; starts[group] = Records.Count; }
        public static int GetCurrentGroup() => group;
        public static void SetCurrentGroupName(string name) { }
        public static void CollapseUndoOperations(int value)
        {
            if (!starts.TryGetValue(value, out int count)) return;
            var actions = new List<Action>(); while (Records.Count > count) actions.Add(Records.Pop());
            if (actions.Count > 0) Records.Push(() => { foreach (Action action in actions) action(); });
        }
    }
    public class SerializedObject
    {
        public readonly UObject targetObject; private readonly List<SerializedProperty> properties = new List<SerializedProperty>();
        public SerializedObject(UObject target) { targetObject = target ?? throw new ArgumentNullException(nameof(target)); }
        public void Update() { }
        public SerializedProperty FindProperty(string name)
        {
            FieldInfo field = targetObject.GetType().GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null) return null;
            var value = field.GetValue(targetObject);
            if (value is Array array) value = array.Clone();
            else if (value is IList list)
            {
                var copy = (IList)Activator.CreateInstance(value.GetType());
                foreach (object item in list) copy.Add(item is StageWaveData stage ? new StageWaveData { waves = stage.waves == null ? null : (WaveData[])stage.waves.Clone() } : item);
                value = copy;
            }
            var property = new SerializedProperty(field, value); properties.Add(property); return property;
        }
        public bool ApplyModifiedProperties()
        {
            if (!properties.Any(p => p.changed)) return false;
            Undo.RecordObject(targetObject, "Serialized change");
            foreach (SerializedProperty property in properties.Where(p => p.changed)) { property.field.SetValue(targetObject, property.value); property.changed = false; }
            return true;
        }
    }
    public class SerializedProperty
    {
        internal FieldInfo field; internal object value; internal bool changed; private readonly SerializedProperty root; private readonly int index;
        private SerializedProperty parent; private object parentTarget;
        internal SerializedProperty(FieldInfo field, object value) { this.field = field; this.value = value; root = this; }
        private SerializedProperty(SerializedProperty root, int index) { this.root = root; this.index = index; }
        public int arraySize
        {
            get => ((IList)root.value)?.Count ?? 0;
            set
            {
                Array old = root.value as Array; Type element = root.field.FieldType.GetElementType(); var array = Array.CreateInstance(element, value);
                if (old != null) { Array.Copy(old, array, Math.Min(old.Length, value)); if (value > old.Length && old.Length > 0) for (int i = old.Length; i < value; i++) array.SetValue(old.GetValue(old.Length - 1), i); }
                root.value = array; root.MarkChanged();
            }
        }
        private void MarkChanged()
        {
            changed = true;
            if (parent != null) { field.SetValue(parentTarget, value); parent.MarkChanged(); }
        }
        public UObject objectReferenceValue { get => (UObject)((IList)root.value)[index]; set { ((IList)root.value)[index] = value; root.MarkChanged(); } }
        public SerializedProperty GetArrayElementAtIndex(int index) => new SerializedProperty(root, index);
        public SerializedProperty FindPropertyRelative(string name)
        {
            object target = ((IList)root.value)[index];
            FieldInfo field = target.GetType().GetField(name);
            object value = field.GetValue(target); if (value is Array array) value = array.Clone();
            return new SerializedProperty(field, value) { parent = root, parentTarget = target };
        }
        public void DeleteArrayElementAtIndex(int index)
        {
            // Object reference arrays first clear non-null elements instead of removing them.
            if (((IList)root.value)[index] != null) { ((IList)root.value)[index] = null; root.MarkChanged(); return; }
            var old = (Array)root.value; var array = Array.CreateInstance(old.GetType().GetElementType(), old.Length - 1);
            for (int from = 0, to = 0; from < old.Length; from++) if (from != index) array.SetValue(old.GetValue(from), to++);
            root.value = array; root.MarkChanged();
        }
        public void MoveArrayElement(int from, int to)
        {
            var array = (Array)root.value; object item = array.GetValue(from);
            if (from < to) for (int i = from; i < to; i++) array.SetValue(array.GetValue(i + 1), i);
            else for (int i = from; i > to; i--) array.SetValue(array.GetValue(i - 1), i);
            array.SetValue(item, to); root.MarkChanged();
        }
    }
    public static class HandleUtility { public static int nearestControl; public static Ray GUIPointToWorldRay(Vector2 point) => default; public static void AddDefaultControl(int id) => nearestControl = id; }
    public static class Handles { public static Color color; public static void DrawSolidDisc(Vector3 center, Vector3 normal, float radius) { } public static void Label(Vector3 center, string label) { } }
}
namespace UnityEditor.SceneManagement
{
    public static class EditorSceneManager
    {
        public static readonly List<UnityEngine.SceneManagement.Scene> Dirty = new List<UnityEngine.SceneManagement.Scene>(), Saved = new List<UnityEngine.SceneManagement.Scene>();
        public static bool MarkSceneDirty(UnityEngine.SceneManagement.Scene scene) { Dirty.Add(scene); return true; }
        public static bool SaveScene(UnityEngine.SceneManagement.Scene scene) { Saved.Add(scene); return true; }
    }
}
namespace NUnit.Framework { }
public class ManagerBase : UnityEngine.MonoBehaviour { protected virtual IEnumerator OnConnected(GameManager manager) { yield break; } protected virtual void OnDisconnected() { } }
public class GameManager : UnityEngine.MonoBehaviour { public static GameManager Instance; public WaveManager Wave; }
public class PlacementManager : UnityEngine.MonoBehaviour { public static PlacementManager Instance; public UnityEngine.Tilemaps.Tilemap tilemap; }
public class PlacementController : UnityEngine.MonoBehaviour { public static void RemoveAllObject() { MonsterBase._monsters.Clear(); } }
public class StageMapLoader : UnityEngine.MonoBehaviour { public StageMapData MapData; }
public class MonsterBase : UnityEngine.MonoBehaviour { public static readonly List<UnityEngine.GameObject> _monsters = new List<UnityEngine.GameObject>(); }
public class BattleManager : UnityEngine.MonoBehaviour
{
    public static BattleManager Instance; public bool IsBattleActive; public int LastStageId;
    public static bool HasRemainingMonsters() => MonsterBase._monsters.Count > 0;
    public void CompleteBattle() { IsBattleActive = false; }
    public void BeginBattle(int stageId, WaveSetter setter) { IsBattleActive = true; LastStageId = stageId; }
}
public class StageUIController : UnityEngine.MonoBehaviour { public static StageUIController Instance; public void UpdateWave() { } }
namespace UnityEngine.Events { public class UnityEvent { public event Action Handler; public void Invoke() => Handler?.Invoke(); public void AddListener(Action action) => Handler += action; } }
public class tempcontroller : UnityEngine.MonoBehaviour
{
    public bool allow = true;
    public bool CanEnterStage(int index) => allow && index >= 0;
    public bool TryGetRequiredProgress(int index, out int value) { value = 0; return index >= 0; }
}
public static class ProgressManager { public static int Progress; }
public enum UIType { CharacterSelect, Stage, Menu }
public static class UIManager { public static void ClaimPopUp(string title, string message, string action) { } public static void ClaimCloseUI(UIType type) { } }
public class ModeManager : UnityEngine.MonoBehaviour { public static ModeManager Instance; public enum GameMode { None } public void ChangeMode(GameMode mode) { } }
