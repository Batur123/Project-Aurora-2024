using System;
using System.Collections.Generic;
using System.Linq;
using ECS;
using ECS.Bakers;
using ECS.Components;
using ECS.Libraries;
using ScriptableObjects;
using TMPro;
using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.SocialPlatforms;
using UnityEngine.UI;

public class UIController : MonoBehaviour {
    public static UIController Instance { get; private set; }

    public Dictionary<TextType, string> _textData = new();
    public Dictionary<TextType, Text> _texts = new();

    public class InventorySlotData {
        public GameObject SlotObject { get; set; }
        public SlotType Type { get; set; }

        public InventorySlotData(GameObject slotObject, SlotType type) {
            SlotObject = slotObject;
            Type = type;
        }
    }

    public class LevelUpSlotData {
        public GameObject SlotObject { get; set; }
        public SlotType Type { get; set; }

        public LevelUpSlotData(GameObject slotObject, SlotType type) {
            SlotObject = slotObject;
            Type = type;
        }
    }
    
    public Dictionary<int, InventorySlotData> _inventorySlots = new();
    public Dictionary<int, LevelUpSlotData> _levelUpSlots = new();
    
    private bool isLevelUpPanelOpen;

    public GameObject levelUpPanel;
    public GameObject tooltipPanel;
    
    public TMP_Text weaponName;
    public TMP_Text tierModifier;
    public TMP_Text damageModifier;
    public TMP_Text attackSpeedModifier;
    public TMP_Text recoilModifier;
    public TMP_Text spreadModifier;
    public TMP_Text reloadSpeedModifier;
    public TMP_Text projectilePerShotModifier;
    public TMP_Text ammoCapacityModifier;
    public TMP_Text bonusModifier_1;
    public TMP_Text bonusModifier_2;
    public TMP_Text bonusModifier_3;

    private Image healthBarBackground;
    private Image healthBarForeground;

    [Header("Inventory Item Prefabs")] 
    public GameObject itemPrefab;
    [Header("Level Up Item Selection Prefabs")] 
    public GameObject levelUpPrefab;

    public enum TextType {
        AMMO_TEXT,
        COUNTDOWN_TEXT,
        SCOREBOARD_TEXT,
        INFO_TEXT,
        HEALTH_TEXT,
        ARMOR_TEXT,
        ITEM_DROP_TEXT,
    }

    private GameObject _screenSpaceCanvasObject;
    private Canvas _screenSpaceCanvas;

    private Canvas mainCanvas;
    public Canvas inventoryCanvas;

    public EntityManager entityManager;
    private Entity playerEntity;

    private void Awake() {
        if (Instance == null) {
            Instance = this;
            LoadUI();
            DontDestroyOnLoad(gameObject);
        }
        else {
            Destroy(gameObject);
        }
    }

    private void Start() {
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
    }

    private void Update() {
        if (entityManager == null) {
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        }

        var query = entityManager.CreateEntityQuery(typeof(PlayerSingleton));
        if (query.CalculateEntityCount() == 0) {
            return;
        }
        
        var singleton = query.GetSingleton<PlayerSingleton>();
        playerEntity = singleton.PlayerEntity;

        if (playerEntity != Entity.Null && entityManager.Exists(playerEntity)) {
            var playerData = entityManager.GetComponentData<PlayerData>(playerEntity);
            if (playerData.pendingLevelUps > 0 && !levelUpPanel.activeSelf) {
                Debug.Log("Handle Done");
                HandlePendingLevelUp();
            }
        }
    }

    public void ShowLevelUpPanel()
    {
        levelUpPanel.SetActive(true);
    }
    
    public void HandlePendingLevelUp() {
        ShowLevelUpPanel();
    }

    private void LoadUI() {
        LoadScreenSpaceCanvas();

        CreateText(new TextSettings {
            Font = Resources.Load<Font>("Fonts/SampleFont"),
            FontSize = 32,
            Alignment = TextAnchor.UpperRight,
            Color = Color.green,
            HorizontalOverflow = HorizontalWrapMode.Overflow,
            VerticalOverflow = VerticalWrapMode.Truncate,
            TextType = TextType.AMMO_TEXT,
            AnchorMin = new Vector2(1, 1),
            AnchorMax = new Vector2(1, 1),
            Pivot = new Vector2(1, 1),
            AnchoredPosition = new Vector2(-10, -10),
            GetTextValue = () => GetTextValue(TextType.AMMO_TEXT)
        }, "AmmoText");

        CreateText(new TextSettings {
            Font = Resources.Load<Font>("Fonts/SampleFont"),
            FontSize = 32,
            Alignment = TextAnchor.MiddleCenter,
            Color = Color.green,
            HorizontalOverflow = HorizontalWrapMode.Overflow,
            VerticalOverflow = VerticalWrapMode.Truncate,
            TextType = TextType.COUNTDOWN_TEXT,
            AnchorMin = new Vector2(0.5f, 1f),
            AnchorMax = new Vector2(0.5f, 1f),
            Pivot = new Vector2(0.5f, 1f),
            AnchoredPosition = new Vector2(0, -10),
            GetTextValue = () => GetTextValue(TextType.COUNTDOWN_TEXT)
        }, "CountdownText");

        CreateText(new TextSettings {
            Font = Resources.Load<Font>("Fonts/SampleFont"),
            FontSize = 32,
            Alignment = TextAnchor.UpperLeft,
            Color = Color.green,
            HorizontalOverflow = HorizontalWrapMode.Overflow,
            VerticalOverflow = VerticalWrapMode.Truncate,
            TextType = TextType.SCOREBOARD_TEXT,
            AnchorMin = new Vector2(0, 1),
            AnchorMax = new Vector2(0, 1),
            Pivot = new Vector2(0, 1),
            AnchoredPosition = new Vector2(10, -10),
            GetTextValue = () => GetTextValue(TextType.SCOREBOARD_TEXT)
        }, "ScoreboardText");

        CreateText(new TextSettings {
            Font = Resources.Load<Font>("Fonts/SampleFont"),
            FontSize = 32,
            Alignment = TextAnchor.MiddleCenter,
            Color = Color.green,
            HorizontalOverflow = HorizontalWrapMode.Overflow,
            VerticalOverflow = VerticalWrapMode.Truncate,
            TextType = TextType.INFO_TEXT,
            AnchorMin = new Vector2(0.5f, 1f),
            AnchorMax = new Vector2(0.5f, 1f),
            Pivot = new Vector2(0.5f, 5f),
            AnchoredPosition = new Vector2(0, 0),
            GetTextValue = () => GetTextValue(TextType.INFO_TEXT)
        }, "InfoMessage");

        //CreateText(new TextSettings {
        //    Font = Resources.Load<Font>("Fonts/SampleFont"),
        //    FontSize = 32,
        //    Alignment = TextAnchor.UpperLeft,
        //    Color = Color.black,
        //    HorizontalOverflow = HorizontalWrapMode.Overflow,
        //    VerticalOverflow = VerticalWrapMode.Truncate,
        //    TextType = TextType.HEALTH_TEXT,
        //    AnchorMin = new Vector2(0, 1),
        //    AnchorMax = new Vector2(0, 1),
        //    Pivot = new Vector2(0, 1.5f),
        //    AnchoredPosition = new Vector2(10, -10),
        //    GetTextValue = () => GetTextValue(TextType.HEALTH_TEXT)
        //}, "HealthText");

        CreateText(new TextSettings {
            Font = Resources.Load<Font>("Fonts/SampleFont"),
            FontSize = 32,
            Alignment = TextAnchor.UpperLeft,
            Color = Color.green,
            HorizontalOverflow = HorizontalWrapMode.Overflow,
            VerticalOverflow = VerticalWrapMode.Truncate,
            TextType = TextType.ARMOR_TEXT,
            AnchorMin = new Vector2(0, 1),
            AnchorMax = new Vector2(0, 1),
            Pivot = new Vector2(0, 2),
            AnchoredPosition = new Vector2(10, -10),
            GetTextValue = () => GetTextValue(TextType.ARMOR_TEXT)
        }, "ArmorText");

        CreateText(new TextSettings {
            Font = Resources.Load<Font>("Fonts/SampleFont"),
            FontSize = 32,
            Alignment = TextAnchor.UpperLeft,
            Color = Color.green,
            HorizontalOverflow = HorizontalWrapMode.Overflow,
            VerticalOverflow = VerticalWrapMode.Truncate,
            TextType = TextType.ITEM_DROP_TEXT,
            AnchorMin = new Vector2(0, 1),
            AnchorMax = new Vector2(0, 1),
            Pivot = new Vector2(0, 2),
            AnchoredPosition = new Vector2(10, -10),
            GetTextValue = () => GetTextValue(TextType.ITEM_DROP_TEXT)
        }, "DroppedItemText");

        CreateHealthBar();
        CreateInventorySlotsManual();
        CreateLevelUpItemsManual();
    }

    SlotType SelectSlotType(string slotTag) {
        if (slotTag == "InventorySlot") {
            return SlotType.Item;
        }

        if (slotTag == "AttachmentSlot") {
            return SlotType.Attachment;
        }

        if (slotTag == "WeaponSlot") {
            return SlotType.Weapon;
        }

        if (slotTag == "LevelUpSlot") {
            return SlotType.Level_Up_Item;
        }

        return SlotType.None;
    }

    public void RenderLevelUpItem(int index, Sprite itemImage, ItemType currentItemType) {
        if (!_levelUpSlots.TryGetValue(index, out var foundSlotItem)) {
            Debug.Log("[RenderItem]: " + index + " not found");
            return;
        }

        var inventorySlotItem = foundSlotItem.SlotObject.GetComponent<LevelUpSlot>();
        if (inventorySlotItem.CurrentItem != null) {
            Destroy(inventorySlotItem.CurrentItem.gameObject);
            inventorySlotItem.RemoveItem();
        }

        GameObject itemObjectSpawned = Instantiate(levelUpPrefab, foundSlotItem.SlotObject.transform);
        var image = itemObjectSpawned.GetComponent<Image>();
        image.sprite = itemImage;
        image.preserveAspect = true;

        var rectTransform = itemObjectSpawned.GetComponent<RectTransform>();
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = new Vector2(187.3195f, 176.6627f);

        if (_levelUpSlots.TryGetValue(index, out var foundSlotItem2)) {
            LevelUpSlot slot = foundSlotItem2.SlotObject.GetComponent<LevelUpSlot>();

            if (slot.CurrentItem != null) {
                Destroy(slot.CurrentItem.gameObject);
                slot.RemoveItem();
            }

            slot.AssignItem(itemObjectSpawned.GetComponent<LevelUpItem>(), currentItemType);
            return;
        }
        Debug.LogError($"[AssignItemToSlot]: Slot index {index} not found.");
    }

    
    public void RenderItem(int index, Sprite itemImage, SlotType slotType, ItemType currentItemType) {
        if (!_inventorySlots.TryGetValue(index, out var foundSlotItem)) {
            Debug.Log("[RenderItem]: " + index + " not found");
            return;
        }

        var inventorySlotItem = foundSlotItem.SlotObject.GetComponent<InventorySlot>();
        if (inventorySlotItem.CurrentItem != null) {
            Destroy(inventorySlotItem.CurrentItem.gameObject);
            inventorySlotItem.RemoveItem();
        }

        GameObject itemObjectSpawned = Instantiate(itemPrefab, foundSlotItem.SlotObject.transform);
        var image = itemObjectSpawned.GetComponent<Image>();
        image.sprite = itemImage;
        image.preserveAspect = true;

        var rectTransform = itemObjectSpawned.GetComponent<RectTransform>();
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = new Vector2(187.3195f, 176.6627f);

        AssignItemToSlot(index, itemObjectSpawned.GetComponent<InventoryItem>(), slotType, currentItemType);
    }

    void CreateInventorySlotsManual() {
        int i = 0;
        // Create Inventory Slots
        GameObject[] inventorySlots = GameObject.FindGameObjectsWithTag("InventorySlot");
        foreach (GameObject slot in inventorySlots) {
            InventorySlot inventorySlot = slot.AddComponent<InventorySlot>();
            SlotType slotType = SelectSlotType("InventorySlot");
            inventorySlot.Initialize(i, slotType, ItemType.NONE);
            _inventorySlots.Add(i, new InventorySlotData(slot, slotType));
            i++;
        }

        // Create Attachment Slots
        GameObject[] attachmentSlots = GameObject.FindGameObjectsWithTag("AttachmentSlot");
        foreach (GameObject slot in attachmentSlots) {
            InventorySlot inventorySlot = slot.AddComponent<InventorySlot>();
            SlotType slotType = SelectSlotType("AttachmentSlot");
            inventorySlot.Initialize(i, slotType, ItemType.NONE);
            _inventorySlots.Add(i, new InventorySlotData(slot, slotType));
            i++;
        }

        // Create Main Weapon Slot
        GameObject weaponSlot = GameObject.FindGameObjectWithTag("WeaponSlot");
        InventorySlot weaponSlotInv = weaponSlot.AddComponent<InventorySlot>();
        SlotType weaponSlotType = SelectSlotType("WeaponSlot");
        weaponSlotInv.Initialize(i, weaponSlotType, ItemType.NONE);
        _inventorySlots.Add(i, new InventorySlotData(weaponSlot, weaponSlotType));

        Debug.Log($"[Inventory]: Created {i} inventory slot.");
    }

    void CreateLevelUpItemsManual() {
        levelUpPanel.SetActive(true);
        var i = 0;
        GameObject[] levelUpSlots = GameObject.FindGameObjectsWithTag("LevelUpSlot");
        foreach (GameObject slot in levelUpSlots) {
            LevelUpSlot levelUpSlot = slot.AddComponent<LevelUpSlot>();
            SlotType slotType = SelectSlotType("LevelUpSlot");
            levelUpSlot.Initialize(i, slotType, ItemType.NONE);
            _levelUpSlots.Add(i, new LevelUpSlotData(slot, slotType));
            RenderLevelUpItem(i, null, ItemType.MATERIAL);
            i++;
        }
        levelUpPanel.SetActive(false);
    }

    public bool SwapItems(int index1, int index2) {
        if (_inventorySlots.TryGetValue(index1, out var slotObj1) &&
            _inventorySlots.TryGetValue(index2, out var slotObj2)) {

            InventorySlot slot1 = slotObj1.SlotObject.GetComponent<InventorySlot>();
            InventorySlot slot2 = slotObj2.SlotObject.GetComponent<InventorySlot>();

            var slot1Type = slot1.SlotType;
            var slot2Type = slot2.SlotType;

            var slot1ItemType = slot1.CurrentItemType;
            var slot2ItemType = slot2.CurrentItemType;

            InventoryItem item1 = slot1.CurrentItem;
            InventoryItem item2 = slot2.CurrentItem;

            Debug.Log($"Attempting to swap items in Slot {index1} and Slot {index2}");
            Debug.Log($"[   Swap  ] - {slot1.SlotIndex} => {slot2.SlotIndex}");
            Debug.Log($"[Slot Type] - {slot1.SlotType} => {slot2.SlotType}");
            Debug.Log($"[Item Type] - {slot1.CurrentItemType} => {slot2.CurrentItemType}");

            if (
                (slot1Type == SlotType.Attachment && slot2Type == SlotType.Weapon) ||
                (slot2Type == SlotType.Attachment && slot1Type == SlotType.Weapon)
            ) {
                Debug.LogWarning($"[Item Swap Warning]: Attempted to swap between {slot1Type} and {slot2Type}. It is not allowed.");
                return false;
            }

            // Item to Item changes between inventory slots are allowed if its not WEAPON or ATTACHMENT special slots.
            if (slot1Type == SlotType.Item && slot2Type == SlotType.Item) {
                Debug.Log($"[Swap 2]: Start swapping item - item tags.");
                slot1.AssignItem(item2, slot2ItemType);
                slot2.AssignItem(item1, slot1ItemType);

                Entity entity1 = FindItemEntityFromIndex(slot1.SlotIndex);
                Entity entity2 = FindItemEntityFromIndex(slot2.SlotIndex);

                if (entity1 != Entity.Null && entity2 != Entity.Null) {
                    Item itemComp1 = entityManager.GetComponentData<Item>(entity1);
                    Item itemComp2 = entityManager.GetComponentData<Item>(entity2);

                    (itemComp1.slot, itemComp2.slot) = (itemComp2.slot, itemComp1.slot);

                    entityManager.SetComponentData(entity1, itemComp1);
                    entityManager.SetComponentData(entity2, itemComp2);
                    return true;
                }

                if (entity1 != Entity.Null && entity2 == Entity.Null) {
                    Item itemComp1 = entityManager.GetComponentData<Item>(entity1);
                    itemComp1.slot = slot2.SlotIndex;
                    entityManager.SetComponentData(entity1, itemComp1);
                    Debug.Log($"TEST 1");
                }
                
                if (entity1 == Entity.Null && entity2 != Entity.Null) {
                    Item itemComp2 = entityManager.GetComponentData<Item>(entity2);
                    itemComp2.slot = slot1.SlotIndex;
                    entityManager.SetComponentData(entity1, itemComp2);
                    Debug.Log($"TEST 2");
                }
            }

            // If you move item from ItemTab to WeaponTab
            if (slot1Type == SlotType.Item && slot2Type == SlotType.Weapon) {
                Debug.Log("[Swap 1]: Start moving item from ItemTab to WeaponTab");

                if (slot1ItemType != ItemType.WEAPON) {
                    Debug.LogWarning("[Swap Warning 1]: Cannot move non-weapon to weapon slot");
                    return false;
                }

                // If there is no item equipped in weapon tab, then equip weapon
                if (slot2ItemType == ItemType.NONE) {
                    Debug.Log("[Swap 1]: Equipped weapon was empty. Equipping new weapon");
                    slot1.AssignItem(item2, slot2ItemType);
                    slot2.AssignItem(item1, slot1ItemType);

                    Entity entity1 = FindItemEntityFromIndex(slot1.SlotIndex);
                    EquipWeapon(entity1);
                    return true;
                }

                // If there is already equipped weapon then swap both.
                if (slot2ItemType == ItemType.WEAPON) {
                    Debug.Log("[Swap 1]: There was already equipped gun, swap both");
                    slot1.AssignItem(item2, slot2ItemType);
                    slot2.AssignItem(item1, slot1ItemType);

                    Entity entity1 = FindItemEntityFromIndex(slot1.SlotIndex);
                    Entity entity2 = FindItemEntityFromIndex(slot2.SlotIndex);

                    Item itemComp1 = entityManager.GetComponentData<Item>(entity1);
                    Item itemComp2 = entityManager.GetComponentData<Item>(entity2);

                    (itemComp1.slot, itemComp2.slot) = (itemComp2.slot, itemComp1.slot);

                    entityManager.SetComponentData(entity1, itemComp1);
                    entityManager.SetComponentData(entity2, itemComp2);

                    // Detach Slot 2
                    DetachWeapon(entity2, slot1.SlotIndex);

                    // Attach Slot 1
                    EquipWeapon(entity1);

                    return true;
                }
            }

            // If you move item from WeaponTab to ItemTab
            if (slot2Type == SlotType.Item && slot1Type == SlotType.Weapon) {
                Debug.Log($"[Swap 2]: Start moving item from WeaponTab to ItemTab Slot 2 {slot2Type} - Slot 1 {slot1Type}");

                if (slot2ItemType != ItemType.WEAPON && slot2ItemType != ItemType.NONE) {
                    Debug.LogWarning("[Swap Warning 2]: Cannot move non-weapon to weapon slot");
                    return false;
                }

                // If there is no item equipped in weapon tab, then equip weapon
                if (slot2ItemType == ItemType.NONE) {
                    Debug.Log("[Swap 2]: Detaching weapon from equipped gun. Because target item was also NONE.");
                    slot1.AssignItem(item2, slot2ItemType);
                    slot2.AssignItem(item1, slot1ItemType);

                    Entity entity1 = FindItemEntityFromIndex(slot1.SlotIndex);
                    DetachWeapon(entity1, slot2.SlotIndex);

                    return true;
                }

                // If there is already equipped weapon then swap both.
                if (slot2ItemType == ItemType.WEAPON) {
                    Debug.Log("[Swap 2]: Swapping both weapons because there was weapon on target slot for both cases");
                    slot1.AssignItem(item2, slot2ItemType);
                    slot2.AssignItem(item1, slot1ItemType);

                    Entity entity1 = FindItemEntityFromIndex(slot1.SlotIndex);
                    Entity entity2 = FindItemEntityFromIndex(slot2.SlotIndex);

                    Item itemComp1 = entityManager.GetComponentData<Item>(entity1);
                    Item itemComp2 = entityManager.GetComponentData<Item>(entity2);

                    (itemComp1.slot, itemComp2.slot) = (itemComp2.slot, itemComp1.slot);

                    entityManager.SetComponentData(entity1, itemComp1);
                    entityManager.SetComponentData(entity2, itemComp2);

                    // Detach Slot 2
                    DetachWeapon(entity1, slot2.SlotIndex);

                    // Attach Slot 1
                    EquipWeapon(entity2);
                    return true;
                }
            }

            // If you move item from ItemTab to AttachmentTab
            if (slot1Type == SlotType.Item && slot2Type == SlotType.Attachment) {
                Debug.Log("[Swap 1]: Start moving item from ItemTab to AttachmentTab");

                if (slot1ItemType != ItemType.ATTACHMENT) {
                    Debug.LogWarning("[Swap Warning 1]: Cannot move non-attachment to attachment slot");
                    return false;
                }

                // If there is no item equipped in attachment tab, then equip attachment
                if (slot2ItemType == ItemType.NONE) {
                    Debug.Log("[Swap 1]: Equipped attachment was empty. Equipping new attachment");
                    slot1.AssignItem(item2, slot2ItemType);
                    slot2.AssignItem(item1, slot1ItemType);

                    Entity entity1 = FindItemEntityFromIndex(slot1.SlotIndex);

                    Item itemComp1 = entityManager.GetComponentData<Item>(entity1);
                    itemComp1.slot = slot2.SlotIndex;
                    entityManager.SetComponentData(entity1, itemComp1);
                    AttachAttachmentToWeapon(entity1);

                    return true;
                }

                // If there is already equipped attachment then swap both.
                // slot 1 attach, slot 2 detach
                if (slot2ItemType == ItemType.ATTACHMENT) {
                    Debug.Log("[Swap 1]: There was already equipped attachment, swap both");
                    slot1.AssignItem(item2, slot2ItemType);
                    slot2.AssignItem(item1, slot1ItemType);

                    Entity entity1 = FindItemEntityFromIndex(slot1.SlotIndex);
                    Entity entity2 = FindItemEntityFromIndex(slot2.SlotIndex);

                    Item itemComp1 = entityManager.GetComponentData<Item>(entity1);
                    Item itemComp2 = entityManager.GetComponentData<Item>(entity2);

                    (itemComp1.slot, itemComp2.slot) = (itemComp2.slot, itemComp1.slot);

                    entityManager.SetComponentData(entity1, itemComp1);
                    entityManager.SetComponentData(entity2, itemComp2);

                    // Detach Slot 2
                    DetachAttachmentFromWeapon(entity2);

                    // Attach Slot 1
                    AttachAttachmentToWeapon(entity1);
                    return true;
                }
            }

            // If you move item from AttachmentTab to ItemTab
            if (slot2Type == SlotType.Item && slot1Type == SlotType.Attachment) {
                Debug.Log("[Swap 2]: Start moving item from AttachmentTab to ItemTab");

                if (slot2ItemType != ItemType.ATTACHMENT && slot2ItemType != ItemType.NONE) {
                    Debug.LogWarning("[Swap Warning 2]: Cannot move non-attachment to attachment slot");
                    return false;
                }

                // Detaching - Attachment Slot was FULL, moved to NONE inventory
                if (slot2ItemType == ItemType.NONE) {
                    Debug.Log("[Swap 2]: Deattaching attachment from weapon to move to the inventory.");

                    slot1.AssignItem(item2, slot2ItemType);
                    slot2.AssignItem(item1, slot1ItemType);

                    Entity entity1 = FindItemEntityFromIndex(slot1.SlotIndex);

                    Item itemComp1 = entityManager.GetComponentData<Item>(entity1);
                    itemComp1.slot = slot2.SlotIndex;
                    entityManager.SetComponentData(entity1, itemComp1);
                    DetachAttachmentFromWeapon(entity1);
                    return true;
                }

                // If there is already equipped attachment then swap both.
                // slot 1 detach, slot 2 attach
                if (slot1ItemType == ItemType.ATTACHMENT) {
                    Debug.Log("[Swap 2]: There was already equipped attachment, swap both");
                    slot1.AssignItem(item2, slot2ItemType);
                    slot2.AssignItem(item1, slot1ItemType);

                    Entity entity1 = FindItemEntityFromIndex(slot1.SlotIndex);
                    Entity entity2 = FindItemEntityFromIndex(slot2.SlotIndex);

                    Item itemComp1 = entityManager.GetComponentData<Item>(entity1);
                    Item itemComp2 = entityManager.GetComponentData<Item>(entity2);

                    (itemComp2.slot, itemComp1.slot) = (itemComp1.slot, itemComp2.slot);

                    entityManager.SetComponentData(entity1, itemComp1);
                    entityManager.SetComponentData(entity2, itemComp2);

                    // Detach Slot 2
                    DetachAttachmentFromWeapon(entity1);

                    // Attach Slot 1
                    AttachAttachmentToWeapon(entity2);
                    return true;
                }
            }

            return false;
        }
        else {
            Debug.LogError("One or both slot indices do not exist in the inventory.");
        }

        return false;
    }

    private void DetachWeapon(Entity weaponEntity, int detachedToIndex) {
        Debug.Log($"Detaching weapon {weaponEntity} to index {detachedToIndex}");
        if (weaponEntity == Entity.Null) {
            Debug.Log("Weapon Entity was null. No need to detach!");
            return;
        }

        Entity playerSingletonEntity = entityManager.CreateEntityQuery(typeof(PlayerSingleton)).GetSingletonEntity();
        PlayerSingleton playerSingleton = entityManager.GetComponentData<PlayerSingleton>(playerSingletonEntity);
        DynamicBuffer<EquippedGun> equippedGuns = entityManager.GetBuffer<EquippedGun>(playerSingleton.PlayerEntity);
        DynamicBuffer<Inventory> inventory = entityManager.GetBuffer<Inventory>(playerSingleton.PlayerEntity);

        using (var ecb = new EntityCommandBuffer(Allocator.Temp)) {
            // Clear Entire Weapon
            ecb.RemoveComponent<EquippedGun>(playerSingleton.PlayerEntity);
            ecb.AddBuffer<EquippedGun>(playerSingleton.PlayerEntity);

            ecb.SetComponent(weaponEntity, new Item {
                isEquipped = false,
                itemType = ItemType.WEAPON,
                slot = detachedToIndex,
                onGround = false,
                quantity = 1,
                isStackable = false
            });

            DynamicBuffer<Child> attachments = entityManager.GetBuffer<Child>(weaponEntity);
            foreach (Child attachment in attachments) {
                if (entityManager.HasComponent<AttachmentTag>(attachment.Value)) {
                    ecb.SetComponent(attachment.Value, new Item {
                        isEquipped = false,
                        itemType = ItemType.ATTACHMENT,
                        slot = -1,
                        onGround = false,
                        quantity = 1,
                        isStackable = false
                    });
                }
            }

            ecb.AddComponent<DisableSpriteRendererRequest>(weaponEntity);
            ecb.AddComponent<UpdateUserInterfaceTag>(playerSingleton.PlayerEntity);
            ecb.Playback(entityManager);
        }
    }

    private void EquipWeapon(Entity weaponEntity) {
        Debug.Log($"Equipping weapon {weaponEntity}");
        if (weaponEntity == Entity.Null) {
            Debug.Log("Weapon Entity was null. No need to equip!");
            return;
        }

        Entity playerSingletonEntity = entityManager.CreateEntityQuery(typeof(PlayerSingleton)).GetSingletonEntity();
        PlayerSingleton playerSingleton = entityManager.GetComponentData<PlayerSingleton>(playerSingletonEntity);
        DynamicBuffer<EquippedGun> equippedGuns = entityManager.GetBuffer<EquippedGun>(playerSingleton.PlayerEntity);

        using (var ecb = new EntityCommandBuffer(Allocator.Temp)) {
            ecb.AppendToBuffer(playerSingleton.PlayerEntity, new EquippedGun { GunEntity = weaponEntity });
            ecb.SetComponent(weaponEntity, new Item {
                isEquipped = true,
                itemType = ItemType.WEAPON,
                slot = 20, // 20 always main weapon slot
                onGround = false,
                quantity = 1,
                isStackable = false
            });
            //ecb.AppendToBuffer(playerSingleton.PlayerEntity, new Inventory { itemEntity = weaponEntity });

            DynamicBuffer<Child> attachments = entityManager.GetBuffer<Child>(weaponEntity);
            int itemSlot = 16;
            foreach (Child attachment in attachments) {
                if (entityManager.HasComponent<AttachmentTag>(attachment.Value)) {
                    ecb.SetComponent(attachment.Value, new Item {
                        isEquipped = true,
                        itemType = ItemType.ATTACHMENT,
                        slot = itemSlot,
                        onGround = false,
                        quantity = 1,
                        isStackable = false
                    });
                    itemSlot++;
                }
            }

            ecb.AddComponent<EnableSpriteRendererRequest>(weaponEntity);
            ecb.AddComponent<UpdateUserInterfaceTag>(playerSingleton.PlayerEntity);
            ecb.Playback(entityManager);
        }
    }

    private void DetachAttachmentFromWeapon(Entity attachmentEntity) {
        Debug.Log($"Detaching attachment {attachmentEntity} from its parent weapon.");

        if (attachmentEntity == Entity.Null) {
            Debug.Log("Attachment Entity was null. No need to detach!");
            return;
        }

        Entity playerSingletonEntity = entityManager.CreateEntityQuery(typeof(PlayerSingleton)).GetSingletonEntity();
        PlayerSingleton playerSingleton = entityManager.GetComponentData<PlayerSingleton>(playerSingletonEntity);
        DynamicBuffer<EquippedGun> equippedGuns = entityManager.GetBuffer<EquippedGun>(playerSingleton.PlayerEntity);

        if (equippedGuns.IsEmpty) {
            Debug.Log("WEAPON WAS NOT EQUIPPED ATTACHMENT SHOULD NOT BE ATTACHED!!!");
            return;
        }

        using (var commandBuffer = new EntityCommandBuffer(Allocator.Temp)) {
            commandBuffer.RemoveComponent<Parent>(attachmentEntity);
            commandBuffer.Playback(entityManager);
        }

        DynamicBuffer<Inventory> inventoryBuffer = entityManager.GetBuffer<Inventory>(playerSingleton.PlayerEntity);
        inventoryBuffer.Add(new Inventory { itemEntity = attachmentEntity });

        var attachmentSpriteRenderer = entityManager.GetComponentObject<SpriteRenderer>(attachmentEntity);
        attachmentSpriteRenderer.enabled = false;

        Debug.Log($"Successfully detached and moved attachment {attachmentEntity} to inventory.");
    }

    private void AttachAttachmentToWeapon(Entity attachmentEntity) {
        Debug.Log($"Attaching attachment {attachmentEntity} to weapon based");
        if (attachmentEntity == Entity.Null) {
            Debug.Log("Attachment Entity was null. No need to attach!");
            return;
        }

        Entity playerSingletonEntity = entityManager.CreateEntityQuery(typeof(PlayerSingleton)).GetSingletonEntity();
        PlayerSingleton playerSingleton = entityManager.GetComponentData<PlayerSingleton>(playerSingletonEntity);
        DynamicBuffer<EquippedGun> equippedGuns = entityManager.GetBuffer<EquippedGun>(playerSingleton.PlayerEntity);
        var muzzlePointTransform = entityManager.GetComponentData<MuzzlePointTransform>(equippedGuns[0].GunEntity);
        var scopePointTransform = entityManager.GetComponentData<ScopePointTransform>(equippedGuns[0].GunEntity);
        var attachmentType = entityManager.GetComponentData<AttachmentTypeComponent>(attachmentEntity);

        if (equippedGuns.IsEmpty) {
            Debug.Log("WEAPON WAS NOT EQUIPPED ATTACHMENT SHOULD NOT BE ATTACHED!!!");
            return;
        }

        using (var commandBuffer = new EntityCommandBuffer(Allocator.Temp)) {
            commandBuffer.AddComponent(attachmentEntity, new Parent { Value = equippedGuns[0].GunEntity });

            // set offsets of a weapon
            switch (attachmentType.attachmentType) {
                case AttachmentType.Scope: {
                    commandBuffer.SetComponent(attachmentEntity, LocalTransform.FromPositionRotationScale(
                        scopePointTransform.position,
                        scopePointTransform.rotation,
                        1.0f
                    ));
                    break;
                }
                case AttachmentType.Barrel: {
                    commandBuffer.SetComponent(attachmentEntity, LocalTransform.FromPositionRotationScale(
                        muzzlePointTransform.position,
                        muzzlePointTransform.rotation,
                        1.0f
                    ));
                    break;
                }
            }

            commandBuffer.Playback(entityManager);
        }

        var attachmentSpriteRenderer = entityManager.GetComponentObject<SpriteRenderer>(attachmentEntity);
        attachmentSpriteRenderer.enabled = true;

        DynamicBuffer<Inventory> inventoryBuffer = entityManager.GetBuffer<Inventory>(playerSingleton.PlayerEntity);

        int indexToRemove = -1;

        for (int i = 0; i < inventoryBuffer.Length; i++) {
            if (inventoryBuffer[i].itemEntity == attachmentEntity) {
                indexToRemove = i;
                break;
            }
        }

        if (indexToRemove != -1) {
            inventoryBuffer.RemoveAt(indexToRemove);
        }
    }

    public Entity FindItemEntityFromIndex(int index) {
        EntityQuery itemQuery = entityManager.CreateEntityQuery(typeof(Item));

        Entity entity1 = Entity.Null;
        NativeArray<Entity> allItemEntities = itemQuery.ToEntityArray(Allocator.Temp);
        foreach (Entity currentEntity in allItemEntities) {
            if (!entityManager.HasComponent<Item>(currentEntity)) continue;

            Item itemComponent = entityManager.GetComponentData<Item>(currentEntity);
            //Debug.Log($"Found item in slot {itemComponent.slot} - Current index: {currentEntity.Index}");

            if (itemComponent.slot == index) {
                entity1 = currentEntity;
            }

            if (entity1 != Entity.Null) {
                break;
            }
        }

        allItemEntities.Dispose();
        return entity1;
    }

    public Entity FindItemAtIndexFromWeapon(Entity weaponEntity, int slotIndex) {
        Entity item = Entity.Null;

        DynamicBuffer<Child> attachmentsBuffer = entityManager.GetBuffer<Child>(weaponEntity);
        foreach (Child attachment in attachmentsBuffer) {
            if (entityManager.HasComponent<Item>(attachment.Value)) {
                Item itemComponent = entityManager.GetComponentData<Item>(attachment.Value);
                if (itemComponent.slot == slotIndex) {
                    item = attachment.Value;
                    break;
                }
            }
        }

        return item;
    }

    public Entity FindItemAtIndex(int slotIndex) {
        Entity playerSingletonEntity = entityManager.CreateEntityQuery(typeof(PlayerSingleton)).GetSingletonEntity();
        PlayerSingleton playerSingleton = entityManager.GetComponentData<PlayerSingleton>(playerSingletonEntity);
        DynamicBuffer<Inventory> inventoryBuffer = entityManager.GetBuffer<Inventory>(playerSingleton.PlayerEntity);

        Entity item = Entity.Null;

        for (int i = 0; i < inventoryBuffer.Length; i++) {
            Item itemData = entityManager.GetComponentData<Item>(inventoryBuffer[i].itemEntity);
            if (itemData.slot == slotIndex) {
                item = inventoryBuffer[i].itemEntity;
                break;
            }
        }

        return item;
    }

    private Vector2 ClampToScreen(Vector2 position, RectTransform tooltipRect) {
        Vector2 screenBounds = new Vector2(Screen.width, Screen.height);
        Vector2 tooltipSize = tooltipRect.sizeDelta;

        float clampedX = Mathf.Clamp(position.x, tooltipSize.x / 2, screenBounds.x - tooltipSize.x / 2);
        float clampedY = Mathf.Clamp(position.y, tooltipSize.y / 2, screenBounds.y - tooltipSize.y / 2);

        return new Vector2(clampedX, clampedY);
    }

    public void ShowTooltip(Vector2 position, int slotIndex) {
        Debug.Log($"[Tooltip]: Show tooltip at {slotIndex}");
        Entity item = FindItemAtIndex(slotIndex);
        if (item != Entity.Null) {
            var itemData = entityManager.GetComponentData<Item>(item);
            switch (itemData.itemType) {
                case ItemType.WEAPON: {
                    WeaponData weaponData = entityManager.GetComponentData<WeaponData>(item);
                    BaseWeaponData baseWeaponData = entityManager.GetComponentData<BaseWeaponData>(item);
                    weaponName.text = weaponData.weaponName.ToString();
                    SetupWeaponTooltip(weaponData, baseWeaponData);
                    break;
                }
                case ItemType.ATTACHMENT: {
                    AttachmentData attachmentData = entityManager.GetComponentData<AttachmentData>(item);
                    BaseAttachmentData baseAttachmentData = entityManager.GetComponentData<BaseAttachmentData>(item);
                    weaponName.text = attachmentData.attachmentName.ToString();
                    SetupAttachmentTooltip(attachmentData, baseAttachmentData);
                    break;
                }
                case ItemType.PASSIVE_ITEM: {
                    PassiveItem passiveItem = entityManager.GetComponentData<PassiveItem>(item);
                    weaponName.text = passiveItem.itemName.ToString();
                    SetupPassiveItemTooltip(passiveItem);
                    break;
                }
            }

            tooltipPanel.SetActive(true);
            RectTransform tooltipRect = tooltipPanel.GetComponent<RectTransform>();
            tooltipRect.position = ClampToScreen(position, tooltipRect);
            return;
        }

        Entity playerSingletonEntity = entityManager.CreateEntityQuery(typeof(PlayerSingleton)).GetSingletonEntity();
        PlayerSingleton playerSingleton = entityManager.GetComponentData<PlayerSingleton>(playerSingletonEntity);
        DynamicBuffer<EquippedGun> equippedGuns = entityManager.GetBuffer<EquippedGun>(playerSingleton.PlayerEntity);

        if (!equippedGuns.IsEmpty && slotIndex is 16 or 17 or 18 or 19) {
            Entity foundAttachment = FindItemAtIndexFromWeapon(equippedGuns[0].GunEntity, slotIndex);
            if (foundAttachment != Entity.Null) {
                AttachmentData attachmentData = entityManager.GetComponentData<AttachmentData>(foundAttachment);
                BaseAttachmentData baseAttachmentData = entityManager.GetComponentData<BaseAttachmentData>(foundAttachment);

                SetupAttachmentTooltip(attachmentData, baseAttachmentData);
            }
        }

        tooltipPanel.SetActive(true);
        RectTransform tooltipRect2 = tooltipPanel.GetComponent<RectTransform>();
        tooltipRect2.position = ClampToScreen(position, tooltipRect2);
    }

    public void ClearAllTexts() {
        ClearText(tierModifier);
        ClearText(damageModifier);
        ClearText(attackSpeedModifier);
        ClearText(recoilModifier);
        ClearText(spreadModifier);
        ClearText(projectilePerShotModifier);
        ClearText(reloadSpeedModifier);
        ClearText(ammoCapacityModifier);
        ClearText(bonusModifier_1);
        ClearText(bonusModifier_2);
        ClearText(bonusModifier_3);
    }
    
    public void SetupWeaponTooltip(WeaponData weaponData, BaseWeaponData baseWeaponData) {
        ClearAllTexts();

        float startX = 9f;
        float startY = 144.5f;
        float stepY = -30f;

        weaponName.text = weaponData.weaponName.ToString();
        Canvas.ForceUpdateCanvases();

        // Current position tracker
        Vector2 currentPosition = new Vector2(startX, startY);
        float totalHeight = 150f + weaponName.rectTransform.rect.height;

        SetModifier(
            tierModifier,
            "[Tier 10] [Rare]",
            1,
            ref currentPosition,
            stepY,
            ref totalHeight
        );

        if (weaponData.stats.damage != 0) {
            SetModifier(
                damageModifier,
                $"Damage: {weaponData.stats.damage:F1} [{baseWeaponData.range.minDamage} - {baseWeaponData.range.maxDamage}]",
                weaponData.stats.damage,
                ref currentPosition,
                stepY,
                ref totalHeight
            );
        }

        if (weaponData.stats.attackSpeed != 0) {
            SetModifier(
                attackSpeedModifier,
                $"Attack Speed: {weaponData.stats.attackSpeed:F1} [{baseWeaponData.range.minAttackSpeed} - {baseWeaponData.range.maxAttackSpeed}]",
                weaponData.stats.attackSpeed,
                ref currentPosition,
                stepY,
                ref totalHeight
            );
        }

        if (weaponData.stats.recoilAmount != 0) {
            SetModifier(
                recoilModifier,
                $"Recoil: {weaponData.stats.recoilAmount:F1} [{baseWeaponData.range.minRecoilAmount} - {baseWeaponData.range.maxRecoilAmount}]",
                weaponData.stats.recoilAmount,
                ref currentPosition,
                stepY,
                ref totalHeight
            );
        }

        if (weaponData.stats.spreadAmount != 0) {
            SetModifier(
                spreadModifier,
                $"Spread: {weaponData.stats.spreadAmount:F1} [{baseWeaponData.range.minSpreadAmount} - {baseWeaponData.range.maxSpreadAmount}]",
                weaponData.stats.spreadAmount,
                ref currentPosition,
                stepY,
                ref totalHeight
            );
        }

        if (weaponData.stats.bulletsPerShot != 0) {
            SetModifier(
                projectilePerShotModifier,
                $"Projectile per Shot: {weaponData.stats.bulletsPerShot:F1} [{baseWeaponData.range.minBulletsPerShot} - {baseWeaponData.range.maxBulletsPerShot}]",
                weaponData.stats.bulletsPerShot,
                ref currentPosition,
                stepY,
                ref totalHeight
            );
        }

        if (weaponData.stats.reloadSpeed != 0) {
            SetModifier(
                reloadSpeedModifier,
                $"Reload Speed: {weaponData.stats.reloadSpeed:F1} [{baseWeaponData.range.minReloadSpeed} - {baseWeaponData.range.maxReloadSpeed}]",
                weaponData.stats.reloadSpeed,
                ref currentPosition,
                stepY,
                ref totalHeight
            );
        }

        if (weaponData.stats.ammoCapacity != 0) {
            SetModifier(
                ammoCapacityModifier,
                $"Ammo Capacity: {weaponData.stats.ammoCapacity:F1} [{baseWeaponData.range.minAmmoCapacity} - {baseWeaponData.range.maxAmmoCapacity}]",
                weaponData.stats.ammoCapacity,
                ref currentPosition,
                stepY,
                ref totalHeight
            );
        }

        SetModifier(bonusModifier_1, "Poison enemies", 1, ref currentPosition, stepY, ref totalHeight); // Replace with actual bonuses as needed
        SetModifier(bonusModifier_2, "Projectiles explodes on impact", 1, ref currentPosition, stepY, ref totalHeight);
        SetModifier(bonusModifier_3, "TEST BONUS 5", 1, ref currentPosition, stepY, ref totalHeight);
        RectTransform tooltipRect = tooltipPanel.GetComponent<RectTransform>();
        tooltipRect.sizeDelta = new Vector2(tooltipRect.sizeDelta.x, totalHeight);
    }

    public void SetupPassiveItemTooltip(PassiveItem passiveItem) {
        ClearAllTexts();

        float startX = 9f;
        float startY = 144.5f;
        float stepY = -30f;

        weaponName.text = passiveItem.itemName.ToString();
        Canvas.ForceUpdateCanvases();

        // Current position tracker
        Vector2 currentPosition = new Vector2(startX, startY);
        float totalHeight = 150f + weaponName.rectTransform.rect.height;

        SetModifier(
            tierModifier,
            "[Tier 10] [Rare]",
            1,
            ref currentPosition,
            stepY,
            ref totalHeight
        );

        if (passiveItem.amount != 0) {
            SetModifier(
                damageModifier,
                $"Amount: {passiveItem.amount}",
                passiveItem.amount,
                ref currentPosition,
                stepY,
                ref totalHeight
            );
        }

        RectTransform tooltipRect = tooltipPanel.GetComponent<RectTransform>();
        tooltipRect.sizeDelta = new Vector2(tooltipRect.sizeDelta.x, totalHeight);
    }

    public void SetupAttachmentTooltip(AttachmentData attachmentData, BaseAttachmentData baseAttachmentData) {
        ClearAllTexts();

        // Starting positions for the tooltip items
        float startX = 35f;
        float startY = 135.0f;
        float stepY = -30f; // Decrease by 30 for each item

        weaponName.text = attachmentData.attachmentName.ToString();
        Canvas.ForceUpdateCanvases();

        // Current position tracker
        Vector2 currentPosition = new Vector2(startX, startY);
        float totalHeight = 150f + weaponName.rectTransform.rect.height;

        // Set and position tier and stats
        SetModifier(
            tierModifier,
            "[Tier 10] [Rare]",
            1,
            ref currentPosition,
            stepY,
            ref totalHeight
        );

        if (attachmentData.stats.damage != 0) {
            SetModifier(
                damageModifier,
                $"Adds Damage: {attachmentData.stats.damage:F1} [{baseAttachmentData.range.minDamage} - {baseAttachmentData.range.maxDamage}]",
                attachmentData.stats.damage,
                ref currentPosition,
                stepY,
                ref totalHeight
            );
        }

        if (attachmentData.stats.attackSpeed != 0) {
            SetModifier(
                attackSpeedModifier,
                $"Adds Attack Speed: {attachmentData.stats.attackSpeed:F1} [{baseAttachmentData.range.minAttackSpeed} - {baseAttachmentData.range.maxAttackSpeed}]",
                attachmentData.stats.attackSpeed,
                ref currentPosition,
                stepY,
                ref totalHeight
            );
        }

        if (attachmentData.stats.recoilAmount != 0) {
            SetModifier(
                recoilModifier,
                $"Adds Recoil: {attachmentData.stats.recoilAmount:F1} [{baseAttachmentData.range.minRecoilAmount} - {baseAttachmentData.range.maxRecoilAmount}]",
                attachmentData.stats.recoilAmount,
                ref currentPosition,
                stepY,
                ref totalHeight
            );
        }

        if (attachmentData.stats.spreadAmount != 0) {
            SetModifier(
                spreadModifier,
                $"Adds Spread: {attachmentData.stats.spreadAmount:F1} [{baseAttachmentData.range.minSpreadAmount} - {baseAttachmentData.range.maxSpreadAmount}]",
                attachmentData.stats.spreadAmount,
                ref currentPosition,
                stepY,
                ref totalHeight
            );
        }

        if (attachmentData.stats.bulletsPerShot > 1) {
            SetModifier(
                projectilePerShotModifier,
                $"Adds Projectile per Shot: {attachmentData.stats.bulletsPerShot:F1} [{baseAttachmentData.range.minBulletsPerShot} - {baseAttachmentData.range.maxBulletsPerShot}]",
                attachmentData.stats.bulletsPerShot,
                ref currentPosition,
                stepY,
                ref totalHeight
            );
        }

        if (attachmentData.stats.reloadSpeed != 0) {
            SetModifier(
                reloadSpeedModifier,
                $"Increases reload speed: {attachmentData.stats.reloadSpeed:F1}% [{baseAttachmentData.range.minReloadSpeed} - {baseAttachmentData.range.maxReloadSpeed}]",
                attachmentData.stats.reloadSpeed,
                ref currentPosition,
                stepY,
                ref totalHeight
            );
        }

        if (attachmentData.stats.ammoCapacity != 0) {
            SetModifier(
                ammoCapacityModifier,
                $"Adds Ammo Capacity: {attachmentData.stats.ammoCapacity:F1} [{baseAttachmentData.range.minAmmoCapacity} - {baseAttachmentData.range.maxAmmoCapacity}]",
                attachmentData.stats.ammoCapacity,
                ref currentPosition,
                stepY,
                ref totalHeight
            );
        }

        SetModifier(bonusModifier_1, "Attachment Bonus Test 1", 1, ref currentPosition, stepY, ref totalHeight);
        SetModifier(bonusModifier_2, "Adds +3 projectile", 1, ref currentPosition, stepY, ref totalHeight);
        SetModifier(bonusModifier_3, "Adds +35 magazine capacity", 1, ref currentPosition, stepY, ref totalHeight);
        RectTransform tooltipRect = tooltipPanel.GetComponent<RectTransform>();
        tooltipRect.sizeDelta = new Vector2(tooltipRect.sizeDelta.x, totalHeight);
    }

    private void SetModifier(TMP_Text modifier, string text, float value, ref Vector2 position, float stepY, ref float height) {
        //Debug.Log("Set Modifier Text"+ text);
        if (value != 0) {
            modifier.text = text;
            modifier.gameObject.SetActive(true); // Ensure the modifier is visible
            modifier.rectTransform.localPosition = position;
            position.y += stepY; // Move to the next line

            Canvas.ForceUpdateCanvases(); // Force a UI update to get accurate dimensions
            float currentHeight = modifier.rectTransform.rect.height;
            height += currentHeight;
            //Debug.Log($"Set Modifier: {text}, Total Height: {height}, Current Height: {currentHeight}");
        }
        else {
            modifier.gameObject.SetActive(false); // Hide unused modifiers
        }
    }

    private void ClearText(TMP_Text modifier) {
        modifier.text = string.Empty;
        modifier.gameObject.SetActive(false);
    }


    public void HideTooltip() {
        tooltipPanel.SetActive(false);
    }

    public void DropItemAtIndexToGround(int slotIndex) {
        Entity playerSingletonEntity = entityManager.CreateEntityQuery(typeof(PlayerSingleton)).GetSingletonEntity();
        PlayerSingleton playerSingleton = entityManager.GetComponentData<PlayerSingleton>(playerSingletonEntity);
        LocalTransform playerTransform = entityManager.GetComponentData<LocalTransform>(playerSingleton.PlayerEntity);
        DynamicBuffer<EquippedGun> equippedGuns = entityManager.GetBuffer<EquippedGun>(playerSingleton.PlayerEntity);

        using (var commandBuffer = new EntityCommandBuffer(Allocator.Temp)) {
            switch (slotIndex) {
                case 20: {
                    if (equippedGuns.IsEmpty) {
                        Debug.LogWarning("Serious issue, weapon should not be empty while dropping index 0 from inventory");
                        break;
                    }

                    Entity foundWeapon = FindItemAtIndex(slotIndex);
                    if (foundWeapon == Entity.Null) {
                        Debug.LogWarning("Serious issue that weapon does not found even though we had equippedGun buffer not empty");
                        break;
                    }

                    Item weaponItemComponent = entityManager.GetComponentData<Item>(foundWeapon);
                    weaponItemComponent.slot = -1;
                    weaponItemComponent.isEquipped = false;
                    weaponItemComponent.onGround = true;
                    commandBuffer.SetComponent(foundWeapon, weaponItemComponent);

                    DynamicBuffer<Child> attachmentBuffers = entityManager.GetBuffer<Child>(foundWeapon);
                    foreach (Child attachmentBuffer in attachmentBuffers) {
                        if (!entityManager.HasComponent<Item>(attachmentBuffer.Value) || !entityManager.HasComponent<AttachmentTag>(attachmentBuffer.Value)) {
                            continue;
                        }

                        Item attachmentItem = entityManager.GetComponentData<Item>(attachmentBuffer.Value);
                        attachmentItem.slot = -1;
                        attachmentItem.isEquipped = false;
                        attachmentItem.onGround = true;
                        commandBuffer.SetComponent(attachmentBuffer.Value, attachmentItem);
                    }

                    DynamicBuffer<Inventory> inventoryBuffer = entityManager.GetBuffer<Inventory>(playerSingleton.PlayerEntity);

                    int indexToRemove = -1;

                    for (int i = 0; i < inventoryBuffer.Length; i++) {
                        if (inventoryBuffer[i].itemEntity == foundWeapon) {
                            indexToRemove = i;
                            break;
                        }
                    }

                    if (indexToRemove != -1) {
                        inventoryBuffer.RemoveAt(indexToRemove);
                    }

                    commandBuffer.RemoveComponent<EquippedGun>(playerSingleton.PlayerEntity);
                    commandBuffer.AddBuffer<EquippedGun>(playerSingleton.PlayerEntity);
                    commandBuffer.AddComponent<DroppedItemTag>(equippedGuns[0].GunEntity);
                    commandBuffer.AddComponent<UpdateUserInterfaceTag>(playerSingleton.PlayerEntity);
                    break;
                }
                case 16:
                case 17:
                case 18:
                case 19: {
                    if (equippedGuns.IsEmpty) {
                        Debug.LogWarning("Serious issue, weapon should not be empty while dropping index 1-2-3-4 from inventory");
                        break;
                    }

                    Entity foundAttachment = FindItemAtIndexFromWeapon(equippedGuns[0].GunEntity, slotIndex);
                    if (foundAttachment == Entity.Null) {
                        Debug.LogWarning("Attachment from item at index does not found");
                        break;
                    }

                    GunAttachmentHelper.RequestRemoveAttachment(equippedGuns[0].GunEntity, foundAttachment);

                    commandBuffer.AddComponent<DroppedItemTag>(foundAttachment);
                    commandBuffer.SetComponent(foundAttachment, new LocalTransform {
                        Position = playerTransform.Position,
                        Rotation = Quaternion.identity,
                        Scale = 1f
                    });
                    commandBuffer.AddComponent<UpdateUserInterfaceTag>(playerSingleton.PlayerEntity);
                    break;
                }
                default: {
                    Entity itemAtIndex = FindItemAtIndex(slotIndex);
                    if (itemAtIndex == Entity.Null) {
                        Debug.LogWarning("Dropping item from inventory could not found at index " + slotIndex);
                        break;
                    }

                    if (!entityManager.HasComponent<Item>(itemAtIndex)) {
                        Debug.LogWarning("Dropping item does not have Item component" + slotIndex);
                        break;
                    }

                    Item itemData = entityManager.GetComponentData<Item>(itemAtIndex);
                    itemData.slot = -1;
                    itemData.onGround = true;
                    itemData.isEquipped = false;
                    commandBuffer.SetComponent(itemAtIndex, itemData);

                    SpriteRenderer spriteRenderer = entityManager.GetComponentObject<SpriteRenderer>(itemAtIndex);
                    spriteRenderer.enabled = true;

                    commandBuffer.SetComponent(itemAtIndex, new LocalTransform {
                        Position = playerTransform.Position,
                        Rotation = Quaternion.identity,
                        Scale = 1f
                    });
                    commandBuffer.AddComponent<DroppedItemTag>(itemAtIndex);
                    commandBuffer.AddComponent<UpdateUserInterfaceTag>(playerSingleton.PlayerEntity);

                    DynamicBuffer<Inventory> inventoryBuffer = entityManager.GetBuffer<Inventory>(playerSingleton.PlayerEntity);

                    int indexToRemove = -1;

                    for (int i = 0; i < inventoryBuffer.Length; i++) {
                        if (inventoryBuffer[i].itemEntity == itemAtIndex) {
                            indexToRemove = i;
                            break;
                        }
                    }

                    if (indexToRemove != -1) {
                        inventoryBuffer.RemoveAt(indexToRemove);
                    }

                    break;
                }
            }

            commandBuffer.Playback(entityManager);
        }
    }

    public void ClearInventory() {
        foreach (var slotData in _inventorySlots.Values) {
            InventorySlot slot = slotData.SlotObject.GetComponent<InventorySlot>();
            if (slot.CurrentItem != null) {
                Destroy(slot.CurrentItem.gameObject);
                slot.RemoveItem();
            }
        }
    }

    public bool AssignItemToSlot(int slotIndex, InventoryItem item, SlotType slotType, ItemType currentItemType) {
        if (_inventorySlots.TryGetValue(slotIndex, out var foundSlotItem)) {
            InventorySlot slot = foundSlotItem.SlotObject.GetComponent<InventorySlot>();

            if (slot.CurrentItem != null) {
                Destroy(slot.CurrentItem.gameObject);
                slot.RemoveItem();
            }

            slot.AssignItem(item, currentItemType);
            return true;
        }

        Debug.LogError($"[AssignItemToSlot]: Slot index {slotIndex} not found.");
        return false;
    }

    public Canvas GetInventoryCanvas() {
        return inventoryCanvas;
    }

    private void CreateHealthBar() {
        var backgroundObject = new GameObject("HealthBarBackground");
        backgroundObject.transform.SetParent(_screenSpaceCanvasObject.transform);
        healthBarBackground = backgroundObject.AddComponent<Image>();
        healthBarBackground.color = Color.black;

        RectTransform backgroundRectTransform = healthBarBackground.GetComponent<RectTransform>();
        backgroundRectTransform.anchorMin = new Vector2(0, 1);
        backgroundRectTransform.anchorMax = new Vector2(0, 1);
        backgroundRectTransform.pivot = new Vector2(0, 1.5f);
        backgroundRectTransform.anchoredPosition = new Vector2(10, -25);
        backgroundRectTransform.sizeDelta = new Vector2(200, 30);

        var foregroundObject = new GameObject("HealthBarForeground");
        foregroundObject.transform.SetParent(_screenSpaceCanvasObject.transform);
        healthBarForeground = foregroundObject.AddComponent<Image>();
        healthBarForeground.color = Color.green;

        RectTransform foregroundRectTransform = healthBarForeground.GetComponent<RectTransform>();
        foregroundRectTransform.anchorMin = new Vector2(0, 1);
        foregroundRectTransform.anchorMax = new Vector2(0, 1);
        foregroundRectTransform.pivot = new Vector2(0, 1.5f);
        foregroundRectTransform.anchoredPosition = new Vector2(10, -25);
        foregroundRectTransform.sizeDelta = new Vector2(200, 30);
    }

    public void UpdateHealthBar(float currentHealth, float maxHealth) {
        if (healthBarForeground != null && healthBarBackground != null) {
            float healthPercentage = currentHealth / maxHealth;

            RectTransform foregroundRectTransform = healthBarForeground.GetComponent<RectTransform>();
            foregroundRectTransform.sizeDelta = new Vector2(200 * healthPercentage, 30);

            healthBarForeground.color = healthPercentage switch {
                > 0.5f => Color.green,
                > 0.25f => Color.yellow,
                _ => Color.red
            };
        }
    }

    private void LoadScreenSpaceCanvas() {
        _screenSpaceCanvasObject = new GameObject("ScreenSpaceCanvas");
        _screenSpaceCanvas = _screenSpaceCanvasObject.AddComponent<Canvas>();
        _screenSpaceCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _screenSpaceCanvasObject.AddComponent<CanvasScaler>();
        _screenSpaceCanvasObject.AddComponent<GraphicRaycaster>();
    }

    public void HideScreenSpaceCanvas() {
        _screenSpaceCanvasObject.gameObject.SetActive(false);
    }

    public void ShowScreenSpaceCanvas() {
        _screenSpaceCanvasObject.gameObject.SetActive(true);
    }

    public record TextSettings {
        public Font Font;
        public int FontSize;
        public TextAnchor Alignment;
        public Vector2 AnchorMin;
        public Vector2 AnchorMax;
        public Vector2 Pivot;
        public Vector2 AnchoredPosition;
        public Color Color;
        public HorizontalWrapMode HorizontalOverflow;
        public VerticalWrapMode VerticalOverflow;
        public Func<string> GetTextValue;
        public TextType TextType;
    }

    private void CreateText(TextSettings settings, string gameObjectName) {
        var newGameObject = new GameObject(gameObjectName);
        newGameObject.transform.SetParent(_screenSpaceCanvasObject.transform);

        var newText = newGameObject.AddComponent<Text>();
        newText.font = settings.Font;
        newText.fontSize = settings.FontSize;
        newText.alignment = settings.Alignment;
        newText.color = settings.Color;
        newText.text = settings.GetTextValue();
        newText.horizontalOverflow = settings.HorizontalOverflow;
        newText.verticalOverflow = settings.VerticalOverflow;

        RectTransform rectTransform = newText.GetComponent<RectTransform>();
        rectTransform.anchorMin = settings.AnchorMin;
        rectTransform.anchorMax = settings.AnchorMax;
        rectTransform.pivot = settings.Pivot;
        rectTransform.anchoredPosition = settings.AnchoredPosition;
        _texts.Add(settings.TextType, newText);
    }

    public void SetTextValue(TextType type, string value) {
        if (!_texts.TryGetValue(type, out var text)) return;
        text.text = value;
        _textData[type] = value;
    }

    public string GetTextValue(TextType type) {
        return _textData.TryGetValue(type, out var value) ? value : null;
    }
}