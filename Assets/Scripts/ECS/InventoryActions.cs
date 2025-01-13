using TMPro;
using Unity.Burst;
using Unity.Cinemachine;
using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.UI;

namespace ECS {
    public partial class InventoryUIActions : SystemBase {
        public EntityManager entityManager;

        private GameObject inventoryUI;
        private TMP_Text[] textComponents;

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
                }
                else {
                    inventoryUI.gameObject.SetActive(true);
                    isOpened = true;
                    SetupUI();
                    UIController.Instance.HideScreenSpaceCanvas();
                }
            }
        }

        public void SetupUI() {
            PlayerSingleton playerSingleton = SystemAPI.GetSingleton<PlayerSingleton>();
            CharacterStats characterStats = SystemAPI.GetComponent<CharacterStats>(playerSingleton.PlayerEntity);
            var equippedGunBuffer = SystemAPI.GetBuffer<EquippedGun>(playerSingleton.PlayerEntity);

            //GameObject[] panels = GameObject.FindGameObjectsWithTag("UIPanel");
            //foreach (GameObject panel in panels) {
            //    Debug.Log(panel.name + "2");
            //}

            // Render Weapon on Inventory
            GameObject uiPanelWeaponImage = GameObject.FindGameObjectWithTag("UIPanelWeapon");
            if (uiPanelWeaponImage != null) {
                var image = uiPanelWeaponImage.GetComponent<Image>();

                if (!equippedGunBuffer.IsEmpty) {
                    Entities.WithAll<GunTag>().ForEach((Entity entity, SpriteRenderer spriteRenderer) => {
                        if (entity == equippedGunBuffer[0].GunEntity) {

                            image.sprite = spriteRenderer.sprite;
                        }
                    }).WithoutBurst().Run();
                }
                else {
                    image.sprite = null;
                }
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

            GameObject stockImage = GameObject.FindGameObjectWithTag($"UIPanelAttachment1");
            GameObject magazineImage = GameObject.FindGameObjectWithTag($"UIPanelAttachment2");
            GameObject scopeImage = GameObject.FindGameObjectWithTag($"UIPanelAttachment3");
            GameObject barrelImage = GameObject.FindGameObjectWithTag($"UIPanelAttachment4");
            GameObject ammunitionImage = GameObject.FindGameObjectWithTag($"UIPanelAttachment5");
            if (stockImage != null && magazineImage != null && scopeImage != null && barrelImage != null && ammunitionImage != null) {
                var stockImageComponent = stockImage.GetComponent<Image>();
                var magazineImageComponent = magazineImage.GetComponent<Image>();
                var scopeImageComponent = scopeImage.GetComponent<Image>();
                var barrelImageComponent = barrelImage.GetComponent<Image>();
                var ammunitionImageComponent = ammunitionImage.GetComponent<Image>();

                if (!equippedGunBuffer.IsEmpty) {
                    Entities.WithAll<GunTag>().ForEach((Entity entity, SpriteRenderer spriteRenderer) => {
                        if (entity == equippedGunBuffer[0].GunEntity) {
                            var attachmentBuffers = entityManager.GetBuffer<GunAttachment>(entity);
                            if (!attachmentBuffers.IsEmpty) {
                                foreach(var attachmentEntity in attachmentBuffers)
                                {
                                    //switch (attachmentEntity.AttachmentEntity) {
                                    //    
                                    //}
                                }
                            }
                        }
                    }).WithoutBurst().Run();
                }
                else {
                    stockImageComponent.sprite = null;
                    magazineImageComponent.sprite = null;
                    scopeImageComponent.sprite = null;
                    barrelImageComponent.sprite = null;
                    ammunitionImageComponent.sprite = null;
                }
            }
        }
    }
}