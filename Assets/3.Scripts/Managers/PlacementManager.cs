using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PlacementManager : ManagerBase
{
    public static PlacementManager Instance;
    public Tilemap tilemap;
    public static GameObject selectedCharacter;

    void Awake()
    {
        // ���� �ϳ����� �Ŵ����� ����ϱ� ���� ������ �̱��� ����
        Instance = this;
    }

    protected override IEnumerator OnConnected(GameManager newManager)
    {
        yield return null;
    }

    public bool PlaceCharacter(GameObject characterPrefab, Vector3Int cellPos)
    {
        if (characterPrefab == null) return false;

        // 1. �� ��ǥ�� �̸� ����
        string tileName = $"Unit_{cellPos.x}_{cellPos.y}";
        if (tilemap == null)
        {
            Debug.LogWarning("[PlacementManager] tilemap is null. PlaceCharacter aborted.");
            return false;
        }

        Vector3 spawnPos = tilemap.GetCellCenterWorld(cellPos);

        // 2. ���� ��ġ�� ������Ʈ �ִ��� �˻�
        GameObject objectOnTile = GameObject.Find(tileName);
        if (objectOnTile != null)
        {
            UIManager.ClaimPopUp("���", "�̹� �ش� ��ġ�� ������ �ֽ��ϴ�.", "Ȯ��");
            return false;
        }

        // 3. ���� Ŭ�� ���� �� ����
        GameObject existingClone = GameObject.Find(characterPrefab.name);

        if (existingClone != null)
        {
            Debug.Log($"[�̵�] {characterPrefab.name} ��ġ �罺��");
            ObjectManager.DestroyObject(existingClone);
        }
        GameObject newUnit = ObjectManager.CreateObject(characterPrefab, spawnPos);
        newUnit.name = tileName; // Ÿ�� ��ǥ�� �̸� ����

        return true;
    }
    public void RemoveCharacter(GameObject characterPrefab)
    {

    }
    public void RemoveAllCharacter()
    {

    }


    protected override void OnDisconnected()
    {

    }

}