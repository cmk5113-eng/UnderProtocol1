using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

public class ObjectPoolModule
{
    //오브젝트 하나를 관리하는 하청 모듈!
    //오브젝트 하나를 담당할 거라면, 어떤 것들이 필요할까?
    //내가 뭘 하면 되는데?
    PoolSetting _setting;
    public PoolSetting Setting => _setting;

    Transform rootTransform;

    //"대기열"을 만들 겁니다!
    //                                          선  입  선  출
    //                                Queue => 먼저 온 애가 먼저 간다
    //"대기열"에 스스로 들어가는 경우! => 큐를 잡는다! / 돌린다!
    //진열대 => 밖에 있는 거를 먼저 가져와요
    //          맨 마지막에
    //          후   입         선   출 => Stack
    Queue<GameObject> prepareQueue = new();

    //작업중인 애들은 왜 Queue나 Stack이 아니죠?
    //가는데 순서 없다
    //List<GameObject> inProgressList = new();

    //구인할 때, "4년제 대학 이상, 슬라임 조련 자격증 2급 이상, 슬라임 배양소 경력 3년 이상"
    //생성할 때 정보를 넣어주기!
    //생성자! 반환값 => 본인, 이름 => 본인
    public ObjectPoolModule(PoolSetting newSetting)
    {
        _setting = newSetting;
    }

    public void Initialize()
    {
        //부모 - 자식 관계를 만들기!
        //폴더처럼 쓸 수 있는 것을 만들어볼게요!
        rootTransform = new GameObject(Setting.poolName).transform;


        Setting.target?.TryAddComponent<PooledObject>();
        //게임 하면서 미니언을 30개 쓸 거니까! 그 만큼 준비를 미리 해놓아야지!
        //새로운 오브젝트를 미리 대기시킬 거임!
        //탱커 7명 대기해주세요.
        //드럼통 앞에 불 쬐고 있는 친구 세명
        PrepareObjects(Setting.countInitial);
    }

    //대기자! => 관리를 해주려면!
    //대기하고 있는 애들 중에 아무나 데려와도 되는가?
    //새벽 6시 출근, 아침 9시 출근, 아침 10시 출근
    //1명 데려가려고 함 => 아침 9시 너 나와!
    //먼저 온 애는???
    GameObject PrepareObject()
    {
        //Fake Null Check
        if (!Setting.target) return null;
        GameObject result = ObjectManager.CreateObject(Setting.target, rootTransform);
        EnqueueObject(result);
        return result;
    }

    //uint => 마이너스가 존재하면 안됨!
    //unsigned => 부호 없는!
    void PrepareObjects(uint count)
    {
        if (!Setting.target) return;
        for (uint i = 0; i < count; i++)
        {
            GameObject result = CreateFromPrefab();
            EnqueueObject(result);
        }
    }

    //여수에서 원유를 받아요, 여수의 기름값은 비쌉니다. => 원유를 공정해서 => 본사로 보낸 다음에 => 그걸 받아와야 함
    //													대한민국에서 가장 운송거리가 긴 곳
    //여기에서도 뭔가 작업을 애초에 안하는 게 있어야 => 빼돌리는 게 있어야 조금 더 성능상 좋다!
    void PrepareObjects(uint count, out GameObject activeObject)
    {
        if (!Setting.target)
        {
            activeObject = null;
            return;
        }

        activeObject = CreateFromPrefab();

        for (uint i = 1; i < count; i++)
        {
            GameObject result = CreateFromPrefab();
            EnqueueObject(result);
        }
    }

    public GameObject CreateFromPrefab()
    {
        GameObject result = ObjectManager.CreateObject(Setting.target, rootTransform);

        if (result)
        {
            result.name = Setting.poolName;

            if (result.TryGetComponent(out PooledObject pool))
            {
                pool.OnEnqueueEvent -= DestroyObject;
                pool.OnEnqueueEvent += DestroyObject;

            }
        }
        return result;
    }

    //오브젝트를 생성해달라고 부탁!
    public GameObject CreateObject(Transform parent = null)
    {
        GameObject result;
        //대기열에 아무도 없을 때
        if (!prepareQueue.TryDequeue(out result))
        {
            //새로 대기자를 뽑아서 가져오면 됩니다!
            //추가할 때마다 몇 개씩 넣으라고 하는 것을 숫자로 설정해놓았기 때문!
            PrepareObjects(Setting.countAdditional, out result);
        }

        if (result) //만들어졌다면!
        {

               //제거하는 방법은 어떻게 될까?
                
           if (result.TryGetComponent(out PooledObject pool))
            {
                pool.OnDequeue();
            }
            result.SetActive(true);
            Transform currentTransform = result.transform;
            Transform originTransform = Setting.target.transform;
            currentTransform.SetParent(parent);

            if (currentTransform is RectTransform asRectTransform
                 && originTransform is RectTransform originRectTransform)
            {
                asRectTransform.anchorMin = originRectTransform.anchorMin;
                asRectTransform.anchorMax = originRectTransform.anchorMax;
                asRectTransform.pivot = originRectTransform.pivot;

                if(parent)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(parent.transform as RectTransform);
                }

                bool stretchX = asRectTransform.anchorMin.x != asRectTransform.anchorMax.x;
                bool stretchY = asRectTransform.anchorMin.y != asRectTransform.anchorMax.y;
                if (stretchX || stretchY)
                { 
                    asRectTransform.offsetMin = originRectTransform.offsetMin;
                    asRectTransform.offsetMax = originRectTransform.offsetMax;

                    //if(stretchX)
                    //{ 
                    //asRectTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, originRectTransform.offsetMin.x, 0);
                    //asRectTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Right, -originRectTransform.offsetMin.x, 0);
                    //}
                    //if (stretchY)
                    //{
                    //    asRectTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Bottom, originRectTransform.offsetMin.y, 0);
                    //    asRectTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, originRectTransform.offsetMin.y, 0);

                    //}
                }
                else 
                {
                    asRectTransform.anchoredPosition = originRectTransform.anchoredPosition;
                    asRectTransform.sizeDelta = originRectTransform.sizeDelta;
                }
                   
            }
            else
            {
            currentTransform.localPosition = originTransform.localPosition;

            }
            currentTransform.localRotation = originTransform.localRotation;
            currentTransform.localScale = originTransform.localScale;


        }
        return result;
    }

    //오브젝트를 제거해달라고 부탁!
    public void DestroyObject(GameObject target)
	{
        EnqueueObject(target);
        if (target)
        {
            target.transform.SetParent(rootTransform);
        }

	}

	public void EnqueueObject(GameObject target)
	{
		if (!target) return;	

		target.SetActive(false);

		//대기열에 넣기!
		prepareQueue.Enqueue(target);
	}
}
