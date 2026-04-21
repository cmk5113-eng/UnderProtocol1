using Unity.Multiplayer.Center.Common.Analytics;
using UnityEngine;

public enum InteractType
{ 
    None, Talk, Take, Trade, Move, Work, Use, Make, Length
}
public interface IInteractable
{
    public string GetInteractText(GameObject from);
    public bool IsInteractable(GameObject from);
    public InteractType GetInteractType();
    public void Interact(GameObject from);
    public void StopInteract(GameObject from);
}


