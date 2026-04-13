using System.Collections;
using UnityEngine;

//class		: 변수 o 함수 내용 o 객체생성 o
//추상적인
//                    있을수도 없을수도
//abstract	: 변수 o 함수 내용 Δ 객체생성 x 
//interface	: 변수 x 함수 내용 x 객체생성 x

//interface	: (탑승할 수 있는 - 탑승 / 하차기능) -> 비행기, 말 ..
//abstract	: 자동차 => 추상적인 개념
//abstract	: 승용차
//abstract	: 벤츠        : 내놓아 X
//class		: S클래스 => 분류 : 내놓아 O => instance
//instance	: 354너 2384 => 실제 객체

public abstract class ManagerBase : MonoBehaviour
{
    GameManager _connectedManager;

    //프로퍼티에도 virtual을 쓸 수 있다!
    public virtual int LoadCount => 1;

    //Connect를 자유롭게 하기 위해서 Virtual을 써줄 건데!
    //virtual을 쓰려고 하는 순간 생각해야 하는 것!
    //OCP => Open Closed Principle : 개방폐쇄원칙 (확장에는 열려있으나 수정에는 닫혀있음)
    public IEnumerator Connect(GameManager newManager)
    {
        if (_connectedManager != null) Disconnect(); //이미 연결된 애가 있으면 끊고 간다!

        _connectedManager = newManager;
        yield return OnConnected(newManager);
    }

    public void Disconnect()
    {
        _connectedManager = null;
        OnDisconnected();
    }

    //virtual 대신에 abstract : 부모에서 정의하지 않겠다!
    //                          자식이 알아서 만들어라!
    protected abstract IEnumerator OnConnected(GameManager newManager);
    protected abstract void OnDisconnected();

}