/*
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.UI;

namespace ECS
{
    public class HealthBarManager : MonoBehaviour
    {
        // Tracks the background + foreground for each enemy entity
        private Dictionary<Entity, (RectTransform bgRect, RectTransform fgRect)> healthBars =
            new Dictionary<Entity, (RectTransform, RectTransform)>();

        private EntityManager entityManager;
        private Camera mainCam;

        // Adjust as needed
        private float barWidth  = 100f;
        private float barHeight = 10f;
        private float barOffsetY = 0.3f;

        void Start()
        {
            // Grab the ECS world manager and main camera
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            mainCam = Camera.main;
        }

        void Update() {
            var playerQuery = entityManager.CreateEntityQuery(typeof(PlayerSingleton));
            if (playerQuery.IsEmpty)
            {
                Debug.LogWarning("PlayerSingleton entity not found. Skipping health bar updates.");
                return;
            }
            
            // 1) Get all enemies that have (EnemyTag, EnemyData, IsSpawned, LocalTransform)
            using (var enemyArray = entityManager.CreateEntityQuery(
                    ComponentType.ReadOnly<EnemyTag>(),
                    ComponentType.ReadOnly<EnemyData>(),
                    ComponentType.ReadOnly<IsSpawned>(),
                    ComponentType.ReadOnly<LocalTransform>())
                .ToEntityArray(Allocator.Temp))
            {
                Entity playerSingletonEntity = playerQuery.GetSingletonEntity();
                PlayerSingleton playerSingleton = entityManager.GetComponentData<PlayerSingleton>(playerSingletonEntity);
                LocalTransform playerPosition = entityManager.GetComponentData<LocalTransform>(playerSingleton.PlayerEntity);
                
                // 2) Create bars for newly found entities
                foreach (var enemy in enemyArray)
                {
                    if (!healthBars.ContainsKey(enemy))
                    {
                        CreateHealthBarForEnemy(enemy);
                    }
                }

                // 3) Remove bars for entities that no longer exist
                var toRemove = new List<Entity>();
                foreach (var kvp in healthBars)
                {
                    if (!enemyArray.Contains(kvp.Key))
                        toRemove.Add(kvp.Key);
                }
                foreach (var deadEntity in toRemove)
                {
                    // Destroy both background + foreground objects
                    Destroy(healthBars[deadEntity].bgRect.gameObject);
                    Destroy(healthBars[deadEntity].fgRect.gameObject);
                    healthBars.Remove(deadEntity);
                }

                // 4) Update existing health bars (position + fill)
                foreach (var enemy in enemyArray)
                {
                    // Read EnemyData (contains .health, .maxHealth, etc.)
                    EnemyData enemyData = entityManager.GetComponentData<EnemyData>(enemy);

                    // Position from LocalTransform
                    LocalTransform xform = entityManager.GetComponentData<LocalTransform>(enemy);
                    Vector3 worldPos = xform.Position;
                    worldPos.y += barOffsetY; // Offset so it's above the enemy

                    // Convert to screen coords
                    Vector3 screenPos = mainCam.WorldToScreenPoint(worldPos);

                    // Update bar visuals
                    UpdateHealthBar(playerPosition, enemy, enemyData.health, enemyData.maxHealth, screenPos);
                }
            }
        }

        /// <summary>
        /// Create the background and foreground Images for a new enemy.
        /// </summary>
        private void CreateHealthBarForEnemy(Entity enemy)
        {
            // --- Background ---
            GameObject bgObj = new GameObject($"HealthBarBG_{enemy.Index}");
            bgObj.transform.SetParent(this.transform, false); // Child of the HealthBarManager's Canvas/RectTransform

            Image bgImage = bgObj.AddComponent<Image>();
            bgImage.color = Color.black;

            RectTransform bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.sizeDelta = new Vector2(barWidth, barHeight);

            // --- Foreground ---
            GameObject fgObj = new GameObject($"HealthBarFG_{enemy.Index}");
            fgObj.transform.SetParent(this.transform, false);

            Image fgImage = fgObj.AddComponent<Image>();
            fgImage.color = Color.green;

            RectTransform fgRect = fgObj.GetComponent<RectTransform>();
            fgRect.sizeDelta = new Vector2(barWidth, barHeight);

            // Store these in the dictionary
            healthBars[enemy] = (bgRect, fgRect);
        }

        /// <summary>
        /// Update the bar's size, color, and position for a given enemy.
        /// </summary>
        private void UpdateHealthBar(LocalTransform playerTransform, Entity enemy, float currentHealth, float maxHealth, Vector3 screenPos)
        {
            if (!healthBars.TryGetValue(enemy, out var barPair)) return;

            var (bgRect, fgRect) = barPair;

            // Position both background & foreground
            bgRect.position = screenPos;
            fgRect.position = screenPos;

            // Fill calculation
            float healthPercent = Mathf.Clamp01(currentHealth / maxHealth);

            // Scale the foreground's width
            fgRect.sizeDelta = new Vector2(barWidth * healthPercent, barHeight);

            // Simple color coding
            var fgImage = fgRect.GetComponent<Image>();
            if (healthPercent > 0.5f)
                fgImage.color = Color.green;
            else if (healthPercent > 0.25f)
                fgImage.color = Color.yellow;
            else
                fgImage.color = Color.red;
            
            // Fix: Convert screen position to world position
            Vector3 enemyWorldPosition = mainCam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, mainCam.nearClipPlane));
            enemyWorldPosition.z = playerTransform.Position.z; // Match the player's Z-axis for 2D games

            float distance = Vector3.Distance(playerTransform.Position, enemyWorldPosition);
            float maxDistance = 15f;
            float minAlpha = 0.05f; 
            float alpha = Mathf.Clamp01(1 - (distance / maxDistance));

            alpha = Mathf.Lerp(minAlpha, 1f, alpha); // Linearly interpolate between minAlpha and full opacity (1f)
            var bgImage = bgRect.GetComponent<Image>();
            bgImage.color = new Color(bgImage.color.r, bgImage.color.g, bgImage.color.b, alpha);

            fgImage.color = new Color(fgImage.color.r, fgImage.color.g, fgImage.color.b, alpha);
        }
    }
}
*/
