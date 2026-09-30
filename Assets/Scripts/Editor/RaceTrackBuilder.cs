#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using CarRush.Car;
using CarRush.EditorTools;
using CarRush.Game;
using CarRush.Level;
using CarRush.UI;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CarRush.Editor
{
    public static class RaceTrackBuilder
    {
        [MenuItem("CarRush/Build All 5 Race Levels & Register Scenes", false, 3)]
        public static void BuildAllLevels()
        {
            // Ensure LevelData assets & Car Prefab exist
            LevelDataGenerator.CreateLevelDataAssets();
            GameObject carPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CarPrefabBuilder.CarPrefabPath);
            if (carPrefab == null)
            {
                carPrefab = CarPrefabBuilder.CreatePlayerCarPrefab();
            }

            // Create materials
            Material roadMat = CreateMaterial("RoadAsphalt", new Color(0.18f, 0.18f, 0.20f), 0.2f);
            Material curbMat = CreateMaterial("TrackCurb", new Color(0.85f, 0.2f, 0.2f), 0.5f);
            Material barrierMat = CreateMaterial("BarrierMetal", new Color(0.35f, 0.4f, 0.45f), 0.7f);
            Material cpMat = CreateMaterial("CheckpointGlow", new Color(0.2f, 0.8f, 1f, 0.6f), 0.9f);
            Material finishMat = CreateMaterial("FinishArch", new Color(1f, 0.85f, 0.1f), 0.9f);

            // Generate tracks for Levels 1..5
            BuildLevelScene(1, "Level1", roadMat, curbMat, barrierMat, cpMat, finishMat, carPrefab);
            BuildLevelScene(2, "Level2", roadMat, curbMat, barrierMat, cpMat, finishMat, carPrefab);
            BuildLevelScene(3, "Level3", roadMat, curbMat, barrierMat, cpMat, finishMat, carPrefab);
            BuildLevelScene(4, "Level4", roadMat, curbMat, barrierMat, cpMat, finishMat, carPrefab);
            BuildLevelScene(5, "Level5", roadMat, curbMat, barrierMat, cpMat, finishMat, carPrefab);

            // Register all scenes in Build Settings
            RegisterAllScenesInBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("<color=green><b>[CarRush]</b> All 5 Level scenes successfully generated and registered in Build Settings!</color>");
            EditorUtility.DisplayDialog("CarRush Builder", "All 5 Race Levels and the Player Car have been built and registered!\n\nYou can now test the full game from MainMenu!", "Awesome!");
        }

        private static void BuildLevelScene(int levelNum, string sceneName, Material roadMat, Material curbMat, Material barrierMat, Material cpMat, Material finishMat, GameObject carPrefab)
        {
            string scenePath = $"Assets/Scenes/{sceneName}.unity";
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // Lighting & Sky setup
            Light dirLight = Object.FindAnyObjectByType<Light>();
            if (dirLight != null)
            {
                dirLight.color = new Color(1f, 0.95f, 0.88f);
                dirLight.intensity = 1.2f;
                dirLight.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            }

            // Level Data reference
            LevelData levelData = AssetDatabase.LoadAssetAtPath<LevelData>($"Assets/Settings/LevelData/Level{levelNum}.asset");

            // Track geometry points based on level difficulty
            List<Vector3> trackNodes = GenerateTrackNodes(levelNum);

            // Track root
            GameObject trackRoot = new GameObject("Track");
            List<Checkpoint> checkpoints = new List<Checkpoint>();

            // Build road segments
            for (int i = 0; i < trackNodes.Count - 1; i++)
            {
                Vector3 pA = trackNodes[i];
                Vector3 pB = trackNodes[i + 1];
                CreateRoadSegment(trackRoot.transform, pA, pB, roadMat, curbMat, barrierMat);
            }

            // Place Ad Billboards along the roadside
            AdBillboardBuilder.PlaceBillboardsInOpenScene();


            // Create Checkpoints
            int cpCount = levelData != null ? levelData.checkpointCount : 3;
            float step = (float)(trackNodes.Count - 2) / (cpCount + 1);
            for (int i = 1; i <= cpCount; i++)
            {
                int nodeIdx = Mathf.Clamp(Mathf.RoundToInt(i * step), 1, trackNodes.Count - 2);
                Vector3 cpPos = trackNodes[nodeIdx];
                Vector3 nextPos = trackNodes[nodeIdx + 1];
                Vector3 dir = (nextPos - cpPos).normalized;

                Checkpoint cp = CreateCheckpoint(trackRoot.transform, i - 1, cpPos, Quaternion.LookRotation(dir), cpMat);
                checkpoints.Add(cp);
            }

            // Finish Line placed at the start/finish arch of the circuit
            Vector3 startDir = (trackNodes[1] - trackNodes[0]).normalized;
            Quaternion startRot = Quaternion.LookRotation(startDir);

            Vector3 finishPos = trackNodes[0];
            FinishLine finishLine = CreateFinishLine(trackRoot.transform, finishPos, startRot, finishMat);

            // Spawn Player Car 8 meters ahead of the finish line arch facing forward
            Vector3 startPos = trackNodes[0] + startDir * 8f + Vector3.up * 0.5f;

            GameObject carInstance = (GameObject)PrefabUtility.InstantiatePrefab(carPrefab);
            carInstance.name = "PlayerCar";
            carInstance.transform.position = startPos;
            carInstance.transform.rotation = startRot;

            CarController carController = carInstance.GetComponent<CarController>();

            // Main Camera & Camera Follow
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                CarCameraFollow camFollow = mainCam.gameObject.AddComponent<CarCameraFollow>();
                camFollow.SetTarget(carInstance.transform, carInstance.GetComponent<Rigidbody>());
            }

            // Start Spawn Marker
            GameObject spawnMarker = new GameObject("StartSpawnPoint");
            spawnMarker.transform.position = startPos;
            spawnMarker.transform.rotation = startRot;

            // In-Game UI Canvas
            GameObject canvasObj = CreateInGameUI(levelData, out HUDUI hud, out PauseMenuUI pauseMenu, out LevelCompleteUI completeUI);

            // RaceManager
            GameObject raceManagerObj = new GameObject("RaceManager");
            RaceManager raceManager = raceManagerObj.AddComponent<RaceManager>();

            SerializedObject rmSo = new SerializedObject(raceManager);
            rmSo.FindProperty("levelData").objectReferenceValue = levelData;
            rmSo.FindProperty("playerCar").objectReferenceValue = carController;
            rmSo.FindProperty("startSpawnPoint").objectReferenceValue = spawnMarker.transform;
            rmSo.FindProperty("finishLine").objectReferenceValue = finishLine;

            SerializedProperty cpListProp = rmSo.FindProperty("checkpoints");
            cpListProp.arraySize = checkpoints.Count;
            for (int i = 0; i < checkpoints.Count; i++)
            {
                cpListProp.GetArrayElementAtIndex(i).objectReferenceValue = checkpoints[i];
            }
            rmSo.ApplyModifiedProperties();

            // Save scene
            EditorSceneManager.SaveScene(scene, scenePath);
        }

        private static List<Vector3> GenerateTrackNodes(int level)
        {
            List<Vector3> nodes = new List<Vector3>();
            int segments = 12 + level * 4;
            float radius = 70f + level * 20f;

            for (int i = 0; i < segments; i++)
            {
                float angle = (float)i / segments * Mathf.PI * 2f;
                // Add varied curviness per level
                float r = radius + Mathf.Sin(angle * (2 + level)) * (15f + level * 5f);
                float x = Mathf.Cos(angle) * r;
                float z = Mathf.Sin(angle) * r;
                float y = Mathf.Sin(angle * 3f) * (level > 2 ? 4f : 0f); // Hill elevation for higher levels

                nodes.Add(new Vector3(x, y, z));
            }
            // Close loop
            nodes.Add(nodes[0]);
            return nodes;
        }

        private static void CreateRoadSegment(Transform parent, Vector3 pA, Vector3 pB, Material roadMat, Material curbMat, Material barrierMat)
        {
            Vector3 dir = pB - pA;
            float length = dir.magnitude;
            Vector3 mid = (pA + pB) * 0.5f;
            Quaternion rot = Quaternion.LookRotation(dir.normalized);

            // Road Mesh
            GameObject road = GameObject.CreatePrimitive(PrimitiveType.Cube);
            road.name = "RoadSegment";
            road.transform.SetParent(parent, false);
            road.transform.position = mid;
            road.transform.rotation = rot;
            road.transform.localScale = new Vector3(12f, 0.4f, length);
            road.GetComponent<MeshRenderer>().material = roadMat;

            // Left Curb
            GameObject leftCurb = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftCurb.name = "LeftCurb";
            leftCurb.transform.SetParent(parent, false);
            leftCurb.transform.position = mid + rot * new Vector3(-6.3f, 0.3f, 0);
            leftCurb.transform.rotation = rot;
            leftCurb.transform.localScale = new Vector3(0.6f, 0.6f, length);
            leftCurb.GetComponent<MeshRenderer>().material = curbMat;

            // Right Curb
            GameObject rightCurb = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightCurb.name = "RightCurb";
            rightCurb.transform.SetParent(parent, false);
            rightCurb.transform.position = mid + rot * new Vector3(6.3f, 0.3f, 0);
            rightCurb.transform.rotation = rot;
            rightCurb.transform.localScale = new Vector3(0.6f, 0.6f, length);
            rightCurb.GetComponent<MeshRenderer>().material = curbMat;
        }

        private static Checkpoint CreateCheckpoint(Transform parent, int index, Vector3 pos, Quaternion rot, Material cpMat)
        {
            GameObject cpObj = new GameObject($"Checkpoint_{index}");
            cpObj.transform.SetParent(parent, false);
            cpObj.transform.position = pos + Vector3.up * 2f;
            cpObj.transform.rotation = rot;

            BoxCollider col = cpObj.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(14f, 6f, 2f);

            // Visual Gate Arches
            GameObject leftPost = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            leftPost.transform.SetParent(cpObj.transform, false);
            leftPost.transform.localPosition = new Vector3(-6.5f, 0, 0);
            leftPost.transform.localScale = new Vector3(0.4f, 3f, 0.4f);
            leftPost.GetComponent<MeshRenderer>().material = cpMat;
            Object.DestroyImmediate(leftPost.GetComponent<Collider>());

            GameObject rightPost = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rightPost.transform.SetParent(cpObj.transform, false);
            rightPost.transform.localPosition = new Vector3(6.5f, 0, 0);
            rightPost.transform.localScale = new Vector3(0.4f, 3f, 0.4f);
            rightPost.GetComponent<MeshRenderer>().material = cpMat;
            Object.DestroyImmediate(rightPost.GetComponent<Collider>());

            GameObject topBar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            topBar.transform.SetParent(cpObj.transform, false);
            topBar.transform.localPosition = new Vector3(0, 3f, 0);
            topBar.transform.localScale = new Vector3(13.4f, 0.4f, 0.4f);
            topBar.GetComponent<MeshRenderer>().material = cpMat;
            Object.DestroyImmediate(topBar.GetComponent<Collider>());

            Checkpoint cp = cpObj.AddComponent<Checkpoint>();
            cp.CheckpointIndex = index;
            return cp;
        }

        private static FinishLine CreateFinishLine(Transform parent, Vector3 pos, Quaternion rot, Material finishMat)
        {
            GameObject finishObj = new GameObject("FinishLine");
            finishObj.transform.SetParent(parent, false);
            finishObj.transform.position = pos + Vector3.up * 2f;
            finishObj.transform.rotation = rot;

            BoxCollider col = finishObj.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(14f, 6f, 2f);

            // Arch banner
            GameObject banner = GameObject.CreatePrimitive(PrimitiveType.Cube);
            banner.transform.SetParent(finishObj.transform, false);
            banner.transform.localPosition = new Vector3(0, 3.2f, 0);
            banner.transform.localScale = new Vector3(14f, 1.2f, 0.5f);
            banner.GetComponent<MeshRenderer>().material = finishMat;
            Object.DestroyImmediate(banner.GetComponent<Collider>());

            return finishObj.AddComponent<FinishLine>();
        }

        private static GameObject CreateInGameUI(LevelData levelData, out HUDUI hud, out PauseMenuUI pauseMenu, out LevelCompleteUI completeUI)
        {
            GameObject canvasObj = new GameObject("InGameCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            // EventSystem
            if (Object.FindAnyObjectByType<EventSystem>() == null)
            {
                GameObject es = new GameObject("EventSystem", typeof(EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
            }

            // HUD Elements
            hud = canvasObj.AddComponent<HUDUI>();

            // Speedometer (Bottom Right)
            GameObject speedObj = CreateTMPLabel(canvasObj.transform, "SpeedText", new Vector2(1, 0), new Vector2(1, 0), new Vector2(-160, 90), new Vector2(300, 100), "0 KM/H", 48, FontStyles.Bold, TextAlignmentOptions.Right, Color.white);
            GameObject timerObj = CreateTMPLabel(canvasObj.transform, "TimerText", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -70), new Vector2(400, 100), "TIME\n00:00.00", 38, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
            GameObject cpObj = CreateTMPLabel(canvasObj.transform, "CheckpointText", new Vector2(0, 1), new Vector2(0, 1), new Vector2(220, -60), new Vector2(400, 60), "CHECKPOINT 0 / 3", 28, FontStyles.Bold, TextAlignmentOptions.Left, new Color(1f, 0.84f, 0.2f));
            GameObject levelNameObj = CreateTMPLabel(canvasObj.transform, "LevelNameText", new Vector2(0, 1), new Vector2(0, 1), new Vector2(220, -110), new Vector2(400, 40), levelData != null ? levelData.levelName.ToUpper() : "TRAINING ROAD", 20, FontStyles.Normal, TextAlignmentOptions.Left, new Color(0.7f, 0.75f, 0.85f));
            GameObject countdownObj = CreateTMPLabel(canvasObj.transform, "CountdownText", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 60), new Vector2(500, 200), "3", 110, FontStyles.Bold, TextAlignmentOptions.Center, new Color(1f, 0.85f, 0.1f));

            SerializedObject hudSo = new SerializedObject(hud);
            hudSo.FindProperty("speedText").objectReferenceValue = speedObj.GetComponent<TextMeshProUGUI>();
            hudSo.FindProperty("timerText").objectReferenceValue = timerObj.GetComponent<TextMeshProUGUI>();
            hudSo.FindProperty("checkpointText").objectReferenceValue = cpObj.GetComponent<TextMeshProUGUI>();
            hudSo.FindProperty("levelNameText").objectReferenceValue = levelNameObj.GetComponent<TextMeshProUGUI>();
            hudSo.FindProperty("countdownText").objectReferenceValue = countdownObj.GetComponent<TextMeshProUGUI>();
            hudSo.ApplyModifiedProperties();

            // Pause Menu Panel
            pauseMenu = canvasObj.AddComponent<PauseMenuUI>();
            GameObject pausePanel = CreateMenuPanel(canvasObj.transform, "PausePanel", "PAUSED", new Vector2(500, 420));
            Button resumeBtn = CreateButton(pausePanel.transform, "ResumeBtn", "RESUME", new Vector2(0, 30), new Color(0.15f, 0.65f, 0.35f));
            Button restartBtn = CreateButton(pausePanel.transform, "RestartBtn", "RESTART", new Vector2(0, -45), new Color(0.2f, 0.4f, 0.7f));
            Button menuBtn = CreateButton(pausePanel.transform, "MainMenuBtn", "MAIN MENU", new Vector2(0, -120), new Color(0.6f, 0.2f, 0.2f));

            UnityEventTools.AddPersistentListener(resumeBtn.onClick, pauseMenu.OnResumePressed);
            UnityEventTools.AddPersistentListener(restartBtn.onClick, pauseMenu.OnRestartPressed);
            UnityEventTools.AddPersistentListener(menuBtn.onClick, pauseMenu.OnMainMenuPressed);

            SerializedObject pmSo = new SerializedObject(pauseMenu);
            pmSo.FindProperty("pausePanel").objectReferenceValue = pausePanel;
            pmSo.ApplyModifiedProperties();
            pausePanel.SetActive(false);

            // Level Complete Panel
            completeUI = canvasObj.AddComponent<LevelCompleteUI>();
            GameObject victoryPanel = CreateMenuPanel(canvasObj.transform, "VictoryPanel", "LEVEL FINISHED!", new Vector2(560, 480));
            GameObject timeResObj = CreateTMPLabel(victoryPanel.transform, "TimeResultText", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 50), new Vector2(450, 60), "TIME: 00:00.00", 32, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
            GameObject bestBadge = CreateTMPLabel(victoryPanel.transform, "BestBadge", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 10), new Vector2(450, 40), "★ NEW BEST TIME! ★", 24, FontStyles.Bold, TextAlignmentOptions.Center, new Color(1f, 0.85f, 0.1f));

            Button nextLvlBtn = CreateButton(victoryPanel.transform, "NextLevelBtn", "NEXT LEVEL", new Vector2(0, -50), new Color(0.15f, 0.65f, 0.35f));
            Button retryBtn = CreateButton(victoryPanel.transform, "RetryBtn", "RETRY", new Vector2(0, -120), new Color(0.2f, 0.4f, 0.7f));
            Button victoryMenuBtn = CreateButton(victoryPanel.transform, "MenuBtn", "LEVEL SELECT", new Vector2(0, -190), new Color(0.3f, 0.35f, 0.45f));

            UnityEventTools.AddPersistentListener(nextLvlBtn.onClick, completeUI.OnNextLevelPressed);
            UnityEventTools.AddPersistentListener(retryBtn.onClick, completeUI.OnRetryPressed);
            UnityEventTools.AddPersistentListener(victoryMenuBtn.onClick, completeUI.OnLevelSelectPressed);

            SerializedObject compSo = new SerializedObject(completeUI);
            compSo.FindProperty("victoryPanel").objectReferenceValue = victoryPanel;
            compSo.FindProperty("timeResultText").objectReferenceValue = timeResObj.GetComponent<TextMeshProUGUI>();
            compSo.FindProperty("newBestBadge").objectReferenceValue = bestBadge.GetComponent<TextMeshProUGUI>();
            compSo.ApplyModifiedProperties();
            victoryPanel.SetActive(false);

            return canvasObj;
        }

        private static GameObject CreateTMPLabel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size, string text, float sizeFont, FontStyles style, TextAlignmentOptions align, Color color)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            TextMeshProUGUI tmp = obj.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = sizeFont;
            tmp.fontStyle = style;
            tmp.alignment = align;
            tmp.color = color;
            tmp.raycastTarget = false;
            return obj;
        }

        private static GameObject CreateMenuPanel(Transform parent, string name, string title, Vector2 size)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            RectTransform rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = size;

            Image img = panel.GetComponent<Image>();
            img.color = new Color(0.08f, 0.1f, 0.15f, 0.96f);

            CreateTMPLabel(panel.transform, "Title", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -40), new Vector2(400, 60), title, 36, FontStyles.Bold, TextAlignmentOptions.Center, new Color(1f, 0.84f, 0.2f));
            return panel;
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 pos, Color color)
        {
            GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(parent, false);
            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(320, 58);

            Image img = btnObj.GetComponent<Image>();
            img.color = color;

            Button btn = btnObj.GetComponent<Button>();
            CreateTMPLabel(btnObj.transform, "Text", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, label, 26, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
            return btn;
        }

        private static Material CreateMaterial(string name, Color color, float smoothness)
        {
            if (!Directory.Exists("Assets/Art/Materials"))
            {
                if (!Directory.Exists("Assets/Art")) AssetDatabase.CreateFolder("Assets", "Art");
                AssetDatabase.CreateFolder("Assets/Art", "Materials");
            }
            string path = $"Assets/Art/Materials/{name}.mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;

            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = color;
            mat.SetFloat("_Smoothness", smoothness);
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        public static void RegisterAllScenesInBuildSettings()
        {
            string[] scenePaths = new string[]
            {
                "Assets/Scenes/MainMenu.unity",
                "Assets/Scenes/LevelSelect.unity",
                "Assets/Scenes/Level1.unity",
                "Assets/Scenes/Level2.unity",
                "Assets/Scenes/Level3.unity",
                "Assets/Scenes/Level4.unity",
                "Assets/Scenes/Level5.unity"
            };

            List<EditorBuildSettingsScene> buildScenes = new List<EditorBuildSettingsScene>();
            foreach (string p in scenePaths)
            {
                if (File.Exists(p))
                {
                    buildScenes.Add(new EditorBuildSettingsScene(p, true));
                }
            }

            EditorBuildSettings.scenes = buildScenes.ToArray();
            Debug.Log($"[CarRush] Registered {buildScenes.Count} scenes in Build Settings.");
        }
    }
}
#endif
