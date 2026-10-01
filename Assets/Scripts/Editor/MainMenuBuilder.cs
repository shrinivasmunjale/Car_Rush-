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
        private const string MainImagePath = "Assets/image/mainImage.jpg";

        public static TMP_FontAsset GetStylishFont()
        {
            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        }

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

            TMP_FontAsset stylishFont = GetStylishFont();

            // 7. Fullscreen Background Image from Assets/image/mainImage.jpg
            GameObject bgObj = new GameObject("BackgroundPhoto", typeof(RectTransform), typeof(RawImage));
            bgObj.transform.SetParent(canvasObj.transform, false);
            RectTransform bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            RawImage rawImg = bgObj.GetComponent<RawImage>();

            Texture2D bgTex = AssetDatabase.LoadAssetAtPath<Texture2D>(MainImagePath);
            if (bgTex == null)
            {
                // Fallback check for any image in Assets/image
                if (Directory.Exists("Assets/image"))
                {
                    string[] imgs = Directory.GetFiles("Assets/image", "*.*", SearchOption.TopDirectoryOnly);
                    foreach (var imgFile in imgs)
                    {
                        if (imgFile.EndsWith(".jpg") || imgFile.EndsWith(".png") || imgFile.EndsWith(".jpeg"))
                        {
                            bgTex = AssetDatabase.LoadAssetAtPath<Texture2D>(imgFile.Replace("\\", "/"));
                            if (bgTex != null) break;
                        }
                    }
                }
            }

            if (bgTex != null)
            {
                rawImg.texture = bgTex;
                rawImg.color = Color.white;
            }
            else
            {
                rawImg.color = new Color(0.08f, 0.10f, 0.16f);
            }
            rawImg.raycastTarget = false;

            // 8. Cinematic Dark Vignette Overlay for UI readability & contrast
            GameObject overlayObj = new GameObject("VignetteOverlay", typeof(RectTransform), typeof(Image));
            overlayObj.transform.SetParent(canvasObj.transform, false);
            RectTransform overlayRect = overlayObj.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.sizeDelta = Vector2.zero;
            Image overlayImg = overlayObj.GetComponent<Image>();
            overlayImg.color = new Color(0.03f, 0.04f, 0.08f, 0.45f);
            overlayImg.raycastTarget = false;

            // 9. Create Title Panel & Header (TextMeshPro with stylish font)
            GameObject titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObj.transform.SetParent(canvasObj.transform, false);
            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 0.5f);
            titleRect.anchorMax = new Vector2(0.5f, 0.5f);
            titleRect.pivot = new Vector2(0.5f, 0.5f);
            titleRect.anchoredPosition = new Vector2(0, 240);
            titleRect.sizeDelta = new Vector2(1000, 140);

            TextMeshProUGUI titleText = titleObj.GetComponent<TextMeshProUGUI>();
            if (stylishFont != null) titleText.font = stylishFont;
            titleText.text = "CAR RUSH";
            titleText.fontSize = 100;
            titleText.fontStyle = FontStyles.Bold | FontStyles.Italic;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = new Color(1f, 0.82f, 0.15f, 1f); // Vibrant Gold
            titleText.characterSpacing = 8f;
            titleText.raycastTarget = false;

            // Subtitle Badge
            GameObject subTitleBadge = new GameObject("SubtitleBadge", typeof(RectTransform), typeof(Image));
            subTitleBadge.transform.SetParent(canvasObj.transform, false);
            RectTransform badgeRect = subTitleBadge.GetComponent<RectTransform>();
            badgeRect.anchorMin = new Vector2(0.5f, 0.5f);
            badgeRect.anchorMax = new Vector2(0.5f, 0.5f);
            badgeRect.pivot = new Vector2(0.5f, 0.5f);
            badgeRect.anchoredPosition = new Vector2(0, 160);
            badgeRect.sizeDelta = new Vector2(500, 38);
            Image badgeImg = subTitleBadge.GetComponent<Image>();
            badgeImg.color = new Color(0f, 0f, 0f, 0.55f);
            badgeImg.raycastTarget = false;

            GameObject subTitleObj = new GameObject("SubtitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
            subTitleObj.transform.SetParent(subTitleBadge.transform, false);
            RectTransform subTitleRect = subTitleObj.GetComponent<RectTransform>();
            subTitleRect.anchorMin = Vector2.zero;
            subTitleRect.anchorMax = Vector2.one;
            subTitleRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI subTitleText = subTitleObj.GetComponent<TextMeshProUGUI>();
            if (stylishFont != null) subTitleText.font = stylishFont;
            subTitleText.text = "ULTIMATE 3D RACING EXPERIENCE";
            subTitleText.fontSize = 20;
            subTitleText.fontStyle = FontStyles.Bold;
            subTitleText.alignment = TextAlignmentOptions.Center;
            subTitleText.color = new Color(0.85f, 0.90f, 1f, 0.95f);
            subTitleText.characterSpacing = 4f;
            subTitleText.raycastTarget = false;

            // 10. Create Menu Buttons with Stylish Fonts & Modern Accents
            Button playBtn = CreateMenuButton(canvasObj.transform, "PlayButton", "PLAY RACE", new Vector2(0, 35), new Color(0.12f, 0.68f, 0.38f, 0.95f), stylishFont);
            Button settingsBtn = CreateMenuButton(canvasObj.transform, "SettingsButton", "SETTINGS", new Vector2(0, -55), new Color(0.18f, 0.44f, 0.78f, 0.95f), stylishFont);
            Button exitBtn = CreateMenuButton(canvasObj.transform, "ExitButton", "EXIT", new Vector2(0, -145), new Color(0.78f, 0.20f, 0.22f, 0.95f), stylishFont);

            // 11. Create Settings Panel
            GameObject settingsPanelObj = CreateSettingsPanel(canvasObj.transform, stylishFont);
            settingsPanelObj.SetActive(false);

            // 12. Wire references to MainMenuUI
            SerializedObject serializedUI = new SerializedObject(mainMenuUI);
            SerializedProperty settingsProp = serializedUI.FindProperty("settingsPanel");
            if (settingsProp != null)
            {
                settingsProp.objectReferenceValue = settingsPanelObj;
                serializedUI.ApplyModifiedProperties();
            }

            // 13. Wire OnClick events using UnityEventTools
            UnityEventTools.AddPersistentListener(playBtn.onClick, mainMenuUI.OnPlayPressed);
            UnityEventTools.AddPersistentListener(settingsBtn.onClick, mainMenuUI.OnSettingsPressed);
            UnityEventTools.AddPersistentListener(exitBtn.onClick, mainMenuUI.OnExitPressed);

            // 14. Save scene & notify
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

            Debug.Log("<color=green><b>[CarRush]</b> MainMenu UI with custom background photo and stylish font configured successfully and saved!</color>");
        }

        private static Button CreateMenuButton(Transform parent, string name, string label, Vector2 pos, Color normalColor, TMP_FontAsset font)
        {
            GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(parent, false);

            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(380, 68);

            Image img = btnObj.GetComponent<Image>();
            img.color = normalColor;

            Button btn = btnObj.GetComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor = normalColor;
            cb.highlightedColor = normalColor * 1.25f;
            cb.pressedColor = normalColor * 0.75f;
            cb.selectedColor = normalColor;
            btn.colors = cb;

            GameObject textObj = new GameObject("Text (TMP)", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(btnObj.transform, false);

            RectTransform textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.sizeDelta = Vector2.zero;

            TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;
            tmp.text = label;
            tmp.fontSize = 32;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.characterSpacing = 3f;
            tmp.raycastTarget = false;

            return btn;
        }

        private static GameObject CreateSettingsPanel(Transform parent, TMP_FontAsset font)
        {
            GameObject panelObj = new GameObject("SettingsPanel", typeof(RectTransform), typeof(Image));
            panelObj.transform.SetParent(parent, false);

            RectTransform rt = panelObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(700, 560);

            Image img = panelObj.GetComponent<Image>();
            img.color = new Color(0.08f, 0.10f, 0.16f, 0.96f);

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
            if (font != null) title.font = font;
            title.text = "SETTINGS";
            title.fontSize = 40;
            title.fontStyle = FontStyles.Bold;
            title.alignment = TextAlignmentOptions.Center;
            title.color = new Color(1f, 0.82f, 0.15f);
            title.characterSpacing = 4f;
            title.raycastTarget = false;

            // --- AUDIO SECTION ---
            GameObject volLabelObj = new GameObject("VolLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
            volLabelObj.transform.SetParent(panelObj.transform, false);
            RectTransform vlRt = volLabelObj.GetComponent<RectTransform>();
            vlRt.anchoredPosition = new Vector2(-160, 160);
            vlRt.sizeDelta = new Vector2(250, 40);
            TextMeshProUGUI vlTmp = volLabelObj.GetComponent<TextMeshProUGUI>();
            if (font != null) vlTmp.font = font;
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
            if (font != null) vvTmp.font = font;
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
            if (font != null) chTmp.font = font;
            chTmp.text = "🎮 CONTROLS & KEYBINDINGS";
            chTmp.fontSize = 24;
            chTmp.fontStyle = FontStyles.Bold;
            chTmp.alignment = TextAlignmentOptions.Center;
            chTmp.color = new Color(1f, 0.82f, 0.15f);
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
            Button closeBtn = CreateMenuButton(panelObj.transform, "CloseButton", "CLOSE", new Vector2(0, -215), new Color(0.12f, 0.68f, 0.38f, 1f), font);
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
