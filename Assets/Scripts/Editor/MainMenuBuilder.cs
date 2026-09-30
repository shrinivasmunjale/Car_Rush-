#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using CarRush.UI;

namespace CarRush.Editor
{
    public static class MainMenuBuilder
    {
        private const string ScenePath = "Assets/Scenes/MainMenu.unity";

        [MenuItem("CarRush/Setup MainMenu Scene UI", false, 1)]
        public static void BuildMainMenuScene()
        {
            // 1. Ensure scene file exists and open it
            if (!File.Exists(ScenePath))
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, ScenePath);
            }
            else
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            // 2. Ensure scene is in Build Settings
            AddSceneToBuildSettings(ScenePath);

            // 3. Setup Canvas
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
            {
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            // 4. Setup EventSystem
            EventSystem eventSystem = Object.FindAnyObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                GameObject esObj = new GameObject("EventSystem");
                eventSystem = esObj.AddComponent<EventSystem>();
                esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }

            // 5. Add MainMenuUI script to Canvas
            MainMenuUI mainMenuUI = canvasObj.GetComponent<MainMenuUI>();
            if (mainMenuUI == null)
            {
                mainMenuUI = canvasObj.AddComponent<MainMenuUI>();
            }

            // 6. Clean up old UI children if any to avoid duplicates
            for (int i = canvasObj.transform.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(canvasObj.transform.GetChild(i).gameObject);
            }

            // 7. Create Background overlay (subtle dark gradient/vignette feel)
            GameObject bgObj = new GameObject("BackgroundPanel", typeof(RectTransform), typeof(Image));
            bgObj.transform.SetParent(canvasObj.transform, false);
            RectTransform bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            Image bgImg = bgObj.GetComponent<Image>();
            bgImg.color = new Color(0.07f, 0.08f, 0.12f, 0.95f);
            bgImg.raycastTarget = false; // Do not block UI clicks behind/underneath

            // 8. Create TitleText (TextMeshPro)
            GameObject titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObj.transform.SetParent(canvasObj.transform, false);
            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 0.5f);
            titleRect.anchorMax = new Vector2(0.5f, 0.5f);
            titleRect.pivot = new Vector2(0.5f, 0.5f);
            titleRect.anchoredPosition = new Vector2(0, 240);
            titleRect.sizeDelta = new Vector2(900, 160);

            TextMeshProUGUI titleText = titleObj.GetComponent<TextMeshProUGUI>();
            titleText.text = "CAR RUSH";
            titleText.fontSize = 90;
            titleText.fontStyle = FontStyles.Bold;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = new Color(1f, 0.84f, 0.2f, 1f); // Vibrant Gold
            titleText.raycastTarget = false;

            // Subtitle
            GameObject subTitleObj = new GameObject("SubtitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
            subTitleObj.transform.SetParent(canvasObj.transform, false);
            RectTransform subTitleRect = subTitleObj.GetComponent<RectTransform>();
            subTitleRect.anchorMin = new Vector2(0.5f, 0.5f);
            subTitleRect.anchorMax = new Vector2(0.5f, 0.5f);
            subTitleRect.pivot = new Vector2(0.5f, 0.5f);
            subTitleRect.anchoredPosition = new Vector2(0, 160);
            subTitleRect.sizeDelta = new Vector2(600, 50);

            TextMeshProUGUI subTitleText = subTitleObj.GetComponent<TextMeshProUGUI>();
            subTitleText.text = "HIGH SPEED RACING EXPERIENCE";
            subTitleText.fontSize = 24;
            subTitleText.fontStyle = FontStyles.Normal;
            subTitleText.alignment = TextAlignmentOptions.Center;
            subTitleText.color = new Color(0.7f, 0.75f, 0.85f, 0.8f);

            // 9. Create Buttons
            Button playBtn = CreateMenuButton(canvasObj.transform, "PlayButton", "PLAY", new Vector2(0, 40), new Color(0.15f, 0.65f, 0.35f, 1f));
            Button settingsBtn = CreateMenuButton(canvasObj.transform, "SettingsButton", "SETTINGS", new Vector2(0, -50), new Color(0.2f, 0.4f, 0.7f, 1f));
            Button exitBtn = CreateMenuButton(canvasObj.transform, "ExitButton", "EXIT", new Vector2(0, -140), new Color(0.7f, 0.2f, 0.2f, 1f));

            // 10. Create Settings Panel
            GameObject settingsPanelObj = CreateSettingsPanel(canvasObj.transform);
            settingsPanelObj.SetActive(false);

            // 11. Wire references to MainMenuUI
            SerializedObject serializedUI = new SerializedObject(mainMenuUI);
            SerializedProperty settingsProp = serializedUI.FindProperty("settingsPanel");
            if (settingsProp != null)
            {
                settingsProp.objectReferenceValue = settingsPanelObj;
                serializedUI.ApplyModifiedProperties();
            }

            // 12. Wire OnClick events using UnityEventTools
            UnityEventTools.AddPersistentListener(playBtn.onClick, mainMenuUI.OnPlayPressed);
            UnityEventTools.AddPersistentListener(settingsBtn.onClick, mainMenuUI.OnSettingsPressed);
            UnityEventTools.AddPersistentListener(exitBtn.onClick, mainMenuUI.OnExitPressed);

            // 13. Save scene & notify
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

            Debug.Log("<color=green><b>[CarRush]</b> MainMenu UI configured successfully and saved!</color>");
            EditorUtility.DisplayDialog("CarRush Setup", "MainMenu UI has been created and wired up successfully!\n\nYou can now press Play (▶) to test.", "Awesome!");
        }

        private static Button CreateMenuButton(Transform parent, string name, string label, Vector2 pos, Color normalColor)
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
            img.color = normalColor;

            Button btn = btnObj.GetComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor = normalColor;
            cb.highlightedColor = normalColor * 1.25f;
            cb.pressedColor = normalColor * 0.8f;
            cb.selectedColor = normalColor;
            btn.colors = cb;

            GameObject textObj = new GameObject("Text (TMP)", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(btnObj.transform, false);

            RectTransform textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.sizeDelta = Vector2.zero;

            TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 32;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.raycastTarget = false; // Allow click events to pass directly to Button component

            return btn;
        }

        private static GameObject CreateSettingsPanel(Transform parent)
        {
            GameObject panelObj = new GameObject("SettingsPanel", typeof(RectTransform), typeof(Image));
            panelObj.transform.SetParent(parent, false);

            RectTransform rt = panelObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(680, 560);

            Image img = panelObj.GetComponent<Image>();
            img.color = new Color(0.10f, 0.12f, 0.18f, 0.98f);

            // Title
            GameObject titleObj = new GameObject("PanelTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObj.transform.SetParent(panelObj.transform, false);
            RectTransform titleRt = titleObj.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0.5f, 1f);
            titleRt.anchorMax = new Vector2(0.5f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = new Vector2(0, -25);
            titleRt.sizeDelta = new Vector2(500, 50);

            TextMeshProUGUI title = titleObj.GetComponent<TextMeshProUGUI>();
            title.text = "SETTINGS";
            title.fontSize = 38;
            title.fontStyle = FontStyles.Bold;
            title.alignment = TextAlignmentOptions.Center;
            title.color = new Color(1f, 0.84f, 0.2f);
            title.raycastTarget = false;

            // --- AUDIO SECTION ---
            GameObject volLabelObj = new GameObject("VolLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
            volLabelObj.transform.SetParent(panelObj.transform, false);
            RectTransform vlRt = volLabelObj.GetComponent<RectTransform>();
            vlRt.anchoredPosition = new Vector2(-160, 160);
            vlRt.sizeDelta = new Vector2(250, 40);
            TextMeshProUGUI vlTmp = volLabelObj.GetComponent<TextMeshProUGUI>();
            vlTmp.text = "MASTER VOLUME";
            vlTmp.fontSize = 24;
            vlTmp.fontStyle = FontStyles.Bold;
            vlTmp.color = Color.white;
            vlTmp.raycastTarget = false;

            // Slider
            GameObject sliderObj = new GameObject("VolumeSlider", typeof(RectTransform), typeof(Slider));
            sliderObj.transform.SetParent(panelObj.transform, false);
            RectTransform slRt = sliderObj.GetComponent<RectTransform>();
            slRt.anchoredPosition = new Vector2(40, 160);
            slRt.sizeDelta = new Vector2(260, 24);

            Slider slider = sliderObj.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0.8f;

            // Slider Background
            GameObject slBg = new GameObject("Background", typeof(RectTransform), typeof(Image));
            slBg.transform.SetParent(sliderObj.transform, false);
            RectTransform slBgRt = slBg.GetComponent<RectTransform>();
            slBgRt.anchorMin = Vector2.zero;
            slBgRt.anchorMax = Vector2.one;
            slBgRt.sizeDelta = Vector2.zero;
            slBg.GetComponent<Image>().color = new Color(0.2f, 0.24f, 0.32f);

            // Fill Area & Fill
            GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(sliderObj.transform, false);
            RectTransform faRt = fillArea.GetComponent<RectTransform>();
            faRt.anchorMin = Vector2.zero;
            faRt.anchorMax = Vector2.one;
            faRt.sizeDelta = Vector2.zero;

            GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            RectTransform fillRt = fill.GetComponent<RectTransform>();
            fillRt.sizeDelta = Vector2.zero;
            Image fillImg = fill.GetComponent<Image>();
            fillImg.color = new Color(0.15f, 0.65f, 0.35f);
            slider.fillRect = fillRt;

            // Volume Value readout Text
            GameObject volValObj = new GameObject("VolumeValText", typeof(RectTransform), typeof(TextMeshProUGUI));
            volValObj.transform.SetParent(panelObj.transform, false);
            RectTransform vvRt = volValObj.GetComponent<RectTransform>();
            vvRt.anchoredPosition = new Vector2(240, 160);
            vvRt.sizeDelta = new Vector2(80, 40);
            TextMeshProUGUI vvTmp = volValObj.GetComponent<TextMeshProUGUI>();
            vvTmp.text = "80%";
            vvTmp.fontSize = 22;
            vvTmp.color = new Color(0.3f, 0.9f, 0.4f);
            vvTmp.raycastTarget = false;

            // --- CONTROLS CHEATSHEET SECTION ---
            GameObject ctrlHeadingObj = new GameObject("ControlsHeading", typeof(RectTransform), typeof(TextMeshProUGUI));
            ctrlHeadingObj.transform.SetParent(panelObj.transform, false);
            RectTransform chRt = ctrlHeadingObj.GetComponent<RectTransform>();
            chRt.anchoredPosition = new Vector2(0, 80);
            chRt.sizeDelta = new Vector2(560, 36);
            TextMeshProUGUI chTmp = ctrlHeadingObj.GetComponent<TextMeshProUGUI>();
            chTmp.text = "🎮 CONTROLS & KEYBINDINGS";
            chTmp.fontSize = 24;
            chTmp.fontStyle = FontStyles.Bold;
            chTmp.alignment = TextAlignmentOptions.Center;
            chTmp.color = new Color(1f, 0.84f, 0.2f);
            chTmp.raycastTarget = false;

            GameObject guideObj = new GameObject("ControlsGuide", typeof(RectTransform), typeof(TextMeshProUGUI));
            guideObj.transform.SetParent(panelObj.transform, false);
            RectTransform gRt = guideObj.GetComponent<RectTransform>();
            gRt.anchoredPosition = new Vector2(0, -35);
            gRt.sizeDelta = new Vector2(580, 170);

            TextMeshProUGUI guide = guideObj.GetComponent<TextMeshProUGUI>();
            guide.text = "<b>W / ↑ Arrow</b> : Accelerate Forward\n" +
                         "<b>S / ↓ Arrow</b> : Brake / Reverse\n" +
                         "<b>A / D / ← →</b> : Steer Left / Right\n" +
                         "<b>SPACEBAR</b> : Handbrake & Drift\n" +
                         "<b>R Key</b> : Respawn to Last Checkpoint\n" +
                         "<b>ESC Key</b> : Pause Game";
            guide.fontSize = 20;
            guide.alignment = TextAlignmentOptions.Left;
            guide.lineSpacing = 18;
            guide.color = new Color(0.85f, 0.9f, 0.95f);
            guide.raycastTarget = false;

            // --- CLOSE BUTTON ---
            Button closeBtn = CreateMenuButton(panelObj.transform, "CloseButton", "CLOSE", new Vector2(0, -215), new Color(0.15f, 0.65f, 0.35f, 1f));
            closeBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(280, 56);

            // Add SettingsUI component & wire
            CarRush.UI.SettingsUI settingsUI = panelObj.AddComponent<CarRush.UI.SettingsUI>();
            SerializedObject sSo = new SerializedObject(settingsUI);
            sSo.FindProperty("volumeSlider").objectReferenceValue = slider;
            sSo.FindProperty("volumeValueText").objectReferenceValue = vvTmp;
            sSo.FindProperty("closeButton").objectReferenceValue = closeBtn;
            sSo.ApplyModifiedProperties();

            return panelObj;
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
            {
                newScenes[i] = scenes[i];
            }
            newScenes[newScenes.Length - 1] = new EditorBuildSettingsScene(scenePath, true);
            EditorBuildSettings.scenes = newScenes;
        }
    }
}
#endif
