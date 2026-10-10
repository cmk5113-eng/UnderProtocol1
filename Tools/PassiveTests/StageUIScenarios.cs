using System;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static partial class Scenarios
{
    static T PrivateValue<T>(object owner, string field)
        => (T)owner.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(owner);

    static TextMeshProUGUI StageText(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        return go.GetComponent<TextMeshProUGUI>();
    }

    static StageUIController StageUI(CharacterData data = null)
    {
        var root = new GameObject("S_Stage", typeof(RectTransform));
        ((RectTransform)root.transform).sizeDelta = new Vector2(1920, 1080);
        var ui = root.AddComponent<StageUIController>();
        var top = new GameObject("Top"); top.transform.SetParent(root.transform, false);
        var info = new GameObject("StageInfo"); info.transform.SetParent(top.transform, false);
        StageText(info.transform, "wave"); StageText(info.transform, "enemy");
        var icons = new Image[5];
        for (int i = 0; i < icons.Length; i++)
        {
            var box = new GameObject("Skill" + i, typeof(RectTransform), typeof(Image));
            box.transform.SetParent(root.transform, false);
            ((RectTransform)box.transform).sizeDelta = new Vector2(80, 80);
            box.transform.localPosition = new Vector3(-240 + i * 120, -420);
            icons[i] = box.GetComponent<Image>();
        }
        Set(ui, "skill", icons); Set(ui, "currentData", data);
        Call(ui, "Awake");
        return ui;
    }

    static TextMeshProUGUI Counter(StageUIController ui, string name)
        => ui.transform.Find("Top/StageInfo/" + name).GetComponent<TextMeshProUGUI>();

    static void RunStageUIScenarios()
    {
        Case("HUD binds exact wave/enemy nodes and shows integer ordinals", () => {
            var ui = StageUI(); var first = new WaveData(); var second = new WaveData();
            var waves = GameManager.Instance.Wave;
            waves.selectedWaves = new[] { first, second, first };
            Call(ui, "OnEnable"); Check(Counter(ui, "wave").text == "0", "empty stage has a wave number");
            waves.currentWave = first; waves.currentWaveIndex = 1; Call(ui, "LateUpdate");
            Check(Counter(ui, "wave").text == "1", "first wave displayed asset text");
            waves.currentWave = second; waves.currentWaveIndex = 2; Call(ui, "LateUpdate");
            Check(Counter(ui, "wave").text == "2", "second wave not reflected");
            waves.currentWave = first; waves.currentWaveIndex = 3; ui.UpdateWave();
            Check(Counter(ui, "wave").text == "3", "reused WaveData was shown as its first occurrence");
            waves.currentWaveIndex = 0; ui.UpdateWave();
            Check(Counter(ui, "wave").text == "0", "failed/unloaded wave displayed a positive ordinal");
            waves.currentWaveIndex = 3; battle.ResetBattle(); ui.UpdateWave();
            Check(Counter(ui, "wave").text == "0", "battle reset kept a displayed wave ordinal");
        });
        Case("HUD counts live enemies once and refreshes after direct damage, field kills and reset", () => {
            var ui = StageUI(); var a = Enemy(4,4,3); var b = Enemy(6,6,4);
            var dead = Enemy(3,3,1); dead.currentHP = 0;
            var inactive = Enemy(8,8,5); inactive.gameObject.SetActive(false);
            b.Initialize(new MonsterData { hp = 4, footprintSize = new Vector2Int(2,2) });
            Check(b.TryPlace(map,C(6,6)), "large enemy setup failed");
            Call(ui, "LateUpdate");
            Check(Counter(ui,"enemy").text == "2" && BattleManager.CountRemainingMonsters() == 2, "dead/inactive/body cells counted as enemies");
            a.TakeDamage(3); Call(ui,"LateUpdate");
            Check(Counter(ui,"enemy").text == "1" && BattleManager.HasRemainingMonsters(), "direct kill left a stale enemy count");
            battle.Fields.Apply(map,C(6,6),null,SkillTileFieldEffectType.Fire,4,1,Vector3Int.right);
            battle.StartMonsterTurn(); Call(ui,"LateUpdate");
            Check(Counter(ui,"enemy").text == "0" && !BattleManager.HasRemainingMonsters(), "field kill did not decrement the count");
            Enemy(5,5,1); battle.ResetBattle(); ui.UpdateEnemies();
            Check(Counter(ui,"enemy").text == "0", "out-of-battle HUD retained enemies");
        });
        Case("individual HP bars follow initialization, independent damage, healing and reinitialization", () => {
            var a = Enemy(4,4,1); var b = Enemy(6,6,1); var data = new MonsterData { hp = 8 };
            a.Initialize(data); b.Initialize(data);
            var aBar = a.GetComponent<MonsterHealthBar>(); var bBar = b.GetComponent<MonsterHealthBar>();
            a.TakeDamage(3); aBar.Refresh(); bBar.Refresh();
            Check(Math.Abs(PrivateValue<RectTransform>(aBar,"fillRect").anchorMax.x - 0.625f) < 0.0001f, "damage bar ratio did not use current HP");
            Check(PrivateValue<RectTransform>(bBar,"fillRect").anchorMax.x == 1f && data.hp == 8, "one bar changed another monster or shared data");
            a.Heal(2); aBar.Refresh();
            Check(Math.Abs(PrivateValue<RectTransform>(aBar,"fillRect").anchorMax.x - 0.875f) < 0.0001f, "healing did not refill the bar");
            a.Initialize(data);
            Check(a.GetComponentsInChildren<Canvas>(true).Length == 1 && PrivateValue<RectTransform>(aBar,"fillRect").anchorMax.x == 1f, "reinitialization duplicated or retained a damaged HP bar");
            foreach (Graphic graphic in a.GetComponentsInChildren<Graphic>(true)) Check(!graphic.raycastTarget, "HP bar blocks tile input");
            a.TakeDamage(8); aBar.Refresh();
            Check(!PrivateValue<Canvas>(aBar,"canvas").gameObject.activeInHierarchy, "dead monster left a visible HP bar");
        });
        Case("large monster bar sits above scaled visual and shield hits do not lower HP", () => {
            var monster = Enemy(4,4,1);
            var renderer = monster.gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = new Sprite(); renderer.unscaledSize = new Vector3(2,2,1); renderer.sortingOrder = 7;
            monster.transform.localScale = new Vector3(0.6f,0.6f,1);
            monster.Initialize(new MonsterData { hp=8, hasShield=true, footprintSize=new Vector2Int(2,2), fitSpriteToFootprint=true });
            Check(monster.TryPlace(map,C(4,4)), "large visual setup failed");
            var bar = monster.GetComponent<MonsterHealthBar>(); bar.Refresh();
            var canvas = PrivateValue<Canvas>(bar,"canvas"); var rect = PrivateValue<RectTransform>(bar,"barRect");
            Check(rect.position.y > renderer.bounds.max.y && canvas.sortingOrder == 8, "bar not above scaled body or in front of sprite");
            Check(Math.Abs(rect.lossyScale.x - 0.01f) < 0.0001f, "monster scale changed bar thickness/width");
            monster.TakeDamageAtCell(map,C(4,4),2); bar.Refresh();
            Check(PrivateValue<RectTransform>(bar,"fillRect").anchorMax.x == 1f, "shield hit reduced HP bar");
            monster.TakeDamageAtCell(map,C(4,4),2); bar.Refresh();
            Check(PrivateValue<RectTransform>(bar,"fillRect").anchorMax.x == 0.75f, "exposed body damage not reflected in bar");
            monster.gameObject.SetActive(false); bar.Refresh();
            Check(!canvas.gameObject.activeSelf, "inactive monster left bar enabled");
        });
        Case("skill clicks target the matching slot and anchor a single tooltip above it", () => {
            var p = Player(0,4); SelectionManager.CharacterBase = p;
            p.Data.active = new[] { new ActiveSkill { skillName="First", description="First description", range=20 }, new ActiveSkill { skillName="Second", description="Second description", range=20 } };
            var ui = StageUI(p.Data); ui.transform.localScale = new Vector3(0.5f,0.5f,1);
            targeting.UI_StartSkill2();
            Check(PrivateValue<SkillList>(targeting,"currentSkill") == p.Data.active[1], "second click still uses first active skill");
            var tooltip = PrivateValue<UI_SkillTooltip>(ui,"skillTooltip");
            var label = tooltip.GetComponentInChildren<TextMeshProUGUI>(true);
            Check(tooltip.gameObject.activeSelf && label.text.Contains("Second description"), "clicked skill tooltip missing");
            var source = (RectTransform)PrivateValue<Image[]>(ui,"skill")[1].transform;
            var sourceCorners = new Vector3[4]; source.GetWorldCorners(sourceCorners);
            var tooltipCorners = new Vector3[4]; ((RectTransform)tooltip.transform).GetWorldCorners(tooltipCorners);
            Check(tooltipCorners[0].y > sourceCorners[1].y, "tooltip not above scaled skill box");
            targeting.UI_StartSkill1();
            Check(PrivateValue<SkillList>(targeting,"currentSkill") == p.Data.active[0] && label.text.Contains("First description"), "switching skill left stale content");
            Check(ui.GetComponentsInChildren<UI_SkillTooltip>(true).Length == 1, "clicks accumulated tooltip objects");
            foreach (Graphic graphic in tooltip.GetComponentsInChildren<Graphic>(true)) Check(!graphic.raycastTarget, "tooltip blocks skill/tile input");
            p.Data.passive = new[] { new PassiveSkill { skillName = "Passive", description = "Passive description" } };
            ui.OnClickPassiveSkill();
            Check(PrivateValue<SkillList>(targeting,"currentSkill") == null && label.text.Contains("Passive description"), "passive info kept the previous active skill targeting");
            targeting.ClearAllHighlights(); Check(!tooltip.gameObject.activeSelf, "cancel left tooltip visible");
        });
        Case("normal, ultimate and passive descriptions remain available with a blocked caster/gauge", () => {
            var p=Player(0,4);SelectionManager.CharacterBase=p;
            p.Data.normalSkill=new NormalSkill {skillName="Normal",description="Normal detail",range=20};
            p.Data.ultimateSkill=new UltimateSkill {skillName="Ultimate",description="Ultimate detail",range=20};
            p.Data.passive=new[]{new PassiveSkill {skillName="Passive",description="Passive detail"}};
            var ui=StageUI(p.Data);p.isSpawned=false;targeting.UI_StartNormalSkill();
            var tooltip=PrivateValue<UI_SkillTooltip>(ui,"skillTooltip");var label=tooltip.GetComponentInChildren<TextMeshProUGUI>(true);
            Check(label.text.Contains("Normal detail")&&PrivateValue<SkillList>(targeting,"currentSkill")==null,"unspawned unit hid info or started targeting");
            p.isSpawned=true;ScrollUI.Instance=new ScrollUI();ScrollUI.Instance.GGscrollbar.value=0.5f;targeting.UI_Ultimate();
            Check(label.text.Contains("Ultimate detail")&&ScrollUI.Instance.GGscrollbar.value==0.5f,"blocked ultimate tooltip spent gauge");
            ui.OnClickPassiveSkill();Check(label.text.Contains("Passive detail"),"passive click did not show description");
            Call(ui,"OnDisable");Check(!tooltip.gameObject.activeSelf,"leaving stage left tooltip visible");
            SelectionManager.CharacterBase=null;targeting.UI_StartNormalSkill();
            Check(PrivateValue<SkillList>(targeting,"currentSkill")==null,"null caster click started targeting");
        });
        Case("tooltip stays within horizontal bounds, follows resized boxes and clears stale anchors", () => {
            var data=new CharacterData {active=new[]{new ActiveSkill {skillName="Right",description="Details"}}};
            var ui=StageUI(data);var source=(RectTransform)PrivateValue<Image[]>(ui,"skill")[0].transform;
            source.localPosition=new Vector3(940,-400);ui.ShowSkillTooltip(0);
            var tooltip=PrivateValue<UI_SkillTooltip>(ui,"skillTooltip");var panel=(RectTransform)tooltip.transform;var viewport=(RectTransform)ui.transform;
            Check(panel.localPosition.x+panel.sizeDelta.x*0.5f<=viewport.rect.xMax,"tooltip exceeded right screen edge");
            source.localPosition=new Vector3(-940,-300);Call(tooltip,"LateUpdate");
            Check(panel.localPosition.x-panel.sizeDelta.x*0.5f>=viewport.rect.xMin&&panel.localPosition.y>source.localPosition.y+40,"tooltip did not follow moved box within bounds");
            source.gameObject.SetActive(false);Call(tooltip,"LateUpdate");Check(!tooltip.gameObject.activeSelf,"hidden skill box kept its tooltip");
            source.gameObject.SetActive(true);ui.ShowSkillTooltip(0);ui.ShowSkillTooltip(1);
            Check(!tooltip.gameObject.activeSelf,"empty skill slot kept previous description");
        });
        Case("fire and ice stay distinct under active range and AOE previews", () => {
            var p=Player(0,4);
            battle.Fields.Apply(map,C(5,5),p,SkillTileFieldEffectType.Fire,1,3,Vector3Int.right);
            battle.Fields.Apply(map,C(6,5),p,SkillTileFieldEffectType.Ice,1,3,Vector3Int.right);
            var fire=map.GetColor(C(5,5));var ice=map.GetColor(C(6,5));
            Check(fire.r>fire.b&&fire.r>fire.g&&ice.b>ice.r&&ice.b>ice.g,"fire/ice do not have element colors");
            targeting.StartSkillTargeting(Attack(),p);
            Check(map.GetColor(C(5,5)).r>map.GetColor(C(5,5)).b,"blue cast range hid red fire");
            Input.mousePosition=map.GetCellCenterWorld(C(6,5));Call(targeting,"HandleRealtimeAoE");
            var preview=BattleFieldEffectSystem.PreviewColor(map,C(6,5),new Color(1f,0.2f,0.2f,0.5f));
            Check(preview.b>preview.r&&preview.a==1f,"red AOE hid blue ice or faded field");
            targeting.ClearRealtimeAoE();targeting.ClearAllHighlights();
            Check(SameColor(map.GetColor(C(5,5)),fire)&&SameColor(map.GetColor(C(6,5)),ice),"preview cleanup changed element colors");
        });
    }
}
