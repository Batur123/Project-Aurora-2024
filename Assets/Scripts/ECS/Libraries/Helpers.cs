using ECS.Components;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace ECS.Libraries {
    public static class GunAttachmentHelper {
        public static void RequestRemoveAttachment(Entity gunEntity, Entity attachmentEntity) {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var requestEntity = ecb.CreateEntity();
            ecb.AddComponent(requestEntity, new RemoveAttachmentRequest {
                gunEntity = gunEntity,
                attachmentEntity = attachmentEntity
            });
            ecb.Playback(World.DefaultGameObjectInjectionWorld.EntityManager);
            ecb.Dispose();
        }
    }
    
    public static class LootHelper {
        public static void RequestRemoveAttachment(Entity gunEntity, Entity attachmentEntity) {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var requestEntity = ecb.CreateEntity();
            ecb.AddComponent(requestEntity, new RemoveAttachmentRequest {
                gunEntity = gunEntity,
                attachmentEntity = attachmentEntity
            });
            ecb.Playback(World.DefaultGameObjectInjectionWorld.EntityManager);
            ecb.Dispose();
        }

        public static BaseWeaponData GetRandomStats(GunTemplateBlob blobData) {
            var template = blobData.statsRangeData;
            BaseWeaponData stats = new BaseWeaponData {
                range = {
                    minAmmoCapacity = template.minAmmoCapacity,
                    maxAmmoCapacity = template.maxAmmoCapacity,
                    minDamage = template.minDamage,
                    maxDamage = template.maxDamage,
                    minAttackSpeed = template.minAttackSpeed,
                    maxAttackSpeed = template.maxAttackSpeed,
                    minRecoilAmount = template.minRecoilAmount,
                    maxRecoilAmount = template.maxRecoilAmount,
                    minSpreadAmount = template.minSpreadAmount,
                    maxSpreadAmount = template.maxSpreadAmount,
                    minBulletsPerShot = template.minBulletsPerShot,
                    maxBulletsPerShot = template.maxBulletsPerShot,
                    minReloadSpeed = template.minReloadSpeed, 
                    maxReloadSpeed = template.maxReloadSpeed,
                    minPiercingBulletsPerShot = template.minPiercingBulletsPerShot,
                    maxPiercingBulletsPerShot = template.maxPiercingBulletsPerShot,
                },
                stats = {
                    ammoCapacity = GetWeightedRandomValue(template.minAmmoCapacity, template.maxAmmoCapacity, 2.5f),
                    damage = GetWeightedRandomValue(template.minDamage, template.maxDamage, 2.5f),
                    attackSpeed =  GetWeightedRandomValue(template.minAttackSpeed, template.maxAttackSpeed, 2.5f),
                    recoilAmount = GetWeightedRandomValue(template.minRecoilAmount, template.maxRecoilAmount, 2.5f),
                    spreadAmount = GetWeightedRandomValue(template.minSpreadAmount, template.maxSpreadAmount, 2.5f),
                    bulletsPerShot = GetWeightedRandomValue(template.minBulletsPerShot, template.maxBulletsPerShot, 3f),
                    reloadSpeed =  GetWeightedRandomValue(template.minReloadSpeed, template.maxReloadSpeed, 2.5f),
                    piercingBulletsPerShot = GetWeightedRandomValue(template.minPiercingBulletsPerShot, template.maxPiercingBulletsPerShot, 2f)
                },
            };

            return stats;
        }

        public static BaseAttachmentData GetRandomStats(AttachmentTemplateBlob blobData) {
            var template = blobData.statsRangeData;
            BaseAttachmentData stats = new BaseAttachmentData {
                range = {
                    minAmmoCapacity = template.minAmmoCapacity,
                    maxAmmoCapacity = template.maxAmmoCapacity,
                    minDamage = template.minDamage,
                    maxDamage = template.maxDamage,
                    minAttackSpeed = template.minAttackSpeed,
                    maxAttackSpeed = template.maxAttackSpeed,
                    minRecoilAmount = template.minRecoilAmount,
                    maxRecoilAmount = template.maxRecoilAmount,
                    minSpreadAmount = template.minSpreadAmount,
                    maxSpreadAmount = template.maxSpreadAmount,
                    minBulletsPerShot = template.minBulletsPerShot,
                    maxBulletsPerShot = template.maxBulletsPerShot,
                    minReloadSpeed = template.minReloadSpeed, 
                    maxReloadSpeed = template.maxReloadSpeed,
                    minPiercingBulletsPerShot = template.minPiercingBulletsPerShot,
                    maxPiercingBulletsPerShot = template.maxPiercingBulletsPerShot,
                },
                stats = {
                    ammoCapacity = GetWeightedRandomValue(template.minAmmoCapacity, template.maxAmmoCapacity, 2.5f),
                    damage = GetWeightedRandomValue(template.minDamage, template.maxDamage, 2.5f),
                    attackSpeed =  GetWeightedRandomValue(template.minAttackSpeed, template.maxAttackSpeed, 2.5f),
                    recoilAmount = GetWeightedRandomValue(template.minRecoilAmount, template.maxRecoilAmount, 2.5f),
                    spreadAmount = GetWeightedRandomValue(template.minSpreadAmount, template.maxSpreadAmount, 2.5f),
                    bulletsPerShot = GetWeightedRandomValue(template.minBulletsPerShot, template.maxBulletsPerShot, 3f),
                    reloadSpeed =  GetWeightedRandomValue(template.minReloadSpeed, template.maxReloadSpeed, 2.5f),
                    piercingBulletsPerShot = GetWeightedRandomValue(template.minPiercingBulletsPerShot, template.maxPiercingBulletsPerShot, 2f)
                },
            };

            return stats;
        }

        private static float GetWeightedRandomValue(float min, float max, float exponent = 2f)
        {
            float random = UnityEngine.Random.value;
            float weighted = Mathf.Pow(random, exponent);
            return Mathf.Lerp(min, max, weighted);
        }

        private static int GetWeightedRandomValue(int min, int max, float exponent = 2f)
        {
            float random = UnityEngine.Random.value;
            float weighted = Mathf.Pow(random, exponent);
            return Mathf.RoundToInt(Mathf.Lerp(min, max, weighted));
        }

        public static Entity GetRandomPassiveItem(ref SystemState state) {
            SystemHandle systemHandle = state.World.GetExistingSystem<PassiveItemsLibrarySystem>();
            PassiveItemsLibrarySystem systemRef = state.World.Unmanaged.GetUnsafeSystemRef<PassiveItemsLibrarySystem>(systemHandle);
            
            NativeHashMap<int, Entity> allPassiveItems = systemRef.GetAllDescriptors();
            NativeHashMap<int, float> lootWeights = new NativeHashMap<int, float>(allPassiveItems.Count, Allocator.Temp);
            
            if (allPassiveItems.Count == 0) {
                lootWeights.Dispose();
                allPassiveItems.Dispose();
                return Entity.Null;
            }
            
            float totalWeight = 0f;

            foreach (var weapon in allPassiveItems) {
                if (!state.EntityManager.HasComponent<GunTypeComponent>(weapon.Value)) {
                    continue;
                }
                
                GunTypeComponent gunTypeComponent = state.EntityManager.GetComponentData<GunTypeComponent>(weapon.Value);
                float weight = gunTypeComponent.lootWeight;
                lootWeights[weapon.Key] = weight;
                totalWeight += weight;
            }
            
            if (totalWeight <= 0f) {
                lootWeights.Dispose();
                allPassiveItems.Dispose();
                return Entity.Null;
            }

            float randomPoint = Random.Range(0f, totalWeight);
            float cumulativeWeight = 0f;
            Entity selectedEntity = Entity.Null;

            foreach (var loot in lootWeights) {
                cumulativeWeight += loot.Value;
                if (randomPoint <= cumulativeWeight) {
                    selectedEntity = allPassiveItems[loot.Key];
                    break;
                }
            }

            return selectedEntity;
        }
        
        public static Entity GetRandomLoot(ref SystemState state) {
            // Guns
            SystemHandle gunLibrarySystemHandle = state.World.GetExistingSystem<GunLibrarySystem>();
            GunLibrarySystem gunLibrarySystemRef = state.World.Unmanaged.GetUnsafeSystemRef<GunLibrarySystem>(gunLibrarySystemHandle);

            // Attachments
            SystemHandle attachmentLibrarySystemHandle = state.World.GetExistingSystem<AttachmentLibrarySystem>();
            AttachmentLibrarySystem attachmentLibrarySystemRef = state.World.Unmanaged.GetUnsafeSystemRef<AttachmentLibrarySystem>(attachmentLibrarySystemHandle);

            NativeHashMap<int, Entity> allAttachments = attachmentLibrarySystemRef.GetAllDescriptors();
            NativeHashMap<int, Entity> allWeapons = gunLibrarySystemRef.GetAllDescriptors();
            
            NativeHashMap<int, float> lootWeights = new NativeHashMap<int, float>(allWeapons.Count + allAttachments.Count, Allocator.Temp);
            
            if (allWeapons.Count == 0 && allAttachments.Count == 0) {
                //Debug.LogWarning("No loot available to select.");
                lootWeights.Dispose();
                allAttachments.Dispose();
                allWeapons.Dispose();
                return Entity.Null;
            }
            
            float totalWeight = 0f;

            foreach (var weapon in allWeapons) {
                if (!state.EntityManager.HasComponent<GunTypeComponent>(weapon.Value)) {
                    //Debug.LogWarning($"[Loot]: Entity {weapon.Value} is missing GunTypeComponent.");
                    continue;
                }
                
                GunTypeComponent gunTypeComponent = state.EntityManager.GetComponentData<GunTypeComponent>(weapon.Value);
                float weight = gunTypeComponent.lootWeight;
                lootWeights[weapon.Key] = weight;
                totalWeight += weight;
                //Debug.Log($"[Loot]: Weapon Key: {weapon.Key}, Weight: {weight}, Total Weight: {totalWeight}");
            }

            foreach (var attachment in allAttachments) {
                if (!state.EntityManager.HasComponent<AttachmentTypeComponent>(attachment.Value)) {
                    //Debug.LogWarning($"[Loot]: Entity {attachment.Value} is missing AttachmentTypeComponent.");
                    continue;
                }
                
                AttachmentTypeComponent attachmentTypeComponent = state.EntityManager.GetComponentData<AttachmentTypeComponent>(attachment.Value);
                float weight = attachmentTypeComponent.lootWeight;
                lootWeights[attachment.Key] = weight;
                totalWeight += weight;
                //Debug.Log($"[Loot]: Attachment Key: {attachment.Key}, Weight: {weight}, Total Weight: {totalWeight}");
            }

            if (totalWeight <= 0f) {
                //Debug.LogWarning("[Loot]: Total weight is zero, no loot can be selected.");
                lootWeights.Dispose();
                allAttachments.Dispose();
                allWeapons.Dispose();
                return Entity.Null;
            }
            
            //Debug.Log("[Loot]: Total weight: " + totalWeight);
            
            float randomPoint = UnityEngine.Random.Range(0f, totalWeight);
            float cumulativeWeight = 0f;
            Entity selectedEntity = Entity.Null;

            //Debug.Log("[Loot]: Selected random point " + randomPoint);
            foreach (var loot in lootWeights) {
                //Debug.Log("Each iteration value = "+loot.Value);
                cumulativeWeight += loot.Value;
                if (randomPoint <= cumulativeWeight) {
                    //Debug.Log("[Loot]: Loot selected");
                    selectedEntity = allWeapons.ContainsKey(loot.Key) ? allWeapons[loot.Key] : allAttachments[loot.Key];
                    //Debug.Log("[Loot]: Entity = " + selectedEntity);
                    break;
                }
            }
            
            //Debug.Log("[Loot]: Cumulative Weight: "+cumulativeWeight);

            if (state.EntityManager.HasComponent<AttachmentTypeComponent>(selectedEntity)) {
                AttachmentTypeComponent attachmentTypeComponent = state.EntityManager.GetComponentData<AttachmentTypeComponent>(selectedEntity);
                Debug.Log("Spawn loot: " + attachmentTypeComponent.attachmentType + " " + attachmentTypeComponent.variantId + " " + attachmentTypeComponent.lootWeight);
                lootWeights.Dispose();
                allAttachments.Dispose();
                allWeapons.Dispose();
                return selectedEntity;
            }
            
            if (state.EntityManager.HasComponent<GunTypeComponent>(selectedEntity)) {
                GunTypeComponent gunTypeComponent = state.EntityManager.GetComponentData<GunTypeComponent>(selectedEntity);
                Debug.Log("Spawn loot: " + gunTypeComponent.gunType + " " + gunTypeComponent.variantId + " " + gunTypeComponent.lootWeight);
                lootWeights.Dispose();
                allAttachments.Dispose();
                allWeapons.Dispose();
                return selectedEntity;
            }
            
            Debug.LogWarning($"[Loot]: Random Loot is not spawned. Total Count {allWeapons.Count + allAttachments.Count}");
            lootWeights.Dispose();
            allAttachments.Dispose();
            allWeapons.Dispose();
            return Entity.Null;
        }
    }
}