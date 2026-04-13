
using UnityEngine;
using UnityEngine.InputSystem;

public class MouseFollower : MonoBehaviour, IFunctionable
{

    void Start()
    {
        RegistrationFunctions();
      
    
    }

    private void OnDestroy()
    {
        UnregistrationFunctions();
    }



    public static Vector2 playerPosition = new Vector2(0, 0);
    public static bool moved = false;



    public void RegistrationFunctions()
    {
        InputManager.OnCancel += (value) => UIManager.ClaimPopUp("어", "취소당함", "어쩌지");                          
        InputManager.OnMove += (value) => UIManager.ClaimPopUp("어", $"움직임 {playerPosition += value}", "어쩌지");
        
    }

    public void UnregistrationFunctions()
    {
    
    
    }

    void DestroyToMouse(Vector2 screenPosition, Vector3 worldPosition)
    {
        ObjectManager.DestroyObject(GameManager.Instance.Input.GetGameObjectUnderCursor());
    }


    void CreateToMouse(Vector2 screenPosition, Vector3 worldPosition)
    {
      
        ObjectManager.CreateObject("Pikachu",worldPosition);
        

    
    }


    void MoveToMouse(Vector2 screenPosition, Vector3 worldPosition)
    { 
    transform.position = worldPosition;
    }
}