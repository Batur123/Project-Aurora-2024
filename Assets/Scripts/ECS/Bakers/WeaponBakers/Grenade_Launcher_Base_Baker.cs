using ECS.Authorings.BaseGuns;
using ScriptableObjects;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace ECS.Bakers
{
    public class Grenade_Launcher_Base : Baker<Grenade_Launcher_BaseAuthoring>
    {
        public override void Bake(Grenade_Launcher_BaseAuthoring authoring)
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
                type = GunType.GrenadeLauncher
            });
            
            foreach (Transform child in authoring.prefab.transform)
            {
                if (child.name == "MuzzlePoint")
                {
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
