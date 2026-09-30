using CarRush.Car;
using CarRush.Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CarRush.UI
{
    /// <summary>
    /// In-game Heads-Up Display (Speedometer, Race Timer, Checkpoints, Countdown, Controls Hint).
    /// </summary>
    public class HUDUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TextMeshProUGUI speedText;
        [SerializeField] private Image speedBar;
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private TextMeshProUGUI checkpointText;
        [SerializeField] private TextMeshProUGUI countdownText;
        [SerializeField] private TextMeshProUGUI levelNameText;
        [SerializeField] private GameObject respawnHintObj;

        private RaceManager raceManager;
        private CarController car;

        private void Start()
        {
            raceManager = RaceManager.Instance;
            if (raceManager != null)
            {
                car = raceManager.PlayerCar;
                raceManager.OnCheckpointReached += UpdateCheckpointDisplay;
                if (raceManager.LevelData != null && levelNameText != null)
                {
                    levelNameText.text = $"{raceManager.LevelData.levelName.ToUpper()}";
                }
                UpdateCheckpointDisplay(raceManager.CurrentCheckpointIndex, raceManager.TotalCheckpoints);
            }
        }

        private void OnDestroy()
        {
            if (raceManager != null)
            {
                raceManager.OnCheckpointReached -= UpdateCheckpointDisplay;
            }
        }

        private void Update()
        {
            if (raceManager == null) return;
            if (car == null) car = raceManager.PlayerCar;

            // Speed
            if (car != null)
            {
                float speed = car.CurrentSpeedKmh;
                if (speedText != null)
                    speedText.text = $"{Mathf.RoundToInt(speed)} <size=20>KM/H</size>";

                if (speedBar != null)
                    speedBar.fillAmount = Mathf.Clamp01(speed / 160f);
            }

            // Timer
            if (timerText != null)
            {
                float t = raceManager.CurrentRaceTime;
                int minutes = (int)(t / 60);
                int seconds = (int)(t % 60);
                int fraction = (int)((t * 100) % 100);
                timerText.text = $"<size=22>TIME</size>\n{minutes:00}:{seconds:00}.<size=22>{fraction:00}</size>";
            }

            // Countdown
            if (countdownText != null)
            {
                countdownText.text = raceManager.CountdownText;
                countdownText.gameObject.SetActive(!string.IsNullOrEmpty(raceManager.CountdownText));
            }
        }

        private void UpdateCheckpointDisplay(int current, int total)
        {
            if (checkpointText != null)
            {
                checkpointText.text = $"CHECKPOINT <color=#FFD700>{current}</color> / {total}";
            }
        }
    }
}
