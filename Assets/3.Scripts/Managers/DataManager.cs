//C++�� �Ͻô� ���̴�!
//#include��!
//C++�� #include�� �ؾ� ����� �� �� �ִµ�
//C#�� ��� ���� �� ���Դϴ�!
//�ٵ� �տ��ٰ� �̰� ���� ��� �ؿ�!
//NameSpace�� ������
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

//using UnityEditor; <<�̰� �ڵ��ϼ� �Ǵ� ��찡 ����
//�̰� ������ ��尡 �ȵ�!

public class DataManager : ManagerBase
{
	//����ȭ�� ����!
	//CPU�� ���� ����ȭ
	// ���
	//Ram�� ���� ����ȭ

	//���𰡸� �ҷ��� �� Ram���ٰ� �ø��� ����! => Ram�� �������
	//CPU�� ���� �����ϱ� �׳� ���� ��

	//���� �̷��� �ε带 �̸� �س��� �ʾҴ�! => CPU�� �Ź� ������ ã���� ������ ��
	//�� ��ŭ ���� �ɷ���!

	//�ٵ� ����� ���� ����ȭ
	//Ŭ���̾�Ʈ�� Ȥ����Ѽ� ����� �ڵ��ϱ� ���� �Ѵ�! => �߼ұ��, �ε����
	//��ǻ�� ���� ����� �� ��� �ڿ������� �̷��� �Ǳ� ��!
	//���α׷��� ����ȭ <-> ��ǻ�� ����ȭ
	//�����е���.. ���ϴ�..? => ��ǻ�ʹ� �׾�� �ִ�
	//�����е��� ���α׷����� �ϸ鼭 ��� �����ؾ� ���� ������ ������� �� �ִ�!

	//��ü �����͸� �����ϴ� ��ųʸ�!
	static Dictionary<System.Type, Dictionary<string, Object>> dataDictionary = new();



    event System.Action DisconnectEvent;
        
	//������Ƽ�� ������������� �Լ�
	//				int GetLoadCount();
	public override int LoadCount
	{
		get
		{
			//Async => �񵿱� => ������ ���ѳ��� �� �� �� �ϴ� ��
			//LoadCount�� ������ �; ���� �� ���ݾƿ�?
			//LoadCountã�Ƴ�~! �س��� �� ������ �Ϸ� ���� �� ������?
			//�ٽõ��ư� : LoadCount������ �� �ſ����ϱ�...? �׷� ���� ����?
			//�񵿱Ⱑ �ƴ϶� ����� ������ �մϴ�.
			var task = Addressables.LoadResourceLocationsAsync("Global");
			var result = task.WaitForCompletion();
			int count = result.Count; //������ ã�ƿ���!
			//�����е��� ���Կ� �մ��� ã�� �Դ�.
			//ȭ����� ���� ������ => ���� ����� ����
			//�մ��� ������ ����
			//���ϰ����� ���� ������ �ݰڴٴ� �ͱ��� �˷���� �ؿ�
			//�����ž� �Ѵ� ����
			task.Release();
			return count; //�׷��� �� ������ ������!
		}
	}

	protected override IEnumerator OnConnected(GameManager newManager)
	{
		//���� �ε� ��ũ���� ��� ������� �𸥴�.
		//������ �ε� ��ũ���� ������Ʈ���ְ�ʹ�.
		UIBase loading = UIManager.ClaimGetUI(UIType.Loading);
		IProgress<int> progressUI = loading as IProgress<int>;
		IStatus<string> statusUI = loading as IStatus<string>;

		int loaded = 0;
		int total = LoadCount;
		string loadString = "Load Data";
		//���ٴ� ����ü �� �ִ� �ſ���? �� ������ �ִ� ����?
		//���� Lambda �� => �̸��� ���� �Լ�  anonymous function
		//�Լ� �ȿ��� ��������� �Լ� => ������ ������ �� �ִ�!
		//�� �Լ� �ȿ��� ���� �Լ��ϱ� �� �Լ� �ȿ��� ���� ������ �׳� ����� �� �ִ�!
		System.Action ProgressOnLoad = () => 
		{
			loaded++;
			progressUI?.AddCurrent(1);
			statusUI?.SetCurrentStatus($"{loadString} ({loaded}/{total})");
		};

		//���ο� Ÿ���� ���𰡸� �߰��Ͻ� ������ ����� �ֱ�!
		loadString = "Load Game Objects";
		//��ٸ��ǵ�            ���Ϻҷ����� <GameObject>                           ����������
		yield return LoadAllFromAssetBundle<GameObject>("Global", ProgressOnLoad).WaitForTask();

		loadString = "Load Pool Requests";
		yield return LoadAllFromAssetBundle<PoolRequest>("Global", ProgressOnLoad).WaitForTask();
        loadString = "Load ItemDatas";
        yield return LoadAllFromAssetBundle<ItemContainer>("Global", ProgressOnLoad).WaitForTask();
        //�׳� �Լ��� �����ϴ� ���� �ƴ϶�, �� �۾��� ������ �ο��� �����ؾ� �Ѵ�! -> �ش� ���������� ���Ѿ� �Ѵ�!
        //LoadFileFromAssetBundle<GameObject>("Origin/Prefabs/Square.prefab");

        //Interface : ������ => ������ ������ ����� �� �ֵ��� �����ִ� ���
        //            GUI : �׷��� ������, ���콺 ������, ���� ����, Ŭ���ϱ�, �巡��
        //�����츦 �ϴٰ�, ������ �Ѿ��! => Ŭ���ϱ� �������?
        //�̰� "Ŭ��"�̾� => GUI�� Ŭ���� �����ϱ���! => GUI�̱⸸ �ϸ� Ŭ���� �����ϰڱ���!
        //"� ����� ���� �ž�"��� [���]�� �ٷ� Interface
        //IOpenable => ����, �ݱ�, ���, ���ȴ��� Ȯ�ε� �����ϴ�!

        //�ε� ������ => �ִ� �� ������, ���� �� ������ �ߴ���
        //              ���� / �ִ�      1 / 100 = 0.01
        //10��
        //�ݺ��� �� 17�� �����ϴ� ������ ����?
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

	//������ ������ �� �ǵ�, "���"�� �������� ���� �߿��� ����!
	//Resources => ����Ƽ���� Resources������ ����� ���� ����� �� �ִ�!
	// Resources/Prefabs/Square
	//�巡�� - ������� �ִ°� �ƴ϶� ���� ��η� ã�� ������ �����ϱ��?
	//������ ������ �巡���ϴ� ���� �� ���� �ɸ�
	//���� °�� �ε尡 �����ϴ�
	//���� ���ο� �ִ� ������ �ٸ� ���(���α׷��� ��)�� �����ص� ������.
	// => ���� �����Ǿ��� �� ���� Ǯ���� => ���� ���� �� �׳� �ֶ׸ֶ� ����
	//��ȹ ������ ������ ������� ���𰡸� ã�� �� �ֽ��ϴ�.
	//���α׷��� �� ����, ��Ʈ �� ����, ���� �� ���� ..
	//���α׷��� ���� ��Ʈ�� ���� �ȵ��͵� �����ص� �ȴ�.
	//���α׷��� ���� �׳� "���"�� �����س��� (����ó����) �㳯 �Խ��ϴ�.
	//�ٵ� ���� �̹����� �����µ� ���� �Ѻô��� �̹����� ����Ǿ��ִ�!
	bool TryGetFileFromResources<T>(string path, out T result) where T : Object
	{
		//Resources.LoadAll<T>(path);
		result = Resources.Load<T>(path);
		return result != null;
	}

	//1. ��η� ã�� �� ���� �Ŷ�
	//2. ��η� ã�� ���ۿ� ���
	//������.. Ŭ���̾�Ʈ�� ��� ������ ���� �� �ִ°� ����
	//����� ���ø����̼� => �÷��̽����� 200mb����
	//������ �߰� �ٿ�ε� ��...
	//Asset Bundle => ��� (���� ���Ƿ� ������ ī�װ��)
	//DLC => Ư�� ī�װ���� �ִ� ��Ҹ� �ٿ�ε� �ϰ� �� ���ΰ� �� ���ΰ�?
	//Addressable
	//async�Լ��� �񵿱� �Լ� => �ٸ� �Լ��� ���� ���ư� �� �ִ� �Լ�!
	//Coroutine���� ������!
	//Coroutine�� ��¥ "��Ƽ ������"�� �ƴϴ�.
	//���ÿ� �ϴ� ��ó�� ���̴� ����!
	//�ϳ��� �����尡 �����ϴٰ� û���ϴٰ� �����ϴٰ� û���ϴ� �����ϴٰ� û���ϴٰ�
	//�ʹ� ���� �ܻ��� ������� ���� �ٽ� ���ƿ��� ������
	//ȥ������ ���̼� ���ϴ� ��ó�� ���δ�. => ȿ���� ������ �������� => �ᱹ �ѻ���̴ϱ�
	//������ �ɸ� ���� ����! => �����ϴ�!
	//��ٷ��� �ϴ� ���� ����! => �ִ� ȭ������ ����!
	//������ �ߵ� ��Ƽ������ > �ڷ�ƾ
	//�����е��� ���ÿ� ���ư��� �ִ� �������� ����� �ӵ��� ��Ȯ�� �Ȱ��� ���� �� �ֳ���?
	//�� ���� ź�� �߻� ���, ���� ���� ����� �Ѵ�.
	//.NetFrameWork => C#�� ������ ���ķ�����
	//C#���� ���� ���α׷��� .Net�̶�� ���α׷��� Windows��� ���α׷��� �����ݴϴ�~!
	//����Ƽ ������ .Net�� �����ش�
	//C#�� .Net������ ���ư��� ������ ����Ƽ ������ ���ư��� ���� �ƴϴ�!
	//.Net�� ����Ƽ ����� "�̱� ������"�� ������ ���� �ϳ��� �����带 �� ".Net"���� ��û
	//.Net�� ����Ƽ�� ������, ���� ���� ���ο� �����嵵 ���� ������!
	//�׷��� C++�̳� C���� ���� ���ۿ�
	//�������� / ��ü���� / �Լ��� << ��� �ƴ�
	//Java�� JVM�� �����ִµ�
	//���� �� ������ �ֽ��ϴ�.
	//������ / ����������
	//C++, C#, Java
	//Compile : ���� ������ ������ => ���α׷��� �̸� ��谡 ���� �� �ִ� "�����ڵ�"�� �̸� ����� ��
	//                  ����Ƽ������ �ڵ带 �ٲ� ������ ����Ƽ Ŭ���ϸ� ������ �ϸ鼭 �ε�â ����!
	//Interpreter : �뿪��, ������ => �� �� �� �� Ȯ���� �ؼ� �����ڵ带 �� �پ� �����ؼ� ����!
	//Python, JS, Java
	//���� �� ������ ������ �� ������?
	//������ ���� �ξ� ������ ��� ���� �� ��� �ε� �ɷ��� ����̰� �ɸ���!
	//JVM�̶�� �ϴ� ���� ����Ѵ�!
	//JVM���� ���� �ü������ �ٸ� ������ �� ���ݾƿ�?
	//JVM�� �˾Ƽ� �ڵ带 �о �ü���� �°� ������ �� �ǵ�
	//JVM�� ���� �� �ִ� �ڵ带 ������ �ص�!
	//�ʹ� => ���Կ� ������ �մ��� ������ ���� => ��������
	//�����Ϻ��ٴ� �������� => ���������ͺ��� ������
	//Java�� C#�̶� �󵿾�� => �����ؼ� ���ŵ� �״�� �۵��Ѵ�
	//Python�� ���� ����!
	//���������ͷ� ������ �ϴ� �͵� �ִµ�, ������ ���ƿ�!
	//���̽� ����� ���� ������ ������ ���̴̼�!
	//��Ȯ�ϰ� ������ ��θ� ���ư��� ������ ���� ȭ�¾��!
	//���� ���� ��Ű�� �鿩���� �ϰ� ���ݾƿ�?
	//�̰� ��¥ �Ⱦ��Ͻ�
	//���̽㿡�� ��Ű�� ������ ���� �ȳ��� ���� => �����̽� 4ĭ
	//���̽��� �����̽� 4���� �ƴϸ� �鿩����� "�ν�"���� �ʽ��ϴ�.
	//�۾����� "������ ���" �ؾ� �ϴµ�, C++�̳� C���� ������� ���� �����ϴ� ��
	//�� ���� �������ּ���! �ٵ� �װ� ��� �ϴµ�! ��� ������Ʈ�� �װ� ���� ����!
	//���� ���ߴ� �͵� ������, �� ���̱� ������ �� �ȿ� ��� ����� �� �����Ǿ� �־ ��� ���̱⸸ �׷� ��!

	//���� ���� �۾��ҷ�?
	//���� �̷� ���� ��!

	//�����е��� �ٸ� ����� �����Ѵٰ� �����غ��ô�.
	//���� ������ �۾��� �Ѵ�!
	//���� ���� ���̴� �۾��� �Ѵ�!
	//��������� �� �� ��ư����� ���� ���� ��ǰ�ؾ� �Ѵ�!
	//� ����� �ʿ��ұ�?
	//�� ������ "��� ���� ���"�� ���س��ƾ� �ؿ�!
	//�۾��� ������ "� ���μ���"�� �����ؾ� ���� �˷��ֱ�!
	//�� �۾� �� �ϰ� ����, �۾� �� �� ���׶�� ġ��, �� ������ A�� �ڽ����ٰ� ���� �־���� ���ƿͶ�
	//�� ���� �̾߱��Ѵ�! => �Ű������� "�� ��"�� �ִ� ����� ������?
	//��ǻ�Ϳ��� "�� ��"�� "���" => "Function" => �Լ�
	//�Լ��� �Ű������� �Ѱ��� �� �ִ�.
	//�ð� �����ڰ� �ð� ������ �ؼ� "1�� ������� �ñⱺ"
	//������ �Ѵٴ� ���� ������ �Ͻ��ұ�?
	//�ҷ��;� �մϴ� ����
	//������ �� �� ���� �߿��� �� : ��� ���� ���ΰ�
	//������ ������ ��
	//�ż�ĭ�� => ä��
	//����� �� �ʹ����� => ���� ��
	//�����ʹ� �׷��� ��� �����ϴ� �� ���ұ�?
	//������(���ӿ�����Ʈ)
	//�׸�(��������Ʈ)
	//�մ��� ����. �������� �ֽÿ�! => � �������� ���Ͻó���?
	//                               ��ǰ���� �� �����ֽǷ���?
	//1. ������ �����Ѵ�!
	//2. ���� �з��� �����Ѵ�!
	//3. �̸����� �����Ѵ�!
	//������ ���빰�� ã�� => Dictionary
	//GameObject Square17
	//Type                  => String => GameObject
	//                Dictionary<String, GameObject>
	//Dictionary<Type,                              >
	//�������� ���� => ��� �ϰ� �����ôٱ���? => ��������� ������
	//����������� �ܾ �Է��ϸ� => ���� �˷���
	public static void SaveDataFile<T>(T target) where T : Object
	{
		if (target == null) return;
		Dictionary<string, Object> innerDictionary;

		//���ݱ��� �̷� Object�� ������. ó������ Type�̴�
		//innerDictionary�� �������� ���� ���̱� ������!
		if(!dataDictionary.TryGetValue(typeof(T), out innerDictionary))
		{
			//������ �Ѵ�!
			innerDictionary = new();
			//���� �ش� Ÿ������ ������ֱ�!
			dataDictionary.Add(typeof(T), innerDictionary);
		}

		//�� �ؿ����ʹ� ������ innerDictionary�� �ִ�!
		innerDictionary.TryAdd(target.name.ToLower(), target);
	}

	protected static T GetDataFromDictionary<T>(string fileName) where T : Object
	{
		//1.���ڰ� ���� �� fileName is null	   nullString
		//2.���ڰ� ���� �� fileName.length == 0 emptyString
		if (string.IsNullOrEmpty(fileName)) return null;

		fileName = fileName.ToLower();
		//�� ���ڸ� ã�ڴ� : ������ ã�� => ������ �� ã�Ҿ�� => �׷� �� ���µ���?
		if (dataDictionary.TryGetValue(typeof(T), out Dictionary<string, Object> innerDictionary))
		{
			if (innerDictionary.TryGetValue(fileName, out Object result))
			{
				return result as T; //������ �־��� �����ϱ� ���ϵ� �ִ���?
			}
		}

		//else�� �� ����� ���� �ִ� �� ���� if�� ��� ó�� ����!
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

	//ģ���� �۾��� ���ÿ� ���� ������ �� �ǵ� ������ ��� �϶�� ��ħ�� �����ִ� ��
	//LoadAssets�� �Ѿ���� ���� ���� ����!
	//�ϳ��� �ƴϴ� => �����ɸ��� => �ϳ� �� ������ �� ��
	//                                           Action => �ൿ
	//                                           �ൿ�� ������ �Լ�! => ��ȯ���� ���� �Լ�!
	//											 Action				=> void Function()
	//											 Action<int>		=> void Function(int a)
	//											 Action<float>		=> void Function(float a)
	//											 Action<int, float> => void Function(int a, float b)
	//                                           �ִ� 16���� �Ű��������� ����� �� �ִ�!

	//											 Func => �Լ�
	//											 ������ ��ȯ���� �־�� �ϴϱ� => �� �����ʿ� ��ȯ �ڷ���
	//											 Func<float>				=> float Function()
	//											 Func<float, int>			=> int Function(float a)
	//											 Func<float, string, int>	=> int Function(float a, string b)
	public async Task LoadAllFromAssetBundle<T>(string label, System.Action actionForEachLoad) where T : Object
	{
		//                                 V                (�Ű�����) => { ���� }
		var finder = Addressables.LoadAssetsAsync<T>(label, (T loaded) => 
		{
			SaveDataFile(loaded); //�ε� �Ǿ����ϱ� ������ ���ƾ��� ����
			actionForEachLoad();  //�� �� �ִٰ� �ϴϱ� ������� ����
		});
		Task result = finder.Task;
		await result;
		DisconnectEvent +=() => finder.Release();
	}

	public async void LoadFileFromAssetBundle<T>(string address) where T : Object
	{
		//��ٸ��� �ϴµ�, "�񵿱�"�� ��ٸ� ����!
		var finder = Addressables.LoadAssetAsync<T>(address);
		await finder.Task; //Start / Run�� �ش��ϴ� �κ�!
		SaveDataFile(finder.Result);
		finder.Release();

        DisconnectEvent += () => finder.Release();
        //A-�� ���� ����?
        //An-
        //"~�� �ƴ�"
        //"�ݴ�Ǵ�" ���λ�
        //Tan => ATan
        //����ȭ���� �ʴ´�! => �񵿱�
        //���μ����� ����ȭ���� �ʴ´�
        //=> �ϳ��� ���μ����� ������ ���� �ƴϴ�
        //                    ����Ƽ
        //=> ��Ƽ ������ <-> �̱� ������
        //       Thread
        //       ��, ��
        //�� ���� �����ϴ� ����� ����
        //�� �����鼭 �����ϸ鼭 ��Ʃ�꺸�鼭 ����Ʋ�鼭
        //�ð��� ������ �Ϸ�� �� �ִ�
        //������ �ϴ� ���ȿ� ���� �԰� �ִ� ������.
        //���� ��Ÿ�ϴ��� ��ų�� �����ؾ� �ϴµ�, �������� ��� �־
        //�ٵ�.. ����� �� ��Ȳ���� "����"�� ���ݾƿ�?
        //���� ��� ��� ����? => �켱������ �־�� ��!
        //��ǻ�� ���忡����.. ���� �� �� �����帶�� �ϳ���
        //������ �̰� ���ϰ� �������� �Ѿ ���� �����ϴ�.
        //������ �ִ� ����̴�!
        //����� �����Ϸ��� �ߴµ�.. ������� ���� ���� �־ ���ٲ۴�!
        //����� ���� ���ϰ� �׾����� üũ�� ���ΰ�?
        // => �����
        //���� �丸 �Ծ��� ������ �� �Դ� �ð��� ��������
        //�� ���� �ϴµ� �ֿ�?
        //��Դ� ��, ��Ʃ�꺸�� ��, �����ϴ� ��, ���� ��� ��
        //   O           O            X            O
        //�ٸ� �ֵ��� ���� �����ϴ� �� ��ٷȴٰ� ���� �۾��� �ؾ��ؿ�!
        //�����ϴ� �ְ� ���� �߿��� ��ȭ�� �ְ� ���� ���� ���ݾƿ�?
    }
}