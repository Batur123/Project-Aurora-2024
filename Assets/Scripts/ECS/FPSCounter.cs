using UnityEngine;

namespace ECS {
    public class FPSCounter : MonoBehaviour
    {
        private float deltaTime = 0.0f;
        private GUIStyle guiStyle = new GUIStyle();

        void Update()
        {
            // Calculate the time between frames
            deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        }

        void OnGUI()
        {
            // Set up the font size and color for the FPS display
            guiStyle.fontSize = 25;
            guiStyle.normal.textColor = Color.green;

            // Calculate FPS
            float fps = 1.0f / deltaTime;

            // Display the FPS on the screen
            GUI.Label(new Rect(10, 0, 100, 50), "FPS: " + Mathf.Ceil(fps), guiStyle);
        }
    }

}