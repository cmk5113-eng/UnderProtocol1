using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static ModeManager;

public class BattleManager : ManagerBase
{



    // 💡 게임 모드 에뉴머레이션 (몬스터턴과 플레이어턴 포함)
    public enum TurnMode
    {
        PlayerTurn,
        MonsterTurn
    }

    [Header("턴 및 웨이브 상태")]
    [SerializeField] private TurnMode currentTurnMode = TurnMode.PlayerTurn;
    [SerializeField] public static int currentTurn = 1;       // 현재 턴 변수
    [SerializeField] public static int currentWave = 1;       // 현재 웨이브 변수

    [Header("캐릭터 리스트 관리")]
    // 씬에 배치된 플레이어와 몬스터들을 관리할 리스트
    [SerializeField] private List<CharacterBase> playerCharacters = new List<CharacterBase>();
    [SerializeField] private List<CharacterBase> monsterCharacters = new List<CharacterBase>();

    private static BattleManager instance;
    public static BattleManager Instance
    {
        get
        {
            // 💡 인스턴스가 비어있다면 씬 내부에서 직접 컴포넌트를 찾아와 세팅합니다.
            if (instance == null)
            {
                instance = FindFirstObjectByType<BattleManager>();

                // 유니티 구버전을 사용 중이시라면 아래 코드를 사용하세요.
                // instance = FindObjectOfType<BattleManager>();
            }
            return instance;
        }
    }
    
    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // 첫 번째 플레이어 턴 시작
        StartPlayerTurn();
    }

    /// <summary>
    /// 플레이어 캐릭터들이 행동(AP 소비)할 때마다 호출하여 모두 0이 되었는지 체크하는 함수
    /// </summary>
    public void CheckPlayerApAndTryEndTurn()
    {
        if (currentTurnMode != TurnMode.PlayerTurn) return;

        bool allPlayersApZero = true;
        foreach (var player in playerCharacters)
        {
            // 캐릭터에 ap나 mobility 등의 행동력 변수가 있다고 가정 (ex: player.CurrentAp)
            // 여기서는 예시로 player.ap 속성을 체크합니다. 본인 프로젝트의 변수명에 맞게 수정하세요.
            if (player != null && player.actionPoint > 0)
            {
                allPlayersApZero = false;
                break;
            }
        }

        // 💡 모든 캐릭터의 AP가 0이 되면 턴 종료 실행
        if (allPlayersApZero)
        {
            Debug.Log("[Battle] 모든 플레이어의 AP가 0이 되어 자동으로 턴을 종료합니다.");
            EndTurn();
        }
    }

    /// <summary>
    /// 턴 종료 함수 (턴 종료 버튼에 이 함수를 바인딩하면 됩니다!)
    /// </summary>
    public void EndTurn()
    {
        if (currentTurnMode == TurnMode.PlayerTurn)
        {
            Debug.Log($"[Battle] 플레이어 턴 {currentTurn} 종료.");
            // 💡 턴 종료가 되면 몬스터 턴으로 변경 및 시작
            StartMonsterTurn();
        }
    }

    /// <summary>
    /// 몬스터 턴 시작 처리
    /// </summary>
    public void StartMonsterTurn()
    {
        currentTurnMode = TurnMode.MonsterTurn;

        // ModeManager가 있다면 연동 (필요 시 주석 해제)
        // if (ModeManager.Instance != null) ModeManager.Instance.CurrentMode = ModeManager.GameMode.MonsterTurn;

        // 💡 몬스터턴이 시작되면 몬스터턴 함수를 실행
        StartCoroutine(MonsterTurnRoutine());
    }

    /// <summary>
    /// 💡 몬스터턴 함수 (AI 행동을 순차적으로 처리하기 위해 코루틴 사용)
    /// </summary>
    public IEnumerator MonsterTurnRoutine()
    {
        Debug.Log("[Battle] 몬스터 턴 시작 - AI 행동 연산 중...");

        //살아있는 모든 몬스터 순회하며 AI 행동 처리
        foreach (var monster in monsterCharacters)
        {
            if (monster == null) continue;

            // 여기서 몬스터 이동 및 공격 코드를 실행시킵니다.
            // ex) yield return monster.GetComponent<MonsterAI>().ExecuteTurn();
            yield return new WaitForSeconds(1.0f); // 비주얼적 대기 시간 가정
        }

        // 몬스터들의 행동이 전부 끝나면
        EndMonsterTurn();
    }

    /// <summary>
    /// 몬스터 턴 종료 및 플레이어 턴 복귀
    /// </summary>
    public void EndMonsterTurn()
    {
        Debug.Log($"[Battle] 몬스터 턴 {currentTurn} 종료.");

        // 💡 종료되면 플레이어턴으로 변경
        currentTurnMode = TurnMode.PlayerTurn;

        // 💡 현재 턴 변수 +1
        currentTurn++;

        StartPlayerTurn();
    }

    /// <summary>
    /// 플레이어 턴 시작 (AP 리셋 등)
    /// </summary>
    public void StartPlayerTurn()
    {
        Debug.Log($"[Battle] 플레이어 턴 {currentTurn} 시작!");

        if (ModeManager.Instance != null)
            ModeManager.Instance.CurrentMode = ModeManager.GameMode.Movement;

        // 플레이어 캐릭터들의 AP를 다시 최대치로 채워주는 로직
        foreach (var player in playerCharacters)
        {
            if (player != null)
            {
                
            }
        }
    }

    /// <summary>
    /// 💡 몬스터가 죽을 때마다 호출해 주어야 하는 함수
    /// </summary>
    public void OnMonsterDead(CharacterBase deadMonster)
    {
        if (monsterCharacters.Contains(deadMonster))
        {
            monsterCharacters.Remove(deadMonster);
        }

        // 💡 몬스터가 다 죽었는지 검사
        if (monsterCharacters.Count == 0)
        {
            NextWave();
        }
    }

    /// <summary>
    /// 💡 다음 웨이브 진행 함수
    /// </summary>
    private void NextWave()
    {
        StopAllCoroutines(); // 진행 중이던 몬스터 턴 루틴 강제 종료

        // 💡 현재 웨이브 변수에 +1
        currentWave++;
        Debug.Log($"[Battle] ★ 축하합니다! 모든 몬스터 처치. 다음 웨이브 {currentWave} 진행 ★");

        // ObjectManager를 통해 새로운 몬스터 리스트를 생성/가져오기
        SpawnNextWaveMonsters();

        // 새 웨이브 시작 시 플레이어 턴으로 초기화
        currentTurn = 1;
        currentTurnMode = TurnMode.PlayerTurn;
        StartPlayerTurn();
    }

    private void SpawnNextWaveMonsters()
    {
        // 💡 아까 공유해주신 ObjectManager를 활용한 예시 스폰 구조
        // GameObject newMonster = ObjectManager.CreateObject("Orc", new Vector3(2, 3, 0));
        // if(newMonster != null) monsterCharacters.Add(newMonster.GetComponent<CharacterBase>());
    }

    protected override IEnumerator OnConnected(GameManager newManager)
    {
        yield return null;
    }

    protected override void OnDisconnected()
    {

    }
}