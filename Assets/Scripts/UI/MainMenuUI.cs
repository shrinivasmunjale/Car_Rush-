using CarRush.Game;
using UnityEngine;

namespace CarRush.UI
{
    /// <summary>
    /// Main Menu screen: CAR RUSH title + PLAY / SETTINGS / EXIT buttons.
    ///
    /// Add this component to a GameObject in the MainMenu scene (usually the
    /// Canvas), then wire each Button's OnClick() in the Inspector to the
    /// public methods below. This script never types scene names itself; it
    /// asks GameStateManager to handle all navigation.
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Optional popup panel toggled by the SETTINGS button. Leave empty to hide the SETTINGS button's action.")]
        [SerializeField] private GameObject settingsPanel;

        private void Start()
        {
            // Menu screens always show the mouse cursor (races hide it later).
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Start with the settings popup closed.
            if (settingsPanel != null)
                settingsPanel.SetActive(false);
        }

        /// <summary>Wired to the PLAY button's OnClick().</summary>
        public void OnPlayPressed()
        {
            Debug.Log("<color=green>[MainMenuUI] PLAY Button Clicked! Attempting to load LevelSelect...</color>");
            GameStateManager.LoadLevelSelect();
        }

        /// <summary>Wired to the SETTINGS button's OnClick().</summary>
        public void OnSettingsPressed()
        {
            Debug.Log("[MainMenuUI] SETTINGS Button Clicked!");
            ToggleSettingsPanel();
        }

        /// <summary>Wired to the EXIT button's OnClick().</summary>
        public void OnExitPressed()
        {
            Debug.Log("[MainMenuUI] EXIT Button Clicked! Stopping Play mode / Quitting...");
            GameStateManager.QuitGame();
        }

        private void ToggleSettingsPanel()
        {
            if (settingsPanel == null)
            {
                Debug.LogWarning("[MainMenuUI] SETTINGS pressed but no settingsPanel is assigned in the Inspector.");
                return;
            }

            settingsPanel.SetActive(!settingsPanel.activeSelf);
        }
    }
}
