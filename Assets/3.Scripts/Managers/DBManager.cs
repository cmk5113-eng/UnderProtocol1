using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class DBManager : ManagerBase
{
        FirebaseAuth authentication;
        FirebaseUser user;
        DatabaseReference rootDB;
   
    protected override IEnumerator OnConnected(GameManager newManager)
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(InitializeFireBase);
        yield return null;

    }
    protected override void OnDisconnected()
    {
    }

    void InitializeFireBase(Task<DependencyStatus> task)
    {
        if (task.Result == DependencyStatus.Available)
        {
            authentication = FirebaseAuth.DefaultInstance;
            user = authentication.CurrentUser;
            rootDB = FirebaseDatabase.DefaultInstance.RootReference;
            GuestLogin();
            Debug.Log("Firebase Initialized");
        }
        else
        {
            Debug.LogError($"Fail to Initialize Firebase: {task.Exception}");

        }
    }
    public void MakeUserData(string newNickName)
    {
       WriteData(MakeNewUserData(newNickName), "users", "userdata", user.UserId);

    }
    public static void ClaimMakeUserData(string newNickName)
    {
        GameManager.Instance.DB.MakeUserData(newNickName);
    }

    public async void GuestLogin()
    {
        if (authentication is null) return;
        if (user is not null)
        {
            Debug.Log($"로그인데이터 : {user.UserId})");
            UserData resultData = await ReadDataAsync<UserData>("Users", "userData", user.UserId);
            if (resultData is not null)
            { 
                Debug.Log(resultData.nickname);
            }
            else

                WriteData(MakeNewUserData("NoNamed"), "", "", user.UserId);

            return;
        }
        await        authentication.SignInAnonymouslyAsync().ContinueWith(OnLoginResult);
    }

    void OnLoginResult(Task<AuthResult> task)
    {
        if (task.IsCanceled||task.IsFaulted)
        {
            Debug.LogError($"fail to Sign in{task.Exception}");
            return;
        }

        user = task.Result.User;
        WriteData(MakeNewUserData("Nautes"),"users","userdata");
        Debug.Log($"    {user.UserId}");
    }

    [Serializable]
    public class UserData
    {
        public string Userid;
        public string nickname;
        public DateTime assignDate;
        public int userLevel;
        public int money;
        public int attendtime;
    }
    public UserData MakeNewUserData(string wantNickname) => new()
    {
        nickname = wantNickname,
        assignDate = DateTime.Now,
        userLevel = 1,
        money = 100,
        attendtime = 0

    };


    public DatabaseReference GetFinalDirectory(DatabaseReference root, params string[] directory)
    {
        if(directory is null||directory.Length == 0) return root;
        DatabaseReference currenctreference = root;
        foreach (string currentChild in directory)
        {
            currenctreference = currenctreference.Child(currentChild);
        }
        return currenctreference;
    }
    public void WriteData(object wantData, params string[] directory)
    {
        if (rootDB is null) return;





        string jsonData = JsonUtility.ToJson(wantData);
        
        DatabaseReference currenctreference = rootDB;
        GetFinalDirectory(rootDB,directory).SetRawJsonValueAsync(jsonData).ContinueWithOnMainThread(OnTaskResult);
    }
    public void WriteData(Dictionary<string, object> changes, params string[] directory)
    {
        if (rootDB is null || changes is null) return;
        GetFinalDirectory(rootDB, directory).UpdateChildrenAsync(changes).ContinueWithOnMainThread(OnTaskResult);
        
    }
    public void ReadData(Action<Task<DataSnapshot>> OnReadData, params string[] directory)
    {
        GetFinalDirectory(rootDB, directory).GetValueAsync().ContinueWithOnMainThread(OnReadData);
    }
    public IEnumerator ReadDataCoroutine(Action<Task<DataSnapshot>> OnReadData, params string[] directory)
    {
        Task<DataSnapshot> readTask = GetFinalDirectory(rootDB, directory).GetValueAsync();
        yield return readTask.WaitForTask();
        OnReadData?.Invoke(readTask);
    }


    
    //
    //
    public async Task<T> ReadDataAsync<T>(params string[] directory)
    {
        //
        DataSnapshot currentTask = await GetFinalDirectory(rootDB, directory).GetValueAsync();
        if (currentTask is null) return default;
        if (!currentTask.Exists) return default;
        try
        {
            if (currentTask.HasChildren)
        { 
            return JsonUtility.FromJson<T>(currentTask.GetRawJsonValue());
        }

        return (T)System.Convert.ChangeType(currentTask.Value, typeof(T));
        }
        
        catch (Exception e)
        {
            Debug.LogError(e);
            return default;
        }
    }

    private void OnTaskResult(Task task)    
    {
        if (task.IsCanceled || task.IsFaulted)
        {
            Debug.LogError(task.Exception);
        }    
    }
}
