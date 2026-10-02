// Test-only API doubles. Production serialization DTOs and save/restore logic are compiled unchanged.
using System;
using System.Collections.Generic;
using System.Text.Json;
namespace UnityEngine
{
    public static class PlayerPrefs
    {
        private static readonly Dictionary<string,object> values = new Dictionary<string,object>();
        public static bool HasKey(string key) => values.ContainsKey(key);
        public static string GetString(string key) => values.TryGetValue(key,out var v) ? v as string : string.Empty;
        public static int GetInt(string key,int fallback=0) => values.TryGetValue(key,out var v) && v is int n ? n : fallback;
        public static void SetString(string key,string value) => values[key]=value;
        public static void SetInt(string key,int value) => values[key]=value;
        public static void Save() { }
        public static void DeleteAll() => values.Clear();
    }
    public static class Resources
    {
        private static readonly Dictionary<string,Object> assets = new Dictionary<string,Object>();
        public static T Load<T>(string name) where T : Object => assets.TryGetValue(name,out var asset) ? asset as T : null;
        public static void SetForTest(string name,Object asset) => assets[name]=asset;
    }
    public static class JsonUtility
    {
        // Field-only JSON substitute; Unity's native JsonUtility import is still a Play Mode check.
        private static readonly JsonSerializerOptions options = new JsonSerializerOptions { IncludeFields=true };
        public static T FromJson<T>(string json) => JsonSerializer.Deserialize<T>(json,options);
        public static string ToJson(object value) => JsonSerializer.Serialize(value,value.GetType(),options);
    }
}
namespace UnityEngine.UI
{
    public class Button : UnityEngine.Component
    {
        public readonly ClickEvent onClick = new ClickEvent();
        public class ClickEvent { private event Action actions; public void AddListener(Action a)=>actions+=a; public void Invoke()=>actions?.Invoke(); }
    }
}
public class UI_ScreenBase : UIBase { }
public class SkillLoadButton
{
    public enum SkillGroupType { Active1,Active2,Passive1,Passive2,Passive3,Passive4 }
}
public class CanvasManager : UnityEngine.MonoBehaviour
{
    public static CanvasManager Instance;
    public SkillLoadButton.SkillGroupType CurrentSkillGroup;
}
