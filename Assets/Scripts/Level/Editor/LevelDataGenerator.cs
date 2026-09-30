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

            CreateLevel(1,  "Training Road",       "Level1",   85f, LevelDifficulty.Easy,      3);
            CreateLevel(2,  "City Dawn",           "Level2",   75f, LevelDifficulty.Easy,      4);
            CreateLevel(3,  "Mountain Mist",       "Level3",   70f, LevelDifficulty.Medium,    4);
            CreateLevel(4,  "Desert Hazard",       "Level4",   65f, LevelDifficulty.Medium,    5);
            CreateLevel(5,  "Sunset Speedway",     "Level5",   60f, LevelDifficulty.Hard,      5);
            CreateLevel(6,  "Scorched Canyon",     "Level6",   55f, LevelDifficulty.Hard,      6);
            CreateLevel(7,  "Stormy Ridge",        "Level7",   50f, LevelDifficulty.VeryHard,  6);
            CreateLevel(8,  "Cyber Metropolis",    "Level8",   46f, LevelDifficulty.VeryHard,  7);
            CreateLevel(9,  "Inferno Circuit",     "Level9",   42f, LevelDifficulty.Extreme,   7);
            CreateLevel(10, "Grand Championship",  "Level10",  38f, LevelDifficulty.Extreme,   8);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[LevelDataGenerator] Created / updated 10 LevelData assets in " + OutputFolder);
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
