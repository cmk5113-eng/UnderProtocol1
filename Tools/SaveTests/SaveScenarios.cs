using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public static class SaveScenarios
{
    private static int checks,cases;
    private static void Check(bool condition,string message) { checks++; if(!condition) throw new Exception(message); }
    private static void Call(object obj,string name) => obj.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(obj,null);
    private static void ResetStatic(Type type,string name) => type.GetMethod(name,BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
    private static SaveData Stored(int slot) => JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString("SaveData"+slot));
    private static void Case(string name,Action run) { run();cases++;Console.WriteLine("PASS save: "+name); }

    private sealed class Fixture
    {
        public readonly ActiveSkill a=new ActiveSkill { name="SameName",id=0 },b=new ActiveSkill { name="SameName",id=0 };
        public readonly PassiveSkill p=new PassiveSkill { name="Passive",id=0 },q=new PassiveSkill { name="Passive",id=0 };
        public readonly CharacterData[] heroes=new CharacterData[12];
        public readonly SaveCatalog catalog=new SaveCatalog();
        public readonly SaveManager save;
        public Fixture(bool erase=true)
        {
            if(erase) PlayerPrefs.DeleteAll();
            UnityEngine.Object.ResetScene();
            typeof(BattleManager).GetField("instance",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,null);
            typeof(StageUIController).GetProperty("Instance").SetValue(null,null);
            typeof(UseSkill).GetProperty("Instance").SetValue(null,null);
            ResetStatic(typeof(ProgressManager),"ResetRuntimeState");
            ResetStatic(typeof(SaveManager),"ResetStaticState");
            ScrollUI.Instance=null;PlacementManager.Instance=null;CanvasManager.Instance=null;
            catalog.characters=new SaveCharacterEntry[12];
            catalog.skills=new[]{new SaveSkillEntry{id="active-a",skill=a},new SaveSkillEntry{id="active-b",skill=b},
                new SaveSkillEntry{id="passive-p",skill=p},new SaveSkillEntry{id="passive-q",skill=q}};
            for(int i=0;i<12;i++)
            {
                heroes[i]=new CharacterData { characterName="Hero"+i,active=new[]{a,b},passive=new[]{p,q,p,q} };
                catalog.characters[i]=new SaveCharacterEntry { id="hero-"+i,character=heroes[i],defaultActive=new[]{a,b},defaultPassive=new[]{p,q,p,q} };
            }
            Resources.SetForTest("SaveCatalog",catalog);
            GameManager.Instance=new GameManager();save=GameManager.Instance.Save;Call(save,"Awake");
        }
    }

    public static void RunAll()
    {
        Case("all 12 loadouts round-trip across a fresh runtime",()=>{
            var f=new Fixture();
            for(int i=0;i<12;i++) { f.heroes[i].active=new[]{f.b,i%2==0?null:f.a};f.heroes[i].passive=new[]{f.q,null,f.p,f.q}; }
            ProgressManager.RecordStageClear(7,4,72.5f,5);
            Check(f.save.TrySave(0),"save succeeds");var data=Stored(0);
            Check(data.saveVersion==2&&data.characterSkills.Count==12,"version and unspawned roster");
            Check(data.characterSkills.All(x=>x.activeSkillIds.Length==2&&x.passiveSkillIds.Length==4),"all 72 slots recorded");
            var restarted=new Fixture(false);Check(restarted.save.TryLoad(0),"load after restart");
            for(int i=0;i<12;i++)
            {
                Check(restarted.heroes[i].active[0]==restarted.b,"asset ID resolves new runtime reference");
                Check(restarted.heroes[i].active[1]==(i%2==0?null:restarted.a),"explicit empty active slot");
                Check(restarted.heroes[i].passive.SequenceEqual(new[]{restarted.q,null,restarted.p,restarted.q}),"passive order and empty slot");
            }
            var record=ProgressManager.GetStageClearRecord(7);
            Check(record.clearCount==1&&record.lastClearTurn==4&&record.lastRemainingHP==72.5f&&record.lastWaveCount==5,"stage detail round-trip");
            Check(ProgressManager.Progress==1&&record.firstClearedUtc==record.lastClearedUtc,"first-clear progress and time");
        });
        Case("names, numeric IDs and catalog ordering do not select skills",()=>{
            var f=new Fixture();f.heroes[11].active[0]=f.b;Check(f.save.TrySave(0),"save duplicate-name skills");
            f.a.name="Renamed";f.b.name="Renamed";f.heroes[11].characterName="Renamed hero";
            Array.Reverse(f.catalog.characters);Array.Reverse(f.catalog.skills);
            var persistence=new CharacterLoadoutPersistence(f.catalog);persistence.Restore(Stored(0).characterSkills);
            Check(f.heroes[11].active[0]==f.b&&f.heroes[0].active[0]==f.a,"stable IDs survive reordering/renaming");
        });
        Case("three slots isolate loadouts, clear records and empty-slot defaults",()=>{
            var f=new Fixture();f.heroes[0].active[0]=f.b;ProgressManager.RecordStageClear(1,3,80,2);f.save.Save(0);
            Check(f.save.TryLoad(1),"open new slot");
            Check(ProgressManager.Progress==0&&!ProgressManager.IsStageCleared(1)&&f.heroes[0].active[0]==f.a,"new slot does not inherit state");
            foreach(var h in f.heroes)h.passive[3]=null;
            ProgressManager.RecordStageClear(9,6,40,4);f.save.Save(1);
            Check(f.save.TryLoad(0),"switch to first slot");
            Check(f.heroes[0].active[0]==f.b&&f.heroes.All(h=>h.passive[3]==f.q),"first slot loadouts");
            Check(ProgressManager.IsStageCleared(1)&&!ProgressManager.IsStageCleared(9),"first slot stage set");
            Check(f.save.TryLoad(1)&&f.heroes.All(h=>h.passive[3]==null),"second slot explicit empty slots");
            Check(ProgressManager.IsStageCleared(9)&&!ProgressManager.IsStageCleared(1),"second slot stage set");
            Check(f.save.TryLoad(2)&&f.heroes.All(h=>h.active[0]==f.a&&h.passive[3]==f.q),"third slot defaults");
            Check(ProgressManager.GetStageClearRecords().Count==0,"third slot empty records");
        });
        Case("legacy currentGold progress migrates without inventing cleared stages",()=>{
            var f=new Fixture();PlayerPrefs.SetString("SaveData0","{\"currentGold\":7,\"stage\":2,\"cleared\":true}");
            f.heroes[0].active[0]=f.b;
            Check(f.save.TryLoad(0)&&ProgressManager.Progress==7,"old progress read");
            Check(f.heroes[0].active[0]==f.a&&ProgressManager.GetClearedStageIds().Count==0,"missing information uses defaults, no guessed IDs");
            Check(f.save.TrySave(0)&&Stored(0).saveVersion==2&&Stored(0).stage==2,"legacy upgrade on next save");
        });
        Case("v1 stage IDs migrate and replay increments count once",()=>{
            var f=new Fixture();PlayerPrefs.SetString("SaveData0","{\"progressVersion\":1,\"progress\":9,\"currentGold\":44,\"clearedStageIds\":[1,6,6,-1]}");
            Check(f.save.TryLoad(0)&&ProgressManager.Progress==9,"v1 progress wins over legacy gold");
            Check(ProgressManager.GetStageClearRecords().Count==2,"IDs deduplicated");
            var old=ProgressManager.GetStageClearRecord(6);
            Check(old.clearCount==1&&old.bestClearTurn==0&&old.lastRemainingHP==0&&string.IsNullOrEmpty(old.firstClearedUtc),"unknown historical stats stay unknown");
            ProgressManager.RecordStageClear(6,3,55,5);var next=ProgressManager.GetStageClearRecord(6);
            Check(next.clearCount==2&&next.bestClearTurn==3&&ProgressManager.Progress==9,"replay records without progress increase");
            Check(string.IsNullOrEmpty(next.firstClearedUtc)&&!string.IsNullOrEmpty(next.lastClearedUtc),"legacy first-clear time not fabricated");
        });
        Case("best turn and best HP are independent and records are defensive copies",()=>{
            new Fixture();int events=0;ProgressManager.OnProgressChanged+=()=>events++;
            ProgressManager.RecordStageClear(29,5,80,5);ProgressManager.RecordStageClear(29,3,50,5);ProgressManager.RecordStageClear(29,6,95,5);
            var r=ProgressManager.GetStageClearRecord(29);
            Check(r.clearCount==3&&r.lastClearTurn==6&&r.bestClearTurn==3,"best and last turns");
            Check(r.lastRemainingHP==95&&r.bestRemainingHP==95&&ProgressManager.Progress==1&&events==3,"best HP and replay notifications");
            Check(DateTime.TryParse(r.firstClearedUtc,out _)&&DateTime.TryParse(r.lastClearedUtc,out _),"saved UTC dates");
            r.clearCount=100;var list=ProgressManager.GetStageClearRecords();list[0].clearCount=200;
            Check(ProgressManager.GetStageClearRecord(29).clearCount==3,"caller cannot mutate runtime record");
            Check(!ProgressManager.RecordStageClear(-1,1,100,1)&&ProgressManager.Progress==1,"invalid stage excluded");
        });
        Case("missing assets, wrong skill types and partial old arrays load safely",()=>{
            var f=new Fixture();var data=new SaveData {saveVersion=2,characterSkills=new List<CharacterSkillSaveData>{
                null,new CharacterSkillSaveData {characterId="removed-hero"},
                new CharacterSkillSaveData {characterId="hero-0",activeSkillIds=new[]{"deleted-skill","passive-p"},passiveSkillIds=new[]{"active-a",""}},
                new CharacterSkillSaveData {characterId="hero-1",activeSkillIds=new[]{"active-b"}},
                new CharacterSkillSaveData {characterId="hero-1",activeSkillIds=new[]{"active-a"}}
            }};
            PlayerPrefs.SetString("SaveData0",JsonUtility.ToJson(data));Check(f.save.TryLoad(0),"load unknown references");
            Check(f.heroes[0].active.SequenceEqual(new[]{f.a,f.b}),"missing and wrong-type active defaults");
            Check(f.heroes[0].passive.SequenceEqual(new[]{f.p,null,f.p,f.q}),"wrong-type passive default, explicit empty and missing slots");
            Check(f.heroes[1].active.SequenceEqual(new[]{f.b,f.b}),"short array uses defaults and duplicate character ignored");
            Check(f.heroes[11].active[0]==f.a,"unlisted character resets");
        });
        Case("unregistered skills cannot overwrite an existing valid save",()=>{
            var f=new Fixture();f.save.Save(0);string original=PlayerPrefs.GetString("SaveData0");
            f.heroes[11].active[1]=new ActiveSkill();
            Check(!f.save.TrySave(0)&&PlayerPrefs.GetString("SaveData0")==original,"failed capture preserves stored save");
            Check(!f.save.TrySave(-1)&&!f.save.TrySave(3),"invalid slots refused");
        });
        Case("corrupt/future saves preserve current state and cannot be silently overwritten",()=>{
            var f=new Fixture();f.heroes[0].active[0]=f.b;ProgressManager.RecordStageClear(2,4,60,3);f.save.Save(0);
            PlayerPrefs.SetString("SaveData1","invalid-json");
            string future=JsonUtility.ToJson(new SaveData {saveVersion=99,progress=999});PlayerPrefs.SetString("SaveData2",future);
            Check(!f.save.TryLoad(1)&&!f.save.TryLoad(2),"bad load rejected");
            Check(f.save.currentSlot==0&&f.heroes[0].active[0]==f.b&&ProgressManager.IsStageCleared(2),"current slot and state preserved");
            Check(!f.save.TrySave(1)&&!f.save.TrySave(2),"bad/future save overwrite rejected");
            Check(PlayerPrefs.GetString("SaveData1")=="invalid-json"&&PlayerPrefs.GetString("SaveData2")==future,"original bytes preserved");
            Check(!f.save.TryLoad(-1)&&!f.save.TryLoad(3),"invalid loads rejected");
        });
        Case("loading validates first and aborts an active battle only for a valid slot",()=>{
            var f=new Fixture();f.save.Save(0);PlayerPrefs.SetString("SaveData1","bad-json");
            var battle=new BattleManager();Call(battle,"Awake");battle.BeginBattle(11,null);
            Check(!f.save.TryLoad(1)&&battle.IsBattleActive,"invalid load leaves battle running");
            Check(f.save.TryLoad(0)&&!battle.IsBattleActive,"valid load aborts previous battle");
            Check(!ProgressManager.IsStageCleared(11),"aborted battle cannot clear stage");
        });
        Case("real battle completion records the finished turn and autosaves replays",()=>{
            var f=new Fixture();var battle=new BattleManager();Call(battle,"Awake");
            var wave=GameManager.Instance.Wave;wave.selectedWaves=new[]{new WaveData()};wave.currentWave=wave.selectedWaves[0];wave.currentWaveIndex=0;
            battle.BeginBattle(4,null);battle.CompleteBattle();
            Check(battle.IsBattleActive&&!ProgressManager.IsStageCleared(4),"incomplete waves are not recorded");
            wave.currentWaveIndex=1;battle.StartMonsterTurn();Check(BattleManager.currentTurn==2,"next-turn counter already advanced");
            battle.CompleteBattle();var r=ProgressManager.GetStageClearRecord(4);
            Check(!battle.IsBattleActive&&r.lastClearTurn==1&&r.lastWaveCount==1,"completed player turn, no off-by-one");
            Check(Stored(0).stageClearRecords[0].clearCount==1,"first-clear autosave");
            battle.BeginBattle(4,null);battle.StartMonsterTurn();battle.CompleteBattle();
            Check(Stored(0).stageClearRecords[0].clearCount==2&&ProgressManager.Progress==1,"replay autosaved without duplicate progress");
            string saved=PlayerPrefs.GetString("SaveData0");battle.BeginBattle(4,null);battle.AbortBattle();
            Check(PlayerPrefs.GetString("SaveData0")==saved&&ProgressManager.GetStageClearRecord(4).clearCount==2,"abort never saves clear record");
        });
        Case("skill selection autosaves all six slot types and load refreshes open hero UI",()=>{
            var f=new Fixture();var hero=new UI_Hero {characterList=f.heroes.ToList()};
            hero.ActiveSkill=new[]{new UnityEngine.UI.Image(),new UnityEngine.UI.Image()};
            f.a.icon=new Sprite();f.b.icon=new Sprite();
            CanvasManager.Instance=new CanvasManager();
            foreach(SkillLoadButton.SkillGroupType group in Enum.GetValues(typeof(SkillLoadButton.SkillGroupType)))
            {
                CanvasManager.Instance.CurrentSkillGroup=group;var slot=new SkillSlot();slot.AddSkill((int)group<2?(SkillList)f.b:f.q,1);
                var ui=new UI_SkillSlotInfo();ui.ConnectSlot(slot);ui.SelectSkill();
                var stored=Stored(0).characterSkills[0];
                Check((int)group<2?stored.activeSkillIds[(int)group]=="active-b":stored.passiveSkillIds[(int)group-2]=="passive-q","selected slot autosaved");
            }
            Call(hero,"OnEnable");f.heroes[0].active[0]=f.a;hero.RefreshUI();
            Check(hero.ActiveSkill[0].sprite==f.a.icon,"UI temporarily reflects changed loadout");
            Check(f.save.TryLoad(0)&&hero.ActiveSkill[0].sprite==f.b.icon,"load event refreshes restored icon");
            Call(hero,"OnDisable");f.save.TryLoad(1);Call(hero,"OnEnable");
            Check(hero.ActiveSkill[0].sprite==f.a.icon,"reopening UI refreshes default loadout");
            Call(hero,"OnDisable");
        });
        Case("record restoration unions legacy IDs and details without stale records",()=>{
            new Fixture();ProgressManager.RecordStageClear(99,3,80,2);
            var record=new StageClearRecord {stageId=5,cleared=true,clearCount=4,lastClearTurn=8,bestClearTurn=3,lastRemainingHP=22,bestRemainingHP=75,lastWaveCount=5};
            ProgressManager.RestoreProgress(0,new List<int>{2,2,-1},new List<StageClearRecord>{record,record.Copy(),null,new StageClearRecord{stageId=-1,cleared=true}});
            Check(ProgressManager.GetClearedStageIds().SequenceEqual(new[]{2,5})&&ProgressManager.Progress==2,"union and progress lower bound");
            Check(!ProgressManager.IsStageCleared(99)&&ProgressManager.GetStageClearRecord(5).clearCount==4,"previous slot discarded, duplicates not added");
            record.clearCount=9;Check(ProgressManager.GetStageClearRecord(5).clearCount==4,"source DTO cannot mutate runtime record");
        });
        Case("shutdown restores authored defaults and startup loads the last used slot",()=>{
            var f=new Fixture();f.heroes[11].active[0]=f.b;f.heroes[11].passive[3]=null;
            ProgressManager.RecordStageClear(19,4,77,5);Check(f.save.TrySave(1),"save selects slot one");
            Call(f.save,"OnDestroy");
            Check(f.heroes.All(h=>h.active[0]==f.a&&h.passive[3]==f.q),"Play Mode shutdown restores all authored loadouts");
            Check(f.catalog.characters.All(e=>e.defaultActive[0]==f.a&&e.defaultPassive[3]==f.q),"catalog defaults remain immutable");
            var restarted=new Fixture(false);Check(restarted.save.currentSlot==1,"last used slot selected in Awake");
            Call(restarted.save,"Start");
            Check(restarted.heroes[11].active[0]==restarted.b&&restarted.heroes[11].passive[3]==null,"automatic Start load restores loadout");
            Check(ProgressManager.Progress==1&&ProgressManager.IsStageCleared(19),"automatic Start load restores stage record");
        });
        Console.WriteLine($"PASS save system: {cases} scenarios / {checks} behavioural assertions");
    }
}
