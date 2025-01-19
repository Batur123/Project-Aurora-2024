using System.Collections.Generic;
using ScriptableObjects;
using TMPro;
using Unity.Burst;
using Unity.Cinemachine;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace ECS {
    public static class InventoryHelper {
        public static int FindFirstEmptyInventorySlot(DynamicBuffer<Inventory> inventory, ComponentLookup<Item> itemLookup) {
            HashSet<int> occupiedSlots = new HashSet<int>();

            foreach (var inventoryItem in inventory) {
                if (itemLookup.HasComponent(inventoryItem.itemEntity)) {
                    Item itemData = itemLookup[inventoryItem.itemEntity];
                    if (itemData.slot >= 5 && itemData.slot <= 23) {
                        occupiedSlots.Add(itemData.slot);
                    }
                }
            }

            for (int slot = 5; slot <= 23; slot++) {
                if (!occupiedSlots.Contains(slot)) {
                    return slot;
                }
            }

            return -1;
        }
    }
    
    public partial class InventoryUIActions : SystemBase {
        public EntityManager entityManager;

        private GameObject inventoryUI;
        private TMP_Text[] textComponents;

        public Sprite slotImage; // The image for each inventory slot
        public int slotCount = 20; // Number of slots
        public Vector2 slotSize = new Vector2(100, 100); // Size of each slot (Width, Height)
        public float spacing = 10f; // Spacing between slots

        public bool isOpened = false;

        protected override void OnCreate() {
            RequireForUpdate<PlayerSingleton>();
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        }


        protected override void OnUpdate() {
            if (inventoryUI == null) {
                inventoryUI = GameObject.FindGameObjectWithTag("InventoryUI");
                inventoryUI.gameObject.SetActive(false);
                textComponents = inventoryUI.GetComponentsInChildren<TMP_Text>(true);
            }

            PlayerSingleton playerSingleton = SystemAPI.GetSingleton<PlayerSingleton>();
            UIUpdateFlag flag = SystemAPI.GetComponent<UIUpdateFlag>(playerSingleton.PlayerEntity);
            if (flag.needsUpdate) {
                SetupUI();
                SystemAPI.SetComponent(playerSingleton.PlayerEntity, new UIUpdateFlag { needsUpdate = false });
            }


            if (Input.GetKeyDown(KeyCode.I)) {
                if (isOpened) {
                    inventoryUI.gameObject.SetActive(false);
                    isOpened = false;
                    UIController.Instance.ShowScreenSpaceCanvas();
                    entityManager.RemoveComponent<InventoryOpen>(playerSingleton.PlayerEntity);
                }
                else {
                    inventoryUI.gameObject.SetActive(true);
                    isOpened = true;
                    SetupUI();
                    UIController.Instance.HideScreenSpaceCanvas();
                    entityManager.AddComponent<InventoryOpen>(playerSingleton.PlayerEntity);
                }
            }
        }


        public void MoveAttachmentToInventory(Entity weaponEntity, Entity attachmentEntity, int inventorySlotIndex) {
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            // Remove attachment from weapon
            if (entityManager.HasComponent<Parent>(attachmentEntity)) {
                ecb.RemoveComponent<Parent>(attachmentEntity);
            }

            // Update attachment item slot and mark it as not equipped
            ecb.SetComponent(attachmentEntity, new Item {
                slot = inventorySlotIndex,
                isEquipped = false,
                onGround = false
            });

            // Add attachment to player's inventory buffer
            var playerEntity = SystemAPI.GetSingleton<PlayerSingleton>().PlayerEntity;
            var inventory = entityManager.GetBuffer<Inventory>(playerEntity);
            inventory.Add(new Inventory { itemEntity = attachmentEntity });

            ecb.Playback(entityManager);
            ecb.Dispose();
        }

        public void AddAttachmentToWeapon(Entity weaponEntity, Entity attachmentEntity, SlotType slotType) {
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            // Link the attachment to the weapon
            ecb.AddComponent(attachmentEntity, new Parent { Value = weaponEntity });
            ecb.SetComponent(attachmentEntity, new LocalToWorld { Value = float4x4.identity });

            // Mark the attachment as equipped
            ecb.SetComponent(attachmentEntity, new Item {
                slot = (int)slotType, // Set slot based on attachment type
                isEquipped = true,
                onGround = false
            });

            ecb.Playback(entityManager);
            ecb.Dispose();
        }

        SlotType SelectAttachmentSlotType(AttachmentType attachmentType) {
            switch (attachmentType) {
                case AttachmentType.Barrel: return SlotType.Muzzle_Attachment;
                case AttachmentType.Ammunition: return SlotType.Ammunition_Attachment;
                case AttachmentType.Scope: return SlotType.Scope_Attachment;
                case AttachmentType.Magazine: return SlotType.Magazine_Attachment;
                default: return SlotType.Item;
            }
        }
        
        public void SetupUI() {
            UIController.Instance.ClearInventory();
            
            PlayerSingleton playerSingleton = SystemAPI.GetSingleton<PlayerSingleton>();
            CharacterStats characterStats = SystemAPI.GetComponent<CharacterStats>(playerSingleton.PlayerEntity);
            DynamicBuffer<Inventory> playerInventory = SystemAPI.GetBuffer<Inventory>(playerSingleton.PlayerEntity);
            DynamicBuffer<EquippedGun> equippedGun = SystemAPI.GetBuffer<EquippedGun>(playerSingleton.PlayerEntity);

            foreach (Inventory item in playerInventory) {
                // Weapon and Attachment Render for Equipped Weapon
                if (
                    entityManager.HasComponent<GunTag>(item.itemEntity) 
                    && entityManager.HasBuffer<EquippedGun>(playerSingleton.PlayerEntity) 
                    && !equippedGun.IsEmpty
                    && item.itemEntity == equippedGun[0].GunEntity) {
                    var gunSprite = entityManager.GetComponentObject<SpriteRenderer>(item.itemEntity);
                    var gunItem = entityManager.GetComponentData<Item>(item.itemEntity);
                    UIController.Instance.RenderItem(gunItem.slot, gunSprite.sprite, SlotType.Weapon, "");

                    DynamicBuffer<Child> attachments = entityManager.GetBuffer<Child>(item.itemEntity);
                    foreach (Child attachment in attachments) {
                        if (entityManager.HasComponent<AttachmentTag>(attachment.Value)) {
                            var attachmentItem = entityManager.GetComponentData<Item>(attachment.Value);
                            var attachmentSprite = entityManager.GetComponentObject<SpriteRenderer>(attachment.Value);
                            var attachmentType = entityManager.GetComponentData<AttachmentTypeComponent>(attachment.Value);
                            UIController.Instance.RenderItem(attachmentItem.slot, attachmentSprite.sprite, SelectAttachmentSlotType(attachmentType.attachmentType), "");
                        }
                    }
                    continue;
                }
                
                var itemData = entityManager.GetComponentData<Item>(item.itemEntity);
                var itemSprite = entityManager.GetComponentObject<SpriteRenderer>(item.itemEntity);
                Debug.Log("SLOT UPDATE NEW?"+ itemData.slot);
                UIController.Instance.RenderItem(itemData.slot, itemSprite.sprite, SlotType.Item, "");
            }

            foreach (TMP_Text textComponent in textComponents) {
                switch (textComponent.name) {
                    case "Stats_HealthRegeneration_Text": {
                        textComponent.text = characterStats.healthRegeneration.ToString();
                        break;
                    }
                    case "Stats_ArmorRegeneration_Text": {
                        textComponent.text = characterStats.armorRegeneration.ToString();
                        break;
                    }
                    case "Stats_CriticalHitChance_Text": {
                        textComponent.text = characterStats.criticalHitChance.ToString();
                        break;
                    }
                    case "Stats_CriticalHitDamage_Text": {
                        textComponent.text = characterStats.criticalDamage.ToString();
                        break;
                    }
                    case "Stats_Luck_Text": {
                        textComponent.text = characterStats.luck.ToString();
                        break;
                    }
                    case "Stats_Sanity_Text": {
                        textComponent.text = characterStats.sanity.ToString();
                        break;
                    }
                    case "Stats_LifeSteal_Text": {
                        textComponent.text = characterStats.lifeSteal.ToString();
                        break;
                    }
                }
            }
        }
    }
}