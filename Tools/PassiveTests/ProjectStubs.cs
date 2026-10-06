using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
public abstract class ManagerBase : MonoBehaviour { protected abstract IEnumerator OnConnected(GameManager gm); protected abstract void OnDisconnected(); }
public class GameManager { public static GameManager Instance=new GameManager(); public WaveManager Wave=new WaveManager(); public SaveManager Save=new SaveManager(); public static event Action<float> OnPhysicsCharacter; }
public class WaveManager { public WaveData[] selectedWaves; public WaveData currentWave; public int currentWaveIndex; }
public class WaveData {}
public class WaveSetter { public void ReturnToWorld() {} }
public class WaveLoader { public static WaveLoader Instance=new WaveLoader(); public int calls; public void NextWave() { calls++; } }
public enum UIType { Stage }
public class UIManager { public static UIBase ClaimGetUI(UIType type)=>null; public static void ClaimPopUp(string a,string b,string c) {} }
public class ScrollUI
{
    public static ScrollUI Instance;
    public class Scrollbar { public float value=1; }
    public Scrollbar HPscrollbar=new Scrollbar();
    public Scrollbar GGscrollbar=new Scrollbar();
    public float gauge;
    public void SubValue(float v) {HPscrollbar.value-=v;}
    public void PlusGaugevalue(float v) {gauge+=v;}
    public void SubGaugeValue(float v) {gauge-=v;}
}
public class SelectionManager
{
    public static SelectionManager Instance=new SelectionManager();
    public static CharacterBase CharacterBase;
    public static CharacterData _characterData;
    public List<CharacterBase> unitOnStage=new List<CharacterBase>();
    public static void SelectCharacter(CharacterBase p) { CharacterBase=p; }
}
public class PlacementManager
{
    public static PlacementManager Instance;
    public Tilemap tilemap;
    public Dictionary<Vector3Int,TileData> tiles=new Dictionary<Vector3Int,TileData>();
    public TileData GetTileData(Vector3Int c) { if(!tiles.TryGetValue(c,out var d)) tiles[c]=d=new TileData { Type=IsOuterTile(c)?TileData.tiletype.outside:TileData.tiletype.inside }; return d; }
    public bool IsOuterTile(Vector3Int c)=>tilemap.HasTile(c)&&PassiveGeometry.IsOnEdge(c,tilemap.cellBounds);
    public Vector2Int ConvertToCustomPosition(Vector3Int c)=>new Vector2Int(c.x-tilemap.cellBounds.xMin+1,tilemap.cellBounds.yMax-c.y);
}
public class PlacementController { public static void RemoveAllObject() { BattleManager.Instance.ResetBattle(); } }
public static class ObjectManager { public static void DestroyObject(GameObject go)=>go.SetActive(false); }
public class ControllerBase : MonoBehaviour {}
public interface IRunnable {}
public static class InputManager
{
    public static event Action<bool,Vector2,Vector3> OnMouseLeftButton,OnMouseRightButton;
    public static event Action<GameObject,GameObject> OnMouseHover;
    public static event Action<Vector2,Vector3> OnMouseMove;
    public static Vector3 CursorWorldPosition { get; private set; }
    public static GameObject CursorHoverObject { get; private set; }
    public static void SetCursor(Vector3 position,GameObject target=null)
    {
        var old=CursorHoverObject; CursorWorldPosition=position; CursorHoverObject=target;
        OnMouseHover?.Invoke(target,old); OnMouseMove?.Invoke(position,position);
    }
    public static void ResetHover() { OnMouseHover=null; OnMouseMove=null; CursorWorldPosition=new Vector3(); CursorHoverObject=null; }
}
public class ActiveSkill : SkillList {}
public class NormalSkill : SkillList {}
public class LinkSkill : SkillList {}
public class UltimateSkill : SkillList {}
