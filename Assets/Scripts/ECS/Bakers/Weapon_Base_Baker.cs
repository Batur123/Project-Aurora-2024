using ECS.Authorings.BaseGuns;
using ECS.Components;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace ECS.Bakers {
    public class WeaponBaker : Baker<WeaponAuthoring> {
        public override void Bake(WeaponAuthoring authoring) {
            var template = authoring.gunTemplate;

            var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<GunTemplateBlob>();

            var stats = template.statsRangeData;
            
            root.statsRangeData.minAttackSpeed = stats.minAttackSpeed;
            root.statsRangeData.maxAttackSpeed = stats.maxAttackSpeed;
            
            root.statsRangeData.minRecoilAmount = stats.minRecoilAmount;
            root.statsRangeData.maxRecoilAmount = stats.maxRecoilAmount;
            
            root.statsRangeData.minDamage = stats.minDamage;
            root.statsRangeData.maxDamage = stats.maxDamage;
            
            root.statsRangeData.minSpreadAmount = stats.minSpreadAmount;
            root.statsRangeData.maxSpreadAmount = stats.maxSpreadAmount;
            
            root.statsRangeData.minReloadSpeed = stats.minReloadSpeed;
            root.statsRangeData.maxReloadSpeed = stats.maxReloadSpeed;
            
            root.statsRangeData.minAmmoCapacity = stats.minAmmoCapacity;
            root.statsRangeData.maxAmmoCapacity = stats.maxAmmoCapacity;
            
            root.statsRangeData.minBulletsPerShot = stats.minBulletsPerShot;
            root.statsRangeData.maxBulletsPerShot = stats.maxBulletsPerShot;
            
            root.statsRangeData.minPiercingBulletsPerShot = stats.minPiercingBulletsPerShot;
            root.statsRangeData.maxPiercingBulletsPerShot = stats.maxPiercingBulletsPerShot;

            var blobRef = builder.CreateBlobAssetReference<GunTemplateBlob>(Allocator.Persistent);
            builder.Dispose();

            var entity = GetEntity(TransformUsageFlags.None);
            AddBlobAsset(ref blobRef, out var hash);
            AddComponent(entity, new GunBlobReference {
                templateBlob = blobRef
            });

            AddComponent(entity, new BuiltPrefab {
                prefab = GetEntity(authoring.prefab, TransformUsageFlags.Dynamic)
            });

            AddComponent(entity, new GunTypeComponent {
                gunType = authoring.weaponType,
                variantId = authoring.variantId,
                lootWeight = authoring.lootWeight
            });
            
            AddComponent(entity, new WeaponProjectileTypeComponent {
                projectileType = template.projectileType
            });

            foreach (Transform child in authoring.prefab.transform) {
                if (child.name == "MuzzlePoint") {
                    var spriteRenderer = child.GetComponent<SpriteRenderer>();

                    AddComponent(entity, new MuzzlePointTransform {
                        position = child.position,
                        rotation = quaternion.Euler(child.eulerAngles),
                        scale = child.localScale,
                        offset = float3.zero,
                        boundOffset = new float3(spriteRenderer.bounds.extents.x, 0, 0)
                    });
                } else if (child.name == "ScopePoint") {
                    var spriteRenderer = child.GetComponent<SpriteRenderer>();

                    AddComponent(entity, new ScopePointTransform {
                        position = child.position,
                        rotation = quaternion.Euler(child.eulerAngles),
                        scale = child.localScale,
                        offset = float3.zero,
                        boundOffset = new float3(0, -spriteRenderer.bounds.extents.y, 0)
                    });
                }
            }
            
            Debug.Log($"[Baker][Weapon]: {authoring.weaponType} - [Item Name]: {template.gunName} - [Variant]: {authoring.variantId}");
        }
    }
}
