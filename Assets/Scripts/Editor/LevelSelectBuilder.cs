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
            // 1. Make sure the 5 LevelData assets exist.
            LevelDataGenerator.CreateLevelDataAssets();
            LevelData[] levels = new LevelData[5];
            for (int i = 1; i <= 5; i++)
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
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -90),
                new Vector2(900, 90), "SELECT LEVEL", 56, FontStyles.Bold, TextAlignmentOptions.Center, new Color(1f, 0.84f, 0.2f, 1f));

            // 9. Create (or load) the reusable level button prefab.
            LevelSelectButton buttonPrefab = CreateOrLoadButtonPrefab();

            // 10. Level list container with a vertical layout.
            GameObject containerObj = new GameObject("LevelList", typeof(RectTransform));
            containerObj.transform.SetParent(canvasObj.transform, false);
            RectTransform containerRt = containerObj.GetComponent<RectTransform>();
            containerRt.anchorMin = new Vector2(0.5f, 0.5f);
            containerRt.anchorMax = new Vector2(0.5f, 0.5f);
            containerRt.pivot = new Vector2(0.5f, 0.5f);
            containerRt.anchoredPosition = new Vector2(0, 10);
            containerRt.sizeDelta = new Vector2(860, 520);

            VerticalLayoutGroup vlg = containerObj.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.spacing = 16f;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            // 11. BACK button (bottom).
            Button backBtn = CreateTextButton(canvasObj.transform, "BackButton", "BACK",
                new Vector2(0, -420), new Color(0.25f, 0.28f, 0.35f, 1f));

            // 12. Wire everything to LevelSelectUI.
            SerializedObject serializedUI = new SerializedObject(levelSelectUI);
            SerializedProperty levelsProp = serializedUI.FindProperty("levels");
            levelsProp.arraySize = 5;
            for (int i = 0; i < 5; i++)
                levelsProp.GetArrayElementAtIndex(i).objectReferenceValue = levels[i];

            serializedUI.FindProperty("buttonPrefab").objectReferenceValue = buttonPrefab;
            serializedUI.FindProperty("buttonContainer").objectReferenceValue = containerObj.transform;
            serializedUI.ApplyModifiedProperties();

            // 13. Wire BACK button.
            UnityEventTools.AddPersistentListener(backBtn.onClick, levelSelectUI.OnBackPressed);

            // 14. Save scene.
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

            Debug.Log("<color=green><b>[CarRush]</b> LevelSelect UI configured successfully and saved!</color>");
            EditorUtility.DisplayDialog("CarRush Setup",
                "LevelSelect UI has been created and wired up successfully!\n\n" +
                "Level 1 is unlocked. Press Play (▶) to test the Level Select screen.", "Awesome!");
        }

        /// <summary>Creates the reusable LevelSelectButton prefab asset.</summary>
        private static LevelSelectButton CreateOrLoadButtonPrefab()
        {
            if (!Directory.Exists("Assets/Prefabs/UI"))
            {
                if (!Directory.Exists("Assets/Prefabs")) AssetDatabase.CreateFolder("Assets", "Prefabs");
                AssetDatabase.CreateFolder("Assets/Prefabs", "UI");
            }

            // Build a clean hierarchy for the row
            GameObject root = new GameObject("LevelSelectButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            RectTransform rt = root.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(820, 84);

            LayoutElement le = root.GetComponent<LayoutElement>();
            le.minHeight = 84f;
            le.preferredHeight = 84f;
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

            // 1. Number Label (Left aligned)
            TextMeshProUGUI number = CreateLabel(root.transform, "NumberText",
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(90, 0),
                new Vector2(150, 60), "LEVEL 1", 28, FontStyles.Bold, TextAlignmentOptions.Left, new Color(1f, 0.84f, 0.2f));

            // 2. Name Label (Center / left-mid)
            TextMeshProUGUI name = CreateLabel(root.transform, "NameText",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-20, 0),
                new Vector2(380, 60), "Training Road", 30, FontStyles.Bold, TextAlignmentOptions.Left, Color.white);

            // 3. Info / Lock Label (Right aligned)
            TextMeshProUGUI info = CreateLabel(root.transform, "InfoText",
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-110, 0),
                new Vector2(200, 60), "★ EASY", 22, FontStyles.Bold, TextAlignmentOptions.Right, new Color(0.3f, 0.9f, 0.4f));

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