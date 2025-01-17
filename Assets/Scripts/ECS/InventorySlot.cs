using System;
using UnityEngine;
using UnityEngine.UI;

public enum SlotType
{
    None,
    Item,
    Muzzle_Attachment,
    Scope_Attachment,
    Magazine_Attachment,
    Ammunition_Attachment,
    Weapon,
}

public class InventorySlot : MonoBehaviour
{
    [SerializeField] public int SlotIndex;
    [SerializeField] public SlotType SlotType { get; private set; }

    public InventoryItem CurrentItem { get; private set; }

    private UIController uiController;

    private void Awake()
    {
        uiController = UIController.Instance;
    }

    public void Initialize(int index, SlotType slotType)
    {
        SlotIndex = index;
        SlotType = slotType;
    }

    public void AssignItem(InventoryItem item)
    {
        CurrentItem = item;
        if (item != null)
        {
            item.SetSlot(this);
            item.transform.SetParent(this.transform);
            item.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        }
    }

    public void RemoveItem()
    {
        if (CurrentItem != null)
        {
            CurrentItem.SetSlot(null);
            CurrentItem = null;
        }
    }
}