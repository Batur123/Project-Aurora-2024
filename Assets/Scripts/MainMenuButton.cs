using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace ECS {
    
    public class ButtonClickHandler : MonoBehaviour
    {
        public Button yourButton; // Assign this via the Inspector

        void Start()
        {
            if (yourButton != null)
            {
                yourButton.onClick.AddListener(OnButtonClick);
            }
        }

        void OnButtonClick()
        {
            SceneManager.LoadScene("Scenes/MainGameScene", LoadSceneMode.Single);
        }
    }

}