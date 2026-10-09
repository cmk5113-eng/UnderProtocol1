using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public static partial class Scenarios
{
    static SkillList FieldSkill(SkillTileFieldEffectType type, int value=1, int turns=3)
        => new SkillList { type=SkillType.Active, range=20, canRotate=true,
            roePattern=new List<SkillPatternTile> { new SkillPatternTile {
                position=new Vector2Int(0,0), fieldEffect=type,
                fieldEffectValue=value, fieldEffectDuration=turns } } };

    static BattleFieldEffect FieldAt(int x,int y)
    {
        battle.Fields.TryGetField(map,C(x,y),out var field);
        return field;
    }

    static bool SameColor(Color a,Color b) => a.r==b.r&&a.g==b.g&&a.b==b.b&&a.a==b.a;

    static void RunFieldEffectScenarios()
    {
        Case("empty-tile field cast, later occupant, damage ordering and expiry",()=>{
            var p=Player(0,4);var skill=FieldSkill(SkillTileFieldEffectType.Fire,2,2);
            ScrollUI.Instance=new ScrollUI();Cast(p,5,5,skill);
            Check(p.actionPoint==0&&battle.Fields.Count==1,"empty field cast was not committed");
            Check(FieldAt(5,5).Value==2&&FieldAt(5,5).RemainingTurns==2,"field settings not copied");
            Check(PlacementManager.Instance.GetTileData(C(5,5)).isempty,"field blocked an empty tile");
            Check(ScrollUI.Instance.gauge==0,"planting a field counted as enemy damage");
            var e=Enemy(5,5,5);battle.StartMonsterTurn();
            Check(e.currentHP==3&&BattleManager.HP==97,"field must damage before the enemy subtracts remaining HP");
            Check(FieldAt(5,5).RemainingTurns==1,"field duration did not advance once");
            Check(skill.roePattern[0].fieldEffectDuration==2,"runtime mutated the skill asset");
            battle.StartMonsterTurn();
            Check(e.currentHP==1&&BattleManager.HP==96&&battle.Fields.Count==0,"field did not tick exactly twice and expire");
            Check(SameColor(map.GetColor(C(5,5)),Color.white),"expired field left its tile tinted");
            battle.StartMonsterTurn();Check(e.currentHP==1&&BattleManager.HP==95,"expired field still dealt damage");
        });
        Case("field installation follows all four real targeting rotations and shifted map coordinates",()=>{
            map.transform.position=new Vector3(27,-30);map.scale=2.8f;
            var p=Player(0,4);var skill=FieldSkill(SkillTileFieldEffectType.Electric);
            skill.roePattern[0].position=new Vector2Int(1,2);
            var expected=new[]{C(6,7),C(3,6),C(4,3),C(7,4)};
            for(int rotation=0;rotation<4;rotation++)
            {
                battle.BeginBattle(0,null);p.actionPoint=1;SelectionManager.CharacterBase=p;
                targeting.StartSkillTargeting(skill,p);
                for(int i=0;i<rotation;i++) targeting.RotatePatternClockwise();
                Input.mousePosition=map.GetCellCenterWorld(C(5,5));Call(targeting,"HandleRealtimeAoE");
                targeting.ExecuteSkillOnTarget();
                Check(battle.Fields.TryGetField(map,expected[rotation],out var field)&&field.Type==SkillTileFieldEffectType.Electric,
                    "field did not match the rotated preview at rotation "+rotation);
                Check(battle.Fields.Count==1&&p.actionPoint==0,"rotated cast duplicated fields or kept AP");
            }
        });
        Case("field remains on the ROE cell when the original target dies",()=>{
            var p=Player(0,4);var e=Enemy(5,5,1);var skill=FieldSkill(SkillTileFieldEffectType.Fire);
            skill.effectType=SkillEffectType.Damage;skill.roePattern[0].damage=1;
            Cast(p,5,5,skill);
            Check(e.IsDead&&FieldAt(5,5)!=null,"killing the target prevented field installation");
            var replacement=Enemy(5,5,3);battle.StartMonsterTurn();
            Check(replacement.currentHP==2,"field was bound to the dead enemy instead of its cell");
        });
        Case("same cell recast replaces type and refreshes without stacking",()=>{
            var p=Player(0,4);var e=Enemy(5,5,8);Cast(p,5,5,FieldSkill(SkillTileFieldEffectType.Fire,1,3));
            p.actionPoint=1;Cast(p,5,5,FieldSkill(SkillTileFieldEffectType.Electric,2,1));
            Check(battle.Fields.Count==1&&FieldAt(5,5).Type==SkillTileFieldEffectType.Electric&&FieldAt(5,5).RemainingTurns==1,
                "recast did not replace the field settings");
            battle.StartMonsterTurn();
            Check(e.currentHP==6&&battle.Fields.Count==0,"overlapping fields stacked or retained the old duration");
        });
        Case("ice blocks only its configured enemy turns including the expiry turn",()=>{
            var p=Player(0,4);var e=Enemy(5,5,5);Cast(p,5,5,FieldSkill(SkillTileFieldEffectType.Ice,1,1));
            battle.StartMonsterTurn();
            Check(e.currentHP==5&&BattleManager.HP==100&&battle.Fields.Count==0,"ice did not block the last active turn");
            battle.StartMonsterTurn();Check(BattleManager.HP==95,"expired ice permanently disabled an enemy");
        });
        Case("earth and dark reduce shared HP damage and cannot heal the barrier",()=>{
            foreach(var type in new[]{SkillTileFieldEffectType.Earth,SkillTileFieldEffectType.Dark})
            {
                Setup();var p=Player(0,4);var e=Enemy(5,5,5);ScrollUI.Instance=new ScrollUI();
                Cast(p,5,5,FieldSkill(type,2,1));battle.StartMonsterTurn();
                Check(e.currentHP==5&&BattleManager.HP==97&&Math.Abs(ScrollUI.Instance.HPscrollbar.value-0.97f)<0.0001f,
                    type+" did not reduce both shared HP and its UI by the same amount");
                Cast(p,5,5,FieldSkill(type,20,1));battle.StartMonsterTurn();
                Check(BattleManager.HP==97&&e.currentHP==5,"damage reduction above HP healed or damaged the enemy");
                battle.StartMonsterTurn();Check(BattleManager.HP==92,"damage reduction remained after expiry");
            }
        });
        Case("wind uses its own direction, respects occupancy and does not chain fields in one turn",()=>{
            var p=Player(0,4);var e=Enemy(5,5,8);Enemy(5,7,4);
            battle.Fields.Apply(map,C(5,6),p,SkillTileFieldEffectType.Fire,1,3,Vector3Int.right);
            var skill=FieldSkill(SkillTileFieldEffectType.Wind,2,3);
            skill.roePattern[0].fieldPushDirection=SkillPushDirection.Forward;
            SelectionManager.CharacterBase=p;targeting.StartSkillTargeting(skill,p);targeting.RotatePatternClockwise();
            Input.mousePosition=map.GetCellCenterWorld(C(5,5));Call(targeting,"HandleRealtimeAoE");targeting.ExecuteSkillOnTarget();
            battle.StartMonsterTurn();
            Check(map.WorldToCell(e.transform.position)==C(5,6),"wind did not stop before the occupied cell");
            Check(PlacementManager.Instance.GetTileData(C(5,5)).Character==null&&PlacementManager.Instance.GetTileData(C(5,6)).Character==e,
                "wind left incorrect tile occupancy");
            Check(e.currentHP==8&&FieldAt(5,5)!=null,"wind chained the destination field or moved its own field");
            battle.StartMonsterTurn();Check(e.currentHP==7,"enemy standing on the destination field did not tick next turn");
        });
        Case("field ticks once per turn and never damage allies or inactive enemies",()=>{
            var p=Player(0,4);var e=Enemy(5,5,5);var inactive=Enemy(6,6,5);inactive.gameObject.SetActive(false);
            foreach(var cell in new[]{C(5,5),C(6,6),C(0,4)})
                battle.Fields.Apply(map,cell,p,SkillTileFieldEffectType.Fire,1,2,Vector3Int.right);
            battle.Fields.BeforeMonsterTurn(1,Actors(),map,executor,battle.Passives);
            battle.Fields.BeforeMonsterTurn(1,Actors(),map,executor,battle.Passives);
            Check(e.currentHP==4&&inactive.currentHP==5&&p.currentHP==0&&p.actionPoint==1,"duplicate tick or friendly/inactive damage");
            Check(FieldAt(5,5).RemainingTurns==1&&FieldAt(0,4).RemainingTurns==1,"duration did not progress once on empty/friendly fields");
            battle.Fields.BeforeMonsterTurn(2,Actors(),map,executor,battle.Passives);
            Check(e.currentHP==3&&battle.Fields.Count==0,"second field tick did not expire");
        });
        Case("invalid ROE cells, null tiles, z normalization and serialized defaults",()=>{
            var p=Player(0,4);var skill=FieldSkill(SkillTileFieldEffectType.Fire,0,0);
            skill.roePattern.Add(null);skill.roePattern.Add(new SkillPatternTile {position=new Vector2Int(40,40),fieldEffect=SkillTileFieldEffectType.Fire});
            skill.roePattern.Add(new SkillPatternTile {position=new Vector2Int(1,0),fieldEffect=SkillTileFieldEffectType.None});
            executor.ExecutePattern(p,skill,new Vector3Int(5,5,7));
            Check(battle.Fields.Count==1&&FieldAt(5,5).Value==1&&FieldAt(5,5).RemainingTurns==1,"invalid cells or zero settings were not handled safely");
            var defaults=new SkillPatternTile();Check(defaults.fieldEffectValue==1&&defaults.fieldEffectDuration==3,"new field defaults missing");
            battle.ResetBattle();executor.ExecutePattern(p,skill,C(5,5));
            Check(battle.Fields.Count==0,"out-of-battle execution planted fields");
        });
        Case("field tint and hover detail survive targeting and movement preview cleanup",()=>{
            var p=Player(0,4);Cast(p,5,5,FieldSkill(SkillTileFieldEffectType.Fire));
            var tint=map.GetColor(C(5,5));Check(!SameColor(tint,Color.white),"cast left no visible field tint");
            Check(BattleFieldEffectSystem.Describe(map,C(5,5)).Contains("화염")&&BattleFieldEffectSystem.Describe(map,C(5,5)).Contains("3턴"),
                "hover field detail did not contain type and remaining duration");
            p.actionPoint=1;targeting.StartSkillTargeting(Attack(),p);targeting.ClearAllHighlights();
            Check(SameColor(map.GetColor(C(5,5)),tint),"skill cleanup erased field tint");
            p.GetComponent<MoveTileModule>().TileHighlight(p.transform.position,3);p.GetComponent<MoveTileModule>().ClearTileHighlight();
            Check(SameColor(map.GetColor(C(5,5)),tint),"movement cleanup erased field tint");
            battle.ResetBattle();Check(SameColor(map.GetColor(C(5,5)),Color.white)&&BattleFieldEffectSystem.Describe(map,C(5,5))=="",
                "reset left field visuals or hover data");
        });
        Case("field kill grants Kang movement after the normal stamina reset",()=>{
            var p=Player(0,4,"Kang");var e=Enemy(5,5,2);p.steminaPoint=0;
            Cast(p,5,5,FieldSkill(SkillTileFieldEffectType.Fire,2,1));battle.StartMonsterTurn();
            Check(e.IsDead&&BattleManager.HP==100&&p.steminaPoint==p.maxStemina+1,"field kill lost movement credit or dealt damage after death");
            battle.Passives.ApplyStartOfTurnBonuses(p);Check(p.steminaPoint==2,"field kill credit was consumed twice");
        });
        Case("abort, reentry and map switch remove old runtime fields",()=>{
            var p=Player(0,4);Cast(p,5,5,FieldSkill(SkillTileFieldEffectType.Fire));
            var other=new Tilemap();Check(!battle.Fields.TryGetField(other,C(5,5),out _),"field leaked to a different map at the same coordinates");
            PlacementManager.Instance.tilemap=other;
            battle.Fields.BeforeMonsterTurn(1,Actors(),other,executor,battle.Passives);
            Check(battle.Fields.Count==0&&SameColor(map.GetColor(C(5,5)),Color.white),"map switch retained old field state or tint");
            PlacementManager.Instance.tilemap=map;p.actionPoint=1;Cast(p,5,5,FieldSkill(SkillTileFieldEffectType.Ice));
            battle.AbortBattle();Check(battle.Fields.Count==0&&!battle.IsBattleActive,"abort retained fields");
            battle.BeginBattle(0,null);p.actionPoint=1;Cast(p,5,5,FieldSkill(SkillTileFieldEffectType.Fire));
            Check(battle.Fields.Count==1,"reentry could not plant a fresh field");
            battle.BeginBattle(1,null);Check(battle.Fields.Count==0,"new battle inherited previous fields");
        });
    }
}

