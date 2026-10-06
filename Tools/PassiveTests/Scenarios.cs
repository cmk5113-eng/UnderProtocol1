using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Tilemaps;
using UObj=UnityEngine.Object;

public static class Scenarios
{
    static int checks;
    static Tilemap map;
    static BattleManager battle;
    static ExecuteSkill executor;
    static UseSkill targeting;
    static void Check(bool condition,string message)
    { checks++; if(!condition) throw new Exception(message); }
    static void Set(object obj,string field,object value)
    { obj.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public).SetValue(obj,value); }
    static void Call(object obj,string method)
    { obj.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public).Invoke(obj,null); }
    static Vector3Int C(int x,int y)=>new Vector3Int(x,y,0);
    static void Setup()
    {
        UObj.ResetScene();
        typeof(BattleManager).GetField("instance",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,null);
        typeof(StageUIController).GetProperty("Instance").SetValue(null,null);
        SelectionManager.Instance=new SelectionManager(); SelectionManager.CharacterBase=null;
        ScrollUI.Instance=null; GameManager.Instance=new GameManager();
        ModeManager.Instance=new ModeManager(); ModeManager.Instance.CurrentMode=ModeManager.GameMode.Movement;
        map=new Tilemap(); PlacementManager.Instance=new PlacementManager { tilemap=map };
        executor=new ExecuteSkill(); Call(executor,"Awake");
        targeting=new UseSkill(); Call(targeting,"Awake");
        battle=new BattleManager(); Call(battle,"Awake"); battle.BeginBattle(0,null);
    }
    static PassiveSkill Passive(string key)
    {
        string text=File.ReadAllText("Assets/1.Datas/Original/ScriptableObjects/Globals/SkillDatas/InnatePassive/Innate_"+key+".asset");
        int N(string field)=>int.Parse(Regex.Match(text,"(?m)^  "+field+": (\\d+)").Groups[1].Value);
        return new PassiveSkill {skillName=key,type=SkillType.Passive,trigger=(PassiveTrigger)N("trigger"),passiveEffect=(PassiveEffect)N("passiveEffect"),maxActivationsPerTurn=N("maxActivationsPerTurn"),passiveDamage=N("passiveDamage"),movementPoints=N("movementPoints"),extraActions=N("extraActions"),durationTurns=N("durationTurns"),bombRadius=N("bombRadius")};
    }
    static CharacterBase Player(int x,int y,string key=null)
    {
        var p=new CharacterBase {isSpawned=true,maxAP=1,actionPoint=1,maxStemina=1,steminaPoint=1,mobility=3};
        p.transform.position=map.GetCellCenterWorld(C(x,y));
        Set(p,"characterData",new CharacterData {characterName=key??"Actor",staticpassive=key==null?null:Passive(key)});
        var movement=new MoveTileModule();p.gameObject.Attach(movement);movement.OnRegistration(p);
        var tile=PlacementManager.Instance.GetTileData(C(x,y));tile.isempty=false;tile.Character=p;
        return p;
    }
    static MonsterBase Enemy(int x,int y,int hp)
    {
        var e=new MonsterBase {isEnemy=true,currentHP=hp,MaxHP=hp};
        e.transform.position=map.GetCellCenterWorld(C(x,y));e.gameObject.Attach(new Collider2D());
        var tile=PlacementManager.Instance.GetTileData(C(x,y));tile.isempty=false;tile.Character=e;
        return e;
    }
    static SkillList Attack()=>new SkillList {effectType=SkillEffectType.Damage,type=SkillType.Normal,damage=1,range=20,aoe=0};
    static void Cast(CharacterBase p,int x,int y,SkillList skill=null)
    {
        SelectionManager.CharacterBase=p;
        targeting.StartSkillTargeting(skill??Attack(),p);
        Input.mousePosition=map.GetCellCenterWorld(C(x,y));
        if ((bool)typeof(UseSkill).GetField("isSkillTargetingActive",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(targeting))
            Call(targeting,"HandleRealtimeAoE");
        targeting.ExecuteSkillOnTarget();
    }
    static void Move(CharacterBase p,int x,int y)
    {
        var m=p.GetComponent<MoveTileModule>();m.MoveToTileDirect(C(x,y));
        for(int i=0;i<20&&m.IsMoving;i++)m.UpdateToDestination(1);
    }
    static CharacterBase[] Actors()=>UObj.FindObjectsByType<CharacterBase>(FindObjectsSortMode.InstanceID);
    static void Tick(int turn)=>battle.Passives.BeforeMonsterTurn(turn,Actors(),map);
    static void Case(string name,Action run) {Setup();run();Console.WriteLine("PASS "+name);}
    public static void Main()
    {
        Case("monster data HP survives real attacks and remains per instance",()=>{
            var p=Player(0,4);
            var data=new MonsterData {hp=5};
            var target=Enemy(5,5,1);
            var other=Enemy(6,5,1);
            target.Initialize(data); other.Initialize(data);
            Cast(p,5,5);
            Check(target.MaxHP==5&&target.currentHP==4&&!target.IsDead&&target.gameObject.activeSelf,"5-HP monster died to one damage");
            Check(other.currentHP==5&&data.hp==5,"attack changed another instance or its data");
            for(int remaining=3;remaining>=0;remaining--) {
                p.actionPoint=1; Cast(p,5,5);
                Check(target.currentHP==remaining,"damage did not use initialized current HP");
                Check(target.IsDead==(remaining==0),"monster died before its HP was exhausted");
            }
            Check(!target.gameObject.activeSelf&&other.currentHP==5,"death affected the other monster");
            var next=Enemy(7,5,1); next.Initialize(data);
            Check(next.currentHP==5&&next.MaxHP==5,"fresh monster inherited damage from a previous instance");
        });
        Case("area attack respects different monster data HP",()=>{
            var p=Player(0,4);
            var weak=Enemy(5,5,1); weak.Initialize(new MonsterData {hp=1});
            var strong=Enemy(6,5,1); strong.Initialize(new MonsterData {hp=5});
            var result=executor.Execute(p,Attack(),new List<CharacterBase> {weak,strong});
            Check(weak.IsDead&&!weak.gameObject.activeSelf,"1-HP monster survived one damage");
            Check(!strong.IsDead&&strong.currentHP==4&&strong.gameObject.activeSelf,"area attack killed the 5-HP monster");
            Check(result.hitCount==2&&result.killCount==1,"area attack kill count ignored initialized HP");
            Check(BattleManager.HP==100,"monster damage changed shared battle HP");
        });
        Case("12 asset bindings and defaults",()=>{
            foreach(string key in new[]{"Do","Jo","Ryu","Ha","Namgung","Kang","Pyo","Choi","Lee","Min","Seo","Beak"}) {
                var p=Passive(key);Check(p.passiveEffect!=PassiveEffect.None,key+" effect");
                string dir="Assets/1.Datas/Original/ScriptableObjects/";
                string guid=Regex.Match(File.ReadAllText(dir+"Globals/SkillDatas/InnatePassive/Innate_"+key+".asset.meta"),"guid: (\\w+)").Groups[1].Value;
                Check(File.ReadAllText(dir+"Character/"+key+".asset").Contains("staticpassive: {fileID: 11400000, guid: "+guid),key+" wiring");
                Check(p.maxActivationsPerTurn==(key=="Pyo"?1:0),key+" per-turn limit");
            }
        });
        Case("perimeter geometry, shifted bounds and scale",()=>{
            var b=new BoundsInt(-5,-3,0,10,10,1);
            Check(PassiveGeometry.AreSideNeighbours(C(-5,0),C(-5,1),b),"vertical side");
            Check(PassiveGeometry.AreSideNeighbours(C(0,6),C(1,6),b),"horizontal side");
            Check(!PassiveGeometry.AreSideNeighbours(C(-5,0),C(-4,0),b),"inward is not a side");
            Check(!PassiveGeometry.AreSideNeighbours(C(0,6),C(1,5),b),"no diagonal");
            Check(PassiveGeometry.AreOpposite(C(-5,0),C(4,0),b),"horizontal opposite");
            Check(PassiveGeometry.AreOpposite(C(0,-3),C(0,6),b),"vertical opposite");
            Check(!PassiveGeometry.AreOpposite(C(0,-3),C(1,6),b),"opposite aligned only");
            Check(PassiveGeometry.IsCorner(C(4,6),b),"shifted corner");
            Check(!PassiveGeometry.IsCorner(C(5,6),b),"off-board is not a corner");
            Check(PassiveGeometry.FrontCell(C(0,6),C(4,2),b)==C(0,5),"top faces inward");
            map.transform.position=new Vector3(27,-30);map.scale=2.8f;
            var p=Player(0,4,"Do");var front=Enemy(1,4,4);Enemy(5,5,4);Cast(p,5,5);
            Check(front.currentHP==3,"world scale does not change adjacent cell");
        });
        Case("Do front attack after real skill execution",()=>{
            var p=Player(0,4,"Do");var front=Enemy(1,4,4);var far=Enemy(5,5,4);var other=Enemy(2,4,4);
            Cast(p,5,5);Check(front.currentHP==3,"front hit once");Check(far.currentHP==3,"base attack hit");
            Check(other.currentHP==4,"not entire forward row");Check(p.actionPoint==0,"action consumed");
            Check(p.currentHP==0,"zero individual player HP supported");
        });
        Case("Jo persistent bomb, duration, refresh and turn deduplication",()=>{
            var p=Player(0,4,"Jo");var e=Enemy(5,5,10);Cast(p,5,5);
            Check(battle.Passives.BombCount==1&&e.currentHP==9,"plant without immediate explosion");
            Tick(1);Check(e.currentHP==8,"first enemy turn damage");Tick(1);Check(e.currentHP==8,"no duplicate tick");
            Tick(2);Check(e.currentHP==7,"second damage");Tick(3);Check(e.currentHP==6&&battle.Passives.BombCount==0,"third damage and expiry");
            Tick(4);Check(e.currentHP==6,"expired bomb cannot hit");
            p.actionPoint=1;Cast(p,5,5);p.actionPoint=1;Cast(p,5,5);Check(battle.Passives.BombCount==1,"same source/cell refreshes");
            p.actionPoint=1;Cast(p,6,6);Check(battle.Passives.BombCount==2,"different cells persist");
            var later=Enemy(6,6,4);Tick(5);Check(later.currentHP==3,"empty-tile bomb hits later occupant");
        });
        Case("Ryu uses moved ally as nearest-target origin",()=>{
            Player(0,5,"Ryu");var p=Player(0,2);var byMover=Enemy(1,4,4);var byOwner=Enemy(1,5,4);
            Move(p,0,4);Check(byMover.currentHP==3&&byOwner.currentHP==4,"nearest to moving ally");
            p.GetComponent<MoveTileModule>().OnMoveComplete();Check(byMover.currentHP==3,"completion cannot fire twice");
            Move(p,0,3);Check(byMover.currentHP==3,"no trigger when stamina prevents movement");
        });
        Case("Ha after stamina deduction, repeated valid moves and invalid moves",()=>{
            Player(0,5,"Ha");var p=Player(0,2);Move(p,0,4);
            Check(p.steminaPoint==1,"spent one and restored one");
            Check(PlacementManager.Instance.GetTileData(C(0,4)).Character==p,"arrival occupancy updated");
            Check(PlacementManager.Instance.GetTileData(C(0,2)).Character==null,"old occupancy cleared");
            Move(p,0,4);Check(p.steminaPoint==1,"same-cell command gives no reward");
            p.GetComponent<MoveTileModule>().OnMoveComplete();Check(p.steminaPoint==1,"duplicate completion gives no reward");
            Move(p,0,6);Check(p.steminaPoint==1,"another real move still triggers; no unrequested turn cap");
        });
        Case("Namgung random living enemy, burn refresh and expiry",()=>{
            var p=Player(0,4,"Namgung");var dead=Enemy(5,5,1);var survivor=Enemy(4,4,8);
            var inactive=Enemy(7,7,8);inactive.gameObject.SetActive(false);Cast(p,5,5);
            Check(dead.IsDead&&battle.Passives.BurnTurns(survivor)==2,"only remaining live enemy chosen");
            Check(battle.Passives.BurnTurns(inactive)==0,"inactive enemy ignored");
            Tick(1);Check(survivor.currentHP==7&&battle.Passives.BurnTurns(survivor)==1,"burn first tick");
            p.actionPoint=1;Cast(p,8,8);Check(battle.Passives.BurnTurns(survivor)==2,"reapply refreshes duration");
            Tick(2);Check(survivor.currentHP==6,"burn damage does not stack");
            Tick(3);Check(survivor.currentHP==5&&battle.Passives.BurnTurns(survivor)==0,"burn expires");
        });
        Case("Kang actual multi-kills and passive-kill credit",()=>{
            var p=Player(0,4,"Kang");p.steminaPoint=0;var a=Enemy(4,4,1);var b=Enemy(5,4,1);var skill=Attack();
            var result=executor.Execute(p,skill,new List<CharacterBase>{a,b});
            battle.NotifyAttackCompleted(p,skill,result,C(4,4),new[]{C(4,4),C(5,4)});
            Check(result.killCount==2&&p.steminaPoint==2,"two kills give two movement points, uncapped");
            Check(PlacementManager.Instance.GetTileData(C(4,4)).isempty&&PlacementManager.Instance.GetTileData(C(5,4)).Character==null,"death releases occupied cells");
            p.Data.passive=new[]{Passive("Lee")};var extra=Enemy(2,4,1);p.actionPoint=1;Cast(p,8,8);
            Check(extra.IsDead&&p.steminaPoint==3,"own passive kill rewards without re-running attack passives");
            var ally=Player(9,9);var e=Enemy(8,8,1);Cast(ally,8,8);Check(p.steminaPoint==3,"ally kill not credited");
        });
        Case("Taesan action refund once per owner per turn",()=>{
            var p=Player(0,0,"Pyo");var q=Player(9,9);q.Data.staticpassive=p.Data.staticpassive;Enemy(5,5,20);
            Cast(p,5,5);Check(p.actionPoint==1,"refund survives normal AP spending");
            Cast(p,5,5);Check(p.actionPoint==0,"no second refund");
            Cast(q,5,5);Check(q.actionPoint==1,"shared asset has independent per-owner count");
            battle.StartPlayerTurn();Cast(p,5,5);Check(p.actionPoint==0,"same turn start cannot reset limit");
            BattleManager.currentTurn++;battle.StartPlayerTurn();Cast(p,5,5);Check(p.actionPoint==1,"next turn resets limit");
            p.transform.position=map.GetCellCenterWorld(C(0,4));p.actionPoint=1;Cast(p,5,5);Check(p.actionPoint==0,"edge middle not a corner");
        });
        Case("Choi and Lee support attacks do not recursively trigger each other",()=>{
            Player(0,5,"Choi");var actor=Player(0,4,"Lee");var ca=Enemy(1,5,10);var le=Enemy(1,4,10);var direct=Enemy(5,5,10);
            int events=0;battle.Passives.PassiveActivated+=(o,s)=>events++;
            Cast(actor,5,5);Check(ca.currentHP==9&&le.currentHP==9&&direct.currentHP==9,"one owner-centred shot each");
            Check(events==2,"no support attack chain");
        });
        Case("Min opposite aligned ally only",()=>{
            Player(0,4,"Min");var ally=Player(9,4);var close=Enemy(1,4,10);var direct=Enemy(5,5,10);Cast(ally,5,5);
            Check(close.currentHP==9,"opposite ally triggers support near Min");
            ally.transform.position=map.GetCellCenterWorld(C(9,5));ally.actionPoint=1;Cast(ally,5,5);Check(close.currentHP==9,"misaligned opposite does not trigger");
        });
        Case("Seo stun skips shared-HP damage for exactly one enemy turn",()=>{
            var p=Player(0,4,"Seo");var e=Enemy(4,4,5);Cast(p,8,8);
            Check(battle.Passives.StunTurns(e)==1,"stun applied");battle.StartMonsterTurn();
            Check(BattleManager.HP==100&&BattleManager.currentTurn==2,"stun skips monster action");
            battle.StartMonsterTurn();Check(BattleManager.HP==95,"monster acts on following turn");
        });
        Case("Beak survivor follow-up and pushed-out exclusion",()=>{
            var p=Player(0,4,"Beak");var e=Enemy(5,5,3);var untouched=Enemy(7,7,3);Cast(p,5,5);
            Check(e.currentHP==1&&untouched.currentHP==3,"one extra hit on original survivor only");
            p.actionPoint=1;var dead=Enemy(6,6,1);Cast(p,6,6);Check(dead.IsDead&&untouched.currentHP==3,"base kill gives no unrelated extra hit");
            var pushed=Enemy(4,4,5);var skill=Attack();skill.effectType|=SkillEffectType.Push;skill.pushDistance=1;
            var result=executor.Execute(p,skill,new List<CharacterBase>{pushed});
            Check(map.WorldToCell(pushed.transform.position)==C(5,4),"real executor pushed target");
            battle.NotifyAttackCompleted(p,skill,result,C(4,4),new[]{C(4,4)});Check(pushed.currentHP==4,"survivor outside original area excluded");
        });
        Case("duplicate equipped passive runs once and None stays inert",()=>{
            var p=Player(0,4,"Lee");p.Data.passive=new[]{p.Data.staticpassive,p.Data.staticpassive,new PassiveSkill()};var e=Enemy(1,4,10);Cast(p,8,8);
            Check(e.currentHP==9,"duplicate slots deduplicated");
            Check(BattlePassiveSystem.EquippedPassives(p).Count==1,"None ignored");
        });
        Case("burn/bomb damage precedes enemy HP damage",()=>{
            var p=Player(0,4,"Jo");p.Data.passive=new[]{Passive("Namgung")};var e=Enemy(5,5,4);Cast(p,5,5);
            battle.StartMonsterTurn();Check(e.currentHP==1&&BattleManager.HP==99,"3 HP minus burn/bomb, then 1 incoming damage");
            battle.StartMonsterTurn();Check(e.IsDead&&BattleManager.HP==99,"DOT kill cannot attack player");
        });
        Case("movement and turn-transition ordering",()=>{
            Player(0,5,"Ha");var p=Player(0,2);var move=p.GetComponent<MoveTileModule>();move.MoveToTileDirect(C(0,4));
            battle.EndTurn();Check(battle.IsPlayerTurn,"turn cannot end while movement pending");
            Cast(p,8,8);Check(p.actionPoint==1,"cannot attack during movement");
            while(move.IsMoving)move.UpdateToDestination(1);
            Check(p.steminaPoint==1&&battle.CanAcceptPlayerAction,"arrival passive resolved before next action");
            battle.EndTurn();Check(!battle.IsPlayerTurn,"turn ends after movement completes");
            var e=Enemy(1,4,5);p.Data.staticpassive=Passive("Lee");battle.NotifyAttackCompleted(p,Attack(),new SkillExecuteResult(),C(1,4),new[]{C(1,4)});
            Check(e.currentHP==5,"enemy-turn notification ignored");
        });
        Case("Kang DOT kill reward survives the next turn stamina reset",()=>{
            var p=Player(0,4,"Kang");p.Data.passive=new[]{Passive("Namgung")};p.steminaPoint=0;
            var e=Enemy(5,5,2);Cast(p,5,5);battle.StartMonsterTurn();
            Check(e.IsDead&&p.steminaPoint==p.maxStemina+1,"DOT kill bonus applied after stamina reset");
            battle.Passives.ApplyStartOfTurnBonuses(p);Check(p.steminaPoint==2,"pending bonus consumed once");
        });
        Case("missing executor does not spend action",()=>{
            var p=Player(0,4,"Lee");typeof(ExecuteSkill).GetProperty("Instance").SetValue(null,null);Cast(p,8,8);
            Check(p.actionPoint==1,"failed execution keeps AP");
        });
        Case("reset clears runtime state and prevents out-of-battle effects",()=>{
            var p=Player(0,0,"Jo");p.Data.passive=new[]{Passive("Namgung"),Passive("Seo"),Passive("Pyo")};var e=Enemy(5,5,10);Cast(p,5,5);
            Check(battle.Passives.BombCount==1&&battle.Passives.BurnTurns(e)>0&&battle.Passives.StunTurns(e)>0,"states exist");
            battle.ResetBattle();Check(battle.Passives.BombCount==0&&battle.Passives.BurnTurns(e)==0&&battle.Passives.StunTurns(e)==0,"all state cleared");
            battle.NotifyAttackCompleted(p,Attack(),new SkillExecuteResult(),C(5,5),new[]{C(5,5)});Check(battle.Passives.BombCount==0,"outside battle ignored");
            battle.BeginBattle(0,null);p.actionPoint=1;Cast(p,5,5);Check(p.actionPoint==1,"new battle resets once-per-turn grant");
        });
        Case("UI displays live AP and stamina instead of shared asset defaults",()=>{
            var p=Player(0,4);SelectionManager.CharacterBase=p;p.actionPoint=2;p.steminaPoint=5;p.Data.actionPoint=0;p.Data.steminaPoint=0;
            var ui=new StageUIController();var ap=new TMPro.TextMeshProUGUI();var sp=new TMPro.TextMeshProUGUI();Set(ui,"AP",ap);Set(ui,"SP",sp);
            ui.RefreshActionPoints();Check(ap.text=="2"&&sp.text=="5","live resource UI");
        });
        Console.WriteLine("PASS: "+checks+" behavioural assertions");
        SaveScenarios.RunAll();
    }
}
