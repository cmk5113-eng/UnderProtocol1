using System;
using System.Reflection;
using UnityEngine;

public static class SkillEditorScenarios
{
    static int checks;
    static void Check(bool value,string message)
    {checks++;if(!value)throw new Exception(message);}
    static void Call(SkillListEditor editor,string method,params object[] args)
    {typeof(SkillListEditor).GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(editor,args);}
    static SkillListEditor EditorFor(SkillList skill)
    {
        var editor=new SkillListEditor {target=skill};
        var mode=typeof(SkillListEditor).GetField("mode",BindingFlags.Instance|BindingFlags.NonPublic);
        mode.SetValue(editor,Enum.ToObject(mode.FieldType,1));return editor;
    }
    static void Click(SkillListEditor editor,SkillList skill,Vector2Int position,int button)
    {
        GUILayout.ResetClick((5-position.y)*11+position.x+5,button);
        Call(editor,"DrawGrid",skill);
    }
    public static void Main()
    {
        var skill=new SkillList();var editor=EditorFor(skill);var cell=new Vector2Int(1,2);
        foreach(int expected in new[]{1,2,3,1})
        {
            Click(editor,skill,cell,0);
            Check(skill.roePattern.Count==1&&skill.roePattern[0].damage==expected,"left click did not cycle 1,2,3,1");
        }
        skill.roePattern[0].push=true;skill.roePattern[0].pushDistance=3;
        skill.roePattern[0].fieldEffect=SkillTileFieldEffectType.Fire;
        Click(editor,skill,cell,0);
        Check(skill.roePattern[0].damage==2&&skill.roePattern[0].pushDistance==3&&skill.roePattern[0].fieldEffect==SkillTileFieldEffectType.Fire,
            "changing damage overwrote the tile's other effects");
        Click(editor,skill,cell,1);Check(skill.roePattern.Count==0,"right click did not remove the tile");
        Click(editor,skill,cell,1);
        Check(skill.roePattern.Count==1&&skill.roePattern[0].damage==1&&skill.roePattern[0].pushDistance==0,
            "right click did not create a tile with editor defaults");
        Console.WriteLine("PASS editor: real grid left-click cycle and right-click toggle");

        skill.rangePattern.Add(new Vector2Int(3,0));
        Call(editor,"ApplyPreset",skill,new[]{new Vector2Int(1,0),new Vector2Int(2,0),new Vector2Int(3,0)});
        Check(skill.roePattern.Count==3&&skill.rangePattern.Count==1,"ROE preset modified cast range or retained old cells");
        foreach(var tile in skill.roePattern)
            Check(tile.damage==1&&!tile.push&&tile.pushDistance==0&&tile.distanceFromCaster==Math.Abs(tile.position.x)+Math.Abs(tile.position.y),
                "preset imported damage or push from other data");
        Console.WriteLine("PASS editor: presets use per-tile defaults and preserve the range tab");

        GUILayout.ResetClick(125);editor.OnInspectorGUI();
        Check(skill.roePattern.Count==0,"pattern reset left ROE data");
        GUILayout.ResetClick(-1);editor.OnInspectorGUI();
        Check(skill.roePattern.Count==0&&skill.rangePattern.Count==1,"inspector recreated a cleared ROE or changed range");
        Console.WriteLine("PASS editor: reset remains empty after another Inspector draw");
        Console.WriteLine("PASS: "+checks+" editor assertions");
    }
}
