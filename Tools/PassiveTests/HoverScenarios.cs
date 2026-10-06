using System;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

// Runs the real hover UI, character damage and TileData with the harness's Unity API doubles.
public static class HoverScenarios
{
    static int checks;
    static UI_TargetHoverInfo panel;
    static TextMeshProUGUI summary;
    static Image image;
    static Tilemap map;
    static Vector3Int C(int x,int y) => new Vector3Int(x,y);
    static void Check(bool value,string message)
    {
        checks++;
        if (!value) throw new Exception("Hover: "+message);
    }
    static void Set(object owner,string field,object value)
        => owner.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(owner,value);
    static void Call(string method) => typeof(UI_TargetHoverInfo)
        .GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(panel,null);
    static void Setup(bool follow=false)
    {
        UnityEngine.Object.ResetScene();
        InputManager.ResetHover();
        map=new Tilemap();
        PlacementManager.Instance=new PlacementManager { tilemap=map };
        BattleManager.HP=100;
        panel=new UI_TargetHoverInfo();
        summary=new TextMeshProUGUI(); summary.transform.SetParent(panel.transform);
        image=new Image(); image.transform.SetParent(panel.transform);
        Set(panel,"summaryText",summary); Set(panel,"portrait",image); Set(panel,"followCursor",follow);
        Call("Awake"); Call("OnEnable");
    }
    static void Refresh(Vector3 world,GameObject hovered=null)
    {
        InputManager.SetCursor(world,hovered);
        Call("LateUpdate");
    }
    static void Refresh(Vector3Int cell,GameObject hovered=null) => Refresh(map.GetCellCenterWorld(cell),hovered);
    static MonsterBase Monster(Vector3Int cell,string name="적",int hp=7)
    {
        var monster=new MonsterBase();
        monster.gameObject.name="EnemyPrefab(Clone)";
        monster.transform.position=map.GetCellCenterWorld(cell);
        var data=new MonsterData { monsterName=name,hp=hp,atk=3 };
        monster.Initialize(data);
        monster.gameObject.Attach(new Collider2D());
        monster.gameObject.Attach(new SpriteRenderer { sprite=new Sprite() });
        return monster;
    }
    static CharacterBase Player(Vector3Int cell)
    {
        var character=new CharacterBase { currentHP=0,MaxHP=0,actionPoint=2,maxAP=3,steminaPoint=4,maxStemina=5 };
        character.transform.position=map.GetCellCenterWorld(cell);
        var data=new CharacterData { characterName="이성계",Portrait=new Sprite(),actionPoint=99,steminaPoint=99 };
        typeof(CharacterBase).GetField("characterData",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(character,data);
        PlacementManager.Instance.GetTileData(cell).Character=character;
        return character;
    }
    static void Case(string name,Action run)
    {
        Setup(); run(); Console.WriteLine("PASS hover: "+name);
    }
    public static void RunAll()
    {
        Case("monster data name, portrait fallback and live HP without another hover event",()=>
        {
            var monster=Monster(C(4,4),"돌격병",7);
            Refresh(C(4,4),monster.gameObject);
            Check(summary.text.Contains("돌격병")&&summary.text.Contains("HP: 7 / 7"),"data name and initialized HP");
            Check(summary.text.Contains("공격력: 3"),"data attack");
            Check(image.sprite==monster.GetComponent<SpriteRenderer>().sprite&&image.enabled&&image.preserveAspect,"monster sprite fallback");
            monster.TakeDamage(2); Call("LateUpdate");
            Check(summary.text.Contains("HP: 5 / 7"),"damage updates while cursor stays still");
            monster.Heal(1); Call("LateUpdate");
            Check(summary.text.Contains("HP: 6 / 7"),"healing updates while cursor stays still");
        });
        Case("child collider resolves to its unit and child sprite supplies an image",()=>
        {
            var monster=Monster(C(5,5));
            monster.GetComponent<SpriteRenderer>().sprite=null;
            var child=new GameObject(); child.transform.SetParent(monster.transform);
            child.transform.position=monster.transform.position;
            var sprite=new Sprite(); child.Attach(new SpriteRenderer { sprite=sprite });
            child.Attach(new Collider2D());
            Refresh(C(5,5),child);
            Check(summary.text.Contains("HP: 7 / 7"),"hover child uses parent CharacterBase");
            Check(image.sprite==sprite,"sprite lookup includes children");
        });
        Case("player uses shared barrier and runtime resources even with zero individual HP",()=>
        {
            var player=Player(C(0,4)); BattleManager.HP=73;
            Refresh(C(0,4));
            Check(summary.text.Contains("이성계")&&summary.text.Contains("공용 결계 HP: 73"),"player is live without individual HP");
            Check(summary.text.Contains("행동력: 2/3")&&summary.text.Contains("이동력: 4/5"),"runtime resources, not asset defaults");
            Check(image.sprite==player.Data.Portrait,"character portrait");
            player.actionPoint=0; player.steminaPoint=1; BattleManager.HP=42; Call("LateUpdate");
            Check(summary.text.Contains("공용 결계 HP: 42")&&summary.text.Contains("행동력: 0/3")&&summary.text.Contains("이동력: 1/5"),"resources update without cursor movement");
        });
        Case("empty inner and outer tiles show their sprite, role and display coordinates",()=>
        {
            var sprite=new Sprite(); map.sprites[C(3,6)]=sprite;
            Refresh(C(3,6));
            Check(summary.text.Contains("내부 타일")&&summary.text.Contains("비어 있음"),"inner empty tile");
            Check(summary.text.Contains("몬스터 배치 영역")&&summary.text.Contains("좌표: (4, 4)"),"inner role and inverted-Y coordinate");
            Check(image.sprite==sprite,"actual tile sprite");
            Refresh(C(0,4));
            Check(summary.text.Contains("외곽 타일")&&summary.text.Contains("아군 이동·배치 영역")&&summary.text.Contains("좌표: (1, 6)"),"outer tile");
            Check(!image.enabled&&image.sprite==null,"missing tile sprite clears previous portrait");
        });
        Case("tile coordinates follow a translated, scaled map with negative cell origin",()=>
        {
            map.cellBounds=new BoundsInt(-8,-5,0,10,10,1);
            map.transform.position=new Vector3(30,-20); map.scale=2;
            Refresh(C(-8,4));
            Check(summary.text.Contains("외곽 타일")&&summary.text.Contains("좌표: (1, 1)"),"top-left logical position");
            Refresh(C(-5,-1));
            Check(summary.text.Contains("내부 타일")&&summary.text.Contains("좌표: (4, 6)"),"scaled world-to-cell coordinate");
        });
        Case("tile center finds an unregistered monster from the tile's corner",()=>
        {
            var monster=Monster(C(6,6));
            Check(PlacementManager.Instance.GetTileData(C(6,6)).Character==null,"wave-style monster has no tile registration");
            Refresh(map.GetCellCenterWorld(C(6,6))+new Vector3(0.35f,0.35f));
            Check(summary.text.Contains("HP: 7 / 7"),"small collider's tile still shows monster");
        });
        Case("moved unit is not retained on its old tile",()=>
        {
            var player=Player(C(0,4)); Refresh(C(0,4));
            player.transform.position=map.GetCellCenterWorld(C(0,5));
            Call("LateUpdate");
            Check(!summary.text.Contains("이성계"),"stale old-tile registration does not retain the moved unit");
            PlacementManager.Instance.GetTileData(C(0,4)).Character=null;
            PlacementManager.Instance.GetTileData(C(0,5)).Character=player;
            Call("LateUpdate");
            Check(summary.text.Contains("외곽 타일")&&!summary.text.Contains("이성계")&&summary.text.Contains("비어 있음"),"old tile refreshes after movement");
            Refresh(C(0,5)); Check(summary.text.Contains("이성계"),"new tile shows player");
        });
        Case("dead or inactive unit never leaves old HP in the panel",()=>
        {
            var cell=C(4,4); var monster=Monster(cell); var tile=PlacementManager.Instance.GetTileData(cell);
            tile.Character=monster; Refresh(cell,monster.gameObject);
            monster.TakeDamage(7); Call("LateUpdate");
            Check(tile.Character==null&&summary.text.Contains("비어 있음")&&!summary.text.Contains("HP:"),"damage death returns to tile info");
            monster=Monster(cell); Refresh(cell,monster.gameObject); monster.gameObject.SetActive(false);
            Call("LateUpdate"); Check(!summary.text.Contains("HP:"),"inactive cached hover ignored");
            monster=Monster(cell); Refresh(cell,monster.gameObject); monster.currentHP=0;
            Call("LateUpdate"); Check(!summary.text.Contains("HP:"),"dead unit ignored even before its GameObject is removed");
        });
        Case("off-map, missing map and disabled map reset both text and image",()=>
        {
            var monster=Monster(C(4,4)); Refresh(C(4,4),monster.gameObject);
            Refresh(C(15,15),monster.gameObject);
            Check(summary.text.Contains("커서를 유닛이나 타일에 올리세요.")&&!image.enabled&&image.sprite==null,"outside map clears display");
            Refresh(C(4,4),monster.gameObject); map.gameObject.SetActive(false); Call("LateUpdate");
            Check(summary.text.Contains("대상 정보")&&!summary.text.Contains("HP:"),"inactive map clears display");
            PlacementManager.Instance=null; Call("LateUpdate");
            Check(!image.enabled,"missing manager safe");
        });
        Case("UI controls do not expose a tile behind them and information graphics ignore raycasts",()=>
        {
            Check(!summary.raycastTarget&&!image.raycastTarget,"panel graphics do not block world hover");
            var button=new Selectable(); var child=new GameObject(); child.transform.SetParent(button.transform);
            Refresh(C(4,4),child);
            Check(summary.text.Contains("대상 정보")&&!summary.text.Contains("내부 타일"),"button child prevents world info");
            Refresh(C(4,4),image.gameObject);
            Check(summary.text.Contains("대상 정보"),"self hover does not inspect world behind panel");
        });
        Case("stage reopening and replacing the map do not retain the old unit",()=>
        {
            var monster=Monster(C(4,4)); Refresh(C(4,4),monster.gameObject);
            Call("OnDisable"); Call("OnEnable");
            Check(summary.text.Contains("대상 정보")&&!image.enabled,"enable clears previous selection");
            map=new Tilemap(); map.transform.position=new Vector3(100,100);
            PlacementManager.Instance.tilemap=map;
            Refresh(C(3,3),monster.gameObject);
            Check(summary.text.Contains("내부 타일")&&!summary.text.Contains("HP:"),"old map unit rejected");
        });
        Case("fixed panel stays put; existing popup follows cursor and releases both subscriptions",()=>
        {
            var manager=new UIManager(); panel.transform.position=new Vector3(20,30);
            panel.Registration(manager); var monster=Monster(C(4,4)); Refresh(C(4,4),monster.gameObject);
            Check(panel.transform.position.x==20&&panel.transform.position.y==30,"fixed panel position");
            panel.Unregistration(manager);
            Setup(true); monster=Monster(C(4,4));
            Set(panel,"shiftedPosition",new Vector2(10,15));
            panel.Registration(manager); panel.Registration(manager);
            Refresh(C(4,4),monster.gameObject);
            Check(panel.IsOpen&&summary.text.Contains("HP: 7 / 7"),"legacy popup still works");
            Check(panel.transform.position.x==14.5f&&panel.transform.position.y==19.5f,"legacy cursor offset");
            var flags=BindingFlags.Static|BindingFlags.NonPublic;
            var hover=(Delegate)typeof(InputManager).GetField("OnMouseHover",flags).GetValue(null);
            var move=(Delegate)typeof(InputManager).GetField("OnMouseMove",flags).GetValue(null);
            Check(hover.GetInvocationList().Length==1&&move.GetInvocationList().Length==1,"repeated registration does not duplicate callbacks");
            panel.Unregistration(manager);
            Check(typeof(InputManager).GetField("OnMouseHover",flags).GetValue(null)==null&&typeof(InputManager).GetField("OnMouseMove",flags).GetValue(null)==null,"unregistration removes hover and move");
            panel.Registration(manager); Call("OnDestroy");
            Check(typeof(InputManager).GetField("OnMouseMove",flags).GetValue(null)==null,"destroy also releases subscriptions");
        });
        Console.WriteLine("PASS: "+checks+" hover assertions");
    }
}
