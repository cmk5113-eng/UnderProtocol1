using System;
using UnityEngine;

public class UI_TargetHoverInfo : OpenableUIBase
{ 
    [SerializeField] Vector2 shiftedPosition;
    [SerializeField] TMPro.TextMeshProUGUI nameText;
    [SerializeField] TMPro.TextMeshProUGUI infoText;
    [SerializeField] UnityEngine.UI.Image portrait;
    [SerializeField] TMPro.TextMeshProUGUI skillText;
    CharacterBase target;

    public override void Registration(UIManager manager)
    {
        base.Registration(manager);
        InputManager.OnMouseHover -= HoverInfoChange;
        InputManager.OnMouseHover += HoverInfoChange;
        InputManager.OnMouseMove -= MoveToMouse;
        InputManager.OnMouseMove += MoveToMouse;


    }


    public override void Unregistration(UIManager manager)
    {
        base.Unregistration(manager);
        InputManager.OnMouseHover -= HoverInfoChange;
    }
    void HoverInfoChange(GameObject newTarget, GameObject oldTarget)
    {

        CharacterBase asCharacter = newTarget?.GetComponent<CharacterBase>();
        if (asCharacter)
        {

            nameText.SetText(newTarget.name);
            infoText.SetText(asCharacter.actionPoint.ToString());
            portrait.sprite = asCharacter.portrait;
            skillText?.SetText(asCharacter?.skill1?.name);
            Open();


        }
        else Close();
        target = asCharacter;
    }
    void MoveToMouse(Vector2 screenPosition, Vector3 worldPosition)
    {
        transform.position = screenPosition + shiftedPosition;
   
    }
}
