using UnityEngine;

namespace CarRush.Level
{
    public enum LevelDifficulty
    {
        Easy,
        Medium,
        Hard,
        VeryHard,
        Extreme
    }

    /// <summary>
    /// ScriptableObject that describes one playable level.
    ///
    /// Create instances two ways:
    ///  1. Right-click in the Project window > Create > Car Rush > Level Data, or
    ///  2. Run the LevelDataGenerator editor tool (Tools > Car Rush > Create Level Data)
    ///     which creates all 5 assets with the values from the design doc.
    ///
    /// Fields are public so they are visible/editable in the Inspector and so
    /// the generator tool can fill them. Keep sceneName in sync with the scene
    /// added in Build Profiles.
    /// </summary>
    [CreateAssetMenu(fileName = "LevelData", menuName = "Car Rush/Level Data")]
    public class LevelData : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Position of this level in the game (1..5).")]
        public int levelNumber = 1;

        [Tooltip("Display name, e.g. 'Training Road'.")]
        public string levelName = "New Level";

        [Tooltip("Name of the scene to load, e.g. 'Level1'. Must match a scene in Build Profiles.")]
        public string sceneName = "Level1";

        [Header("Race")]
        [Tooltip("Allowed time in seconds before TIME UP.")]
        public float timeLimit = 120f;

        [Tooltip("Shown on the Level Select screen.")]
        public LevelDifficulty difficulty = LevelDifficulty.Easy;

        [Tooltip("Number of checkpoints before the finish line (HUD shows X / N).")]
        public int checkpointCount = 3;
    }
}
