using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectionManager : ManagerBase
{
    // 싱글톤 인스턴스 (StageUIController가 참조할 수 있도록 유지)
    public static SelectionManager Instance { get; set; }

    // 현재 선택된 캐릭터 (static 변수)
    public static CharacterBase selectCharacter;

    // 스테이지 상에 배치된 아군 리스트
    public List<CharacterBase> unitOnStage = new List<CharacterBase>();

    protected override IEnumerator OnConnected(GameManager newManager)
    {
        // 매니저 연결 시 싱글톤 등록
        Instance = this;
        yield return null;
    }

    protected override void OnDisconnected()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// 외부에서 캐릭터를 선택할 때 호출하는 static 함수
    /// </summary>
    public static void SetSelectedCharacter(CharacterBase character)
    {
        if (character == null) return;

        selectCharacter = character;
        Debug.Log($"[Selection] 현재 선택된 캐릭터가 {character.Name}(으)로 변경되었습니다.");
    }

    /// <summary>
    /// 선택을 해제할 때 호출하는 static 함수
    /// </summary>
    public static void ClearSelectedCharacter()
    {
        selectCharacter = null;
        Debug.Log("[Selection] 캐릭터 선택이 해제되었습니다.");
    }
    
    // 기존의 명칭과 호환성을 위한 역호환성 함수
    public void DeselectCharacter()
    {
        ClearSelectedCharacter();
    }
}