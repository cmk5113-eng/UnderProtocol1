// Headless API doubles only. Production passive/attack/movement/turn code is compiled unchanged.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
namespace UnityEngine
{
    [AttributeUsage(AttributeTargets.All)] public class SerializeField : Attribute { }
    [AttributeUsage(AttributeTargets.Class)] public class DisallowMultipleComponent : Attribute { }
    [AttributeUsage(AttributeTargets.All)] public class HeaderAttribute : Attribute { public HeaderAttribute(string s) {} }
    [AttributeUsage(AttributeTargets.All)] public class TooltipAttribute : Attribute { public TooltipAttribute(string s) {} }
    [AttributeUsage(AttributeTargets.All)] public class MinAttribute : Attribute { public MinAttribute(float f) {} }
    [AttributeUsage(AttributeTargets.All)] public class TextAreaAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.All)] public class HideInInspector : Attribute { }
    [AttributeUsage(AttributeTargets.All)] public class CreateAssetMenuAttribute : Attribute { public string menuName, fileName; }
    public enum RuntimeInitializeLoadType { SubsystemRegistration }
    [AttributeUsage(AttributeTargets.All)] public class RuntimeInitializeOnLoadMethodAttribute : Attribute { public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType t) {} }
    public enum FindObjectsInactive { Exclude, Include }
    public enum FindObjectsSortMode { None, InstanceID }
    public class Object
    {
        static readonly List<Object> objects = new List<Object>();
        static int nextId;
        readonly int id = ++nextId;
        public string name = "TestObject";
        public Object() { objects.Add(this); }
        public int GetInstanceID() => id;
        public static implicit operator bool(Object o) => o != null;
        public static void Destroy(Object o) { if (o is GameObject go) go.SetActive(false); }
        public static T[] FindObjectsByType<T>(FindObjectsSortMode mode) where T : Object
            => FindObjectsByType<T>(FindObjectsInactive.Exclude, mode);
        public static T[] FindObjectsByType<T>(FindObjectsInactive inactive, FindObjectsSortMode mode) where T : Object
            => objects.OfType<T>().Where(o => inactive == FindObjectsInactive.Include || !(o is Component c) || c.gameObject.activeInHierarchy).OrderBy(o=>o.id).ToArray();
        public static T FindFirstObjectByType<T>() where T : Object => FindObjectsByType<T>(FindObjectsSortMode.None).FirstOrDefault();
        public static void ResetScene() { objects.Clear(); }
    }
    public class GameObject : Object
    {
        readonly List<Component> components = new List<Component>();
        public bool activeSelf = true;
        public int layer;
        public bool activeInHierarchy => activeSelf && (transform.parent==null || transform.parent.gameObject.activeInHierarchy);
        public Transform transform;
        public GameObject() : this("GameObject") { }
        public GameObject(string name, params Type[] types)
        {
            this.name=name;
            Type transformType=types.FirstOrDefault(t=>typeof(Transform).IsAssignableFrom(t))??typeof(Transform);
            transform=(Transform)Activator.CreateInstance(transformType);transform.gameObject=this;
            foreach(Type type in types.Where(t=>!typeof(Transform).IsAssignableFrom(t))) Attach((Component)Activator.CreateInstance(type));
        }
        public void SetActive(bool active) { activeSelf = active; }
        public void Attach(Component c) { c.gameObject?.components.Remove(c); c.gameObject = this; components.Add(c); }
        public T AddComponent<T>() where T : Component,new() { var value=new T();Attach(value);return value; }
        public T GetComponent<T>() where T : class => transform as T ?? components.OfType<T>().FirstOrDefault();
        public T GetComponentInParent<T>() where T : class => GetComponent<T>() ?? transform.parent?.gameObject.GetComponentInParent<T>();
        public T[] GetComponentsInParent<T>(bool includeInactive=false) =>
            (includeInactive || activeInHierarchy ? components.OfType<T>() : Enumerable.Empty<T>())
            .Concat(transform.parent?.gameObject.GetComponentsInParent<T>(includeInactive) ?? Array.Empty<T>()).ToArray();
        public T[] GetComponentsInChildren<T>(bool includeInactive=false) => components.OfType<T>()
            .Concat(transform.children.Where(c=>includeInactive||c.gameObject.activeInHierarchy).SelectMany(c=>c.gameObject.GetComponentsInChildren<T>(includeInactive))).ToArray();
    }
    public class Component : Object
    {
        public GameObject gameObject;
        public Transform transform => gameObject.transform;
        public Component() { new GameObject().Attach(this); }
        public T GetComponent<T>() where T : class => gameObject.GetComponent<T>();
        public T GetComponentInParent<T>() where T : class => gameObject.GetComponentInParent<T>();
        public T[] GetComponentsInParent<T>(bool includeInactive=false) => gameObject.GetComponentsInParent<T>(includeInactive);
        public T GetComponentInChildren<T>(bool includeInactive=false) where T : class => gameObject.GetComponentsInChildren<T>(includeInactive).FirstOrDefault();
        public T[] GetComponentsInChildren<T>(bool includeInactive=false) => gameObject.GetComponentsInChildren<T>(includeInactive);
        public bool TryGetComponent<T>(out T value) where T : class { value = GetComponent<T>(); return value != null; }
        public bool CompareTag(string tag) => false;
    }
    public class MonoBehaviour : Component
    {
        public Coroutine StartCoroutine(IEnumerator routine) => new Coroutine { routine = routine };
        public void StopCoroutine(Coroutine routine) { }
    }
    public class Coroutine { public IEnumerator routine; }
    public class ScriptableObject : Object { public static T CreateInstance<T>() where T : ScriptableObject,new() => new T(); }
    public class Transform : Object
    {
        public Vector3 localPosition;
        public Vector3 position { get=>parent==null?localPosition:parent.TransformPoint(localPosition);set=>localPosition=parent==null?value:parent.InverseTransformPoint(value); }
        public Vector3 localScale=new Vector3(1,1,1);
        public Vector3 lossyScale => parent==null?localScale:new Vector3(parent.lossyScale.x*localScale.x,parent.lossyScale.y*localScale.y,parent.lossyScale.z*localScale.z);
        public Quaternion rotation;
        public GameObject gameObject;
        public Transform parent { get; private set; }
        public readonly List<Transform> children=new List<Transform>();
        public void SetParent(Transform value, bool worldPositionStays=true) { Vector3 old=position;parent?.children.Remove(this);parent=value;parent?.children.Add(this);if(worldPositionStays) position=old; }
        public bool IsChildOf(Transform value) => this==value || (parent!=null && parent.IsChildOf(value));
        public void SetAsLastSibling() { parent?.children.Remove(this);parent?.children.Add(this); }
        public T GetComponent<T>() where T : class => gameObject.GetComponent<T>();
        public Transform Find(string path) { Transform node=this;foreach(string name in path.Split('/')) {node=node.children.FirstOrDefault(t=>t.gameObject.name==name);if(node==null)return null;}return node; }
        public Vector3 TransformPoint(Vector3 p)=>position+new Vector3(p.x*lossyScale.x,p.y*lossyScale.y,p.z*lossyScale.z);
        public Vector3 InverseTransformPoint(Vector3 p) {p=p-position;return new Vector3(p.x/lossyScale.x,p.y/lossyScale.y,p.z/lossyScale.z);}
    }
    public struct Quaternion { public static Quaternion identity=>new Quaternion(); }
    public struct Rect
    {
        public float xMin,yMin,width,height;
        public float xMax=>xMin+width;
        public float yMax=>yMin+height;
        public Rect(float x,float y,float w,float h) {xMin=x;yMin=y;width=w;height=h;}
    }
    public class RectTransform : Transform
    {
        public Vector2 anchorMin,anchorMax,pivot=new Vector2(0.5f,0.5f),sizeDelta,offsetMin,offsetMax;
        public Rect rect=>new Rect(-sizeDelta.x*pivot.x,-sizeDelta.y*pivot.y,sizeDelta.x,sizeDelta.y);
        public void GetWorldCorners(Vector3[] corners)
        {
            Rect r=rect;corners[0]=TransformPoint(new Vector3(r.xMin,r.yMin));corners[1]=TransformPoint(new Vector3(r.xMin,r.yMax));
            corners[2]=TransformPoint(new Vector3(r.xMax,r.yMax));corners[3]=TransformPoint(new Vector3(r.xMax,r.yMin));
        }
    }
    public enum RenderMode { WorldSpace,ScreenSpaceOverlay }
    public class Canvas : Component { public RenderMode renderMode;public bool overrideSorting;public int sortingLayerID,sortingOrder; }
    public class Sprite : Object { public Bounds bounds = new Bounds { size = new Vector3(1,1,0) }; }
    public class Material : Object { }
    public struct Bounds { public Vector3 size,center;public Vector3 max=>center+size*0.5f; }
    public class SpriteRenderer : Component
    {
        public Color color; public Sprite sprite; public int sortingLayerID,sortingOrder;
        public bool enabled = true; public Material sharedMaterial;
        public Vector3 unscaledSize=new Vector3(1,1,1);
        public Bounds bounds => new Bounds { center=transform.position,size=new Vector3(Math.Abs(unscaledSize.x*transform.lossyScale.x),Math.Abs(unscaledSize.y*transform.lossyScale.y),1) };
    }
    public struct Color
    {
        public float r,g,b,a;
        public Color(float r,float g,float b,float a=1) {this.r=r;this.g=g;this.b=b;this.a=a;}
        public static Color white => new Color(1,1,1);
        public static Color Lerp(Color a,Color b,float t)=>new Color(a.r+(b.r-a.r)*t,a.g+(b.g-a.g)*t,a.b+(b.b-a.b)*t,a.a+(b.a-a.a)*t);
    }
    public struct Vector2
    {
        public float x,y;
        public Vector2(float x,float y) {this.x=x;this.y=y;}
        public static Vector2 zero=>new Vector2();
        public static Vector2 one=>new Vector2(1,1);
        public float sqrMagnitude => x*x+y*y;
        public static float Distance(Vector2 a,Vector2 b) => (float)Math.Sqrt((a.x-b.x)*(a.x-b.x)+(a.y-b.y)*(a.y-b.y));
        public static implicit operator Vector2(Vector3 v) => new Vector2(v.x,v.y);
        public static Vector2 operator +(Vector2 a,Vector2 b) => new Vector2(a.x+b.x,a.y+b.y);
    }
    public struct Vector3
    {
        public float x,y,z;
        public Vector3(float x,float y,float z=0) {this.x=x;this.y=y;this.z=z;}
        public float magnitude => (float)Math.Sqrt(x*x+y*y+z*z);
        public Vector3 normalized => magnitude > 0 ? new Vector3(x/magnitude,y/magnitude,z/magnitude) : new Vector3();
        public void Normalize() { this=normalized; }
        public static Vector3 operator +(Vector3 a,Vector3 b) => new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);
        public static Vector3 operator -(Vector3 a,Vector3 b) => new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);
        public static Vector3 operator *(float a,Vector3 b) => new Vector3(a*b.x,a*b.y,a*b.z);
        public static Vector3 operator *(Vector3 a,float b) => b*a;
        public static implicit operator Vector3(Vector2 v) => new Vector3(v.x,v.y);
    }
    public struct Vector2Int : IEquatable<Vector2Int>
    {
        public int x,y;
        public Vector2Int(int x,int y) {this.x=x;this.y=y;}
        public static Vector2Int zero => new Vector2Int();
        public bool Equals(Vector2Int b) => x==b.x&&y==b.y;
        public override bool Equals(object b) => b is Vector2Int v&&Equals(v);
        public override int GetHashCode() => x*397^y;
        public static bool operator ==(Vector2Int a,Vector2Int b) => a.Equals(b);
        public static bool operator !=(Vector2Int a,Vector2Int b) => !a.Equals(b);
    }
    public struct Vector3Int : IEquatable<Vector3Int>
    {
        public int x,y,z;
        public Vector3Int(int x,int y,int z=0) {this.x=x;this.y=y;this.z=z;}
        public static Vector3Int zero => new Vector3Int();
        public static Vector3Int right => new Vector3Int(1,0);
        public static Vector3Int left => new Vector3Int(-1,0);
        public static Vector3Int up => new Vector3Int(0,1);
        public static Vector3Int down => new Vector3Int(0,-1);
        public static Vector3Int operator +(Vector3Int a,Vector3Int b) => new Vector3Int(a.x+b.x,a.y+b.y,a.z+b.z);
        public static Vector3Int operator -(Vector3Int a) => new Vector3Int(-a.x,-a.y,-a.z);
        public static Vector3Int operator -(Vector3Int a,Vector3Int b) => a+-b;
        public static Vector3Int operator *(Vector3Int a,int n) => new Vector3Int(a.x*n,a.y*n,a.z*n);
        public bool Equals(Vector3Int b) => x==b.x&&y==b.y&&z==b.z;
        public override bool Equals(object b) => b is Vector3Int v&&Equals(v);
        public override int GetHashCode() => x*397^y*31^z;
        public static bool operator ==(Vector3Int a,Vector3Int b) => a.Equals(b);
        public static bool operator !=(Vector3Int a,Vector3Int b) => !a.Equals(b);
    }
    public struct BoundsInt
    {
        public int xMin,yMin,zMin,sizeX,sizeY,sizeZ;
        public BoundsInt(int x,int y,int z,int sx,int sy,int sz) { xMin=x;yMin=y;zMin=z;sizeX=sx;sizeY=sy;sizeZ=sz; }
        public int xMax=>xMin+sizeX;
        public int yMax=>yMin+sizeY;
        public IEnumerable<Vector3Int> allPositionsWithin { get {for(int x=xMin;x<xMax;x++)for(int y=yMin;y<yMax;y++)yield return new Vector3Int(x,y); } }
    }
    public static class Mathf
    {
        public static int Abs(int a)=>Math.Abs(a);
        public static float Abs(float a)=>Math.Abs(a);
        public static int Max(int a,int b)=>Math.Max(a,b);
        public static float Max(float a,float b)=>Math.Max(a,b);
        public static int Min(int a,int b)=>Math.Min(a,b);
        public static float Min(float a,float b)=>Math.Min(a,b);
        public static int Clamp(int n,int min,int max)=>Math.Min(max,Math.Max(min,n));
        public static float Clamp(float n,float min,float max)=>Math.Min(max,Math.Max(min,n));
        public static float Clamp01(float n)=>Clamp(n,0f,1f);
        public const float Infinity=float.PositiveInfinity;
    }
    public static class Random { static System.Random random=new System.Random(21); public static int Range(int a,int b)=>random.Next(a,b); }
    public static class Debug { public static void Log(object o) {} public static void LogWarning(object o) {} public static void LogError(object o) {} }
    public enum KeyCode { R }
    public static class Input { public static Vector3 mousePosition; public static bool leftDown,rightDown; public static bool GetMouseButtonDown(int i)=>i==0?leftDown:i==1&&rightDown; public static bool GetKeyDown(KeyCode k)=>false; }
    public class Camera { public static Camera main=new Camera(); public Vector3 ScreenToWorldPoint(Vector3 p)=>p; }
    public class Collider2D : Component { }
    public static class Physics2D
    {
        public static Collider2D OverlapPoint(Vector2 p) => Object.FindObjectsByType<Collider2D>(FindObjectsSortMode.None).FirstOrDefault(c=>Vector2.Distance(c.transform.position,p)<0.01f);
        public static Collider2D[] OverlapPointAll(Vector2 p) => Object.FindObjectsByType<Collider2D>(FindObjectsSortMode.None).Where(c=>Vector2.Distance(c.transform.position,p)<0.01f).ToArray();
    }
}
namespace UnityEngine.Tilemaps
{
    using UnityEngine;
    public enum TileFlags { None }
    public class TilemapRenderer : Component
    {
        public int sortingLayerID,sortingOrder;
        public Material sharedMaterial;
    }
    public class Tilemap : Component
    {
        public BoundsInt cellBounds=new BoundsInt(0,0,0,10,10,1);
        public float scale=1;
        public Vector3 cellSize => new Vector3(scale,scale,0);
        public Dictionary<Vector3Int,Sprite> sprites=new Dictionary<Vector3Int,Sprite>();
        public Dictionary<Vector3Int,Color> colors=new Dictionary<Vector3Int,Color>();
        public void CompressBounds() {}
        public bool HasTile(Vector3Int c)=>PassiveGeometry.IsOnBoard(c,cellBounds);
        public Vector3Int WorldToCell(Vector3 p) { p=transform.InverseTransformPoint(p); return new Vector3Int((int)Math.Floor(p.x/scale),(int)Math.Floor(p.y/scale)); }
        public Vector3 GetCellCenterWorld(Vector3Int c)=>transform.TransformPoint(GetCellCenterLocal(c));
        public Vector3 GetCellCenterLocal(Vector3Int c)=>new Vector3((c.x+0.5f)*scale,(c.y+0.5f)*scale,0);
        public Sprite GetSprite(Vector3Int c)=>sprites.TryGetValue(c,out var sprite)?sprite:null;
        public Color GetColor(Vector3Int c)=>colors.TryGetValue(c,out var color)?color:Color.white;
        public void SetColor(Vector3Int c,Color col) { colors[c]=col; }
        public void SetTileFlags(Vector3Int c,TileFlags flags) {}
        public void RefreshAllTiles() {}
    }
}
namespace TMPro
{
    public class TMP_FontAsset : UnityEngine.ScriptableObject { }
    public enum TextAlignmentOptions { TopLeft }
    public class TextMeshProUGUI : UnityEngine.UI.Graphic
    {
        public string text;public TMP_FontAsset font;public float fontSize,fontSizeMin,fontSizeMax;public bool enableAutoSizing;public TextAlignmentOptions alignment;
        public void SetText(string s) {text=s;}
        public UnityEngine.Vector2 GetPreferredValues(string content,float width,float height)
            =>new UnityEngine.Vector2(width,Math.Max(1,(int)Math.Ceiling(content.Length*fontSize*0.5f/width))*fontSize*1.25f);
    }
}
namespace UnityEngine.UI
{
    public class Graphic : UnityEngine.Component { public bool raycastTarget=true;public UnityEngine.Color color; }
    public class Image : Graphic { public UnityEngine.Sprite sprite; public bool enabled=true,preserveAspect; }
    public class Selectable : UnityEngine.MonoBehaviour, UnityEngine.EventSystems.IPointerDownHandler
    { public void OnPointerDown(UnityEngine.EventSystems.PointerEventData eventData) { } }
    public class GraphicRaycaster : UnityEngine.EventSystems.BaseRaycaster { }
}
namespace UnityEngine.TextCore.Text { }
namespace UnityEngine.EventSystems
{
    public class BaseRaycaster { }
    public class PhysicsRaycaster : BaseRaycaster { }
    public interface IEventSystemHandler { }
    public interface IPointerDownHandler : IEventSystemHandler { void OnPointerDown(PointerEventData eventData); }
    public struct RaycastResult { public BaseRaycaster module; public UnityEngine.GameObject gameObject; }
    public class PointerEventData { public UnityEngine.Vector2 position; public PointerEventData(EventSystem events) { } }
    public class EventSystem
    {
        public static EventSystem current; public bool pointerOverUI;
        public List<RaycastResult> raycastResults;
        public void RaycastAll(PointerEventData pointer, List<RaycastResult> hits)
        {
            if (raycastResults != null)
            {
                foreach (var hit in raycastResults)
                {
                    if (hit.gameObject != null && !hit.gameObject.activeInHierarchy) continue;
                    if (hit.module is UnityEngine.UI.GraphicRaycaster && hit.gameObject != null &&
                        hit.gameObject.GetComponent<UnityEngine.UI.Graphic>()?.raycastTarget != true) continue;
                    hits.Add(hit);
                }
                return;
            }
            hits.Add(new RaycastResult { module = pointerOverUI ? (BaseRaycaster)new UnityEngine.UI.GraphicRaycaster() : new PhysicsRaycaster() });
        }
    }
}
namespace JetBrains.Annotations { }

