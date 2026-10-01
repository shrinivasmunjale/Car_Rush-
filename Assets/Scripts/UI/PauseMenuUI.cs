using CarRush.Game;
using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace CarRush.UI
{
    /// <summary>
    /// Pause Menu: toggles with ESC, handles Resume, Restart, Level Select, and Main Menu.
    /// Uses the New Input System (ENABLE_INPUT_SYSTEM) so no InvalidOperationException is thrown.
    /// </summary>
    public class PauseMenuUI : MonoBehaviour
    {
        [Header("UI Panels")]
        [SerializeField] private GameObject pausePanel;

        private bool isPaused = false;

        private void Start()
        {
            if (pausePanel != null)
                pausePanel.SetActive(false);
        }

        private void Update()
        {
#if ENABLE_INPUT_SYSTEM
            bool escPressed = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
            bool escPressed = Input.GetKeyDown(KeyCode.Escape);
#endif
            if (escPressed)
            {
                TogglePause();
            }
        }

        public void TogglePause()
        {
            isPaused = !isPaused;
            if (pausePanel != null)
                pausePanel.SetActive(isPaused);

            Time.timeScale = isPaused ? 0f : 1f;
            AudioListener.pause = isPaused;
            Cursor.lockState = isPaused ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = isPaused;
        }

        public void OnResumePressed()
        {
            if (isPaused) TogglePause();
        }

        public void OnRestartPressed()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void OnLevelSelectPressed()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            GameStateManager.LoadLevelSelect();
        }

        public void OnMainMenuPressed()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            GameStateManager.LoadMainMenu();
        }
    }
}
