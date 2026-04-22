using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;


public class ObjectManager : ManagerBase
{
	//이제 새로운 Global 파일을 추가할 때 글자 하나만 추가하면 됨!
	//바꿀 필요가 없다 => 변수가 아니라, 상수인 셈! => 나중에 바뀌면 안됨!
	//일반적인 상수는 constant variable이 맞습니다!
	//"읽기 전용"으로 바꿔야 합니다!
	readonly string[] globalPoolSettings =
	{
		"GlobalCharacterPool",
		"GlobalControllerPool",
		"GlobalEffectPool",
		"GlobalObjectPool",
		"GlobalUIPool",
	};

	//직렬화가능한 => 유니티에서 보기 위해서 쓴 것!
	//public이라고 하는 건 사실 필요 없고 직렬화만 되면 유니티에서 볼 수 있다!
	//직렬화 변수
	//[SerializeField] PoolSetting[] testSettings;

	//PoolRequest가 있고, 그것을 위한 풀링을 준비하기
	//PoolRequest를 가져와서 저장하려면 어떤 자료구조가 필요할까?
	//리스트 : 배열과 비슷한데 추가 제거가 쉬움	, 용량△, 찾는 속도가 느리다
	//추가 제거가 많고, 전체를 도는 일이 적은

	//배열 : 리스트와 비슷한데 추가 제거가 어려움, 용량▽, 찾는 속도가 빠르다
	//추가 제거가 적고, 전체를 도는 일이 많은

	//PoolRequest는.. 얼마나 자주 추가될까? => 로딩할 때 즈음?
	//로딩되는 횟수보다 대상이 개수가 부족하면 새로 추가하거나 하는 일!
	List<PoolRequest> loadedPoolRequests = new();

	//해당하는 이름의 대상으로 불러주기 위해서
	//[이름 - 게임오브젝트] 자료구조
	static Dictionary<string, ObjectPoolModule> poolDictionary = new();

	//PollRequst에서 그 안에서 string으로 찾아서 그 이름에 맞는 GameObject를 찾으면 되니까!
	//만약 같은 이름으로 똑같은 오브젝트를 만들려고 했는데..
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

		GameObject result = null;//시작할 때에는 암것도 없음!
        wantName = wantName.ToLower();
        //이 이름으로 풀링이 등록 되어 있대요!
        if (poolDictionary.TryGetValue(wantName, out ObjectPoolModule pool))
        {
            result = pool.CreateObject(parent); //갖고 와야겠다 ㅎㅎ
        }
        else

        {
            //풀에 등록되지 않은 야생의 오브젝트를 만드는 방법!
            //데이터에는 있는지 확인해보기!
            if (DataManager.TryLoadDataFile(wantName, out GameObject prefab))
            {

                if (prefab) result = Instantiate(prefab, parent);

            }
        }
        if(!result) UIManager.ClaimErrorMessage(SystemMessage.ObjectNameNotFound(wantName));

        //등록해주는 것 까지!
        RegistrationObject(result); //둘 중에 하나라도 했겠지? 아님 말고!

		return result;
	}
	public static GameObject CreateObject(GameObject prefab, Transform parent = null)
	{
		if (prefab == null) return null;

		//                                      누가 주인인가
		GameObject result = Instantiate(prefab, parent); //만들고
		RegistrationObject(result); //등록함
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
					result.transform.position = position; //절대값을 기준으로
					break;
				case Space.Self:
					result.transform.localPosition = position; //부모를 기준으로
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
					result.transform.position = position; //절대값을 기준으로
					break;
				case Space.Self:
					result.transform.localPosition = position; //부모를 기준으로
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
					result.transform.position = position; //절대값을 기준으로
					result.transform.rotation = rotation;
					break;
				case Space.Self:
					result.transform.localPosition = position; //부모를 기준으로
					result.transform.localRotation = rotation; //부모를 기준으로
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
					result.transform.position = position; //절대값을 기준으로
					result.transform.rotation = rotation;
					break;
				case Space.Self:
					result.transform.localPosition = position; //부모를 기준으로
					result.transform.localRotation = rotation; //부모를 기준으로
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
					result.transform.position = position; //절대값을 기준으로
					result.transform.rotation = rotation;
					result.transform.localScale	= scale; //부모를 기준으로
					break;
				case Space.Self:
					result.transform.localPosition  = position; //부모를 기준으로
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
					result.transform.position = position; //절대값을 기준으로
					result.transform.rotation = rotation;
					result.transform.localScale	= scale; //부모를 기준으로
					//"진짜 월드 스케일" 을 본 적 있습니다.
					// lossyScale
					// 저는 "진짜"크기는 (1,1,1)
					// 근데 "부모"크기는 (2,2,2)
					// 이때 "로컬"크기는 (0.5, 0.5, 0.5)
					// 저의 "진짜" 크기를 => (3,3,3)으로 하고 싶다고 해보죠
					// 설정할 "로컬"크기는 몇일까요? (1.5, 1.5, 1.5)
					// 물론 회전하는 순간 끔살
					// 어떻게 제가 원하는 "진짜" 크기가 되기 위한 "로컬"크기를 구할 수 있을까?
					// "진짜 크기"를 원한다 "부모 크기"랑 비교를 해서 값을 가져주면 좋겠다!
					// 여기서 문제! 부모의 부모는 어떻게 해요?
					// 조부모 부모  로컬    월드크기       월드크기
					// 0.5    1.5  1.2  => 0.9        =>  3
					// 0.5    1.5  4    => 3
					// 로컬 => 월드  로컬 * (월드 / 로컬) = 월드
					//               1.2 *  (0.9 / 1.2) = 0.9
					// 월드 => 로컬  월드 * (로컬 / 월드) = 로컬
					//               0.9 *  (1.2 / 0.9) = 1.2
					//               3   *  (4/3)       = 4
					//벡터는 행렬이기 때문에, 행렬 곱을 할 수 있는 조건은 => xy행렬이라면 yx행렬이어야 한다
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
					result.transform.localPosition  = position; //부모를 기준으로
					result.transform.localRotation  = rotation; 
					result.transform.localScale		= scale; 
					break;
			}
		}
		return result;
	}

	public static void RegistrationObject(GameObject target) //실제로 등록하는 기능
	{
		if (target)
		{
			//이 친구가 등록 가능한지를 어떻게 체크할까?
			//저희가 만드는 건 "컴포넌트"를 만드는 것이지
			//"게임 오브젝트"를 만드는 것이 아니기 때문에
			//IFunctionable이 들어간 곳은 "컴포넌트"다!
			//GetComponent : 컴포넌트를 가져옴 (제일 첫번째 컴포넌트)
			//GetComponent<IFunctionable>() => IFunctionable 하나
			//GetComponents<IFunctionable>() => IFunctionable을 상속받는 모든 컴포넌트
			//GetComponentsInChild<IFunctionable>() => (나포함) 자식한테 있는 IFunctionable을 상속받는 모든 컴포넌트
			//GetComponentsInChildren<IFunctionable>() => (나포함)자식들한테 있는 IFunctionable을 상속받는 모든 컴포넌트
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
		//명령!
        poolName = poolName.ToLower();
		PoolRequest currentRequest = DataManager.LoadDataFile<PoolRequest>(poolName);
		if (currentRequest == null) return;
        if (currentRequest.settings == null) return;
		loadedPoolRequests.Add(currentRequest);
		//애들마다 하나씩!
		//        학생          다음학생    in   3학년 4반
		foreach (PoolSetting currentSetting in currentRequest.settings)
		{
			string currentName = currentSetting.poolName.ToLower();
			GameObject currentPrefab = currentSetting.target;
			//다음학생이.. 오늘 학교 안왔대요!
			//=> 헌혈차를 접어버리면 안되고
			//=> 다음 학생을 불러야 한다!
			if (currentPrefab == null) continue;
			//문제가 생길 여지가 하나 더 있다!
			//프리팹을 찾아봤으니까, 이름에서 문제가 생길 수 있는 여지!
			//딕셔너리에는 같은 키값을 두 개 넣을 수 없다!
			if (poolDictionary.ContainsKey(currentName)) continue;
			//나의 시련을 모두 통과하다니. 너를 정식 기사로 임명해주마
			poolDictionary.Add(currentName, new(currentSetting));
		}
	}

	//"가변 인자" => 인자의 개수가 무한정 늘어날 수 있는 함수
	//"변인" => 영어로 뭐죠? Parameter : "변인들"이 된다면? Parameters
	//Parameters => params
	public void RegistrationPool(params string[] poolNames)
	{
		foreach (string poolName in poolNames)
		{
			//가변인자는 "우선순위"가 낮습니다!
			//가변인자다 보니까 개수가 "고정인자"를 가진 함수랑 똑같아질 수 있잖아요?
			//"고정된 인자"를 가지고 있는 함수를 먼저 인식해서 실행한다!
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
