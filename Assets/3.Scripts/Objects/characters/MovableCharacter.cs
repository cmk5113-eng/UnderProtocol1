//using UnityEngine;


////pawn: ������ �� ������ �̵��Ҽ� �� �� ĳ����

   

//public class MovableCharacter : CharacterBase,IFunctionable
//{
//    [SerializeField] float moveSpeed = 5f;

//    Vector2 moveInput;   
    
//    public void RegistrationFunctions()
//    {

//        InputManager.OnMove -= MoveInput;
//        InputManager.OnMove += MoveInput;
//    }

//    public void UnregistrationFunctions()
//    {

//        InputManager.OnMove -= MoveInput;
//    }

//    protected virtual void OnEnable()
//    {
        
//    }

//    protected virtual void OnDisable()
//    {
        
//    }

//    void MoveInput(Vector2 value)
//    {
//        moveInput = value;
//    }

//    protected virtual void Update()
//    {
//        Move();
//    }

//    void Move()
//    {
//        Vector3 direction = new Vector3(moveInput.x, 0, moveInput.y);

//        transform.position += direction * moveSpeed * Time.deltaTime;
//    }


//}
    
