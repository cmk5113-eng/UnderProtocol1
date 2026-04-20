//C++을 하시는 분이다!
//#include다!
//C++은 #include를 해야 대상을 볼 수 있는데
//C#은 사실 모든게 다 보입니다!
//근데 앞에다가 이걸 원래 써야 해요!
//NameSpace기 때문에
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

//using UnityEditor; <<이게 자동완성 되는 경우가 있음
//이게 들어오면 빌드가 안됨!

public class DataManager : ManagerBase
{
	//최적화의 방향!
	//CPU를 위한 최적화
	// 상극
	//Ram을 위한 최적화

	//무언가를 불러올 때 Ram에다가 올리고 있음! => Ram은 힘들겠지
	//CPU는 램에 있으니까 그냥 갖다 씀

	//만약 이렇게 로드를 미리 해놓지 않았다! => CPU가 매번 파일을 찾으러 떠나야 함
	//그 만큼 렉이 걸려요!

	//근데 사람을 위한 최적화
	//클라이언트를 혹사시켜서 사람이 코딩하기 좋게 한다! => 중소기업, 인디게임
	//컴퓨터 내부 기능을 잘 몰라서 자연스럽게 이렇게 되긴 함!
	//프로그래머 최적화 <-> 컴퓨터 최적화
	//여러분들이.. 편하다..? => 컴퓨터는 죽어가고 있다
	//여러분들은 프로그래밍을 하면서 평생 불편해야 빠른 게임을 만들어줄 수 있다!

	//전체 데이터를 저장하는 딕셔너리!
	static Dictionary<System.Type, Dictionary<string, Object>> dataDictionary = new();



    event System.Action DisconnectEvent;
        
	//프로퍼티는 변수모양이지만 함수
	//				int GetLoadCount();
	public override int LoadCount
	{
		get
		{
			//Async => 비동기 => 남한테 시켜놓고 제 할 일 하는 거
			//LoadCount를 가지고 싶어서 여기 온 거잖아요?
			//LoadCount찾아놔~! 해놓고 제 할일을 하러 떠날 수 있을까?
			//다시돌아감 : LoadCount가지고 온 거였으니까...? 그럼 이제 모함?
			//비동기가 아니라 동기로 만들어야 합니다.
			var task = Addressables.LoadResourceLocationsAsync("Global");
			var result = task.WaitForCompletion();
			int count = result.Count; //개수를 찾아오기!
			//여러분들의 가게에 손님이 찾아 왔다.
			//화장실을 가러 나왔음 => 문을 잠그죠 ㅎㅎ
			//손님이 갇힌다 ㅎㅎ
			//파일같은건 열어 놓으면 닫겠다는 것까지 알려줘야 해요
			//닫으셔야 한다 ㅎㅎ
			task.Release();
			return count; //그래서 그 개수를 돌려줌!
		}
	}

	protected override IEnumerator OnConnected(GameManager newManager)
	{
		//나는 로딩 스크린이 어떻게 생겼는지 모른다.
		//하지만 로딩 스크린을 업데이트해주고싶다.
		UIBase loading = UIManager.ClaimGetUI(UIType.Loading);
		IProgress<int> progressUI = loading as IProgress<int>;
		IStatus<string> statusUI = loading as IStatus<string>;

		int loaded = 0;
		int total = LoadCount;
		string loadString = "Load Data";
		//람다는 도대체 왜 있는 거예요? 왜 가르쳐 주는 거임?
		//람다 Lambda λ => 이름이 없는 함수  anonymous function
		//함수 안에서 만들어지는 함수 => 변수로 저장할 수 있다!
		//내 함수 안에서 만든 함수니까 내 함수 안에서 만든 변수도 그냥 사용할 수 있다!
		System.Action ProgressOnLoad = () => 
		{
			loaded++;
			progressUI?.AddCurrent(1);
			statusUI?.SetCurrentStatus($"{loadString} ({loaded}/{total})");
		};

		//새로운 타입의 무언가를 추가하실 때마다 여기다 넣기!
		loadString = "Load Game Objects";
		//기다릴건데            파일불러오기 <GameObject>                           끝날때까지
		yield return LoadAllFromAssetBundle<GameObject>("Global", ProgressOnLoad).WaitForTask();

		loadString = "Load Pool Requests";
		yield return LoadAllFromAssetBundle<PoolRequest>("Global", ProgressOnLoad).WaitForTask();

		//그냥 함수를 실행하는 것이 아니라, 이 작업을 시작할 인원을 모집해야 한다! -> 해당 스레드한테 시켜야 한다!
		//LoadFileFromAssetBundle<GameObject>("Origin/Prefabs/Square.prefab");

		//Interface : 연결고리 => 무엇이 무엇을 사용할 수 있도록 열어주는 기능
		//            GUI : 그래픽 보여줌, 마우스 움직임, 누르 떼기, 클릭하기, 드래그
		//윈도우를 하다가, 맥으로 넘어간다! => 클릭하기 어려울까요?
		//이게 "클릭"이야 => GUI는 클릭이 가능하구나! => GUI이기만 하면 클릭을 지원하겠구나!
		//"어떤 기능이 있을 거야"라는 [약속]이 바로 Interface
		//IOpenable => 열기, 닫기, 토글, 열렸는지 확인도 가능하다!

		//로딩 진행율 => 최대 몇 개인지, 현재 몇 개까지 했는지
		//              현재 / 최대      1 / 100 = 0.01
		//10개
		//반복할 때 17을 좋아하는 이유가 뭘까?
		//00  0
		//07  7
		//14  4
		//21  1
		//28  8
		//35  5
		//42  2
		//49  9
		//56  6
		//63  3

		yield return null;
	}

	protected override void OnDisconnected()
	{

	}

	//파일을 가지고 올 건데, "경로"로 가져오는 것이 중요한 이유!
	//Resources => 유니티에서 Resources폴더를 만들고 나면 사용할 수 있다!
	// Resources/Prefabs/Square
	//드래그 - 드롭으로 넣는게 아니라 파일 경로로 찾는 이유는 무엇일까요?
	//파일이 많으면 드래그하는 데에 한 세월 걸림
	//폴더 째로 로드가 가능하다
	//폴더 내부에 있는 파일을 다른 사람(프로그래머 외)이 수정해도 괜찮다.
	// => 원래 지정되었던 게 전부 풀리고 => 새로 들어온 건 그냥 멀뚱멀뚱 있음
	//기획 문서를 가지고 사람들이 무언가를 찾을 수 있습니다.
	//프로그래밍 팀 따로, 아트 팀 따로, 사운드 팀 따로 ..
	//프로그래밍 팀은 아트가 아직 안들어와도 진행해도 된다.
	//프로그래밍 팀이 그냥 "경로"를 설정해놓고 (예외처리만) 담날 왔습니다.
	//근데 원래 이미지가 없었는데 오늘 켜봤더니 이미지가 적용되어있다!
	bool TryGetFileFromResources<T>(string path, out T result) where T : Object
	{
		//Resources.LoadAll<T>(path);
		result = Resources.Load<T>(path);
		return result != null;
	}

	//1. 경로로 찾는 건 좋은 거라서
	//2. 경로로 찾을 수밖에 없어서
	//파일을.. 클라이언트가 모두 가지고 있을 수 있는가 여부
	//모바일 애플리케이션 => 플레이스토어에서 200mb까지
	//컨텐츠 추가 다운로드 중...
	//Asset Bundle => 경로 (제가 임의로 지정한 카테고리)
	//DLC => 특정 카테고리에 있는 요소를 다운로드 하게 할 것인가 말 것인가?
	//Addressable
	//async함수는 비동기 함수 => 다른 함수와 같이 돌아갈 수 있는 함수!
	//Coroutine과의 차이점!
	//Coroutine은 진짜 "멀티 스레드"가 아니다.
	//동시에 하는 것처럼 보이는 역할!
	//하나의 스레드가 공부하다가 청소하다가 공부하다가 청소하다 공부하다가 청소하다가
	//너무 빨라서 잔상이 사라지기 전에 다시 돌아오기 때문에
	//혼자지만 둘이서 일하는 것처럼 보인다. => 효율은 오지게 떨어진다 => 결국 한사람이니까
	//데드락에 걸릴 일이 없다! => 안전하다!
	//기다려야 하는 일은 없다! => 최대 화력으로 돌림!
	//관리가 잘된 멀티스레드 > 코루틴
	//여러분들은 동시에 돌아가고 있는 여러개의 기능의 속도를 정확히 똑같이 맞출 수 있나요?
	//한 명은 탄막 발사 기능, 보스 패턴 계산을 한다.
	//.NetFrameWork => C#을 돌리는 에뮬레이터
	//C#으로 만든 프로그램은 .Net이라는 프로그램은 Windows라는 프로그램이 돌려줍니다~!
	//유니티 게임은 .Net이 돌려준다
	//C#이 .Net위에서 돌아가는 것이지 유니티 위에서 돌아가는 것이 아니다!
	//.Net이 유니티 기능을 "싱글 스레드"로 돌리는 동안 하나의 스레드를 더 ".Net"한테 요청
	//.Net은 유니티도 돌리고, 제가 원한 새로운 스레드도 같이 돌린다!
	//그래서 C++이나 C보다 느릴 수밖에
	//절차지향 / 객체지향 / 함수형 << 요건 아님
	//Java는 JVM이 돌려주는데
	//언어에는 두 종류가 있습니다.
	//컴파일 / 인터프리터
	//C++, C#, Java
	//Compile : 엮다 번역을 끝내둠 => 프로그램을 미리 기계가 돌릴 수 있는 "목적코드"로 미리 만들어 둠
	//                  유니티에서도 코드를 바꾼 다음에 유니티 클릭하면 컴파일 하면서 로딩창 나옴!
	//Interpreter : 통역사, 번역기 => 그 때 그 때 확인을 해서 목적코드를 한 줄씩 생성해서 실행!
	//Python, JS, Java
	//뭐가 더 빠르게 실행할 수 있을까?
	//컴파일 언어는 훨씬 빠르다 대신 만들 때 계속 로딩 걸려서 딜레이가 걸린다!
	//JVM이라고 하는 것을 사용한다!
	//JVM같은 경우는 운영체제마다 다른 실행을 할 거잖아요?
	//JVM이 알아서 코드를 읽어서 운영체제에 맞게 실행을 할 건데
	//JVM이 읽을 수 있는 코드를 컴파일 해둠!
	//초벌 => 가게에 보내서 손님이 구워서 먹음 => 벌집삼겹살
	//컴파일보다는 느리지만 => 인터프리터보단 빠르다
	//Java는 C#이랑 상동언어 => 복붙해서 쓰셔도 그대로 작동한다
	//Python이 느린 이유!
	//인터프리터로 돌려야 하는 것도 있는데, 제한이 많아요!
	//파이썬 만드신 분이 강박이 많으신 분이셨다!
	//정확하게 정해진 대로만 돌아가지 않으면 몹시 화냈어요!
	//저희가 지금 탭키로 들여쓰기 하고 있잖아요?
	//이거 진짜 싫어하심
	//파이썬에서 탭키를 누르면 탭이 안나감 ㅎㅎ => 스페이스 4칸
	//파이썬은 스페이스 4개가 아니면 들여쓰기로 "인식"하지 않습니다.
	//작업물도 "정해진 대로" 해야 하는데, C++이나 C에서 사람들이 자주 구현하던 걸
	//그 분이 구현해주셨음! 근데 그걸 써야 하는데! 모든 프로젝트에 그게 맞질 않음!
	//끼워 맞추는 것도 문제고, 한 줄이긴 하지만 그 안에 모든 기능이 다 구현되어 있어서 사실 보이기만 그럴 뿐!

	//나랑 같이 작업할래?
	//나는 이런 일을 해!

	//여러분들이 다른 사람과 협업한다고 생각해봅시다.
	//각자 집에서 작업을 한다!
	//인형 눈을 붙이는 작업을 한다!
	//결과적으로 한 데 모아가지고 인형 눈을 납품해야 한다!
	//어떤 약속이 필요할까?
	//다 끝내면 "모아 놓을 장소"를 정해놓아야 해요!
	//작업이 끝나면 "어떤 프로세스"를 진행해야 할지 알려주기!
	//너 작업 다 하고 나서, 작업 한 거 동그라미 치고, 그 다음에 A동 박스에다가 갖다 넣어놓고 돌아와라
	//할 일을 이야기한다! => 매개변수로 "할 일"을 넣는 방법이 있을까?
	//컴퓨터에서 "할 일"은 "기능" => "Function" => 함수
	//함수를 매개변수로 넘겨줄 수 있다.
	//시간 여행자가 시간 여행을 해서 "1차 세계대전 시기군"
	//저장을 한다는 것은 무엇을 암시할까?
	//불러와야 합니다 ㅎㅎ
	//저장을 할 때 가장 중요한 건 : 어떻게 꺼낼 것인가
	//냉장고를 정리할 때
	//신선칸에 => 채소
	//냉장고 앞 쪽문에는 => 마실 것
	//데이터는 그러면 어떻게 저장하는 게 편할까?
	//프리팹(게임오브젝트)
	//그림(스프라이트)
	//손님이 왔음. 프리팹을 주시오! => 어떤 프리팹을 원하시나요?
	//                               제품명을 좀 말해주실래요?
	//1. 종류로 저장한다!
	//2. 세부 분류를 저장한다!
	//3. 이름으로 저장한다!
	//종류로 내용물을 찾음 => Dictionary
	//GameObject Square17
	//Type                  => String => GameObject
	//                Dictionary<String, GameObject>
	//Dictionary<Type,                              >
	//사전들의 사전 => 영어를 하고 싶으시다구요? => 영어사전에 가세요
	//영어사전에서 단어를 입력하면 => 뜻을 알려줌
	public static void SaveDataFile<T>(T target) where T : Object
	{
		if (target == null) return;
		Dictionary<string, Object> innerDictionary;

		//지금까지 이런 Object는 없었다. 처음보는 Type이다
		//innerDictionary가 존재하지 않을 것이기 때문에!
		if(!dataDictionary.TryGetValue(typeof(T), out innerDictionary))
		{
			//만들어야 한다!
			innerDictionary = new();
			//만들어서 해당 타입으로 등록해주기!
			dataDictionary.Add(typeof(T), innerDictionary);
		}

		//이 밑에부터는 무조건 innerDictionary가 있다!
		innerDictionary.TryAdd(target.name.ToLower(), target);
	}

	protected static T GetDataFromDictionary<T>(string fileName) where T : Object
	{
		//1.글자가 없을 때 fileName is null	   nullString
		//2.글자가 없을 때 fileName.length == 0 emptyString
		if (string.IsNullOrEmpty(fileName)) return null;

		fileName = fileName.ToLower();
		//룬 문자를 찾겠다 : 사전을 찾음 => 사전을 못 찾았어요 => 그런 거 없는데요?
		if (dataDictionary.TryGetValue(typeof(T), out Dictionary<string, Object> innerDictionary))
		{
			if (innerDictionary.TryGetValue(fileName, out Object result))
			{
				return result as T; //사전도 있었고 들어가보니까 파일도 있던데?
			}
		}

		//else는 안 적어야 위에 있는 두 겹의 if를 모두 처리 가능!
		return null;
	}

	public static T LoadDataFile<T>(string fileName) where T : Object
	{
		T result = GetDataFromDictionary<T>(fileName);
		if(!result) UIManager.ClaimErrorMessage(SystemMessage.FileNameNotFound(fileName));
		return result;
	}

	public static bool TryLoadDataFile<T>(string fileName, out T result) where T : Object
	{
		result = GetDataFromDictionary<T>(fileName);
		return result;
	}

	//친구랑 작업을 동시에 각자 집에서 할 건데 끝나면 어떻게 하라고 지침을 보내주는 것
	//LoadAssets로 넘어오는 순간 생긴 문제!
	//하나가 아니다 => 오래걸린다 => 하나 할 때마다 할 일
	//                                           Action => 행동
	//                                           행동은 언제나 함수! => 반환값이 없는 함수!
	//											 Action				=> void Function()
	//											 Action<int>		=> void Function(int a)
	//											 Action<float>		=> void Function(float a)
	//											 Action<int, float> => void Function(int a, float b)
	//                                           최대 16개의 매개변수까지 등록할 수 있다!

	//											 Func => 함수
	//											 수식은 반환값이 있어야 하니까 => 맨 오른쪽에 반환 자료형
	//											 Func<float>				=> float Function()
	//											 Func<float, int>			=> int Function(float a)
	//											 Func<float, string, int>	=> int Function(float a, string b)
	public async Task LoadAllFromAssetBundle<T>(string label, System.Action actionForEachLoad) where T : Object
	{
		//                                 V                (매개변수) => { 내용 }
		var finder = Addressables.LoadAssetsAsync<T>(label, (T loaded) => 
		{
			SaveDataFile(loaded); //로드 되었으니까 저장해 놓아야지 ㅎㅎ
			actionForEachLoad();  //할 일 있다고 하니까 해줘야지 ㅎㅎ
		});
		Task result = finder.Task;
		await result;
		DisconnectEvent +=() => finder.Release();
	}

	public async void LoadFileFromAssetBundle<T>(string address) where T : Object
	{
		//기다리긴 하는데, "비동기"로 기다릴 거임!
		var finder = Addressables.LoadAssetAsync<T>(address);
		await finder.Task; //Start / Run에 해당하는 부분!
		SaveDataFile(finder.Result);
		finder.Release();

        DisconnectEvent += () => finder.Release();
        //A-는 뜻이 뭘까?
        //An-
        //"~이 아닌"
        //"반대되는" 접두사
        //Tan => ATan
        //동기화하지 않는다! => 비동기
        //프로세스가 동기화되지 않는다
        //=> 하나의 프로세스로 돌리는 것이 아니다
        //                    유니티
        //=> 멀티 스레드 <-> 싱글 스레드
        //       Thread
        //       줄, 실
        //한 번에 실행하는 기능의 개수
        //밥 먹으면서 게임하면서 유튜브보면서 음악틀면서
        //시간이 빠르게 완료될 수 있다
        //게임을 하는 동안에 밥을 먹고 있단 말이죠.
        //지금 한타하느라 스킬을 조준해야 하는데, 숟가락을 들고 있어서
        //근데.. 저희는 그 상황에서 "결정"을 하잖아요?
        //손을 어따 써야 할지? => 우선순위가 있어야 함!
        //컴퓨터 입장에서는.. 지금 할 일 스레드마다 하나씩
        //어차피 이거 안하고 다음으로 넘어갈 수가 없습니다.
        //데미지 주는 기능이다!
        //생명력 감소하려고 했는데.. 생명력을 누가 쓰고 있어서 못바꾼다!
        //생명력 감소 안하고 죽었는지 체크할 것인가?
        // => 데드락
        //원래 밥만 먹었을 때보다 밥 먹는 시간은 느려진다
        //다 같이 하는데 왜요?
        //밥먹는 애, 유튜브보는 애, 게임하는 애, 음악 듣는 애
        //   O           O            X            O
        //다른 애들이 전부 게임하는 애 기다렸다가 다음 작업을 해야해요!
        //게임하는 애가 뭔가 중요한 변화를 주고 끝낼 수도 있잖아요?
    }
}