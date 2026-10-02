using System;
using System.Collections.Generic;

[Serializable]
public class SaveData
{
    public const int CurrentVersion = 2;
    public int saveVersion;
    public int stage;
    public int currentGold;
    public bool cleared;
    public int progressVersion;
    public int progress;
    public List<int> clearedStageIds = new List<int>();
    public List<CharacterSkillSaveData> characterSkills = new List<CharacterSkillSaveData>();
    public List<StageClearRecord> stageClearRecords = new List<StageClearRecord>();
}

[Serializable]
public class CharacterSkillSaveData
{
    // 에셋 GUID를 저장한다. 스킬 이름, 정렬 순서, 런타임 InstanceID는 사용하지 않는다.
    public string characterId;
    public string[] activeSkillIds;
    public string[] passiveSkillIds;
}

[Serializable]
public class StageClearRecord
{
    public int stageId;
    public bool cleared;
    public int clearCount;
    public int lastClearTurn;
    public int bestClearTurn;
    public float lastRemainingHP;
    public float bestRemainingHP;
    public int lastWaveCount;
    public string firstClearedUtc;
    public string lastClearedUtc;

    public StageClearRecord Copy() => (StageClearRecord)MemberwiseClone();
}
