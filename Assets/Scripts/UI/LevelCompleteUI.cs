using CarRush.Game;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CarRush.UI
{
    /// <summary>
    /// Level Complete & Time Up overlay screens.
    /// Shows final time, best time badges, Next Level, Retry, and Menu navigation.
    /// </summary>
    public class LevelCompleteUI : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject victoryPanel;
        [SerializeField] private GameObject timeUpPanel;

        [Header("Victory Text Elements")]
        [SerializeField] private TextMeshProUGUI timeResultText;
        [SerializeField] private TextMeshProUGUI newBestBadge;

        private RaceManager raceManager;

        private void Start()
        {
            if (victoryPanel != null) victoryPanel.SetActive(false);
            if (timeUpPanel != null) timeUpPanel.SetActive(false);

            raceManager = RaceManager.Instance;
            if (raceManager != null)
            {
                raceManager.OnRaceFinished += HandleVictory;
                raceManager.OnTimeUp += HandleTimeUp;
            }
        }

        private void OnDestroy()
        {
            if (raceManager != null)
            {
                raceManager.OnRaceFinished -= HandleVictory;
                raceManager.OnTimeUp -= HandleTimeUp;
            }
        }

        private void HandleVictory(float finalTime, bool isNewBest)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (victoryPanel != null)
            {
                victoryPanel.transform.SetAsLastSibling();
                victoryPanel.SetActive(true);
            }

            int minutes = (int)(finalTime / 60);
            int seconds = (int)(finalTime % 60);
            int fraction = (int)((finalTime * 100) % 100);

            if (timeResultText != null)
                timeResultText.text = $"TIME: {minutes:00}:{seconds:00}.{fraction:00}";

            if (newBestBadge != null)
                newBestBadge.gameObject.SetActive(isNewBest);
        }

        private void HandleTimeUp()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (timeUpPanel != null) timeUpPanel.SetActive(true);
        }

        public void OnNextLevelPressed()
        {
            Time.timeScale = 1f;
            int currentLvl = 1;
            if (raceManager != null && raceManager.LevelData != null && raceManager.LevelData.levelNumber > 0)
            {
                currentLvl = raceManager.LevelData.levelNumber;
            }
            else
            {
                string sName = SceneManager.GetActiveScene().name;
                if (sName.StartsWith("Level") && int.TryParse(sName.Substring(5), out int parsed))
                {
                    currentLvl = parsed;
                }
            }

            int nextLvl = currentLvl + 1;
            if (nextLvl <= 10)
            {
                GameStateManager.LoadLevel($"Level{nextLvl}");
            }
            else
            {
                GameStateManager.LoadLevelSelect();
            }
        }

        public void OnRetryPressed()
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
