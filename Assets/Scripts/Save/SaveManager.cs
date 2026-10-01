using UnityEngine;

namespace CarRush.Save
{
    /// <summary>
    /// Small PlayerPrefs-based save system for Car Rush.
    ///
    /// Stores two things:
    ///  - the highest unlocked level number (Level 1 is always unlocked), and
    ///  - the best completion time (in seconds) for each level.
    ///
    /// Used by LevelSelectUI (unlocking) and later by LevelCompleteUI (times).
    /// </summary>
    public static class SaveManager
    {
        private const string UnlockedLevelKey = "CarRush.UnlockedLevel";
        private const string BestTimeKeyPrefix = "CarRush.BestTime.";

        // ------------------------------------------------------------------
        // Progression / unlocking
        // ------------------------------------------------------------------

        /// <summary>Highest level number the player can currently play.</summary>
        public static int GetUnlockedLevel()
        {
            return PlayerPrefs.GetInt(UnlockedLevelKey, 1);
        }

        /// <summary>True if the player may play this level number.</summary>
        public static bool IsLevelUnlocked(int levelNumber)
        {
            return levelNumber <= GetUnlockedLevel();
        }

        /// <summary>Unlocks a level by number. Never downgrades progress.</summary>
        public static void UnlockLevel(int levelNumber)
        {
            int current = GetUnlockedLevel();
            if (levelNumber > current)
            {
                PlayerPrefs.SetInt(UnlockedLevelKey, levelNumber);
                PlayerPrefs.Save();
                Debug.Log($"<color=green><b>[SaveManager] Level {levelNumber} is now UNLOCKED! (Previous was {current})</b></color>");
            }
        }

        /// <summary>Unlocks all 20 levels (helpful for testing).</summary>
        public static void UnlockAllLevels()
        {
            PlayerPrefs.SetInt(UnlockedLevelKey, 20);
            PlayerPrefs.Save();
            Debug.Log("<color=green><b>[SaveManager] All 20 levels unlocked!</b></color>");
        }

        /// <summary>Resets progress back to Level 1 only.</summary>
        public static void ResetProgress()
        {
            PlayerPrefs.SetInt(UnlockedLevelKey, 1);
            PlayerPrefs.Save();
            Debug.Log("<color=yellow><b>[SaveManager] Progress reset to Level 1.</b></color>");
        }

        // ------------------------------------------------------------------
        // Best times
        // ------------------------------------------------------------------

        /// <summary>True if a best time has been recorded for this level.</summary>
        public static bool HasBestTime(int levelNumber)
        {
            return PlayerPrefs.HasKey(BestTimeKeyFor(levelNumber));
        }

        /// <summary>Best time in seconds, or -1 if none recorded yet.</summary>
        public static float GetBestTime(int levelNumber)
        {
            return PlayerPrefs.GetFloat(BestTimeKeyFor(levelNumber), -1f);
        }

        /// <summary>Stores the time only if it beats the current best.</summary>
        public static void SaveBestTime(int levelNumber, float time)
        {
            float current = GetBestTime(levelNumber);
            if (current < 0f || time < current)
            {
                PlayerPrefs.SetFloat(BestTimeKeyFor(levelNumber), time);
                PlayerPrefs.Save();
            }
        }

        private static string BestTimeKeyFor(int levelNumber)
        {
            return BestTimeKeyPrefix + levelNumber;
        }

#if UNITY_EDITOR
        [UnityEditor.MenuItem("CarRush/Progression/🔓 Unlock All Levels (1..20)", false, 100)]
        private static void MenuUnlockAll()
        {
            UnlockAllLevels();
            UnityEditor.EditorUtility.DisplayDialog("Save Manager", "All levels 1 to 20 are now UNLOCKED!", "OK");
        }

        [UnityEditor.MenuItem("CarRush/Progression/🔒 Reset Progress (Lock to Level 1)", false, 101)]
        private static void MenuResetProgress()
        {
            ResetProgress();
            UnityEditor.EditorUtility.DisplayDialog("Save Manager", "Progress reset: Only Level 1 is unlocked.", "OK");
        }
#endif
    }
}
