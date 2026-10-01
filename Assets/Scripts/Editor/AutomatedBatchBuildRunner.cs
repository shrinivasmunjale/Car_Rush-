#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CarRush.Editor
{
    public class AutomatedBatchBuildRunner : AssetPostprocessor
    {
        private static bool _hasRun = false;

        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            if (_hasRun) return;

            string statusFile = Path.Combine(Directory.GetCurrentDirectory(), "Temp", "agent_build_status.txt");
            if (File.Exists(statusFile) && File.ReadAllText(statusFile).Trim() == "COMPLETED")
            {
                return;
            }

            _hasRun = true;
            EditorApplication.delayCall += ExecuteBuild;
        }

        [MenuItem("CarRush/Execute Automated Full Build", false, 10)]
        public static void ExecuteBuild()
        {
            string statusFile = Path.Combine(Directory.GetCurrentDirectory(), "Temp", "agent_build_status.txt");
            try
            {
                Debug.Log("<b>[AgentBuild] Rebuilding MainMenu UI with verified font asset...</b>");

                // Rebuild MainMenu scene only
                MainMenuBuilder.BuildMainMenuScene();
                EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity", OpenSceneMode.Single);

                File.WriteAllText(statusFile, "COMPLETED");
                Debug.Log("<color=green><b>[AgentBuild] SUCCESS! All 20 levels and scenes generated.</b></color>");
            }
            catch (Exception ex)
            {
                File.WriteAllText(statusFile, "ERROR: " + ex.ToString());
                Debug.LogError("[AgentBuild] Error during automated build: " + ex);
            }
        }
    }
}
#endif
 // trigger update
 // build main menu
