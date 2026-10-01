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
    /// Supports up to 20 levels across two pages (10 per page).
    /// Each page shows 10 level buttons in a single vertical list.
    /// Prev / Next page buttons navigate between the two pages.
    /// Also handles the BACK button and RESET PROGRESS.
    ///
    /// Requires in the scene:
    ///  - a LevelSelectButton prefab (button with number/name/info labels)
    ///  - a container (e.g. an empty GameObject under the Canvas)
    ///  - optionally leftContainer / rightContainer for two-column layout
    ///  - optional prevPageButton / nextPageButton UI Buttons
    /// </summary>
    public class LevelSelectUI : MonoBehaviour
    {
        [Header("Data")]
        [Tooltip("All LevelData assets in level order (1..20).")]
        [SerializeField] private LevelData[] levels;

        [Header("UI")]
        [Tooltip("Reusable button row prefab (has a LevelSelectButton component).")]
        [SerializeField] private LevelSelectButton buttonPrefab;

        [Tooltip("Transform the spawned rows are parented under (fallback).")]
        [SerializeField] private Transform buttonContainer;

        [Tooltip("Optional Left column (for odd levels on a page).")]
        [SerializeField] private Transform leftContainer;

        [Tooltip("Optional Right column (for even levels on a page).")]
        [SerializeField] private Transform rightContainer;

        [Header("Pagination")]
        [Tooltip("Levels shown per page (default 10).")]
        [SerializeField] private int levelsPerPage = 10;

        [Tooltip("PREV PAGE button.")]
        [SerializeField] private Button prevPageButton;

        [Tooltip("NEXT PAGE button.")]
        [SerializeField] private Button nextPageButton;

        [Tooltip("Optional label showing page info, e.g. 'Page 1 / 2'.")]
        [SerializeField] private TMPro.TextMeshProUGUI pageLabel;

        // Internal
        private int currentPage = 0; // 0-indexed
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

            // Wire pagination buttons
            if (prevPageButton != null)
                prevPageButton.onClick.AddListener(OnPrevPage);
            if (nextPageButton != null)
                nextPageButton.onClick.AddListener(OnNextPage);

            BuildList();
        }

        // ── Data loading ──────────────────────────────────────────────────────

        private void EnsureLevelsLoaded()
        {
#if UNITY_EDITOR
            if (levels == null || levels.Length < 20 || HasNullOrEmpty(levels))
            {
                List<LevelData> lvlList = new List<LevelData>();
                for (int i = 1; i <= 20; i++)
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

        // ── Build UI ──────────────────────────────────────────────────────────

        public void BuildList()
        {
            EnsureLevelsLoaded();
            ClearList();

            if (levels == null || levels.Length == 0)
            {
                Debug.LogWarning("[LevelSelectUI] No levels assigned. Drag the LevelData assets into the Levels list.");
                return;
            }

            int totalPages = GetTotalPages();
            currentPage = Mathf.Clamp(currentPage, 0, totalPages - 1);

            int startIndex = currentPage * levelsPerPage;
            int endIndex   = Mathf.Min(startIndex + levelsPerPage, levels.Length);

            for (int i = startIndex; i < endIndex; i++)
            {
                LevelData data = levels[i];
                if (data == null) continue;

                // Two-column layout: first half → left, second half → right
                int pageRelativeIndex = i - startIndex;
                int half = (endIndex - startIndex + 1) / 2;
                Transform parent = buttonContainer;
                if (leftContainer != null && rightContainer != null)
                {
                    parent = (pageRelativeIndex < half) ? leftContainer : rightContainer;
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

            UpdatePaginationButtons();

            // Force layout rebuild
            Canvas.ForceUpdateCanvases();
            if (leftContainer  is RectTransform lrt) LayoutRebuilder.ForceRebuildLayoutImmediate(lrt);
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

        // ── Pagination ────────────────────────────────────────────────────────

        private int GetTotalPages()
        {
            if (levels == null || levels.Length == 0) return 1;
            return Mathf.CeilToInt((float)levels.Length / levelsPerPage);
        }

        private void UpdatePaginationButtons()
        {
            int totalPages = GetTotalPages();

            if (prevPageButton != null)
                prevPageButton.interactable = currentPage > 0;

            if (nextPageButton != null)
                nextPageButton.interactable = currentPage < totalPages - 1;

            if (pageLabel != null)
                pageLabel.text = $"Page {currentPage + 1} / {totalPages}";
        }

        public void OnPrevPage()
        {
            if (currentPage > 0)
            {
                currentPage--;
                BuildList();
            }
        }

        public void OnNextPage()
        {
            if (currentPage < GetTotalPages() - 1)
            {
                currentPage++;
                BuildList();
            }
        }

        // ── Actions ───────────────────────────────────────────────────────────

        /// <summary>Resets player progression back to Level 1 and refreshes the UI instantly.</summary>
        public void OnResetProgressPressed()
        {
            SaveManager.ResetProgress();
            currentPage = 0;
            BuildList();
            Debug.Log("<color=yellow><b>[LevelSelectUI] Progression reset! Only Level 1 is unlocked.</b></color>");
        }

        /// <summary>Wired to the BACK button's OnClick().</summary>
        public void OnBackPressed() => GameStateManager.LoadMainMenu();
    }
}
