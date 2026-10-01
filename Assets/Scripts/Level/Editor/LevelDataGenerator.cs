using System.IO;
using CarRush.Level;
using UnityEditor;
using UnityEngine;

namespace CarRush.EditorTools
{
    /// <summary>
    /// One-click editor tool that creates all 20 LevelData assets with the
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

            // ── Page 1: Levels 1-10 ──────────────────────────────────────────
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

            // ── Page 2: Levels 11-20 (harder, tighter time limits) ───────────
            CreateLevel(11, "Frozen Peaks",        "Level11",  35f, LevelDifficulty.Extreme,   8);
            CreateLevel(12, "Neon Underground",    "Level12",  33f, LevelDifficulty.Extreme,   9);
            CreateLevel(13, "Volcanic Fury",       "Level13",  31f, LevelDifficulty.Extreme,   9);
            CreateLevel(14, "Arctic Drift",        "Level14",  30f, LevelDifficulty.Extreme,   9);
            CreateLevel(15, "Phantom Highway",     "Level15",  28f, LevelDifficulty.Extreme,  10);
            CreateLevel(16, "Crimson Ravine",      "Level16",  27f, LevelDifficulty.Extreme,  10);
            CreateLevel(17, "Thunder Valley",      "Level17",  26f, LevelDifficulty.Extreme,  10);
            CreateLevel(18, "Black Ice Speedway",  "Level18",  24f, LevelDifficulty.Extreme,  11);
            CreateLevel(19, "Ghost Circuit",       "Level19",  22f, LevelDifficulty.Extreme,  11);
            CreateLevel(20, "Apex Limit",          "Level20",  20f, LevelDifficulty.Extreme,  12);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[LevelDataGenerator] Created / updated 20 LevelData assets in " + OutputFolder);
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

            data.levelNumber    = number;
            data.levelName      = name;
            data.sceneName      = scene;
            data.timeLimit      = timeLimit;
            data.difficulty     = difficulty;
            data.checkpointCount = checkpoints;

            EditorUtility.SetDirty(data);
        }
    }
}
