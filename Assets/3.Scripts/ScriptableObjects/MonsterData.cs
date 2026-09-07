using UnityEngine;
[CreateAssetMenu(menuName = "Game/Monster Data")]
public class MonsterData : ScriptableObject
{

    public int id;
    public string monsterName;
    public GameObject prefab;
    public int hp = 100;
    public int atk = 1;
    public bool isDead = false;
    public bool isActive = false;

}