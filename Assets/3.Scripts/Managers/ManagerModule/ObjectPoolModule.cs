using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

public class ObjectPoolModule
{
    //������Ʈ �ϳ��� �����ϴ� ��û ���!
    //������Ʈ �ϳ��� ����� �Ŷ��, � �͵��� �ʿ��ұ�?
    //���� �� �ϸ� �Ǵµ�?
    PoolSetting _setting;
    public PoolSetting Setting => _setting;

    Transform rootTransform;

    //"��⿭"�� ���� �̴ϴ�!
    //                                          ��  ��  ��  ��
    //                                Queue => ���� �� �ְ� ���� ����
    //"��⿭"�� ������ ���� ���! => ť�� ��´�! / ������!
    //������ => �ۿ� �ִ� �Ÿ� ���� �����Ϳ�
    //          �� ��������
    //          ��   ��         ��   �� => Stack
    Queue<GameObject> prepareQueue = new();

    //�۾����� �ֵ��� �� Queue�� Stack�� �ƴ���?
    //���µ� ���� ����
    //List<GameObject> inProgressList = new();

    //������ ��, "4���� ���� �̻�, ������ ���� �ڰ��� 2�� �̻�, ������ ���� ��� 3�� �̻�"
    //������ �� ������ �־��ֱ�!
    //������! ��ȯ�� => ����, �̸� => ����
    public ObjectPoolModule(PoolSetting newSetting)
    {
        _setting = newSetting;
    }

    public void Initialize()
    {
        //�θ� - �ڽ� ���踦 �����!
        //����ó�� �� �� �ִ� ���� �����Կ�!
        rootTransform = new GameObject(Setting.poolName).transform;


        Setting.target?.TryAddComponent<PooledObject>();
        //���� �ϸ鼭 �̴Ͼ��� 30�� �� �Ŵϱ�! �� ��ŭ �غ� �̸� �س��ƾ���!
        //���ο� ������Ʈ�� �̸� ����ų ����!
        //��Ŀ 7�� ������ּ���.
        //�巳�� �տ� �� �ذ� �ִ� ģ�� ����
        PrepareObjects(Setting.countInitial);
    }

    //�����! => ������ ���ַ���!
    //����ϰ� �ִ� �ֵ� �߿� �ƹ��� �����͵� �Ǵ°�?
    //���� 6�� ���, ��ħ 9�� ���, ��ħ 10�� ���
    //1�� ���������� �� => ��ħ 9�� �� ����!
    //���� �� �ִ�???
    GameObject PrepareObject()
    {
        //Fake Null Check
        if (!Setting.target) return null;
        GameObject result = ObjectManager.CreateObject(Setting.target, rootTransform);
        EnqueueObject(result);
        return result;
    }

    //uint => ���̳ʽ��� �����ϸ� �ȵ�!
    //unsigned => ��ȣ ����!
    void PrepareObjects(uint count)
    {
        if (!Setting.target) return;
        for (uint i = 0; i < count; i++)
        {
            GameObject result = CreateFromPrefab();
            EnqueueObject(result);
        }
    }

    //�������� ������ �޾ƿ�, ������ �⸧���� ��Դϴ�. => ������ �����ؼ� => ����� ���� ������ => �װ� �޾ƿ;� ��
    //													���ѹα����� ���� ��۰Ÿ��� �� ��
    //���⿡���� ���� �۾��� ���ʿ� ���ϴ� �� �־�� => �������� �� �־�� ���� �� ���ɻ� ����!
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

    //������Ʈ�� �����ش޶�� ��Ź!
    public GameObject CreateObject(Transform parent = null)
    {
        GameObject result;
        //��⿭�� �ƹ��� ���� ��
        if (!prepareQueue.TryDequeue(out result))
        {
            //���� ����ڸ� �̾Ƽ� �������� �˴ϴ�!
            //�߰��� ������ �� ���� ������� �ϴ� ���� ���ڷ� �����س��ұ� ����!
            PrepareObjects(Setting.countAdditional, out result);
        }

        if (result) //��������ٸ�!
        {

               //�����ϴ� ����� ��� �ɱ�?
                
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

    //������Ʈ�� �����ش޶�� ��Ź!
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

		//��⿭�� �ֱ�!
		prepareQueue.Enqueue(target);
	}
}
