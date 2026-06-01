using UnityEngine;

public interface IOpenable
{
	//ISP => Interface Segragation Principle => �������̽� �и� ��Ģ
	public bool IsOpen { get; }
	public void Open(); //isOpenable�� �ִ� ��� : ����
	public void Close(); //isClosable : ����
	public void Toggle(); //isTogglable : ����
}
