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
        [Tooltip("All 5 LevelData assets, in level order (1..5).")]
        [SerializeField] private LevelData[] levels;

        [Header("UI")]
        [Tooltip("Reusable button row prefab (has a LevelSelectButton component).")]
        [SerializeField] private LevelSelectButton buttonPrefab;

        [Tooltip("Transform the spawned rows are parented under (a vertical layout group works nicely).")]
        [SerializeField] private Transform buttonContainer;

        private readonly List<LevelSelectButton> spawnedButtons = new List<LevelSelectButton>();

        private void Awake()
        {
#if UNITY_EDITOR
            // Safety fallback: if levels array has missing references, auto-fill from Assets
            if (levels == null || levels.Length < 5 || levels[0] == null)
            {
                levels = new LevelData[5];
                for (int i = 1; i <= 5; i++)
                {
                    levels[i - 1] = UnityEditor.AssetDatabase.LoadAssetAtPath<LevelData>($"Assets/Settings/LevelData/Level{i}.asset");
                }
            }
#endif
        }

        private void Start()
        {
            // Level Select is a menu screen: always show the cursor.
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            BuildList();
        }

        private void BuildList()
        {
            ClearList();

            if (levels == null || levels.Length == 0)
            {
                Debug.LogWarning("[LevelSelectUI] No levels assigned. Drag the LevelData assets into the Levels list.");
                return;
            }

            foreach (LevelData data in levels)
            {
                if (data == null)
                    continue;

                bool unlocked = SaveManager.IsLevelUnlocked(data.levelNumber);

                LevelSelectButton row = Instantiate(buttonPrefab, buttonContainer);
                row.Setup(data, unlocked);

                if (unlocked)
                {
                    // Capture the level in a local so the lambda remembers which
                    // level this specific row belongs to.
                    LevelData captured = data;
                    row.GetComponent<Button>().onClick.AddListener(() =>
                    {
                        Debug.Log($"<color=green>[LevelSelectUI] Selected {captured.levelName} ({captured.sceneName}). Loading...</color>");
                        GameStateManager.LoadLevel(captured.sceneName);
                    });
                }

                spawnedButtons.Add(row);
            }

            // Force layout rebuild so all 5 rows immediately arrange with correct spacing
            if (buttonContainer is RectTransform rt)
            {
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
            }
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

        /// <summary>Wired to the BACK button's OnClick().</summary>
        public void OnBackPressed() => GameStateManager.LoadMainMenu();
    }
}
