using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CameraManager : ManagerBase
{
	public Camera MainCamera { get; private set; }
	
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
		
	}

	
	public void GetRaycastResult(Vector2 screenPosition, List<RaycastResult> outResult)
	{
        EventSystem currentEvent = EventSystem.current;
        //���� �̺�Ʈ �ý��ۿ��� ���𰡸� ��������� ��!
        PointerEventData eventData = new (currentEvent);
		eventData.position = screenPosition;
		//������� �� �������� ��������?
		//�հ� ���� �ϴ� ����!
		//������ġ => �Ƴ� �Ƹ��� : ������ ��Ʈ��ĵ => ����ĳ��Ʈ�� �ؼ� ���� ��󿡰� ����
		//                         �տ� �Ʊ� => ��
		//                         �տ� ���� => ��
		//�տ� Ǯ���� ��Ŀ�� ��¯�Ÿ��� �־��! => �� �ְ� ����
		//Ǯ���� ������ ��Ŀ ���� �����ϰ� �ڿ� �ִ� �������� ���� ���� �� �־�� ��!
		//���� �������� NPC�� ���Ͷ� ���������� => ���͸� ������!
		//���콺 Ŭ���ϴٰ� ���ڱ� ��� �������� ����Ʈ�� ���ļ� ����Ʈ�� Ŭ���Ǹ�=>??
        currentEvent.RaycastAll(eventData, outResult);
	}
}
