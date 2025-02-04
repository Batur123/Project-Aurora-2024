using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace ECS {
    
    public class StartButtonClickHandler : MonoBehaviour
    {
        public Button button;

        void Start()
        {
            if (button != null)
            {
                button.onClick.AddListener(OnButtonClick);
            }
        }

        void OnButtonClick()
        {
            SceneManager.LoadScene("Scenes/MainGameScene", LoadSceneMode.Single);
        }
    }

}