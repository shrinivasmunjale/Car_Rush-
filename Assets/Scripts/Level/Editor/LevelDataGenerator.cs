using System.IO;
using CarRush.Level;
using UnityEditor;
using UnityEngine;

namespace CarRush.EditorTools
{
    /// <summary>
    /// One-click editor tool that creates the 5 LevelData assets with the
    /// values from the design doc, so you never have to hand-fill them.
    ///
    /// Run via: Tools > Car Rush > Create Level Data
    /// Output: Assets/Settings/LevelData/LevelN.asset
    /// Safe to run repeatedly: existing assets are updated, not duplicated.
    /// </summary>
    public static class LevelDataGenerator
    {
        private const string OutputFolder = "Assets/Settings/LevelData";

        [MenuItem("Tools/Car Rush/Create Level Data")]
        public static void CreateLevelDataAssets()
        {
            if (!Directory.Exists(OutputFolder))
                AssetDatabase.CreateFolder("Assets/Settings", "LevelData");

            CreateLevel(1, "Training Road",   "Level1", 120f, LevelDifficulty.Easy,      3);
            CreateLevel(2, "City Drive",      "Level2", 110f, LevelDifficulty.Medium,    4);
            CreateLevel(3, "Mountain Road",   "Level3", 100f, LevelDifficulty.Hard,      4);
            CreateLevel(4, "Obstacle Race",   "Level4",  90f, LevelDifficulty.VeryHard,  5);
            CreateLevel(5, "Final Race",      "Level5", 120f, LevelDifficulty.Extreme,   6);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[LevelDataGenerator] Created / updated 5 LevelData assets in " + OutputFolder);
        }

        private static void CreateLevel(int number, string name, string scene,
            float timeLimit, LevelDifficulty difficulty, int checkpoints)
        {
            string path = OutputFolder + "/Level" + number + ".asset";

            LevelData data = AssetDatabase.LoadAssetAtPath<LevelData>(path);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<LevelData>();
                AssetDatabase.CreateAsset(data, path);
            }

            data.levelNumber = number;
            data.levelName = name;
            data.sceneName = scene;
            data.timeLimit = timeLimit;
            data.difficulty = difficulty;
            data.checkpointCount = checkpoints;

            EditorUtility.SetDirty(data);
        }
    }
}
