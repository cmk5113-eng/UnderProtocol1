using UnityEngine;

public interface IOpenable
{
	//ISP => Interface Segragation Principle => 인터페이스 분리 원칙
	public bool IsOpen { get; }
	public void Open(); //isOpenable만 있는 경우 : 숏컷
	public void Close(); //isClosable : 봉인
	public void Toggle(); //isTogglable : 레버
}
