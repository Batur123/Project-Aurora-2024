using System;
using System.Collections.Generic;
using System.Linq;
using ECS;
using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
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

    public Dictionary<int, InventorySlotData> _inventorySlots = new();


    private Image healthBarBackground;
    private Image healthBarForeground;

    public GameObject inventoryPanel;
    public Sprite slotImage;
    public int slotCount = 30;
    public Vector2 slotSize = new Vector2(100, 100);
    public float spacing = 10f;

    [Header("Inventory Item Prefabs")] public GameObject itemPrefab;

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
    }

    private void LoadUI() {
        LoadScreenSpaceCanvas();

        CreateText(new TextSettings {
            Font = Resources.Load<Font>("Fonts/SampleFont"),
            FontSize = 32,
            Alignment = TextAnchor.UpperRight,
            Color = Color.black,
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
            Color = Color.black,
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
            Color = Color.black,
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
            Color = Color.black,
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
            Color = Color.black,
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
            Color = Color.black,
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
    }

    SlotType SelectSlotType(string slotTag) {
        if (slotTag == "InventorySlot") {
            return SlotType.Item;
        }

        if (slotTag == "UIPanelAttachment1") {
            return SlotType.Muzzle_Attachment;
        }

        if (slotTag == "UIPanelAttachment2") {
            return SlotType.Magazine_Attachment;
        }

        if (slotTag == "UIPanelAttachment3") {
            return SlotType.Scope_Attachment;
        }

        if (slotTag == "UIPanelAttachment4") {
            return SlotType.Ammunition_Attachment;
        }

        if (slotTag == "UIPanelWeapon") {
            return SlotType.Weapon;
        }


        return SlotType.None;
    }

    public void RenderItem(int index, Sprite itemImage, SlotType slotType, string itemStats) {
        if (!_inventorySlots.TryGetValue(index, out var foundSlotItem)) {
            Debug.Log("[RenderItem]: "+ index + " not found");
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
        rectTransform.sizeDelta = new Vector2(50, 50);

        AssignItemToSlot(index, itemObjectSpawned.GetComponent<InventoryItem>(), slotType);
    }

    void CreateInventorySlotsManual() {
        string[] slotTags = {
            "UIPanelWeapon", // 0
            "UIPanelAttachment1", // 1
            "UIPanelAttachment2", // 2
            "UIPanelAttachment3", // 3
            "UIPanelAttachment4", // 4
            "InventorySlot" // rest of items
        };

        int i = 0;
        foreach (string slotTag in slotTags) {
            GameObject[] slots = GameObject.FindGameObjectsWithTag(slotTag);

            foreach (GameObject slot in slots) {
                InventorySlot inventorySlot = slot.AddComponent<InventorySlot>();
                SlotType slotType = SelectSlotType(slotTag);
                inventorySlot.Initialize(i, SelectSlotType(slotTag));
                _inventorySlots.Add(i, new InventorySlotData(slot, slotType));
                i++;
            }
        }
    }

    public void SwapItems(int index1, int index2) {
        Debug.Log($"Attempting to swap items in Slot {index1} and Slot {index2}");

        if (_inventorySlots.TryGetValue(index1, out var slotObj1) &&
            _inventorySlots.TryGetValue(index2, out var slotObj2)) {

            InventorySlot slot1 = slotObj1.SlotObject.GetComponent<InventorySlot>();
            InventorySlot slot2 = slotObj2.SlotObject.GetComponent<InventorySlot>();

            InventoryItem item1 = slot1.CurrentItem;
            InventoryItem item2 = slot2.CurrentItem;

            Debug.Log($"Before Swap - Slot 1 Index: {slot1.SlotIndex}, Slot 2 Index: {slot2.SlotIndex}");

            slot1.AssignItem(item2);
            slot2.AssignItem(item1);

            Debug.Log("SWAP STARTS");

            Entity playerSingletonEntity = entityManager.CreateEntityQuery(typeof(PlayerSingleton)).GetSingletonEntity();
            PlayerSingleton playerSingleton = entityManager.GetComponentData<PlayerSingleton>(playerSingletonEntity);
            Entity player = playerSingleton.PlayerEntity;
            
            EntityQuery itemQuery = entityManager.CreateEntityQuery(typeof(Item));

            Entity entity1 = Entity.Null;
            Entity entity2 = Entity.Null;
            
            NativeArray<Entity> allItemEntities = itemQuery.ToEntityArray(Allocator.Temp);
            foreach (Entity currentEntity in allItemEntities) {
                if (!entityManager.HasComponent<Item>(currentEntity)) continue;

                Item itemComponent = entityManager.GetComponentData<Item>(currentEntity);
                Debug.Log($"Found item in slot {itemComponent.slot} - Current index: {currentEntity.Index}");

                if (itemComponent.slot == index1) {
                    entity1 = currentEntity;
                }
                else if (itemComponent.slot == index2) {
                    entity2 = currentEntity;
                }

                if (entity1 != Entity.Null && entity2 != Entity.Null) {
                    break;
                }
            }
            allItemEntities.Dispose();

            if (entity1 != Entity.Null && entity2 != Entity.Null) {
                SwapEntitySlots(entity1, entity2, slotObj1.Type, slotObj2.Type);
            }
            else if (entity1 != Entity.Null && entity2 == Entity.Null) {
                MoveEntityToSlot(entity1, index2, slotObj1.Type, slotObj2.Type);
            }
            else if (entity1 == Entity.Null && entity2 != Entity.Null) {
                MoveEntityToSlot(entity2, index1, slotObj2.Type, slotObj1.Type);
            }
            else {
                Debug.LogWarning("Both slots are empty. No items to swap.");
            }
        }
        else {
            Debug.LogError("One or both slot indices do not exist in the inventory.");
        }
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
    
    public void DropItemAtIndexToGround(int slotIndex) {

        Entity playerSingletonEntity = entityManager.CreateEntityQuery(typeof(PlayerSingleton)).GetSingletonEntity();
        PlayerSingleton playerSingleton = entityManager.GetComponentData<PlayerSingleton>(playerSingletonEntity);
        LocalTransform playerTransform = entityManager.GetComponentData<LocalTransform>(playerSingleton.PlayerEntity);
        DynamicBuffer<EquippedGun> equippedGuns = entityManager.GetBuffer<EquippedGun>(playerSingleton.PlayerEntity);

        /*
         * 0 Weapon 1-2-3-4 Attachment, Rest Item
         */
        if (slotIndex == 0) {
            if (equippedGuns.IsEmpty) {
                Debug.LogWarning("Serious issue, weapon should not be empty while dropping index 0 from inventory");
                return;
            }

            Entity foundWeapon = FindItemAtIndex(slotIndex);
            if (foundWeapon == Entity.Null) {
                Debug.LogWarning("Serious issue that weapon does not found even though we had equippedGun buffer not empty");
                return;
            }

            // First item changes
            Item weaponItemComponent = entityManager.GetComponentData<Item>(foundWeapon);
            weaponItemComponent.slot = -1;
            weaponItemComponent.isEquipped = false;
            weaponItemComponent.onGround = true;
            entityManager.SetComponentData(foundWeapon, weaponItemComponent);
            
            // We dont drop attachments individually just update their items
            DynamicBuffer<Child> attachmentBuffers = entityManager.GetBuffer<Child>(foundWeapon);
            foreach (Child attachmentBuffer in attachmentBuffers) {
                if (!entityManager.HasComponent<Item>(attachmentBuffer.Value) || !entityManager.HasComponent<AttachmentTag>(attachmentBuffer.Value)) {
                    continue;
                }
                
                Item attachmentItem = entityManager.GetComponentData<Item>(attachmentBuffer.Value);
                attachmentItem.slot = -1;
                attachmentItem.isEquipped = false;
                attachmentItem.onGround = true;
                entityManager.SetComponentData(attachmentBuffer.Value, attachmentItem);
            }
            
            // Remove from inventory
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
            
            //Remove from equipped gun buffer
            using (var commandBuffer = new EntityCommandBuffer(Allocator.Temp)) {
                commandBuffer.AddComponent<DroppedItemTag>(equippedGuns[0].GunEntity);
                equippedGuns.Clear();
                commandBuffer.SetComponent(playerSingleton.PlayerEntity, new UIUpdateFlag { needsUpdate = true });
                commandBuffer.Playback(entityManager);
            }
        }
    }

    public void DropWeaponWithAttachments(Entity weaponEntity, Vector2 dropPosition) {
        
    }
    
    private void SwapEntitySlots(Entity entity1, Entity entity2, SlotType slotType1, SlotType slotType2) {
        Item itemComp1 = entityManager.GetComponentData<Item>(entity1);
        Item itemComp2 = entityManager.GetComponentData<Item>(entity2);

        Debug.Log($"Before Swap - Item1 Slot: {itemComp1.slot} (Type: {slotType1}), Item2 Slot: {itemComp2.slot} (Type: {slotType2})");

        (itemComp1.slot, itemComp2.slot) = (itemComp2.slot, itemComp1.slot);

        entityManager.SetComponentData(entity1, itemComp1);
        entityManager.SetComponentData(entity2, itemComp2);

        HandleParentChildRelationships(entity1, itemComp1, slotType1, entity2, itemComp2, slotType2);

        Debug.Log($"After Swap - Item1 Slot: {itemComp1.slot}, Item2 Slot: {itemComp2.slot}");
    }

    private void MoveEntityToSlot(Entity entity, int newSlot, SlotType sourceSlotType, SlotType destinationSlotType) {
        Item itemComp = entityManager.GetComponentData<Item>(entity);

        Debug.Log($"Before Move - Item Slot: {itemComp.slot} (Type: {sourceSlotType}), Moving to Slot: {newSlot} (Type: {destinationSlotType})");

        itemComp.slot = newSlot;

        entityManager.SetComponentData(entity, itemComp);

        HandleParentChildRelationships(entity, itemComp, sourceSlotType, Entity.Null, default, destinationSlotType);

        Debug.Log($"After Move - Item Slot: {itemComp.slot}");
    }
    
    private void HandleParentChildRelationships(Entity entity1, Item itemComp1, SlotType slotType1, Entity entity2, Item itemComp2, SlotType slotType2) {
        if (IsAttachmentSlot(slotType1) && !IsAttachmentSlot(slotType2)) {
            DetachItemFromWeapon(entity1);
        }

        if (IsAttachmentSlot(slotType2) && !IsAttachmentSlot(slotType1)) {
            AttachItemToWeapon(entity1, slotType2);
        }

        if (IsAttachmentSlot(slotType1) && !IsAttachmentSlot(slotType2)) {
            AttachItemToWeapon(entity2, slotType1);
        }
    }
    
    private bool IsAttachmentSlot(SlotType slotType) {
        return slotType == SlotType.Muzzle_Attachment ||
               slotType == SlotType.Scope_Attachment ||
               slotType == SlotType.Magazine_Attachment ||
               slotType == SlotType.Ammunition_Attachment;
    }
    
    private void DetachItemFromWeapon(Entity attachmentEntity) {
        Debug.Log($"Detaching attachment {attachmentEntity} from its parent weapon.");
        if (entityManager.HasComponent<Parent>(attachmentEntity)) {
            var parent = entityManager.GetComponentData<Parent>(attachmentEntity);
            Debug.Log("Parent was "+ parent.Value);
        }
        
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

    private void AttachItemToWeapon(Entity attachmentEntity, SlotType attachmentSlotType) {
        Debug.Log($"Attaching attachment {attachmentEntity} to weapon based on SlotType {attachmentSlotType}.");
        if (attachmentEntity == Entity.Null) {
            Debug.Log("Attachment Entity was null. No need to attach!");
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
            commandBuffer.AddComponent(attachmentEntity, new Parent { Value = equippedGuns[0].GunEntity });
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
    
    public void ClearInventory() {
        foreach (var slotData in _inventorySlots.Values) {
            InventorySlot slot = slotData.SlotObject.GetComponent<InventorySlot>();
            if (slot.CurrentItem != null) {
                Destroy(slot.CurrentItem.gameObject);
                slot.RemoveItem();
            }
        }
    }

    public bool AssignItemToSlot(int slotIndex, InventoryItem item, SlotType slotType) {
        if (_inventorySlots.TryGetValue(slotIndex, out var foundSlotItem)) {
            InventorySlot slot = foundSlotItem.SlotObject.GetComponent<InventorySlot>();

            if (slot.CurrentItem != null) {
                Destroy(slot.CurrentItem.gameObject);
                slot.RemoveItem();
            }

            slot.AssignItem(item);
            return true;
        }
        else {
            Debug.LogError($"[AssignItemToSlot]: Slot index {slotIndex} not found.");
            return false;
        }
    }
    public Canvas GetInventoryCanvas() {
        return inventoryCanvas;
    }
    
    public Canvas GetScreenSpaceCanvas() {
        if (_screenSpaceCanvasObject == null) {
            return null;
        }

        return _screenSpaceCanvasObject.GetComponent<Canvas>();
    }
    
    public Canvas GetCanvas() {
        return GetInventoryCanvas();
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

    public void UpdateTextPosition(TextType type, Vector2 anchoredPosition) {
        if (_texts.TryGetValue(type, out var text)) {
            RectTransform rectTransform = text.GetComponent<RectTransform>();
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.anchorMin = anchoredPosition;
            rectTransform.anchorMax = anchoredPosition;
            rectTransform.pivot = anchoredPosition;
        }
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