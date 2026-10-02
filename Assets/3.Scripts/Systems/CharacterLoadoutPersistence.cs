using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>비활성/미배치 캐릭터도 카탈로그의 에셋 참조를 통해 저장한다.</summary>
public sealed class CharacterLoadoutPersistence
{
    public const int ActiveSlotCount = 2;
    public const int PassiveSlotCount = 4;
    private readonly List<SaveCharacterEntry> characters = new List<SaveCharacterEntry>();
    private readonly Dictionary<string, SaveCharacterEntry> charactersById = new Dictionary<string, SaveCharacterEntry>();
    private readonly Dictionary<string, SkillList> skillsById = new Dictionary<string, SkillList>();
    private readonly Dictionary<SkillList, string> idsBySkill = new Dictionary<SkillList, string>();
    public bool IsReady { get; private set; }

    public CharacterLoadoutPersistence(SaveCatalog catalog)
    {
        if (catalog == null || catalog.characters == null || catalog.skills == null)
        {
            Debug.LogError("[Save] Resources/SaveCatalog가 없습니다. Tools/Save/Rebuild Save Catalog를 실행해주세요.");
            return;
        }

        var characterAssets = new HashSet<CharacterData>();
        foreach (SaveCharacterEntry entry in catalog.characters)
        {
            if (entry == null || entry.character == null || string.IsNullOrEmpty(entry.id)
                || charactersById.ContainsKey(entry.id) || !characterAssets.Add(entry.character))
            {
                Debug.LogError("[Save] 캐릭터 카탈로그에 누락/중복된 항목이 있습니다.");
                return;
            }
            characters.Add(entry);
            charactersById.Add(entry.id, entry);
        }
        foreach (SaveSkillEntry entry in catalog.skills)
        {
            if (entry == null || entry.skill == null || string.IsNullOrEmpty(entry.id)
                || skillsById.ContainsKey(entry.id) || idsBySkill.ContainsKey(entry.skill))
            {
                Debug.LogError("[Save] 스킬 카탈로그에 누락/중복된 항목이 있습니다.");
                return;
            }
            skillsById.Add(entry.id, entry.skill);
            idsBySkill.Add(entry.skill, entry.id);
        }
        IsReady = characters.Count > 0;
    }

    public bool TryCapture(out List<CharacterSkillSaveData> result)
    {
        result = new List<CharacterSkillSaveData>();
        if (!IsReady) return false;
        foreach (SaveCharacterEntry entry in characters)
        {
            if (!TryCaptureSlots(entry.character.active, ActiveSlotCount, out string[] active)
                || !TryCaptureSlots(entry.character.passive, PassiveSlotCount, out string[] passive))
                return false;
            result.Add(new CharacterSkillSaveData
            {
                characterId = entry.id,
                activeSkillIds = active,
                passiveSkillIds = passive
            });
        }
        return true;
    }

    private bool TryCaptureSlots<T>(T[] slots, int count, out string[] result) where T : SkillList
    {
        result = new string[count];
        for (int i = 0; i < count; i++)
        {
            T skill = slots != null && i < slots.Length ? slots[i] : null;
            if (skill == null) { result[i] = string.Empty; continue; }
            if (!idsBySkill.TryGetValue(skill, out result[i]))
            {
                Debug.LogError($"[Save] {skill.name} 스킬이 저장 카탈로그에 없습니다. 카탈로그를 다시 생성해주세요.");
                return false;
            }
        }
        return true;
    }

    public void ResetToDefaults()
    {
        if (!IsReady) return;
        foreach (SaveCharacterEntry entry in characters)
        {
            entry.character.active = CopySlots(entry.defaultActive, ActiveSlotCount);
            entry.character.passive = CopySlots(entry.defaultPassive, PassiveSlotCount);
        }
    }

    public void Restore(List<CharacterSkillSaveData> saved)
    {
        // 스킬 정보가 없는 예전 세이브/빈 슬롯도 이전 슬롯의 상태를 물려받지 않는다.
        ResetToDefaults();
        if (!IsReady || saved == null) return;
        var restoredIds = new HashSet<string>();
        foreach (CharacterSkillSaveData data in saved)
        {
            if (data == null || string.IsNullOrEmpty(data.characterId)
                || !restoredIds.Add(data.characterId)) continue;
            if (!charactersById.TryGetValue(data.characterId, out SaveCharacterEntry entry))
            {
                Debug.LogWarning($"[Save] 삭제되거나 등록되지 않은 캐릭터: {data.characterId}");
                continue;
            }
            RestoreSlots(data.activeSkillIds, entry.character.active);
            RestoreSlots(data.passiveSkillIds, entry.character.passive);
        }
    }

    private void RestoreSlots<T>(string[] saved, T[] target) where T : SkillList
    {
        if (saved == null) return;
        for (int i = 0; i < Math.Min(saved.Length, target.Length); i++)
        {
            string id = saved[i];
            if (string.IsNullOrEmpty(id)) { target[i] = null; continue; }
            if (skillsById.TryGetValue(id, out SkillList skill) && skill is T matchingSkill)
                target[i] = matchingSkill;
            else
                Debug.LogWarning($"[Save] 스킬 {id}를 복원할 수 없어 해당 칸의 기본 스킬을 유지합니다.");
        }
    }

    private static T[] CopySlots<T>(T[] source, int count)
    {
        var result = new T[count];
        if (source != null) Array.Copy(source, result, Math.Min(source.Length, count));
        return result;
    }
}
