using System.Xml.Linq;
using TMPro;
using UnityEngine;

public class UI_InfoWindows : UIBase, IOpenable
{
    public bool IsOpen => gameObject.activeSelf;
    public void Close() => gameObject.SetActive(false);
    public void Open() => gameObject.SetActive(true);
    public void Toggle() => gameObject.SetActive(!IsOpen);
    [SerializeField] private TextMeshProUGUI characterNameText;
    public void UpdateUI()
    {
    GameObject name = PlacementManager.currentCharacter;
    }
}