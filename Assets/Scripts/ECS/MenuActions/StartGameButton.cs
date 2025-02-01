using UnityEngine;
using UnityEngine.UI;

namespace ECS {
    public class ExitButtonClickHandler : MonoBehaviour {
        public Button button;

        void Start() {
            if (button != null) {
                button.onClick.AddListener(OnButtonClick);
            }
        }

        void OnButtonClick() {
            #if UNITY_EDITOR
                        UnityEditor.EditorApplication.isPlaying = false;
            #else
                        Application.Quit();
            #endif
        }
    }
}