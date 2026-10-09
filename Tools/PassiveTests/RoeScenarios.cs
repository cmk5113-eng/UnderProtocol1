using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public static partial class Scenarios
{
    static void RunRoeScenarios()
    {
        Case("sparse ROE preview and damage use only authored cells and values",()=>{
            var p=Player(0,4);var left=Enemy(3,4,9);var right=Enemy(5,4,9);
            var upper=Enemy(4,5,9);var pivot=Enemy(4,4,9);ScrollUI.Instance=new ScrollUI();
            var skill=Attack();skill.roePattern.Clear();
            skill.roePattern.Add(new SkillPatternTile {position=new Vector2Int(-1,0),damage=1});
            skill.roePattern.Add(new SkillPatternTile {position=new Vector2Int(1,0),damage=2});
            skill.roePattern.Add(new SkillPatternTile {position=new Vector2Int(0,1),damage=3});
            targeting.StartSkillTargeting(skill,p);Input.mousePosition=map.GetCellCenterWorld(C(4,4));
            Call(targeting,"HandleRealtimeAoE");
            var preview=(List<Vector3Int>)typeof(UseSkill).GetField("aoeTiles",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(targeting);
            Check(preview.Count==3&&preview.Contains(C(3,4))&&preview.Contains(C(5,4))&&preview.Contains(C(4,5))&&!preview.Contains(C(4,4)),
                "preview filled an unauthored pivot or missing ROE cell");
            targeting.ExecuteSkillOnTarget();
            Check(left.currentHP==8&&right.currentHP==7&&upper.currentHP==6&&pivot.currentHP==9,
                "executor replaced per-cell damage or filled a hole in the pattern");
            Check(p.actionPoint==0&&Math.Abs(ScrollUI.Instance.gauge-0.06f)<0.0001f,"ROE hits spent extra actions or lost hit rewards");
        });
        Case("editing ROE after preview refreshes execution and passive contact cells",()=>{
            var p=Player(0,4,"Beak");var old=Enemy(4,4,8);var updated=Enemy(5,4,8);var skill=Attack();
            targeting.StartSkillTargeting(skill,p);Input.mousePosition=map.GetCellCenterWorld(C(4,4));
            Call(targeting,"HandleRealtimeAoE");
            skill.roePattern[0].position=new Vector2Int(1,0);skill.roePattern[0].damage=3;
            targeting.ExecuteSkillOnTarget();Tick(1);
            Check(old.currentHP==8&&updated.currentHP==4,"a cached preview overrode the new ROE or excluded its survivor from the follow-up area");
        });
        Case("empty ROE cannot fall back to an attack or spend an action",()=>{
            var p=Player(0,4,"Jo");var e=Enemy(4,4,8);ScrollUI.Instance=new ScrollUI();
            var skill=Attack();skill.roePattern.Clear();Cast(p,4,4,skill);
            var result=executor.ExecutePattern(p,skill,C(4,4));
            Check(e.currentHP==8&&p.actionPoint==1&&battle.Passives.BombCount==0&&ScrollUI.Instance.gauge==0,
                "empty ROE still attacked, consumed AP, rewarded hits or invoked attack passives");
            Check(result.hitCount==0&&result.killCount==0&&ModeManager.Instance.CurrentMode==ModeManager.GameMode.Movement,
                "empty ROE returned hits or left unusable targeting active");
        });
        Case("clearing an active ROE cancels without stale preview damage",()=>{
            var p=Player(0,4);var e=Enemy(4,4,8);var skill=Attack();
            targeting.StartSkillTargeting(skill,p);Input.mousePosition=map.GetCellCenterWorld(C(4,4));
            Call(targeting,"HandleRealtimeAoE");skill.roePattern.Clear();targeting.ExecuteSkillOnTarget();
            Check(e.currentHP==8&&p.actionPoint==1&&ModeManager.Instance.CurrentMode==ModeManager.GameMode.Movement,
                "cleared ROE reused preview cells or consumed an action");
        });
        Case("executor excludes ally and missing-map contacts from authored ROE",()=>{
            var p=Player(0,4);var ally=Player(4,4);ally.currentHP=ally.MaxHP=8;var offMap=Enemy(13,13,8);
            var skill=Attack(3);skill.roePattern.Add(new SkillPatternTile {position=new Vector2Int(9,9),damage=3});
            var result=executor.ExecutePattern(p,skill,C(4,4));
            Check(ally.currentHP==8&&offMap.currentHP==8&&result.hitCount==0,"authored pattern hit an ally or nonexistent map tile");
        });
        Case("push distance comes from the contacted ROE tile",()=>{
            var p=Player(0,4);var e=Enemy(4,4,8);var skill=Attack(0);
            skill.effectType=SkillEffectType.Push;skill.roePattern[0].push=true;
            skill.roePattern[0].pushDirection=SkillPushDirection.Forward;skill.roePattern[0].pushDistance=2;
            Cast(p,4,4,skill);Check(e.currentHP==8&&map.WorldToCell(e.transform.position)==C(6,4),"per-cell push was replaced by a shared distance");
            p.actionPoint=1;skill.roePattern[0].pushDistance=0;Cast(p,6,4,skill);
            Check(map.WorldToCell(e.transform.position)==C(6,4),"zero ROE push distance used a fallback");
        });
        Case("an unconfigured ultimate does not charge gauge",()=>{
            var p=Player(0,4);SelectionManager.CharacterBase=p;
            p.Data.ultimateSkill=new UltimateSkill {range=20,effectType=SkillEffectType.Damage};
            var ui=new StageUIController();Set(ui,"currentData",p.Data);Call(ui,"Awake");
            ScrollUI.Instance=new ScrollUI();ScrollUI.Instance.gauge=1;
            targeting.UI_Ultimate();Check(ScrollUI.Instance.gauge==1&&p.actionPoint==1,"empty ultimate consumed its charge");
            p.Data.ultimateSkill.roePattern.Add(new SkillPatternTile {position=new Vector2Int(0,0),damage=3});
            var e=Enemy(4,4,8);targeting.UI_Ultimate();Input.mousePosition=map.GetCellCenterWorld(C(4,4));
            Call(targeting,"HandleRealtimeAoE");targeting.ExecuteSkillOnTarget();
            Check(ScrollUI.Instance.gauge>=0&&ScrollUI.Instance.gauge<0.1f&&e.currentHP==5&&p.actionPoint==0,
                "configured ultimate failed to use its ROE or spent the charge more than once");
        });
    }
}
