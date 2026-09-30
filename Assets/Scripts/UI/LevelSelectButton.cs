using CarRush.Level;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CarRush.UI
{
    /// <summary>
    /// One row in the Level Select list.
    ///
    /// Put this on a Button prefab that contains TextMeshPro children for the
    /// level number, name and an info line (difficulty / LOCKED). Setup() only
    /// fills labels and locks/unlocks the row; it does NOT decide what happens
    /// on click (LevelSelectUI owns that wiring).
    /// </summary>
    public class LevelSelectButton : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Text like 'LEVEL 1'.")]
        [SerializeField] private TextMeshProUGUI numberLabel;

        [Tooltip("Text like 'Training Road'.")]
        [SerializeField] private TextMeshProUGUI nameLabel;

        [Tooltip("Text like 'Easy' when unlocked, or 'LOCKED'.")]
        [SerializeField] private TextMeshProUGUI infoLabel;

        [Tooltip("Optional background image, tinted to show locked state.")]
        [SerializeField] private Image background;

        [Header("Locked styling")]
        [SerializeField] private Color lockedColor = new Color(0.12f, 0.14f, 0.18f, 0.6f);
        [SerializeField] private Color unlockedColor = new Color(0.18f, 0.24f, 0.36f, 0.95f);

        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
        }

        /// <summary>Fills the row with level info and enables/disables it.</summary>
        public void Setup(LevelData data, bool unlocked)
        {
            if (numberLabel != null)
            {
                numberLabel.text = $"LEVEL {data.levelNumber}";
                numberLabel.color = unlocked ? new Color(1f, 0.84f, 0.2f) : new Color(0.6f, 0.6f, 0.6f);
            }

            if (nameLabel != null)
            {
                nameLabel.text = data.levelName;
                nameLabel.color = unlocked ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            }

            if (infoLabel != null)
            {
                infoLabel.text = unlocked ? data.difficulty.ToString().ToUpper() : "LOCKED";
                infoLabel.color = unlocked ? new Color(0.3f, 0.9f, 0.4f) : new Color(0.85f, 0.35f, 0.35f);
            }

            if (background != null)
                background.color = unlocked ? unlockedColor : lockedColor;

            if (button != null)
                button.interactable = unlocked;
        }
    }
}
