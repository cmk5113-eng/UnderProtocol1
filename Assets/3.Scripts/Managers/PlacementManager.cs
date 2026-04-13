using System.Collections;
using UnityEngine;

public class PlacementManager : ManagerBase
{
    public int characterCounter = 0;

    // 외부(Button)에서 이 인스턴스에 접근하기 위한 프로퍼티 (싱글톤 예시)
    public static PlacementManager Instance { get; private set; }

    protected override IEnumerator OnConnected(GameManager newManager)
	{
		yield return null;
	}

	protected override void OnDisconnected()
	{

	}
}
