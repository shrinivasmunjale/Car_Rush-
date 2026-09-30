#if UNITY_EDITOR
using System.IO;
using CarRush.EditorTools;
using CarRush.Level;
using CarRush.UI;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CarRush.Editor
{
    public static class LevelSelectBuilder
    {
        private const string ScenePath = "Assets/Scenes/LevelSelect.unity";
        private const string ButtonPrefabPath = "Assets/Prefabs/UI/LevelSelectButton.prefab";

        [MenuItem("CarRush/Setup LevelSelect Scene UI", false, 2)]
        public static void BuildLevelSelectScene()
        {
            // 1. Make sure all 10 LevelData assets exist.
            LevelDataGenerator.CreateLevelDataAssets();
            LevelData[] levels = new LevelData[10];
            for (int i = 1; i <= 10; i++)
            {
                levels[i - 1] = AssetDatabase.LoadAssetAtPath<LevelData>("Assets/Settings/LevelData/Level" + i + ".asset");
            }

            // 2. Ensure scene file exists and open it.
            if (!File.Exists(ScenePath))
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, ScenePath);
            }
            else
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }
            AddSceneToBuildSettings(ScenePath);

            // 3. Setup Canvas.
            Canvas canvas = Object.FindAnyObjectByType<Canvas>();
            GameObject canvasObj;
            if (canvas == null)
            {
                canvasObj = new GameObject("Canvas");
                canvas = canvasObj.AddComponent<Canvas>();
            }
            else
            {
                canvasObj = canvas.gameObject;
            }
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
            if (scaler == null) scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            if (canvasObj.GetComponent<GraphicRaycaster>() == null)
                canvasObj.AddComponent<GraphicRaycaster>();

            // 4. Setup EventSystem.
            EventSystem eventSystem = Object.FindAnyObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                GameObject esObj = new GameObject("EventSystem");
                eventSystem = esObj.AddComponent<EventSystem>();
                esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }

            // 5. Add LevelSelectUI script to Canvas.
            LevelSelectUI levelSelectUI = canvasObj.GetComponent<LevelSelectUI>();
            if (levelSelectUI == null)
                levelSelectUI = canvasObj.AddComponent<LevelSelectUI>();

            // 6. Clean up old UI children to avoid duplicates.
            for (int i = canvasObj.transform.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(canvasObj.transform.GetChild(i).gameObject);

            // 7. Background overlay.
            GameObject bgObj = new GameObject("BackgroundPanel", typeof(RectTransform), typeof(Image));
            bgObj.transform.SetParent(canvasObj.transform, false);
            RectTransform bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            Image bgImg = bgObj.GetComponent<Image>();
            bgImg.color = new Color(0.06f, 0.08f, 0.12f, 0.96f);
            bgImg.raycastTarget = false;

            // 8. Title "SELECT LEVEL".
            CreateLabel(canvasObj.transform, "TitleText",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -80),
                new Vector2(900, 80), "SELECT LEVEL", 54, FontStyles.Bold, TextAlignmentOptions.Center, new Color(1f, 0.84f, 0.2f, 1f));

            // 9. Create (or load) the reusable level button prefab.
            LevelSelectButton buttonPrefab = CreateOrLoadButtonPrefab();

            // 10. Level list container with two dedicated vertical columns (Left: 1..5, Right: 6..10)
            GameObject columnsObj = new GameObject("LevelColumns", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            columnsObj.transform.SetParent(canvasObj.transform, false);
            RectTransform colsRt = columnsObj.GetComponent<RectTransform>();
            colsRt.anchorMin = new Vector2(0.5f, 0.5f);
            colsRt.anchorMax = new Vector2(0.5f, 0.5f);
            colsRt.pivot = new Vector2(0.5f, 0.5f);
            colsRt.anchoredPosition = new Vector2(0, 25);
            colsRt.sizeDelta = new Vector2(1240, 480);

            HorizontalLayoutGroup hlg = columnsObj.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 32f;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;

            // Left Column (Levels 1..5)
            GameObject leftCol = new GameObject("LeftColumn", typeof(RectTransform), typeof(VerticalLayoutGroup));
            leftCol.transform.SetParent(columnsObj.transform, false);
            VerticalLayoutGroup vlgL = leftCol.GetComponent<VerticalLayoutGroup>();
            vlgL.spacing = 14f;
            vlgL.childAlignment = TextAnchor.UpperCenter;
            vlgL.childControlWidth = true;
            vlgL.childControlHeight = false;
            vlgL.childForceExpandWidth = true;
            vlgL.childForceExpandHeight = false;

            // Right Column (Levels 6..10)
            GameObject rightCol = new GameObject("RightColumn", typeof(RectTransform), typeof(VerticalLayoutGroup));
            rightCol.transform.SetParent(columnsObj.transform, false);
            VerticalLayoutGroup vlgR = rightCol.GetComponent<VerticalLayoutGroup>();
            vlgR.spacing = 14f;
            vlgR.childAlignment = TextAnchor.UpperCenter;
            vlgR.childControlWidth = true;
            vlgR.childControlHeight = false;
            vlgR.childForceExpandWidth = true;
            vlgR.childForceExpandHeight = false;

            // 11. Bottom Action Buttons (BACK & RESET PROGRESS).
            Button backBtn = CreateTextButton(canvasObj.transform, "BackButton", "BACK",
                new Vector2(-190, -425), new Color(0.25f, 0.28f, 0.35f, 1f));

            Button resetBtn = CreateTextButton(canvasObj.transform, "ResetButton", "RESET PROGRESS",
                new Vector2(190, -425), new Color(0.65f, 0.15f, 0.15f, 1f));

            // 12. Wire everything to LevelSelectUI.
            SerializedObject serializedUI = new SerializedObject(levelSelectUI);
            SerializedProperty levelsProp = serializedUI.FindProperty("levels");
            levelsProp.arraySize = 10;
            for (int i = 0; i < 10; i++)
                levelsProp.GetArrayElementAtIndex(i).objectReferenceValue = levels[i];

            serializedUI.FindProperty("buttonPrefab").objectReferenceValue = buttonPrefab;
            serializedUI.FindProperty("leftContainer").objectReferenceValue = leftCol.transform;
            serializedUI.FindProperty("rightContainer").objectReferenceValue = rightCol.transform;
            serializedUI.FindProperty("buttonContainer").objectReferenceValue = columnsObj.transform;
            serializedUI.ApplyModifiedProperties();

            // 13. Wire Buttons.
            UnityEventTools.AddPersistentListener(backBtn.onClick, levelSelectUI.OnBackPressed);
            UnityEventTools.AddPersistentListener(resetBtn.onClick, levelSelectUI.OnResetProgressPressed);

            // 14. Save scene.
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

            Debug.Log("<color=green><b>[CarRush]</b> LevelSelect UI configured with 10 levels and Reset Progress button!</color>");
            EditorUtility.DisplayDialog("CarRush Setup",
                "LevelSelect UI has been created with all 10 Levels & Reset Progress button!\n\n" +
                "Press Play (▶) to test.", "Awesome!");
        }

        /// <summary>Creates the reusable LevelSelectButton prefab asset.</summary>
        private static LevelSelectButton CreateOrLoadButtonPrefab()
        {
            if (!Directory.Exists("Assets/Prefabs/UI"))
            {
                if (!Directory.Exists("Assets/Prefabs")) AssetDatabase.CreateFolder("Assets", "Prefabs");
                AssetDatabase.CreateFolder("Assets/Prefabs", "UI");
            }

            // Build a clean hierarchy for the row (570 wide x 80 high in grid)
            GameObject root = new GameObject("LevelSelectButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            RectTransform rt = root.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(570, 80);

            LayoutElement le = root.GetComponent<LayoutElement>();
            le.minHeight = 80f;
            le.preferredHeight = 80f;
            le.flexibleWidth = 1f;

            Image img = root.GetComponent<Image>();
            img.color = new Color(0.18f, 0.24f, 0.36f, 0.95f);

            Button btn = root.GetComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor = new Color(0.18f, 0.24f, 0.36f, 0.95f);
            cb.highlightedColor = new Color(0.28f, 0.38f, 0.55f, 1f);
            cb.pressedColor = new Color(0.12f, 0.16f, 0.24f, 1f);
            cb.selectedColor = new Color(0.18f, 0.24f, 0.36f, 0.95f);
            cb.disabledColor = new Color(0.12f, 0.14f, 0.18f, 0.6f);
            btn.colors = cb;

            // 1. Number Label (Left: x = 15 to 135)
            TextMeshProUGUI number = CreateLabel(root.transform, "NumberText",
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(75, 0),
                new Vector2(110, 50), "LEVEL 1", 22, FontStyles.Bold, TextAlignmentOptions.Left, new Color(1f, 0.84f, 0.2f));

            // 2. Name Label (Middle: x = 145 to 420)
            TextMeshProUGUI name = CreateLabel(root.transform, "NameText",
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(280, 0),
                new Vector2(270, 50), "Training Road", 22, FontStyles.Bold, TextAlignmentOptions.Left, Color.white);

            // 3. Info / Lock Label (Right: x = 430 to 555)
            TextMeshProUGUI info = CreateLabel(root.transform, "InfoText",
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-70, 0),
                new Vector2(115, 50), "EASY", 19, FontStyles.Bold, TextAlignmentOptions.Right, new Color(0.3f, 0.9f, 0.4f));

            // Add the LevelSelectButton component and wire its serialized fields.
            LevelSelectButton row = root.AddComponent<LevelSelectButton>();
            SerializedObject so = new SerializedObject(row);
            so.FindProperty("numberLabel").objectReferenceValue = number;
            so.FindProperty("nameLabel").objectReferenceValue = name;
            so.FindProperty("infoLabel").objectReferenceValue = info;
            so.FindProperty("background").objectReferenceValue = img;
            so.ApplyModifiedProperties();

            // Save as a prefab asset
            if (File.Exists(ButtonPrefabPath)) AssetDatabase.DeleteAsset(ButtonPrefabPath);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, ButtonPrefabPath);
            Object.DestroyImmediate(root);

            return prefab.GetComponent<LevelSelectButton>();
        }

        private static TextMeshProUGUI CreateLabel(Transform parent, string name, Vector2 anchorMin,
            Vector2 anchorMax, Vector2 anchoredPos, Vector2 size, string text, float fontSize,
            FontStyles style, TextAlignmentOptions alignment, Color color)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            obj.transform.SetParent(parent, false);

            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            TextMeshProUGUI tmp = obj.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.alignment = alignment;
            tmp.color = color;
            tmp.raycastTarget = false; // Let clicks pass to the Button
            return tmp;
        }

        private static Button CreateTextButton(Transform parent, string name, string label, Vector2 pos, Color color)
        {
            GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(parent, false);

            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(360, 68);

            Image img = btnObj.GetComponent<Image>();
            img.color = color;

            Button btn = btnObj.GetComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor = color;
            cb.highlightedColor = color * 1.25f;
            cb.pressedColor = color * 0.8f;
            cb.selectedColor = color;
            btn.colors = cb;

            CreateLabel(btnObj.transform, "Text (TMP)",
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                label, 32, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);

            return btn;
        }

        private static void AddSceneToBuildSettings(string scenePath)
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            foreach (var s in scenes)
            {
                if (s.path == scenePath) return;
            }

            var newScenes = new EditorBuildSettingsScene[scenes.Length + 1];
            for (int i = 0; i < scenes.Length; i++)
                newScenes[i] = scenes[i];
            newScenes[newScenes.Length - 1] = new EditorBuildSettingsScene(scenePath, true);
            EditorBuildSettings.scenes = newScenes;
        }
    }
}
#endif