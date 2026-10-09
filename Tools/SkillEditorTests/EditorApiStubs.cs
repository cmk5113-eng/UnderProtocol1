// Headless IMGUI inputs. Compile the real SkillListEditor without Unity installed.
using System;
using System.Collections.Generic;
using UnityEngine;

public class CharacterBase : MonoBehaviour { }

namespace UnityEngine
{
    public enum FontStyle { Normal, Bold }
    public class GUIStyle
    {
        public FontStyle fontStyle;
        public int fontSize;
        public GUIStyle() { }
        public GUIStyle(GUIStyle source) { }
    }
    public class GUISkin { public GUIStyle button=new GUIStyle(); }
    public static class GUI { public static GUISkin skin=new GUISkin(); }
    public class GUIContent { public GUIContent(string text,string tooltip="") { } }
    public class Event { public static Event current=new Event(); public int button; }
    public class GUILayoutOption { }
    public static class GUILayout
    {
        public static int clickIndex=-1,buttonIndex;
        public static void ResetClick(int index,int mouseButton=0)
        { clickIndex=index;buttonIndex=0;Event.current.button=mouseButton; }
        public static bool Button(string text,params GUILayoutOption[] options)=>buttonIndex++==clickIndex;
        public static bool Button(string text,GUIStyle style,params GUILayoutOption[] options)=>buttonIndex++==clickIndex;
        public static bool Toggle(bool value,string text,string style)=>value;
        public static void FlexibleSpace() { }
        public static GUILayoutOption Width(float value)=>new GUILayoutOption();
        public static GUILayoutOption Height(float value)=>new GUILayoutOption();
    }
}
namespace UnityEditor
{
    [AttributeUsage(AttributeTargets.Class)]
    public class CustomEditor : Attribute { public CustomEditor(Type type,bool children=false) { } }
    public class SerializedProperty { }
    public class SerializedObject
    {
        public void Update() { }
        public void ApplyModifiedProperties() { }
        public SerializedProperty FindProperty(string name)=>new SerializedProperty();
    }
    public class Editor
    {
        public UnityEngine.Object target;
        public SerializedObject serializedObject=new SerializedObject();
        public virtual void OnInspectorGUI() { }
    }
    public enum MessageType { Info }
    public static class EditorStyles
    {
        public static GUIStyle boldLabel=new GUIStyle(),miniLabel=new GUIStyle(),centeredGreyMiniLabel=new GUIStyle();
    }
    public static class EditorGUI
    {
        public static void BeginChangeCheck() { }
        public static bool EndChangeCheck()=>false;
        public static void BeginDisabledGroup(bool disabled) { }
        public static void EndDisabledGroup() { }
    }
    public static class EditorGUILayout
    {
        public class HorizontalScope : IDisposable { public void Dispose() { } }
        public static void Space(int space) { }
        public static void LabelField(string text,GUIStyle style) { }
        public static void HelpBox(string text,MessageType type) { }
        public static void PropertyField(SerializedProperty property,GUIContent label,bool children) { }
        public static Vector2Int Vector2IntField(string label,Vector2Int value)=>value;
        public static int IntField(string label,int value)=>value;
        public static int IntField(GUIContent label,int value)=>value;
        public static bool Toggle(string label,bool value)=>value;
        public static Enum EnumPopup(string label,Enum value)=>value;
    }
    public static class Undo { public static void RecordObject(UnityEngine.Object target,string name) { } }
    public static class EditorUtility { public static void SetDirty(UnityEngine.Object target) { } }
}
