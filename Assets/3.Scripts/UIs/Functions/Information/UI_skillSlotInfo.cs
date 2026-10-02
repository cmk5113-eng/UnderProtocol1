using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static SkillLoadButton;

public class UI_SkillSlotInfo : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI amountText;

    private UI_Hero heroUI;
    private SkillSlot connectedDataSlot;

    public void ConnectSlot(SkillSlot dataSlot)
    {
        connectedDataSlot = dataSlot;

        if (heroUI == null)
            heroUI = FindFirstObjectByType<UI_Hero>();

        UpdateUI();
    }

    public void DisconnectSlot()
    {
        connectedDataSlot = null;
    }

    public void UpdateUI()
    {
        if (connectedDataSlot == null || connectedDataSlot.GetIsEmpty())
        {
            if (iconImage) iconImage.sprite = null;
            if (amountText) amountText.text = "";
            return;
        }

        // 데이터가 살아있는 인벤토리가 연결되었으므로 정상적으로 아이콘과 개수가 박힙니다.
        if (iconImage)
            iconImage.sprite = connectedDataSlot.GetSkill().icon;

        if (amountText)
            amountText.text = connectedDataSlot.GetStack().ToString();
    }

    public void SelectSkill()
    {
        if (connectedDataSlot == null || CanvasManager.Instance == null) return;

        if (heroUI == null)
            heroUI = FindFirstObjectByType<UI_Hero>();

        CharacterData current = heroUI != null ? heroUI.GetCurrentCharacter() : null;
        if (current == null) return;

        SkillList skill = connectedDataSlot.GetSkill();
        if (skill == null) return;

        ActiveSkill activeSkill = skill as ActiveSkill;
        PassiveSkill passiveSkill = skill as PassiveSkill;

        if (current.active == null || current.active.Length < 2) System.Array.Resize(ref current.active, 2);
        if (current.passive == null || current.passive.Length < 4) System.Array.Resize(ref current.passive, 4);

        switch (CanvasManager.Instance.CurrentSkillGroup)
        {
            case SkillLoadButton.SkillGroupType.Active1:
                if (activeSkill == null) return;
                current.active[0] = activeSkill;
                break;
            case SkillLoadButton.SkillGroupType.Active2:
                if (activeSkill == null) return;
                current.active[1] = activeSkill;
                break;
            case SkillLoadButton.SkillGroupType.Passive1:
                if (passiveSkill == null) return;
                current.passive[0] = passiveSkill;
                break;
            case SkillLoadButton.SkillGroupType.Passive2:
                if (passiveSkill == null) return;
                current.passive[1] = passiveSkill;
                break;
            case SkillLoadButton.SkillGroupType.Passive3:
                if (passiveSkill == null) return;
                current.passive[2] = passiveSkill;
                break;
            case SkillLoadButton.SkillGroupType.Passive4:
                if (passiveSkill == null) return;
                current.passive[3] = passiveSkill;
                break;
            default:
                return;
        }

        heroUI.RefreshUI();
        if (UseSkill.Instance != null) UseSkill.Instance.ClearAllHighlights();
        SaveManager save = GameManager.Instance != null ? GameManager.Instance.Save : null;
        if (save != null) save.Save(save.currentSlot);
    }
}
