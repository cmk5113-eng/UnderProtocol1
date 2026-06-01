using UnityEngine;
using UnityEngine.UI;

public class UI_SkillContainer : OpenableUIBase
{
    [SerializeField] Inventory targetInventory;
    [SerializeField] LayoutGroup layout;
    [SerializeField] string itemSlotPrefabName;
    bool isRegistrated;

    public override void Open()
    {
        base.Open();
        transform.SetAsLastSibling();
    }
    public override void Registration(UIManager manager)
    {
        if (isRegistrated) return;
        base.Registration(manager);
        targetInventory?.Initialize();
        ConnectInventory(targetInventory);
        isRegistrated = true;
    }

    public override void Unregistration(UIManager manager)
    {
        base.Unregistration(manager);
        DisconnectInventory();

    }

    public void ConnectInventory(Inventory newInventory)
    {
        if (!newInventory) return;
        targetInventory = newInventory;
        if (!layout) return;
        if (layout is GridLayoutGroup asGridLayout)
        {
            asGridLayout.constraintCount = targetInventory.columns;
        }
        
        foreach (ItemSlot currentSlot in newInventory.GetAllSlot())
        {
            if (currentSlot ==null) continue;
            GameObject instance = ObjectManager.CreateObject(itemSlotPrefabName, layout.transform);
            if (!instance) continue;
            if (instance.TryGetComponent(out UI_ItemSlotInfo createdSlot))
            {
                createdSlot.ConnectSlot(currentSlot);
            }
            
        }
    }
    public void DisconnectInventory()
    {
        if (!layout) return;
        while (layout.transform.childCount > 0)
        {
            Transform targetChild = layout.transform.GetChild(0);
            targetChild.SetParent(null);
            ObjectManager.DestroyObject(targetChild.gameObject);
        }
    }

}
