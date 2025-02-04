using ECS;
using UnityEngine;

public enum SlotType
{
    None,
    Item,
    //Muzzle_Attachment,
    //Scope_Attachment,
    //Magazine_Attachment,
    //Ammunition_Attachment,
    Weapon,
    Material,
    Attachment,
}

public class InventorySlot : MonoBehaviour
{
    [SerializeField] public int SlotIndex; // index
    [SerializeField] public SlotType SlotType; // slot type item weapon or attachment
    [SerializeField] public ItemType CurrentItemType; // item type from inventory!

    public InventoryItem CurrentItem { get; private set; }

    private UIController uiController;

    private void Awake()
    {
        uiController = UIController.Instance;
    }

    public void Initialize(int index, SlotType slotType, ItemType currentItemType)
    {
        SlotIndex = index;
        SlotType = slotType;
        CurrentItemType = currentItemType;

    }

    public void AssignItem(InventoryItem item, ItemType currentItemType)
    {
        CurrentItem = item;
        CurrentItemType = currentItemType;
        //Debug.Log($"Assigning item {item} --- {currentItemType}");
        if (item != null)
        {
            item.SetSlot(this);
            item.transform.SetParent(this.transform);
            item.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        }
    }

    public void RemoveItem()
    {
        if (CurrentItem != null) {
            CurrentItemType = ItemType.NONE;
            CurrentItem.SetSlot(null);
            CurrentItem = null;
        }
    }
}