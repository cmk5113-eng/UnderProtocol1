using UnityEngine;

[System.Serializable]
public class SaveData
{
    public int stage;
    public int currentGold;
    public bool cleared;
    public int progressVersion;
    public int progress;
    public System.Collections.Generic.List<int> clearedStageIds = new System.Collections.Generic.List<int>();
}
