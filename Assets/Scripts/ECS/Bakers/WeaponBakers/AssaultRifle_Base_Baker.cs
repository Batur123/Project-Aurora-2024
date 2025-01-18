using ECS.Authorings.BaseGuns;
using ScriptableObjects;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace ECS.Bakers
{
    public struct MuzzlePointTransform : IComponentData
    {
        public float3 position;   // Position of the muzzle point
        public quaternion rotation; // Rotation of the muzzle point
        public float3 scale;      // Scale of the muzzle point
        public float3 offset;
    }

    public struct ScopePointTransform : IComponentData
    {
        public float3 position;   // Position of the scope point
        public quaternion rotation; // Rotation of the scope point
        public float3 scale;      // Scale of the scope point
        public float3 offset;
    }
    public struct GunTemplateBlob
    {
        public int ammoCapacity;
        public int durability;
        public float damage;
        public float reloadTime;
        public float attackRate;
        public float recoilAmount;
        public float spreadAmount;
        public float lastAttackTime;
        public int bulletsPerShot;
    }

    public struct GunBlobReference : IComponentData
    {
        public BlobAssetReference<GunTemplateBlob> templateBlob;
    }

    public class AssaultRifle_Base : Baker<AssaultRifle_BaseAuthoring>
    {
        public override void Bake(AssaultRifle_BaseAuthoring authoring)
        {
            var template = authoring.gunTemplate;
        
            var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<GunTemplateBlob>();
            root.attackRate = template.attackRate;
            root.recoilAmount = template.recoilAmount;
            root.damage = template.damage;
            root.durability = template.durability;
            root.spreadAmount = template.spreadAmount;
            root.reloadTime = template.reloadTime;
            root.lastAttackTime = template.lastAttackTime;
            root.ammoCapacity = template.ammoCapacity;
            root.bulletsPerShot = template.bulletsPerShot;

            var blobRef = builder.CreateBlobAssetReference<GunTemplateBlob>(Allocator.Persistent);
            builder.Dispose();

            var entity = GetEntity(TransformUsageFlags.None);
            AddBlobAsset(ref blobRef, out var hash);
            AddComponent(entity, new GunBlobReference {
                templateBlob = blobRef
            });
            
            AddComponent(entity, new BuiltPrefab
            {
                prefab = GetEntity(authoring.prefab, TransformUsageFlags.Dynamic),
            });
            AddComponent(entity, new GunTypeComponent
            {
                type = GunType.Rifle
            });
            
            foreach (Transform child in authoring.prefab.transform)
            {
                if (child.name == "MuzzlePoint")
                {
                    Debug.Log("Muzzlepoint init");
                    var muzzleTransform = child;
                    AddComponent(entity, new MuzzlePointTransform
                    {
                        position = muzzleTransform.transform.position,
                        rotation = quaternion.Euler(muzzleTransform.transform.eulerAngles),
                        scale = muzzleTransform.localScale,
                        offset = new float3(0,0,0)
                    });
                }
                else if (child.name == "ScopePoint")
                {
                    var scopeTransform = child;
                    AddComponent(entity, new ScopePointTransform
                    {
                        // Local Transform of Child to Weapon. 
                        position = scopeTransform.transform.position,
                        rotation = quaternion.Euler(scopeTransform.transform.eulerAngles),
                        scale = scopeTransform.transform.localScale,
                        offset = new float3(0,0,0)
                    });
                }
            }
        }
    }
}
