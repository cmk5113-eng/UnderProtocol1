using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static partial class Scenarios
{
    static MonsterBase LargeEnemy(int x,int y,int hp=8,int width=2,int height=2,bool fit=false)
    {
        var enemy=Enemy(x,y,hp);
        if(fit) enemy.gameObject.Attach(new SpriteRenderer { sprite=new Sprite() });
        enemy.Initialize(new MonsterData { hp=hp,footprintSize=new Vector2Int(width,height),fitSpriteToFootprint=fit });
        Check(enemy.TryPlace(map,C(x,y)),"large enemy footprint could not be placed");
        return enemy;
    }
    static SkillList QuadAttack(int damage=1,int push=0)
    {
        var skill=new SkillList { type=SkillType.Normal,range=20,effectType=SkillEffectType.Damage|SkillEffectType.Push };
        foreach(var offset in new[]{new Vector2Int(0,0),new Vector2Int(1,0),new Vector2Int(0,1),new Vector2Int(1,1)})
            skill.roePattern.Add(new SkillPatternTile { position=offset,damage=damage,push=push>0,
                pushDistance=push,pushDirection=SkillPushDirection.Forward });
        return skill;
    }
    static void RunMultiCellMonsterScenarios()
    {
        Case("2x2 monster owns four cells but deals shared HP damage only once",()=>{
            var e=LargeEnemy(4,4,8);
            foreach(var cell in e.GetOccupiedCells(map))
                Check(PlacementManager.Instance.GetTileData(cell).Character==e&&!PlacementManager.Instance.GetTileData(cell).isempty,
                    "body cell was not occupied by the same monster");
            Check(e.GetAnchorCell(map)==C(4,4)&&map.WorldToCell(e.transform.position)==C(5,5),"visual center replaced the stored anchor");
            battle.StartMonsterTurn();Check(BattleManager.HP==92,"large monster attacked once for every body tile");
        });
        Case("every body corner is targetable without a collider at that cell",()=>{
            var p=Player(0,4);var e=LargeEnemy(4,4,8);
            foreach(var cell in new[]{C(4,4),C(4,5),C(5,4),C(5,5)})
            {
                p.actionPoint=1;int before=e.currentHP;Cast(p,cell.x,cell.y);
                Check(e.currentHP==before-1,"clicking a body tile did not hit its owner");
            }
            Check(e.currentHP==4,"body-tile attacks used separate HP pools");
        });
        Case("overlapping ROE uses maximum damage once regardless of tile order",()=>{
            var p=Player(0,4,"Kang");var e=LargeEnemy(4,4,8);ScrollUI.Instance=new ScrollUI();
            var skill=QuadAttack();skill.roePattern[1].damage=3;skill.roePattern[2].damage=2;skill.roePattern[3].damage=0;
            Cast(p,4,4,skill);Check(e.currentHP==5&&Math.Abs(ScrollUI.Instance.gauge-0.02f)<0.0001f,"ROE summed body hits or granted four hit rewards");
            skill.roePattern.Reverse();p.actionPoint=1;Cast(p,4,4,skill);
            Check(e.currentHP==2,"overlapping ROE damage depended on the first tile");
            p.actionPoint=1;p.steminaPoint=0;Cast(p,4,4,skill);
            Check(e.IsDead&&p.steminaPoint==1,"large monster death counted as several kills");
            foreach(var cell in new[]{C(4,4),C(4,5),C(5,4),C(5,5)})
                Check(PlacementManager.Instance.GetTileData(cell).Character==null&&PlacementManager.Instance.GetTileData(cell).isempty,"death retained body occupancy");
        });
        Case("legacy duplicate target list also produces one hit and one kill",()=>{
            var p=Player(0,4);var e=LargeEnemy(4,4,1);
            var result=executor.Execute(p,Attack(),new List<CharacterBase>{e,e,e,e});
            Check(result.hitCount==1&&result.killCount==1&&result.damagedTargets.Count==1,"legacy list treated body cells as distinct targets");
        });
        Case("multi-cell push checks the full leading edge and allows self overlap",()=>{
            var e=LargeEnemy(4,4);var blocker=Enemy(7,5,4);
            Check(executor.TryPush(e,Vector3Int.right,3)==1&&e.GetAnchorCell(map)==C(5,4),"push ignored a blocker outside the anchor row");
            Check(PlacementManager.Instance.GetTileData(C(4,4)).Character==null&&PlacementManager.Instance.GetTileData(C(4,5)).Character==null,
                "push left its trailing edge occupied");
            foreach(var cell in e.GetOccupiedCells(map)) Check(PlacementManager.Instance.GetTileData(cell).Character==e,"push lost overlapping/new body cells");
            Check(PlacementManager.Instance.GetTileData(C(7,5)).Character==blocker,"push overwrote the blocking monster");
            Check(executor.TryPush(e,Vector3Int.down,20)==4&&e.GetAnchorCell(map)==C(5,0),"push crossed the map boundary");
            Check(executor.TryPush(e,Vector3Int.down,1)==0,"blocked push still moved the footprint");
        });
        Case("placement failure keeps the previous full footprint intact",()=>{
            var e=LargeEnemy(4,4);Vector3 position=e.transform.position;
            Check(!e.TryPlace(map,C(9,9))&&e.GetAnchorCell(map)==C(4,4),"invalid placement changed the anchor");
            Check(e.transform.position.x==position.x&&e.transform.position.y==position.y,"invalid placement moved the visual");
            PlacementManager.Instance.GetTileData(C(7,5)).isempty=false;
            Check(!e.TryPlace(map,C(6,4)),"large monster ignored an obstacle on a non-anchor cell");
            foreach(var cell in e.GetOccupiedCells(map)) Check(PlacementManager.Instance.GetTileData(cell).Character==e,"failed placement partially released old cells");
        });
        Case("support attack and bomb distance use the closest body cell",()=>{
            var p=Player(0,4,"Lee");var large=LargeEnemy(1,4,8,5,1);var small=Enemy(1,5,4);
            Cast(p,8,8);Check(large.currentHP==7&&small.currentHP==4,"nearest support target used its visual center");
            p.Data.staticpassive=Passive("Jo");p.actionPoint=1;Cast(p,1,4);Tick(1);
            Check(large.currentHP==5,"bomb did not hit an edge body cell or ticked several times");
        });
        Case("front passive and survivor followup cover edge cells",()=>{
            var p=Player(0,4,"Do");var e=LargeEnemy(1,3,8);Cast(p,8,8);
            Check(e.currentHP==7,"front passive missed the body cell directly ahead");
            Setup();p=Player(0,4,"Beak");e=LargeEnemy(4,4,8);Cast(p,4,4,QuadAttack(1,1));
            Check(e.GetAnchorCell(map)==C(5,4)&&e.currentHP==6,"partly overlapping survivor lost its one followup hit");
            Setup();p=Player(0,4,"Beak");e=LargeEnemy(4,4,8);Cast(p,4,4,QuadAttack(1,2));
            Check(e.GetAnchorCell(map)==C(6,4)&&e.currentHP==7,"fully pushed-out survivor still got a followup");
        });
        Case("large body fields apply maximum strength once per effect type",()=>{
            var p=Player(0,4);var e=LargeEnemy(4,4,10);
            battle.Fields.Apply(map,C(4,4),p,SkillTileFieldEffectType.Fire,1,1,Vector3Int.right);
            battle.Fields.Apply(map,C(4,5),p,SkillTileFieldEffectType.Fire,3,1,Vector3Int.right);
            battle.Fields.Apply(map,C(5,4),p,SkillTileFieldEffectType.Electric,2,1,Vector3Int.right);
            battle.StartMonsterTurn();Check(e.currentHP==5&&BattleManager.HP==95,"fields stacked the same type or missed non-center body cells");
            Check(battle.Fields.Count==0,"unused/weaker body fields did not consume duration");
            Setup();p=Player(0,4);e=LargeEnemy(4,4);
            battle.Fields.Apply(map,C(4,4),p,SkillTileFieldEffectType.Wind,1,1,Vector3Int.right);
            battle.Fields.Apply(map,C(5,4),p,SkillTileFieldEffectType.Wind,2,1,Vector3Int.right);
            battle.StartMonsterTurn();Check(e.GetAnchorCell(map)==C(6,4),"wind pushed once per body tile instead of once per type");
        });
        Case("sprite fitting follows shifted scaled maps without accumulating scale",()=>{
            map.transform.position=new Vector3(27,-30);map.scale=2.8f;var e=LargeEnemy(4,4,8,2,2,true);
            var renderer=e.GetComponent<SpriteRenderer>();
            Check(Math.Abs(renderer.bounds.size.x-5.6f)<0.0001f&&Math.Abs(renderer.bounds.size.y-5.6f)<0.0001f,"visual did not fit four scaled tiles");
            Check(e.TryPlace(map,C(4,4))&&Math.Abs(renderer.bounds.size.x-5.6f)<0.0001f,"repositioning multiplied visual scale again");
            e.Initialize(new MonsterData{hp=2});Check(e.TryPlace(map,C(7,7))&&e.FootprintSize==new Vector2Int(1,1),"reinitialization retained old footprint");
            Check(renderer.transform.localScale.x==1&&renderer.transform.localScale.y==1,"reinitialization retained stretched visual scale");
        });
        Case("disabled or moved-to-another-map monster releases only its own cells",()=>{
            var e=LargeEnemy(4,4);var oldTile=PlacementManager.Instance.GetTileData(C(4,4));
            PlacementManager.Instance=new PlacementManager{tilemap=new UnityEngine.Tilemaps.Tilemap()};
            var other=Enemy(4,4,3);e.ReleaseOccupancy();
            Check(oldTile.Character==null&&PlacementManager.Instance.GetTileData(C(4,4)).Character==other,"old footprint cleanup erased a different map's unit");
            Setup();e=LargeEnemy(4,4);e.gameObject.SetActive(false);Call(e,"OnDisable");
            Check(PlacementManager.Instance.GetTileData(C(4,5)).Character==null&&BattleTileOccupancy.FindAt(map,C(4,5))==null,
                "disabled monster left occupied or targetable body cells");
        });
        Case("sample 2x2 asset references the new prefab and keeps default monsters single-cell",()=>{
            const string root="Assets/1.Datas/Original/";
            string asset=File.ReadAllText(root+"ScriptableObjects/Enemy/LargeMonster_2x2.asset");
            string meta=File.ReadAllText(root+"Prefabs/Globals/GameObject/Enemies/LargeMonster_2x2.prefab.meta");
            Check(asset.Contains("id: 1000")&&asset.Contains("footprintSize: {x: 2, y: 2}")&&asset.Contains("hp: 8"),"sample data not configured");
            Check(meta.Contains("guid: c7ecb6f5f4df4eb5aa32e199cead119f")&&asset.Contains("guid: c7ecb6f5f4df4eb5aa32e199cead119f"),"sample prefab reference not wired");
            Check(new MonsterData().FootprintSize==new Vector2Int(1,1),"existing monsters changed size by default");
        });
    }
}
