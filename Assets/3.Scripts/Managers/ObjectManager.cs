using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;


public class ObjectManager : ManagerBase
{
	//���� ���ο� Global ������ �߰��� �� ���� �ϳ��� �߰��ϸ� ��!
	//�ٲ� �ʿ䰡 ���� => ������ �ƴ϶�, ����� ��! => ���߿� �ٲ�� �ȵ�!
	//�Ϲ����� ����� constant variable�� �½��ϴ�!
	//"�б� ����"���� �ٲ�� �մϴ�!
	readonly string[] globalPoolSettings =
	{
		"GlobalCharacterPool",
		"GlobalControllerPool",
		"GlobalEffectPool",
		"GlobalObjectPool",
		"GlobalUIPool",
	};

	//����ȭ������ => ����Ƽ���� ���� ���ؼ� �� ��!
	//public�̶�� �ϴ� �� ��� �ʿ� ���� ����ȭ�� �Ǹ� ����Ƽ���� �� �� �ִ�!
	//����ȭ ����
	//[SerializeField] PoolSetting[] testSettings;

	//PoolRequest�� �ְ�, �װ��� ���� Ǯ���� �غ��ϱ�
	//PoolRequest�� �����ͼ� �����Ϸ��� � �ڷᱸ���� �ʿ��ұ�?
	//����Ʈ : �迭�� ����ѵ� �߰� ���Ű� ����	, �뷮��, ã�� �ӵ��� ������
	//�߰� ���Ű� ����, ��ü�� ���� ���� ����

	//�迭 : ����Ʈ�� ����ѵ� �߰� ���Ű� �����, �뷮��, ã�� �ӵ��� ������
	//�߰� ���Ű� ����, ��ü�� ���� ���� ����

	//PoolRequest��.. �󸶳� ���� �߰��ɱ�? => �ε��� �� ����?
	//�ε��Ǵ� Ƚ������ ����� ������ �����ϸ� ���� �߰��ϰų� �ϴ� ��!
	List<PoolRequest> loadedPoolRequests = new();

	//�ش��ϴ� �̸��� ������� �ҷ��ֱ� ���ؼ�
	//[�̸� - ���ӿ�����Ʈ] �ڷᱸ��
	static Dictionary<string, ObjectPoolModule> poolDictionary = new();

	//PollRequst���� �� �ȿ��� string���� ã�Ƽ� �� �̸��� �´� GameObject�� ã���� �Ǵϱ�!
	//���� ���� �̸����� �Ȱ��� ������Ʈ�� ������� �ߴµ�..
	protected override IEnumerator OnConnected(GameManager newManager)
	{
        RegistrationInHierachy();
		RegistrationPool(globalPoolSettings);
        InitializePool();

		yield return null;
	}

	protected override void OnDisconnected()
	{

	}
    
	public static GameObject CreateObject(string wantName, Transform parent = null)
	{
        if(string.IsNullOrEmpty(wantName)) return null;

		GameObject result = null;//������ ������ �ϰ͵� ����!
        wantName = wantName.ToLower();
        //�� �̸����� Ǯ���� ��� �Ǿ� �ִ��!
        if (poolDictionary.TryGetValue(wantName, out ObjectPoolModule pool))
        {
            result = pool.CreateObject(parent); //���� �;߰ڴ� ����
        }
        else

        {
            //Ǯ�� ��ϵ��� ���� �߻��� ������Ʈ�� ����� ���!
            //�����Ϳ��� �ִ��� Ȯ���غ���!
            if (DataManager.TryLoadDataFile(wantName, out GameObject prefab))
            {

                if (prefab) result = Instantiate(prefab, parent);

            }
        }
        if(!result) UIManager.ClaimErrorMessage(SystemMessage.ObjectNameNotFound(wantName));

        //������ִ� �� ����!
        RegistrationObject(result); //�� �߿� �ϳ��� �߰���? �ƴ� ����!

		return result;
	}
	public static GameObject CreateObject(GameObject prefab, Transform parent = null)
	{
		if (prefab == null) return null;

		//                                      ���� �����ΰ�
		GameObject result = Instantiate(prefab, parent); //�����
		RegistrationObject(result); //�����
		return result;
	}

	public static GameObject CreateObject(string wantName, Vector3 position)
	{
		GameObject result = CreateObject(wantName);
		if (result) result.transform.position = position;
		return result;
	}
	public static GameObject CreateObject(GameObject prefab, Vector3 position)
	{
		GameObject result = CreateObject(prefab);
		if(result) result.transform.position = position;
		return result;
	}

	public static GameObject CreateObject(string wantName, Vector3 position, Quaternion rotation)
	{
		GameObject result = CreateObject(wantName);
		if (result)
		{
			result.transform.position = position;
			result.transform.rotation = rotation;
		}
		return result;
	}
	public static GameObject CreateObject(GameObject prefab, Vector3 position, Quaternion rotation)
	{
		GameObject result = CreateObject(prefab);
		if (result)
		{
			result.transform.position = position;
			result.transform.rotation = rotation;
		}
		return result;
	}

	public static GameObject CreateObject(string wantName, Vector3 position, Quaternion rotation, Vector3 scale)
	{
		GameObject result = CreateObject(wantName);
		if (result)
		{
			result.transform.position = position;
			result.transform.rotation = rotation;
			result.transform.localScale = scale;
		}
		return result;
	}
	public static GameObject CreateObject(GameObject prefab, Vector3 position, Quaternion rotation, Vector3 scale)
	{
		GameObject result = CreateObject(prefab);
		if (result)
		{
			result.transform.position = position;
			result.transform.rotation = rotation;
			result.transform.localScale = scale;
		}
		return result;
	}

	public static GameObject CreateObject(string wantName, Transform parent, Vector3 position, Space space = Space.Self)
	{
		GameObject result = CreateObject(wantName, parent);
		if (result)
		{
			switch(space)
			{
				case Space.World:
					result.transform.position = position; //���밪�� ��������
					break;
				case Space.Self:
					result.transform.localPosition = position; //�θ� ��������
					break;
			}
		}
		return result;
	}
	public static GameObject CreateObject(GameObject prefab, Transform parent, Vector3 position, Space space = Space.Self)
	{
		GameObject result = CreateObject(prefab, parent);
		if (result)
		{
			switch(space)
			{
				case Space.World:
					result.transform.position = position; //���밪�� ��������
					break;
				case Space.Self:
					result.transform.localPosition = position; //�θ� ��������
					break;
			}
		}
		return result;
	}

	public static GameObject CreateObject(string wantName, Transform parent, Vector3 position, Quaternion rotation, Space space = Space.Self)
	{
		GameObject result = CreateObject(wantName, parent);
		if (result)
		{
			switch (space)
			{
				case Space.World:
					result.transform.position = position; //���밪�� ��������
					result.transform.rotation = rotation;
					break;
				case Space.Self:
					result.transform.localPosition = position; //�θ� ��������
					result.transform.localRotation = rotation; //�θ� ��������
					break;
			}
		}
		return result;
	}
	public static GameObject CreateObject(GameObject prefab, Transform parent, Vector3 position, Quaternion rotation, Space space = Space.Self)
	{
		GameObject result = CreateObject(prefab, parent);
		if (result)
		{
			switch (space)
			{
				case Space.World:
					result.transform.position = position; //���밪�� ��������
					result.transform.rotation = rotation;
					break;
				case Space.Self:
					result.transform.localPosition = position; //�θ� ��������
					result.transform.localRotation = rotation; //�θ� ��������
					break;
			}
		}
		return result;
	}

	public static GameObject CreateObject(string wantName, Transform parent, Vector3 position, Quaternion rotation, Vector3 scale, Space space = Space.Self)
	{
		GameObject result = CreateObject(wantName, parent);
		if (result)
		{
			switch (space)
			{
				case Space.World:
					result.transform.position = position; //���밪�� ��������
					result.transform.rotation = rotation;
					result.transform.localScale	= scale; //�θ� ��������
					break;
				case Space.Self:
					result.transform.localPosition  = position; //�θ� ��������
					result.transform.localRotation  = rotation; 
					result.transform.localScale		= scale; 
					break;
			}
		}
		return result;
	}
	public static GameObject CreateObject(GameObject prefab, Transform parent, Vector3 position, Quaternion rotation, Vector3 scale, Space space = Space.Self)
	{
		GameObject result = CreateObject(prefab, parent);
		if (result)
		{
			switch (space)
			{
				case Space.World:
					result.transform.position = position; //���밪�� ��������
					result.transform.rotation = rotation;
					result.transform.localScale	= scale; //�θ� ��������
					//"��¥ ���� ������" �� �� �� �ֽ��ϴ�.
					// lossyScale
					// ���� "��¥"ũ��� (1,1,1)
					// �ٵ� "�θ�"ũ��� (2,2,2)
					// �̶� "����"ũ��� (0.5, 0.5, 0.5)
					// ���� "��¥" ũ�⸦ => (3,3,3)���� �ϰ� �ʹٰ� �غ���
					// ������ "����"ũ��� ���ϱ��? (1.5, 1.5, 1.5)
					// ���� ȸ���ϴ� ���� ����
					// ��� ���� ���ϴ� "��¥" ũ�Ⱑ �Ǳ� ���� "����"ũ�⸦ ���� �� ������?
					// "��¥ ũ��"�� ���Ѵ� "�θ� ũ��"�� �񱳸� �ؼ� ���� �����ָ� ���ڴ�!
					// ���⼭ ����! �θ��� �θ�� ��� �ؿ�?
					// ���θ� �θ�  ����    ����ũ��       ����ũ��
					// 0.5    1.5  1.2  => 0.9        =>  3
					// 0.5    1.5  4    => 3
					// ���� => ����  ���� * (���� / ����) = ����
					//               1.2 *  (0.9 / 1.2) = 0.9
					// ���� => ����  ���� * (���� / ����) = ����
					//               0.9 *  (1.2 / 0.9) = 1.2
					//               3   *  (4/3)       = 4
					//���ʹ� ����̱� ������, ��� ���� �� �� �ִ� ������ => xy����̶�� yx����̾�� �Ѵ�
					//           (0)
					// (1,2,3) x (1)
 					//           (2)
					//Vector3 originLocalScale = result.transform.localScale;
					//Vector3 originLossyScale = result.transform.lossyScale;
					//float scaledScaleX = scale.x * (originLocalScale.x / originLossyScale.x);
					//float scaledScaleY = scale.y * (originLocalScale.y / originLossyScale.y);
					//float scaledScaleZ = scale.z * (originLocalScale.z / originLossyScale.z);
					//result.transform.localScale = new Vector3(scaledScaleX, scaledScaleY, scaledScaleZ);
					break;
				case Space.Self:
					result.transform.localPosition  = position; //�θ� ��������
					result.transform.localRotation  = rotation; 
					result.transform.localScale		= scale; 
					break;
			}
		}
		return result;
	}

	public static void RegistrationObject(GameObject target) //������ ����ϴ� ���
	{
		if (target)
		{
			//�� ģ���� ��� ���������� ��� üũ�ұ�?
			//���� ����� �� "������Ʈ"�� ����� ������
			//"���� ������Ʈ"�� ����� ���� �ƴϱ� ������
			//IFunctionable�� �� ���� "������Ʈ"��!
			//GetComponent : ������Ʈ�� ������ (���� ù��° ������Ʈ)
			//GetComponent<IFunctionable>() => IFunctionable �ϳ�
			//GetComponents<IFunctionable>() => IFunctionable�� ��ӹ޴� ��� ������Ʈ
			//GetComponentsInChild<IFunctionable>() => (������) �ڽ����� �ִ� IFunctionable�� ��ӹ޴� ��� ������Ʈ
			//GetComponentsInChildren<IFunctionable>() => (������)�ڽĵ����� �ִ� IFunctionable�� ��ӹ޴� ��� ������Ʈ
			foreach (var current in target.GetComponentsInChildren<IFunctionable>())
			{
				current.RegistrationFunctions();
			}
		}
	}

	public static void DestroyObject(GameObject target)
	{
		if (!target) return;
		UnregistrationObject(target);

        if (target.TryGetComponent(out PooledObject pool))
        {
            pool.OnEnqueue();
        }
        else
        {
            Destroy(target);
        }
	}

	public static void UnregistrationObject(GameObject target)
	{
		if (!target) return;

		foreach (var current in target.GetComponentsInChildren<IFunctionable>())
		{
			current.UnregistrationFunctions();
		}
	}
  
    public void RegistrationInHierachy()
    {        
        foreach(MonoBehaviour current in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (current is IFunctionable currentFunctionable)
            { 
                currentFunctionable.RegistrationFunctions();
            }
        }    
    }

	public void RegistrationPool(string poolName)
	{
		//���!
        poolName = poolName.ToLower();
		PoolRequest currentRequest = DataManager.LoadDataFile<PoolRequest>(poolName);
		if (currentRequest == null) return;
        if (currentRequest.settings == null) return;
		loadedPoolRequests.Add(currentRequest);
		//�ֵ鸶�� �ϳ���!
		//        �л�          �����л�    in   3�г� 4��
		foreach (PoolSetting currentSetting in currentRequest.settings)
		{
			string currentName = currentSetting.poolName.ToLower();
			GameObject currentPrefab = currentSetting.target;
			//�����л���.. ���� �б� �ȿԴ��!
			//=> �������� ��������� �ȵǰ�
			//=> ���� �л��� �ҷ��� �Ѵ�!
			if (currentPrefab == null) continue;
			//������ ���� ������ �ϳ� �� �ִ�!
			//�������� ã�ƺ����ϱ�, �̸����� ������ ���� �� �ִ� ����!
			//��ųʸ����� ���� Ű���� �� �� ���� �� ����!
			if (poolDictionary.ContainsKey(currentName)) continue;
			//���� �÷��� ��� ����ϴٴ�. �ʸ� ���� ���� �Ӹ����ָ�
			poolDictionary.Add(currentName, new(currentSetting));
		}
	}

	//"���� ����" => ������ ������ ������ �þ �� �ִ� �Լ�
	//"����" => ����� ����? Parameter : "���ε�"�� �ȴٸ�? Parameters
	//Parameters => params
	public void RegistrationPool(params string[] poolNames)
	{
		foreach (string poolName in poolNames)
		{
			//�������ڴ� "�켱����"�� �����ϴ�!
			//�������ڴ� ���ϱ� ������ "��������"�� ���� �Լ��� �Ȱ����� �� ���ݾƿ�?
			//"������ ����"�� ������ �ִ� �Լ��� ���� �ν��ؼ� �����Ѵ�!
			RegistrationPool(poolName);
		}
	}

	public void InitializePool()
	{
		foreach(ObjectPoolModule currentPool in poolDictionary.Values)
		{
			currentPool?.Initialize();
		}
	}
}
