using UnityEngine;
using UnityEngine.SceneManagement;

namespace CarRush.Game
{
    /// <summary>
    /// Central navigation for Car Rush.
    ///
    /// Every scene name lives in ONE place (here), and UI scripts call the
    /// typed methods below instead of hard-coding scene name strings.
    /// Level scenes are not listed here on purpose: levels are loaded later
    /// through their LevelData asset (ScriptableObject) so levels never
    /// need to be hard-coded either.
    /// </summary>
    public static class GameStateManager
    {
        // ------------------------------------------------------------------
        // Scene names (single source of truth for menu scenes).
        // Keep these in sync with the scenes added in Build Profiles.
        // ------------------------------------------------------------------
        public const string MainMenuScene = "MainMenu";
        public const string LevelSelectScene = "LevelSelect";

        /// <summary>Loads the Main Menu scene.</summary>
        public static void LoadMainMenu() => SceneManager.LoadScene(MainMenuScene);

        /// <summary>Loads the Level Select scene.</summary>
        public static void LoadLevelSelect() => SceneManager.LoadScene(LevelSelectScene);

        /// <summary>
        /// Loads a level scene by name (supplied by a LevelData asset later).
        /// Public now so Level Select can reuse it once levels exist.
        /// </summary>
        public static void LoadLevel(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogError("[GameStateManager] LoadLevel called with an empty scene name.");
                return;
            }

            SceneManager.LoadScene(sceneName);
        }

        /// <summary>
        /// Exits the game. In the Editor this stops Play Mode so the EXIT
        /// button can actually be tested; in a real build it quits the app.
        /// </summary>
        public static void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
