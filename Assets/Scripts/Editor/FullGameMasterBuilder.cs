#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CarRush.Editor
{
    public static class FullGameMasterBuilder
    {
        [MenuItem("CarRush/⚡ Build Full Game (Everything 1-Click)", false, 0)]
        public static void BuildEntireGame()
        {
            Debug.Log("<b>[CarRush] Starting Full Game Build...</b>");

            // Step 1: Main Menu UI & Scene
            MainMenuBuilder.BuildMainMenuScene();

            // Step 2: Level Select UI, Prefabs & 5 LevelData assets
            LevelSelectBuilder.BuildLevelSelectScene();

            // Step 3: Player Car Prefab, 3D Tracks, RaceManager, HUD, and Levels 1 to 5
            RaceTrackBuilder.BuildAllLevels();

            // Step 4: Open MainMenu scene ready for playing
            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity", OpenSceneMode.Single);

            Debug.Log("<color=green><b>===========================================\n[CarRush] FULL GAME SUCCESSFULLY BUILT!\nMainMenu -> LevelSelect -> Levels 1..20 with Obstacles & Ads are ready.\nPress Play (▶) to race!\n===========================================</b></color>");

            EditorUtility.DisplayDialog("CarRush Complete!",
                "Car Rush full game has been built and configured!\n\n" +
                "✓ Main Menu Scene & UI\n" +
                "✓ Level Select Scene & 20 Level Assets with Reset Progress\n" +
                "✓ 3D Player Car with WheelCollider physics & Smooth Interpolation\n" +
                "✓ Levels 1 to 20 with 3D Tracks, Checkpoints, Finish Lines & Road Obstacles\n" +
                "✓ Roadside Advertisement Billboards with Custom Graphic\n" +
                "✓ In-game HUD, Speedometer, Pause & Complete Menus\n" +
                "✓ All 22 scenes registered in Build Settings\n\n" +
                "Press Play (▶) in Unity to test the full game!", "Let's Race! 🏁");
        }
    }
}
#endif
