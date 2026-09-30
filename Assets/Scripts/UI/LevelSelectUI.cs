using CarRush.Game;
using CarRush.Level;
using CarRush.Save;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CarRush.UI
{
    /// <summary>
    /// Level Select screen.
    ///
    /// Holds the ordered list of LevelData assets, spawns one button row per
    /// level into a container, locks locked levels, and loads the matching
    /// scene when an unlocked row is clicked. Also handles the BACK button.
    ///
    /// Requires in the scene:
    ///  - a LevelSelectButton prefab (button with number/name/info labels)
    ///  - a container (e.g. an empty GameObject under the Canvas)
    /// </summary>
    public class LevelSelectUI : MonoBehaviour
    {
        [Header("Data")]
        [Tooltip("All LevelData assets in level order (1..10).")]
        [SerializeField] private LevelData[] levels;

        [Header("UI")]
        [Tooltip("Reusable button row prefab (has a LevelSelectButton component).")]
        [SerializeField] private LevelSelectButton buttonPrefab;

        [Tooltip("Transform the spawned rows are parented under (or fallback).")]
        [SerializeField] private Transform buttonContainer;

        [Tooltip("Optional Left column (for Levels 1..5).")]
        [SerializeField] private Transform leftContainer;

        [Tooltip("Optional Right column (for Levels 6..10).")]
        [SerializeField] private Transform rightContainer;

        private readonly List<LevelSelectButton> spawnedButtons = new List<LevelSelectButton>();

        private void Awake()
        {
            EnsureLevelsLoaded();
        }

        private void Start()
        {
            // Level Select is a menu screen: always show the cursor.
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            BuildList();
        }

        private void EnsureLevelsLoaded()
        {
#if UNITY_EDITOR
            if (levels == null || levels.Length < 10 || HasNullOrEmpty(levels))
            {
                List<LevelData> lvlList = new List<LevelData>();
                for (int i = 1; i <= 10; i++)
                {
                    LevelData ld = UnityEditor.AssetDatabase.LoadAssetAtPath<LevelData>($"Assets/Settings/LevelData/Level{i}.asset");
                    if (ld != null) lvlList.Add(ld);
                }
                if (lvlList.Count > 0) levels = lvlList.ToArray();
            }
#endif
        }

        private bool HasNullOrEmpty(LevelData[] array)
        {
            if (array == null || array.Length == 0) return true;
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i] == null) return true;
            }
            return false;
        }

        public void BuildList()
        {
            EnsureLevelsLoaded();
            ClearList();

            if (levels == null || levels.Length == 0)
            {
                Debug.LogWarning("[LevelSelectUI] No levels assigned. Drag the LevelData assets into the Levels list.");
                return;
            }

            for (int i = 0; i < levels.Length; i++)
            {
                LevelData data = levels[i];
                if (data == null) continue;

                // Pick container: Left for 1..5, Right for 6..10
                Transform parent = buttonContainer;
                if (leftContainer != null && rightContainer != null)
                {
                    parent = (i < 5) ? leftContainer : rightContainer;
                }

                bool unlocked = SaveManager.IsLevelUnlocked(data.levelNumber);

                LevelSelectButton row = Instantiate(buttonPrefab, parent);
                row.Setup(data, unlocked);

                if (unlocked)
                {
                    LevelData captured = data;
                    row.GetComponent<Button>().onClick.AddListener(() =>
                    {
                        Debug.Log($"<color=green>[LevelSelectUI] Selected {captured.levelName} ({captured.sceneName}). Loading...</color>");
                        GameStateManager.LoadLevel(captured.sceneName);
                    });
                }

                spawnedButtons.Add(row);
            }

            // Force layout rebuild
            Canvas.ForceUpdateCanvases();
            if (leftContainer is RectTransform lrt) LayoutRebuilder.ForceRebuildLayoutImmediate(lrt);
            if (rightContainer is RectTransform rrt) LayoutRebuilder.ForceRebuildLayoutImmediate(rrt);
            if (buttonContainer is RectTransform brt) LayoutRebuilder.ForceRebuildLayoutImmediate(brt);
        }

        private void ClearList()
        {
            foreach (LevelSelectButton row in spawnedButtons)
            {
                if (row != null)
                    Destroy(row.gameObject);
            }
            spawnedButtons.Clear();
        }

        /// <summary>Resets player progression back to Level 1 and refreshes the UI instantly.</summary>
        public void OnResetProgressPressed()
        {
            SaveManager.ResetProgress();
            BuildList();
            Debug.Log("<color=yellow><b>[LevelSelectUI] Progression reset! Only Level 1 is unlocked.</b></color>");
        }

        /// <summary>Wired to the BACK button's OnClick().</summary>
        public void OnBackPressed() => GameStateManager.LoadMainMenu();
    }
}
