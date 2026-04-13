using UnityEngine;

public class UI_LoadingScreen : UI_ScreenBase
    , IOpenable, IProgress<int>, IStatus<string>
{
    //프로퍼티를 만들 때에 항상 원본이 되는 변수를 만들어줬는데
    
    //get;set;만 있는 경우에는 그냥 변수처럼 쓸 수 있어요!
    //set만 protected인 변수처럼 활용!
    public bool IsOpen => gameObject.activeSelf;

    public int Current { get; protected set; }
    public int Max { get; protected set; }

    public float Progress => Max != 0 ? (float)Current / Max : 0.0f;

    public int AddCurrent(int value) => Set(Current + value, Max);
    public int AddMax(int value) => Set(Current, Max + value);

    public void Close() => gameObject.SetActive(false);
    public void Open() => gameObject.SetActive(true);


    //함수는 함수끼리
    //프로퍼티는 프로퍼티끼리
    //변수는 변수끼리
    //변수는 크기가 큰 순서에서 작은 순서로 배치
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
        //글자로 보여줄 때에, 특정한 형태로 글자를 보여주는 규칙
        //Format String => 서식
        //                                        : 0 => 1글자
        //                                        : 0000000000 => 10글자
        progressText.SetText($"{Progress * 100.0f: 0.00}%");
        return Current;
    }

    public int Set(int newCurrent, int newMax)
    {
        Max = newMax;
        return Set(newCurrent);
    }

    public void Toggle() => gameObject.SetActive(!IsOpen);
}