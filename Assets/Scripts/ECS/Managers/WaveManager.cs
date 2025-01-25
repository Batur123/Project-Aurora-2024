using Unity.Burst;
using Unity.Entities;
using Math = System.Math;

namespace ECS {
    [BurstCompile]
    public partial class WaveManagerSystem : SystemBase {
        protected override void OnCreate() {
            RequireForUpdate<PlayerSingleton>();
            Entity waveManagerEntity = EntityManager.CreateEntity(ComponentType.ReadWrite<WaveManager>());
            SystemAPI.SetSingleton(new WaveManager { currentWave = 1, isActive = false, waveTimer = 20f });
            EntityManager.SetName(waveManagerEntity, "WaveManagerEntity");

        }

        protected override void OnUpdate() {
            WaveManager waveManager = SystemAPI.GetSingleton<WaveManager>();
            var currentText = waveManager.isActive ? $"- Time Left: {Math.Round(waveManager.waveTimer)}" : "";
            UIController.Instance.SetTextValue(UIController.TextType.COUNTDOWN_TEXT, $"Wave: {waveManager.currentWave} {currentText}");
            UIController.Instance.SetTextValue(UIController.TextType.ARMOR_TEXT, !waveManager.isActive ? "Press E to start wave" : "");

            // test
            //UIController.Instance.SetTextValue(UIController.TextType.ITEM_DROP_TEXT, "ITS THE ITEM");
            //UIController.Instance.UpdateTextPosition(UIController.TextType.ITEM_DROP_TEXT, new Vector2(5f, 5f));

            if (!waveManager.isActive) {
                return;
            }

            waveManager.waveTimer -= SystemAPI.Time.DeltaTime;
            if (waveManager.waveTimer <= 0) {
                waveManager.isActive = false;
            }

            SystemAPI.SetSingleton(waveManager);
        }
    }
}