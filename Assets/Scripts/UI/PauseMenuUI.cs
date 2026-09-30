using CarRush.Game;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CarRush.UI
{
    /// <summary>
    /// Pause Menu: toggles with ESC, handles Resume, Restart, Level Select, and Main Menu.
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
            if (Input.GetKeyDown(KeyCode.Escape))
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
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void OnLevelSelectPressed()
        {
            Time.timeScale = 1f;
            GameStateManager.LoadLevelSelect();
        }

        public void OnMainMenuPressed()
        {
            Time.timeScale = 1f;
            GameStateManager.LoadMainMenu();
        }
    }
}
