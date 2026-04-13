using UnityEditor.EditorTools;
using UnityEngine;

public class CreateHa : MonoBehaviour
{
    [SerializeField] private string wantName;
    [SerializeField] private Transform spawnParent;

    public void Create()
    {
        // 1. 단순 생성 (풀링 X, 매번 새로 만듦)
        // ObjectManager.CreateObject(wantName, Vector3.zero);

        // 2. 풀링 사용 (추천: 이름으로 해당 풀을 찾아 꺼내옴)
        // 만약 PoolManager를 만드셨다면 아래처럼 호출하게 됩니다.
        GameObject newCharacter = ObjectManager.CreateObject("Ha", Vector3.back );

        if (newCharacter != null)
        {
            Debug.Log($"{wantName} 캐릭터 소환 완료!");

            // 2. (옵션) UI 카운트 증가 로직을 여기서 부를 수 있습니다.
            // UI_CharcterSelectWindows.Instance.AddCount();
        }
    }

}