using ECS.Authorings.BaseGuns;
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
            root.attackSpeed = template.attackSpeed;
            root.recoilAmount = template.recoilAmount;
            root.damage = template.damage;
            root.spreadAmount = template.spreadAmount;
            root.reloadSpeed = template.reloadSpeed;
            root.ammoCapacity = template.ammoCapacity;
            root.bulletsPerShot = template.bulletsPerShot;
            root.piercingBulletsPerShot = template.piercingBulletsPerShot;

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
