using ECS.Authorings.BaseGuns;
using ScriptableObjects;
using Unity.Entities;
using UnityEngine;

namespace ECS.Bakers {
    public class Pistol_Base : Baker<Pistol_BaseAuthoring> {
        public override void Bake(Pistol_BaseAuthoring authoring) {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new BuiltPrefab {
                prefab = GetEntity(authoring.prefab, TransformUsageFlags.Dynamic),
            });
            AddComponent(entity, new GunTypeComponent {
                type = GunType.Pistol
            });
            
            var muzzlePointTransform = authoring.prefab.transform.Find("MuzzlePoint");
            if (muzzlePointTransform != null) {
                Debug.Log(muzzlePointTransform.position);
                AddComponent(entity, new MuzzlePoint {
                    position = muzzlePointTransform.position
                });
            }
        }
    }
}