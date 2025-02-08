/*namespace ECS {
    using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBar : MonoBehaviour
{
    // Reference to a Screen-Space or World-Space Canvas
    [SerializeField] private Canvas _canvas;
    
    // References to the background and foreground images
    private Image healthBarBackground;
    private Image healthBarForeground;

    // Called once when this script first runs
    private void Awake()
    {
        CreateHealthBar();
    }

    /// <summary>
    /// Create the health bar in code.
    /// </summary>
    private void CreateHealthBar()
    {
        // 1) Background
        GameObject bgObj = new GameObject("HealthBarBackground");
        bgObj.transform.SetParent(_canvas.transform, false); // 'false' = don't keep world position
        healthBarBackground = bgObj.AddComponent<Image>();
        healthBarBackground.color = Color.black;

        // Set a default size for the background
        RectTransform bgRect = healthBarBackground.rectTransform;
        bgRect.sizeDelta = new Vector2(200, 30);

        // 2) Foreground
        GameObject fgObj = new GameObject("HealthBarForeground");
        fgObj.transform.SetParent(_canvas.transform, false);
        healthBarForeground = fgObj.AddComponent<Image>();
        healthBarForeground.color = Color.green;

        RectTransform fgRect = healthBarForeground.rectTransform;
        fgRect.sizeDelta = new Vector2(200, 30);
    }

    /// <summary>
    /// Update the health bar fill based on current and max health.
    /// </summary>
    public void UpdateHealthBar(float currentHealth, float maxHealth)
    {
        if (healthBarForeground == null || healthBarBackground == null) return;

        float healthPercentage = currentHealth / maxHealth;

        // Scale the foreground bar width
        RectTransform fgRect = healthBarForeground.rectTransform;
        fgRect.sizeDelta = new Vector2(200 * healthPercentage, 30);

        // Color code the bar (green/yellow/red) based on remaining health
        healthBarForeground.color = healthPercentage switch
        {
            > 0.5f => Color.green,
            > 0.25f => Color.yellow,
            _ => Color.red
        };
    }

    /// <summary>
    /// Example of positioning the health bar above an enemy in world space.
    /// Call this in LateUpdate or somewhere else each frame.
    /// </summary>
    /// <param name="enemyWorldPos">The enemy's 3D world position.</param>
    /// <param name="yOffset">Vertical offset so the bar floats above the enemy.</param>
    public void PositionHealthBar(Vector3 enemyWorldPos, float yOffset = 2f)
    {
        if (_canvas == null) return;
        
        // Adjust enemy position so the bar floats above its head
        enemyWorldPos.y += yOffset;

        // Convert the world position to screen space
        Vector3 screenPos = Camera.main.WorldToScreenPoint(enemyWorldPos);

        // Move both background and foreground to that screen position
        RectTransform bgRect = healthBarBackground.rectTransform;
        RectTransform fgRect = healthBarForeground.rectTransform;
        bgRect.position = screenPos;
        fgRect.position = screenPos;
    }
}

}*/