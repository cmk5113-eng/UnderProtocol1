using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static partial class Scenarios
{
    static MonsterBase ShieldedEnemy(int x,int y,int hp=8,int width=2,int height=2)
    {
        var e=Enemy(x,y,hp);
        e.Initialize(new MonsterData { hp=hp,footprintSize=new Vector2Int(width,height),hasShield=true });
        Check(e.TryPlace(map,C(x,y)),"shielded enemy could not be placed");
        return e;
    }

    static void RunCellShieldScenarios()
    {
        Case("cell shields absorb one whole attack and preserve other body cells",()=>{
            var p=Player(0,4);var e=ShieldedEnemy(4,4);var skill=Attack();skill.damage=3;
            Check(e.ShieldCount==4&&e.ShieldCapacity==4,"2x2 monster did not start with four shields");
            Cast(p,5,5,skill);
            Check(e.currentHP==8&&e.ShieldCount==3&&!e.HasCellShield(map,C(5,5)),"shield leaked damage or broke the wrong body cell");
            Check(e.HasCellShield(map,C(4,4))&&e.HasCellShield(map,C(4,5))&&e.HasCellShield(map,C(5,4)),"hit broke unrelated shields");
            p.actionPoint=1;Cast(p,5,5,skill);
            Check(e.currentHP==5&&e.ShieldCount==3,"a previously exposed body cell did not take HP damage");
            p.actionPoint=1;Cast(p,4,4,skill);
            Check(e.currentHP==5&&e.ShieldCount==2,"another body shield shared the first cell's broken state");
        });
        Case("shield state belongs to each spawn instead of shared monster data",()=>{
            var data=new MonsterData { hp=8,hasShield=true,footprintSize=new Vector2Int(2,2) };
            var a=Enemy(2,2,8);var b=Enemy(6,6,8);a.Initialize(data);b.Initialize(data);
            Check(a.TryPlace(map,C(2,2))&&b.TryPlace(map,C(6,6)),"shared data fixture did not place");
            a.TakeDamageAtCell(map,C(2,3),1);
            Check(a.ShieldCount==3&&b.ShieldCount==4&&b.HasCellShield(map,C(6,7)),"one spawn changed another spawn's shields");
            Check(data.hasShield&&data.footprintSize==new Vector2Int(2,2),"damage mutated authored monster data");
        });
        Case("area attacks break contacted shields without multiplying HP damage or rewards",()=>{
            var p=Player(0,4,"Kang");var e=ShieldedEnemy(4,4);ScrollUI.Instance=new ScrollUI();
            Cast(p,4,4,QuadAttack(3));
            Check(e.currentHP==8&&e.ShieldCount==0&&!e.IsDead,"area attack damaged freshly shielded cells");
            Check(Math.Abs(ScrollUI.Instance.gauge-0.02f)<0.0001f&&p.steminaPoint==1,"shield hits multiplied hit or kill rewards");
            p.actionPoint=1;Cast(p,4,4,QuadAttack(3));
            Check(e.currentHP==5,"exposed body area damage stacked four times");
            e.currentHP=2;p.actionPoint=1;p.steminaPoint=0;Cast(p,4,4,QuadAttack(3));
            Check(e.IsDead&&p.steminaPoint==1&&e.ShieldCount==0,"whole-body death did not count exactly one kill");
            foreach(var cell in new[]{C(4,4),C(4,5),C(5,4),C(5,5)})
                Check(PlacementManager.Instance.GetTileData(cell).isempty,"shielded monster death retained body occupancy");
        });
        Case("mixed exposed and shielded ROE cells use only exposed damage regardless of order",()=>{
            foreach(bool reverse in new[]{false,true})
            {
                Setup();var p=Player(0,4);var e=ShieldedEnemy(4,4);
                e.TakeDamageAtCell(map,C(4,4),1);var skill=QuadAttack(3);skill.roePattern[0].damage=1;
                if(reverse)skill.roePattern.Reverse();Cast(p,4,4,skill);
                Check(e.currentHP==7&&e.ShieldCount==0,"strong shielded tiles contributed HP damage or depended on hit order");
            }
        });
        Case("duplicate ROE contacts break a cell shield once while zero damage and push keep it",()=>{
            var p=Player(0,4);var e=ShieldedEnemy(4,4);var skill=QuadAttack(3);
            var hit=new SkillTargetHit(e,skill.roePattern[0],C(4,4));
            var result=executor.ExecutePattern(p,skill,C(4,4),new List<SkillTargetHit>{hit,hit,hit});
            Check(e.currentHP==8&&e.ShieldCount==3&&result.hitCount==1&&result.damagedTargets.Count==0,
                "duplicate hit penetrated a shield in the same cast");
            e.TakeDamageAtCell(map,C(4,5),0);e.TakeDamageAtCell(map,C(4,5),-3);
            Check(e.HasCellShield(map,C(4,5)),"zero or negative damage consumed a shield");
            var push=QuadAttack(0,1);push.effectType=SkillEffectType.Push;Cast(p,4,4,push);
            Check(e.GetAnchorCell(map)==C(5,4)&&e.ShieldCount==3&&e.currentHP==8,"push-only skill consumed a shield or damaged HP");
        });
        Case("body-local shield damage survives pushing and failed placement without regeneration",()=>{
            var e=ShieldedEnemy(4,4);e.TakeDamageAtCell(map,C(5,5),1);
            Check(executor.TryPush(e,Vector3Int.right,1)==1&&!e.HasCellShield(map,C(6,5))&&e.HasCellShield(map,C(5,4)),
                "moving lost the broken shield's body offset");
            Check(!e.TryPlace(map,C(9,9))&&e.ShieldCount==3&&!e.HasCellShield(map,C(6,5)),"failed placement regenerated or moved shields");
            Check(e.TryPlace(map,C(5,4))&&e.ShieldCount==3,"same-position placement regenerated shields");
            e.TakeDamageAtCell(map,C(6,5),3);Check(e.currentHP==5,"moved exposed body cell became shielded");
            e.Initialize(e.MonsterData);Check(e.TryPlace(map,C(5,4))&&e.ShieldCount==4&&e.currentHP==8,"new spawn did not restore shields and HP");
            e.gameObject.SetActive(false);Call(e,"OnDisable");Check(e.ShieldCount==0,"disabled spawn retained runtime shields");
        });
        Case("real targeting rotations and translated scaled maps hit the correct shield cell",()=>{
            var pivots=new[]{C(4,4),C(4,4),C(5,4),C(5,5)};
            var expected=new[]{C(5,4),C(4,5),C(4,4),C(5,4)};
            for(int rotation=0;rotation<4;rotation++)
            {
                Setup();map.transform.position=new Vector3(27,-30);map.scale=2.8f;
                var p=Player(0,4);var e=ShieldedEnemy(4,4);var skill=FieldSkill(SkillTileFieldEffectType.None);
                skill.effectType=SkillEffectType.Damage;skill.roePattern[0].position=new Vector2Int(1,0);skill.roePattern[0].damage=3;
                SelectionManager.CharacterBase=p;targeting.StartSkillTargeting(skill,p);
                for(int i=0;i<rotation;i++)targeting.RotatePatternClockwise();
                Input.mousePosition=map.GetCellCenterWorld(pivots[rotation]);Call(targeting,"HandleRealtimeAoE");targeting.ExecuteSkillOnTarget();
                Check(e.currentHP==8&&e.ShieldCount==3&&!e.HasCellShield(map,expected[rotation]),"rotation stripped the wrong shield at "+rotation);
            }
        });
        Case("legacy area damage forwards contacted cells and direct fallback respects shields",()=>{
            var p=Player(0,4);var e=ShieldedEnemy(4,4);var skill=Attack();skill.damage=3;skill.aoe=1;
            Cast(p,4,4,skill);
            Check(e.currentHP==8&&e.ShieldCount<4&&e.ShieldCount>0,"legacy AOE lost impact cells or broke the entire body");
            Setup();e=ShieldedEnemy(4,4);CharacterBase target=e;target.TakeDamage(3);
            Check(e.currentHP==8&&e.ShieldCount==3,"polymorphic damage bypassed the fallback shield");
            target.TakeDamage(3);Check(e.currentHP==5&&e.ShieldCount==3,"fallback jumped to an unhit shield instead of the same body cell");
            Check(e.TakeDamageAtCell(map,C(1,1),3)==0&&e.currentHP==5,"off-body damage hit HP");
            var otherMap=new UnityEngine.Tilemaps.Tilemap();
            Check(e.TakeDamageAtCell(otherMap,C(4,4),3)==0&&e.currentHP==5,"another map damaged this body");
        });
        Case("front and nearest support attacks contact their actual body cell",()=>{
            var p=Player(0,4,"Do");var e=ShieldedEnemy(1,3);Cast(p,8,8);
            Check(e.currentHP==8&&!e.HasCellShield(map,C(1,4))&&e.HasCellShield(map,C(1,3)),"front passive damaged HP or the anchor instead of the front cell");
            Setup();p=Player(0,4,"Lee");e=ShieldedEnemy(1,3);Cast(p,8,8);
            Check(e.currentHP==8&&!e.HasCellShield(map,C(1,4)),"nearest support bypassed an edge shield");
            p.actionPoint=1;Cast(p,8,8);Check(e.currentHP==7&&e.ShieldCount==3,"nearest support did not reuse the exposed edge cell");
            Setup();var owner=Player(0,4,"Ryu");var actor=Player(0,5);e=ShieldedEnemy(1,4);
            battle.NotifyMovementCompleted(actor,C(0,6),C(0,5));
            Check(!e.HasCellShield(map,C(1,5))&&e.HasCellShield(map,C(1,4))&&e.currentHP==8,
                "Ryu shield contact used the passive owner instead of the moved ally");
        });
        Case("shield-only attacks do not cause survivor followup to bypass the same shield",()=>{
            var p=Player(0,4,"Beak");var e=ShieldedEnemy(4,4);Cast(p,4,4,QuadAttack(1));
            Check(e.currentHP==8&&e.ShieldCount==0,"survivor followup fired after a shield-only cast");
            p.actionPoint=1;Cast(p,4,4,QuadAttack(1));
            Check(e.currentHP==6,"exposed survivor did not receive one direct hit and one followup");
        });
        Case("bombs break each shield in their radius and burn uses one closest body cell",()=>{
            var p=Player(0,4,"Jo");p.Data.staticpassive.bombRadius=1;p.Data.staticpassive.durationTurns=2;
            var e=ShieldedEnemy(4,4);var skill=Attack();skill.damage=0;Cast(p,4,4,skill);Tick(1);
            Check(e.currentHP==8&&e.ShieldCount==1&&e.HasCellShield(map,C(5,5)),"bomb ignored its radius or stacked damage through new shields");
            Tick(2);Check(e.currentHP==7&&e.ShieldCount==1,"persistent bomb did not damage exposed cells once");
            Setup();p=Player(0,4,"Namgung");p.Data.staticpassive.durationTurns=2;e=ShieldedEnemy(4,4);Cast(p,8,8);Tick(1);
            Check(e.currentHP==8&&e.ShieldCount==3&&!e.HasCellShield(map,C(4,4)),"burn bypassed shields or stripped every body cell");
            Tick(2);Check(e.currentHP==7&&e.ShieldCount==3,"burn failed to reuse its exposed contact");
        });
        Case("fields contact all occupied shields once and damage before wind movement",()=>{
            var p=Player(0,4);var e=ShieldedEnemy(4,4);
            battle.Fields.Apply(map,C(4,4),p,SkillTileFieldEffectType.Wind,1,1,Vector3Int.right);
            battle.Fields.Apply(map,C(4,5),p,SkillTileFieldEffectType.Fire,1,2,Vector3Int.right);
            battle.Fields.Apply(map,C(5,5),p,SkillTileFieldEffectType.Fire,3,2,Vector3Int.right);
            battle.StartMonsterTurn();
            Check(e.GetAnchorCell(map)==C(5,4)&&e.currentHP==8&&e.ShieldCount==2,
                "wind shifted the body before old fields could break the right shields");
            Check(!e.HasCellShield(map,C(5,5))&&!e.HasCellShield(map,C(6,5))&&e.HasCellShield(map,C(5,4)),"field shield damage lost its body offset");
            Check(FieldAt(4,5).RemainingTurns==1&&FieldAt(5,5).RemainingTurns==1,"blocked fields did not age once");
            battle.StartMonsterTurn();Check(e.currentHP==5&&e.ShieldCount==2,"next field tick missed the exposed moved body cell");
        });
        Case("field HP damage and kill credit come from the strongest exposed cell",()=>{
            var weak=Player(0,4,"Kang");var strong=Player(0,6,"Kang");weak.steminaPoint=strong.steminaPoint=0;
            var e=ShieldedEnemy(4,4,1);e.TakeDamageAtCell(map,C(4,4),1);
            battle.Fields.Apply(map,C(4,4),weak,SkillTileFieldEffectType.Fire,1,1,Vector3Int.right);
            battle.Fields.Apply(map,C(4,5),strong,SkillTileFieldEffectType.Fire,3,1,Vector3Int.right);
            battle.StartMonsterTurn();
            Check(e.IsDead&&e.ShieldCount==0&&weak.steminaPoint==2&&strong.steminaPoint==1,
                "shielded strong field stole the exposed field's kill credit or death counted twice");
        });
        Case("default and sample data explicitly distinguish shieldless and shielded monsters",()=>{
            var e=LargeEnemy(4,4);e.TakeDamageAtCell(map,C(5,5),3);
            Check(e.currentHP==5&&e.ShieldCapacity==0&&!new MonsterData().hasShield,"shieldless defaults changed");
            string sample=File.ReadAllText("Assets/1.Datas/Original/ScriptableObjects/Enemy/LargeMonster_2x2.asset");
            Check(sample.Contains("hasShield: 1")&&sample.Contains("footprintSize: {x: 2, y: 2}"),"sample did not enable one shield per body cell");
        });
    }
}
