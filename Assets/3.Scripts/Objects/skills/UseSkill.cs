using UnityEngine;
public class UseSkill : MonoBehaviour
{
    public PlayerController player;
    public ActiveSkill skill;
    public void SummonAndSelect(ActiveSkill skill, Vector3 pos)
    {
       
    }
    Vector3 GetSpawnPosition()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            return hit.point;
        }

        return Vector3.zero;
    }
    public void SummonAndSelect()
    {
        Debug.Log("��ư Ŭ����");

        Vector3 pos = GetSpawnPosition();
        Debug.Log("Spawn Pos: " + pos);

        player.SummonAndSelect(skill, pos);


    }

    public void ChangeCurrentSkill(GameObject selectedPrefab)
    {
        //PlacementController.CurrentSkill = selectedPrefab;

        //Debug.Log($"dd{PlacementController.CurrentSkill}");
        
    }
}