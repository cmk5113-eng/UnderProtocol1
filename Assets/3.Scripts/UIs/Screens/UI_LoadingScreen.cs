using UnityEngine;

public class UI_LoadingScreen : UI_ScreenBase
    , IOpenable, IProgress<int>, IStatus<string>
{
    //������Ƽ�� ���� ���� �׻� ������ �Ǵ� ������ �������µ�
    
    //get;set;�� �ִ� ��쿡�� �׳� ����ó�� �� �� �־��!
    //set�� protected�� ����ó�� Ȱ��!

    public int Current { get; protected set; }
    public int Max { get; protected set; }

    public float Progress => Max != 0 ? (float)Current / Max : 0.0f;

    public int AddCurrent(int value) => Set(Current + value, Max);
    public int AddMax(int value) => Set(Current, Max + value);



    //�Լ��� �Լ�����
    //������Ƽ�� ������Ƽ����
    //������ ��������
    //������ ũ�Ⱑ ū �������� ���� ������ ��ġ
    public UnityEngine.UI.Slider progressBar;
    public TMPro.TextMeshProUGUI progressText;
    public TMPro.TextMeshProUGUI explainText;

    // IStatus<T>
    public string SetCurrentStatus(string newText)
    {
        explainText.SetText(newText);
        return newText;
    }

    public int Set(int newCurrent)
    {
        //					(0, 1)		0
        //					(0, -10)	-10
        //					(0, 999)	0
        Current = Mathf.Min(newCurrent, Max);
        progressBar.value = Progress;
        //���ڷ� ������ ����, Ư���� ���·� ���ڸ� �����ִ� ��Ģ
        //Format String => ����
        //                                        : 0 => 1����
        //                                        : 0000000000 => 10����
        progressText.SetText($"{Progress * 100.0f: 0.00}%");
        return Current;
    }

    public int Set(int newCurrent, int newMax)
    {
        Max = newMax;
        return Set(newCurrent);
    }

}