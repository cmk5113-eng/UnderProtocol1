using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.Text;


public delegate void MovementEvent(Vector3 move);
public delegate void LookAtEvent(Vector3 direction);
public delegate void DamageEvent(GameObject damageCauser,ControllerBase instigator, float damage);
// --- ���� ������ (� ��ũ��Ʈ������ ���� �����ϵ��� Ŭ���� �ۿ� ��ġ) ---








public class CharacterBase : MonoBehaviour
{

    [SerializeField] private SpriteRenderer spriteRenderer;
    public Sprite portrait;
    public bool selectable = true;
    public string Name;
    public int MaxHP;
    public int currentHP;

    public int maxAP = 0;
    public int actionPoint = 0;
    public int maxStemina = 0;
    public int steminaPoint = 0;
    public int mobility = 0;
    public bool isEnemy = false;
    public bool isSpawned = false;
    public event MovementEvent OnMovement;
    public void MovementNotify(Vector3 move) => OnMovement?.Invoke(move);
    
    
    
    public event LookAtEvent OnLookAt;
    public void LookAtNotify(Vector3 direction) => OnLookAt?.Invoke(direction);

    public event DamageEvent OnDamage;
    public void DamageNotify(GameObject damageCauser, ControllerBase instigator, float damage)
    => OnDamage?.Invoke(damageCauser, instigator, damage);

    ControllerBase _controller;
    public ControllerBase Controller => _controller;

    protected Vector3 _lookRotation;
    protected Vector3 LookRotation =>_lookRotation;

    public virtual string DisplayName => "character";



    Dictionary<System.Type, CharacterModule> moduleDictionary = new();
    //�߰� /���� /�˻�

    [SerializeField] private CharacterData characterData;
    public CharacterData Data => characterData;

    private MovementModule movementModule;

    private void Awake()
    {
        if (CompareTag("Enemy") || gameObject.name.Contains("Enemy"))
        {
            isEnemy = true;
        }
        movementModule = GetComponent<MovementModule>();
        // 만약 OnRegistration을 수동으로 호출하는 구조라면 기획에 맞게 연결하세요.
        if (movementModule != null) movementModule.OnRegistration(this);
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (currentHP <= 0 && MaxHP > 0)
        {
            currentHP = MaxHP;
        }
    }

    public void OnSelected()
    {
        movementModule ??= GetComponent<MovementModule>();
        movementModule.OnPressMoveButton();
    }
    public void OnDeSelected()
    {
        movementModule = GetComponent<MovementModule>();
        movementModule.ClearTileHighlight();
    }
    public void AddModule(System.Type wantType, CharacterModule wantModule)
    {
        if (moduleDictionary.TryAdd(wantType, wantModule))
        {
            wantModule.OnRegistration(this);
        }

    }

   
    public void AddAllModuleFromObject(GameObject target)
    {

        if (!target) return;
        foreach (CharacterModule currentModule in target.GetComponentsInChildren<CharacterModule>())
        {
            AddModule(currentModule.RegistrationType, currentModule);
        }

    }
    public bool IsDead => currentHP <= 0;

    public void InitializeHP()
    {
        currentHP = Mathf.Max(0, MaxHP);
    }

    public virtual void TakeDamage(int damage)
    {
        if (IsDead)
            return;

        damage = Mathf.Max(0, damage);
        currentHP = Mathf.Max(0, currentHP - damage);

        Debug.Log($"[Damage] {name} : {damage} damage / HP {currentHP}/{MaxHP}");

        if (IsDead)
        {
            Die();
        }
    }

    public virtual void Heal(int amount)
    {
        if (IsDead)
            return;

        amount = Mathf.Max(0, amount);
        currentHP = Mathf.Min(MaxHP, currentHP + amount);

        Debug.Log($"[Heal] {name} : {amount} heal / HP {currentHP}/{MaxHP}");
    }

    protected virtual void Die()
    {
        Debug.Log($"[Death] {name}");
        // Auto-attacks and DOT use the same death path. Release any occupied/pushed tile too.
        if (PlacementManager.Instance != null && PlacementManager.Instance.tilemap != null)
        {
            Vector3Int cell = PlacementManager.Instance.tilemap.WorldToCell(transform.position);
            cell.z = 0;
            TileData tile = PlacementManager.Instance.GetTileData(cell);
            if (tile != null && tile.Character == this)
            {
                tile.Character = null;
                tile.isempty = true;
            }
        }
        ObjectManager.DestroyObject(gameObject);
    }

    public void UpdateActionStateVisual()
    {
        if (isEnemy)
            return;

        if (spriteRenderer == null) return;

        if (actionPoint == 0)
        {
            // 💡 행동 완료: 어두운 회색조로 변경
            spriteRenderer.color = new Color(0.3f, 0.3f, 0.3f, 1.0f);
        }
        else
        {
            // 💡 행동 가능: 원래 밝은 색상(정상)으로 복구
            spriteRenderer.color = Color.white;
        }
    }
    public void RemoveModule(System.Type wantType)
    {
        if (moduleDictionary.ContainsKey(wantType))
        {
            moduleDictionary[wantType]?.OnUnregistration(this);
            moduleDictionary.Remove(wantType);

        }

    }
    public void RemoveAllModule()
    {
        foreach (CharacterModule currentModule in moduleDictionary.Values)
        {
            currentModule.OnUnregistration(this);
        }
    }


    public T GetModule<T>() where T : CharacterModule
    {
        moduleDictionary.TryGetValue(typeof(T), out CharacterModule result);
        return result as T;
    }
    public virtual void OnPossessed(ControllerBase newcontroller)
    {

    }
    public ControllerBase Possessed(ControllerBase from)
    {

        if(_controller) Unpossessed();
        _controller = from;
        AddAllModuleFromObject(gameObject);
        OnPossessed(Controller);
        return Controller;
    }


    public virtual void OnUnpossessed(ControllerBase oldcontroller)
    { 
    }
    public void Unpossessed()
    {

        if(Controller)OnUnpossessed(_controller);
        RemoveAllModule();
        _controller = null;
    }
    public bool Unpossessed(ControllerBase oldController)
    {
        if (Controller != oldController) return false;
        Unpossessed();
        return true;
    
    }

    public bool CanAct()
    {
        return false;
    }
    public bool CanMove() 
    {
        return false;
    }
    public bool CanUseSkill()
    {
        return false;
    }

}
