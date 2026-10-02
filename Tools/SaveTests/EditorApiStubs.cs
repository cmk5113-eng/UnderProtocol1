// Compile checks for the Editor catalog builder; not a substitute for Unity AssetDatabase import.
using System;
using UnityEngine;
namespace UnityEditor
{
    [AttributeUsage(AttributeTargets.Class)] public class InitializeOnLoadAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Method)] public class MenuItem : Attribute { public MenuItem(string path) { } }
    public enum PlayModeStateChange { EnteredEditMode }
    public static class EditorApplication
    {
        public static bool isPlayingOrWillChangePlaymode,isCompiling,isUpdating;
        public static event Action<PlayModeStateChange> playModeStateChanged;
        public static event Action delayCall;
    }
    public static class AssetDatabase
    {
        public static string[] FindAssets(string filter,string[] folders)=>Array.Empty<string>();
        public static T LoadAssetAtPath<T>(string path) where T : UnityEngine.Object => null;
        public static string GUIDToAssetPath(string guid)=>guid;
        public static string GetAssetPath(UnityEngine.Object asset)=>asset.name;
        public static string AssetPathToGUID(string path)=>path;
        public static bool IsValidFolder(string path)=>true;
        public static void CreateFolder(string parent,string name) { }
        public static void CreateAsset(UnityEngine.Object asset,string path) { }
        public static void SaveAssets() { }
    }
    public static class EditorUtility { public static void SetDirty(UnityEngine.Object asset) { } }
    public class AssetPostprocessor { }
}
namespace UnityEditor.Build.Reporting { public class BuildReport { } }
namespace UnityEditor.Build
{
    public interface IPreprocessBuildWithReport { int callbackOrder { get; } void OnPreprocessBuild(UnityEditor.Build.Reporting.BuildReport report); }
    public class BuildFailedException : Exception { public BuildFailedException(string message):base(message) { } }
}
