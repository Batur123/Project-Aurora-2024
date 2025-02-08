using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

namespace ECS
{
    public class StartButtonClickHandler : MonoBehaviour
    {
        [Header("UI References")]
        public Button startButton;
        public GameObject loadingPanel;   // Drag your LoadingPanel here
        public Slider progressBar;        // Optional, for loading progress

        private void Start()
        {
            if (startButton != null)
                startButton.onClick.AddListener(OnStartButtonClick);

            // Ensure loading panel is hidden by default
            if (loadingPanel != null)
                loadingPanel.SetActive(false);
        }

        private void OnStartButtonClick()
        {
            // Hide the main menu panel if you want
            // mainMenuPanel.SetActive(false);

            // Show the loading UI
            if (loadingPanel != null)
                loadingPanel.SetActive(true);

            // Start loading the game scene
            StartCoroutine(LoadGameSceneAsync("Scenes/MainGameScene"));
        }

        private IEnumerator LoadGameSceneAsync(string sceneName)
        {
            // Begin loading the scene asynchronously
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);

            // OPTIONAL: Stop the scene from activating automatically
            // if you want to control exactly when it switches:
            // asyncLoad.allowSceneActivation = false;

            // While the scene is still loading...
            while (!asyncLoad.isDone)
            {
                // Update your progress bar if you have one
                if (progressBar != null)
                {
                    // The async load progress goes from 0 to 0.9 while loading,
                    // then 0.9 to 1.0 when the scene is ready to be activated.
                    float progressValue = Mathf.Clamp01(asyncLoad.progress / 0.9f);
                    progressBar.value = progressValue;
                }

                // OPTIONAL: If you disabled scene auto-activation,
                // you can manually activate once it's near 0.9
                // if (asyncLoad.progress >= 0.9f)
                // {
                //    asyncLoad.allowSceneActivation = true;
                // }

                yield return null;  // Wait for next frame
            }

            // Once isDone == true, the scene is loaded
        }
    }
}
