using UnityEngine;

public class UI_Button_CreateObject : MonoBehaviour
{
    [SerializeField] private PoolRequest poolRequest; // ScriptableObject 연결
    [SerializeField] private int poolIndex;            // 이 버튼이 사용할 설정 번호
    [SerializeField] private Transform spawnParent;    // 생성된 오브젝트가 들어갈 부모(맵/캔버스)

    private ObjectPoolModule _myModule;

    void Awake()
    {
        // 1. 시작하자마자 내 인덱스에 맞는 설정으로 모듈 생성 및 초기화
        if (poolRequest != null && poolIndex < poolRequest.settings.Length)
        {
            _myModule = new ObjectPoolModule(poolRequest.settings[poolIndex]);
            _myModule.Initialize();
        }
    }

    public void Create()
    {
        // 2. 버튼 클릭 시 모듈을 통해 오브젝트 소환
        if (_myModule != null)
        {
            GameObject newObj = _myModule.CreateObject(spawnParent);

            // 성공적으로 소환되었다면 UI 카운트 등 추가 로직 실행 가능
            if (newObj != null)
            {
                Debug.Log($"{_myModule.Setting.poolName} 소환 성공!");
            }
        }
    }
}