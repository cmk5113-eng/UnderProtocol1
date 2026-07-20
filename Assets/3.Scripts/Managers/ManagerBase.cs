using System.Collections;
using UnityEngine;

//class		: ���� o �Լ� ���� o ��ü���� o
//�߻�����
//                    �������� ��������
//abstract	: ���� o �Լ� ���� �� ��ü���� x 
//interface	: ���� x �Լ� ���� x ��ü���� x

//interface	: (ž���� �� �ִ� - ž�� / �������) -> �����, �� ..
//abstract	: �ڵ��� => �߻����� ����
//abstract	: �¿���
//abstract	: ����        : ������ X
//class		: SŬ���� => �з� : ������ O => instance
//instance	: 354�� 2384 => ���� ��ü

public abstract class ManagerBase : MonoBehaviour
{
    GameManager _connectedManager;

    //������Ƽ���� virtual�� �� �� �ִ�!
    public virtual int LoadCount => 1;

    //Connect�� �����Ӱ� �ϱ� ���ؼ� Virtual�� ���� �ǵ�!
    //virtual�� ������ �ϴ� ���� �����ؾ� �ϴ� ��!
    //OCP => Open Closed Principle : ��������Ģ (Ȯ�忡�� ���������� �������� ��������)
    public IEnumerator Connect(GameManager newManager)
    {
        if (_connectedManager != null) Disconnect(); //�̹� ����� �ְ� ������ ���� ����!

        _connectedManager = newManager;
        yield return OnConnected(newManager);
    }

    public void Disconnect()
    {
        _connectedManager = null;
        OnDisconnected();
    }

    //virtual ��ſ� abstract : �θ𿡼� �������� �ʰڴ�!
    //                          �ڽ��� �˾Ƽ� ������!
    protected abstract IEnumerator OnConnected(GameManager newManager);
    protected abstract void OnDisconnected();

}