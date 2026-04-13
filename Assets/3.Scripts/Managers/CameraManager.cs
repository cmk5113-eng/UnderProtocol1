using System.Collections;
using System.Collections.Generic;
using UnityEditor.MPE;
using UnityEngine;
using UnityEngine.EventSystems;

public class CameraManager : ManagerBase
{
	public Camera MainCamera { get; private set; }
	public Physics2DRaycaster	Raycaster2D { get; private set; }
	public PhysicsRaycaster		Raycaster3D { get; private set; }

	protected override IEnumerator OnConnected(GameManager newManager)
	{
		SetMainCamera(Camera.main);
		yield return null;
	}

	protected override void OnDisconnected()
	{

	}

	public void SetMainCamera(Camera wantCamera)
	{
		MainCamera = wantCamera;
		if(MainCamera)
		{
			Raycaster2D = wantCamera.GetComponent<Physics2DRaycaster>();
			Raycaster3D = wantCamera.GetComponent<PhysicsRaycaster>();
		}
	}

	public void GetRaycastResult2D(Vector2 screenPosition, List<RaycastResult> outResult)
	{
		PointerEventData eventData = new(EventSystem.current);
		eventData.position = screenPosition;
		if(Raycaster2D) Raycaster2D.Raycast(eventData, outResult);
	}

	public void GetRaycastResult3D(Vector2 screenPosition, List<RaycastResult> outResult)
	{
		//현재 이벤트 시스템에서 무언가를 가져와줘야 함!
		PointerEventData eventData = new(EventSystem.current);
		eventData.position = screenPosition;
		//결과물은 왜 여러개가 나오나요?
		//뚫고 가야 하는 이유!
		//오버워치 => 아나 아마리 : 공격이 히트스캔 => 레이캐스트를 해서 맞은 대상에게 공격
		//                         앞에 아군 => 힐
		//                         앞에 적군 => 딜
		//앞에 풀피인 탱커가 알짱거리고 있어요! => 딜 넣고 싶음
		//풀피일 때에는 탱커 힐을 무시하고 뒤에 있는 적군에게 딜을 넣을 수 있어야 함!
		//공격 눌러놓고 NPC랑 몬스터랑 겹쳐있으면 => 몬스터를 때린다!
		//마우스 클릭하다가 갑자기 상대 위쪽으로 이펙트가 겹쳐서 이펙트가 클릭되면=>??
		if (Raycaster3D) Raycaster3D.Raycast(eventData, outResult);
	}
}
