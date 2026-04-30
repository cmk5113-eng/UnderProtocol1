using UnityEngine;
public class UseSkill : MonoBehaviour
{
    public PlayerController player;
    public SkillData skill; 
    public void SummonAndSelect(SkillData skill, Vector3 pos)
    {
        if (skill == null || skill.summonPrefab == null) return;

        GameObject obj = Instantiate(skill.summonPrefab, pos, Quaternion.identity);

        var character = obj.GetComponent<CharacterBase>();
        if (character == null) return;

        player.SelectCharacter(character);
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
        Debug.Log("¹öÆ° Å¬¸¯µÊ");

        Vector3 pos = GetSpawnPosition();
        Debug.Log("Spawn Pos: " + pos);

        player.SummonAndSelect(skill, pos);
    }
}