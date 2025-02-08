using ECS.Components;
using Unity.Burst;
using Unity.Entities;
using Math = System.Math;

namespace ECS {
    [BurstCompile]
    public partial class WaveManagerSystem : SystemBase {
        protected override void OnCreate() {
            RequireForUpdate<PlayerSingleton>();
            Entity waveManagerEntity = EntityManager.CreateEntity(ComponentType.ReadWrite<WaveManager>());
            SystemAPI.SetSingleton(new WaveManager { currentWave = 1, isActive = false, waveTimer = 100f, totalEnemy = 0});
            EntityManager.SetName(waveManagerEntity, "WaveManagerEntity");

        }

        protected override void OnUpdate() {
            WaveManager waveManager = SystemAPI.GetSingleton<WaveManager>();
            int totalEnemyNumber = EntityManager.CreateEntityQuery(typeof(EnemyTag), typeof(IsSpawned)).CalculateEntityCount();

            
            var currentText = waveManager.isActive ? $"- Time Left: {Math.Round(waveManager.waveTimer)}" : $"";
            

            UIController.Instance.SetTextValue(UIController.TextType.COUNTDOWN_TEXT, $"Wave: {waveManager.currentWave} {currentText} - [Total Enemy]: {totalEnemyNumber}");
            //UIController.Instance.SetTextValue(UIController.TextType.ARMOR_TEXT, !waveManager.isActive ? "Press E to start wave" : "");

            if (!waveManager.isActive) {
                return;
            }

            waveManager.waveTimer -= SystemAPI.Time.DeltaTime;
           // if (waveManager.waveTimer <= 0) {
                waveManager.isActive = false;
           // }

            SystemAPI.SetSingleton(waveManager);
        }
    }
}