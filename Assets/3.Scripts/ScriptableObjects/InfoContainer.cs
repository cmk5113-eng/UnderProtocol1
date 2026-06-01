using UnityEngine;
//[CreateAssetMenu](fileName = "InfoContainer, menuName = "ScriptableObjects/InfoContainer")]
public abstract class InfoContainer : ScriptableObject
{
    public Sprite icon;
    public string displayName;
    public string explain;
}

