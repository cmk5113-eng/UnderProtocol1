using UnityEngine;
using UnityEngine.EventSystems;

public delegate void DragStartEvent(UI_DraggableWindows dragTarget, Vector2 startPosition);

public class UI_DraggableWindows : UIBase, IPointerDownHandler
{
    public event DragStartEvent OnDragStart;

    //드래그하면 어떤 트랜스폼을 움직여야 할까?
    [SerializeField] RectTransform rootTransform;

    /// <summary> 마지막으로 수신받은 마우스의 위치 </summary>
    Vector2 currentScreenPosition;


    Vector2 shiftedPosition;
    public void OnPointerDown(PointerEventData eventData)
    {
        OnDragStart?.Invoke(this, eventData.position);
    }

    public void SetMouseStartPosition(Vector2 screenPosition)
    {
        currentScreenPosition = screenPosition;
        shiftedPosition = Vector2.zero;
    }

    public void SetMouseCurrentPosition(Vector2 screenPosition)
    {
        //마우스의 위치가 바뀌었단 말이죠
        //얼마나 움직였는지 마우스의 값을 받아오기
        //움직인 거리 = 목적지 - 출발지
        //               5   -   3    =  2
        //움직인 거리가 원래 배율이 1이었으면 1만큼 움직이면 됐음!
        //배율이 1.3배가 되었다면 1만큼 움직이고 싶어도 1.3만큼 더 가게 되어버려요!
        //부모의 사이즈를 제거해줘야 하니까 1.3을 1로 만들려면 1.3으로 나눠주면 됩니다!
        Vector2 screenDelta = screenPosition - currentScreenPosition;
        currentScreenPosition = screenPosition;

        if (shiftedPosition.x * screenDelta.x > 0.0f)
        {
            float counter = Mathf.Min(Mathf.Abs(screenDelta.x), Mathf.Abs(shiftedPosition.x));
            counter *= Mathf.Sign(shiftedPosition.x);
            shiftedPosition.x -= counter;
            screenDelta.x -= counter;
            Debug.Log($"{screenDelta.x} {screenDelta} =>{screenPosition}");
            
        }

        if (shiftedPosition.y * screenDelta.y > 0.0f)
        {
            float counter = Mathf.Min(Mathf.Abs(screenDelta.y), Mathf.Abs(shiftedPosition.y));
            counter *= Mathf.Sign(shiftedPosition.y);
            shiftedPosition.y -= counter;
            screenDelta.y -= counter;
        }
        if(screenDelta.sqrMagnitude == 0.0f) return;
       
        //inverseAABB => 대상이 누구인가?
        //이게 갇혀 있는 상자
        //rect에서 실제로 제공하는 위치 : 실제 이 친구의 위치가 X => 왜?
        //월드 포지션, 로컬포지션, 특정 대상 위치 => 뭘...달라는 거야?
        //근데 주고 있음! => 본인의 크기는 맞습니다!
        //위치는 기본적으로 어디를 주는가?
        //본인의 "Pivot"위치를 기준으로!
        Rect rootRect = rootTransform.rect;

        //지금은 안 나갔겠죠? 바뀐 뒤에 비교해봐야 한다
        //                                   원래 위치             +  이동량
        rootRect.position += (Vector2)rootTransform.localPosition + screenDelta;
        //바꾸고 나서 얼만큼 튀어나갔는가를 확인해보기!
        //튀어나온 걸 보정해주는 값을 InversedAABB가 돌려주니까
        //보정해주는 만큼 위치 이동을 자제한다!
        Vector2 overScreen = rootRect.InversedAABB(UIManager.UIBoundary);

        shiftedPosition += overScreen;
        screenDelta += overScreen;


        Vector3 positionDelta = (Vector3)screenDelta;

        if (UIManager.UIScale > 0.0f) positionDelta /= UIManager.UIScale;

        rootTransform.localPosition += positionDelta;
    }
}