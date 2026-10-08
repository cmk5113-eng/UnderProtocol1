using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(menuName = "Game/Monster Data")]
public class MonsterData : ScriptableObject
{

    public int id;
    public string monsterName;
    public GameObject prefab;
    public int hp = 100;
    public int atk = 1;
    [Header("점유 타일")]
    [Tooltip("기준 칸은 왼쪽 아래입니다. (2, 2)는 4칸을 차지합니다.")]
    public Vector2Int footprintSize = new Vector2Int(1, 1);
    [Tooltip("스프라이트를 점유 영역 크기에 맞춥니다. 기존 몬스터는 기본적으로 사용하지 않습니다.")]
    public bool fitSpriteToFootprint;
    public Vector2Int FootprintSize => new Vector2Int(Mathf.Max(1, footprintSize.x), Mathf.Max(1, footprintSize.y));

    public IEnumerable<Vector3Int> GetOccupiedCells(Vector3Int anchor)
    {
        Vector2Int size = FootprintSize;
        for (int x = 0; x < size.x; x++)
            for (int y = 0; y < size.y; y++)
                yield return new Vector3Int(anchor.x + x, anchor.y + y, anchor.z);
    }

    public bool OccupiesCell(Vector3Int anchor, Vector3Int cell)
    {
        Vector2Int size = FootprintSize;
        return cell.z == anchor.z && cell.x >= anchor.x && cell.x < anchor.x + size.x
            && cell.y >= anchor.y && cell.y < anchor.y + size.y;
    }

    private void OnValidate() => footprintSize = FootprintSize;
    [Header("상태와 저항")]
    public bool isDead = false;
    public bool isActive = false;
    public float fireResistance = 0f;
    public float iceResistance = 0f;
    public float electricResistance = 0f;
    public float earthResistance = 0f;
    public float windResistance = 0f;
    public float darkResistance = 0f;

}
