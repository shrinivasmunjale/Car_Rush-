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
        [MenuItem("CarRush/Build Level 3 Map", false, 2)]
        public static void BuildLevel3Map()
        {
            LevelDataGenerator.CreateLevelDataAssets();
            GameObject carPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CarPrefabBuilder.CarPrefabPath);
            if (carPrefab == null)
            {
                carPrefab = CarPrefabBuilder.CreatePlayerCarPrefab();
            }

            Material roadMat = CreateMaterial("RoadAsphalt", new Color(0.18f, 0.18f, 0.20f), 0.2f);
            Material curbMat = CreateMaterial("TrackCurb", new Color(0.85f, 0.2f, 0.2f), 0.5f);
            Material barrierMat = CreateMaterial("BarrierMetal", new Color(0.35f, 0.4f, 0.45f), 0.7f);
            Material cpMat = CreateMaterial("CheckpointGlow", new Color(0.2f, 0.8f, 1f, 0.6f), 0.9f);
            Material finishMat = CreateMaterial("FinishArch", new Color(1f, 0.85f, 0.1f), 0.9f);

            BuildLevelScene(3, "Level3", roadMat, curbMat, barrierMat, cpMat, finishMat, carPrefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("<color=green><b>[CarRush]</b> Level 3 map successfully rebuilt with smooth elevation, generous triggers, and fixed complete message!</color>");
            EditorUtility.DisplayDialog("CarRush Builder", "Level 3 Mountain Road has been rebuilt successfully!\n\nHeight issues fixed & Level Complete message verified.", "Great!");
        }

        [MenuItem("CarRush/Build Level 5 Map", false, 2)]
        public static void BuildLevel5Map()
        {
            LevelDataGenerator.CreateLevelDataAssets();
            GameObject carPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CarPrefabBuilder.CarPrefabPath);
            if (carPrefab == null)
            {
                carPrefab = CarPrefabBuilder.CreatePlayerCarPrefab();
            }

            Material roadMat = CreateMaterial("RoadAsphalt", new Color(0.18f, 0.18f, 0.20f), 0.2f);
            Material curbMat = CreateMaterial("TrackCurb", new Color(0.85f, 0.2f, 0.2f), 0.5f);
            Material barrierMat = CreateMaterial("BarrierMetal", new Color(0.35f, 0.4f, 0.45f), 0.7f);
            Material cpMat = CreateMaterial("CheckpointGlow", new Color(0.2f, 0.8f, 1f, 0.6f), 0.9f);
            Material finishMat = CreateMaterial("FinishArch", new Color(1f, 0.85f, 0.1f), 0.9f);

            BuildLevelScene(5, "Level5", roadMat, curbMat, barrierMat, cpMat, finishMat, carPrefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("<color=green><b>[CarRush]</b> Level 5 Sunset Speedway successfully rebuilt!</color>");
            EditorUtility.DisplayDialog("CarRush Builder", "Level 5 (Sunset Speedway) has been built and updated successfully!", "Awesome!");
        }

        [MenuItem("CarRush/Build All 10 Race Levels & Register Scenes", false, 3)]
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

            // Generate tracks for Levels 1..10
            for (int i = 1; i <= 10; i++)
            {
                BuildLevelScene(i, $"Level{i}", roadMat, curbMat, barrierMat, cpMat, finishMat, carPrefab);
            }

            // Also build/update LevelSelect scene with 10 level buttons & reset button
            LevelSelectBuilder.BuildLevelSelectScene();

            // Register all scenes in Build Settings
            RegisterAllScenesInBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("<color=green><b>[CarRush]</b> All 10 Level scenes & obstacles successfully generated and registered in Build Settings!</color>");
            EditorUtility.DisplayDialog("CarRush Builder", "All 10 Race Levels with Road Obstacles, Advertisement Boards, and Level Select have been built and registered!\n\nYou can now test the full game!", "Awesome!");
        }

        private static void BuildLevelScene(int levelNum, string sceneName, Material roadMat, Material curbMat, Material barrierMat, Material cpMat, Material finishMat, GameObject carPrefab)
        {
            string scenePath = $"Assets/Scenes/{sceneName}.unity";
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // Atmosphere & Weather Conditions per Level
            Light dirLight = Object.FindAnyObjectByType<Light>();
            ApplyLevelWeatherAndAtmosphere(levelNum, dirLight);

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

            // Spawn Road Obstacles based on level difficulty
            SpawnTrackObstacles(trackRoot.transform, trackNodes, levelNum);

            // Place Ad Billboards along the roadside
            AdBillboardBuilder.PlaceBillboardsInOpenScene();

            // Create Checkpoints
            int cpCount = levelData != null ? levelData.checkpointCount : (3 + levelNum / 2);
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
            rmSo.FindProperty("fallThresholdY").floatValue = -40f;
            rmSo.FindProperty("maxVerticalDropBelowCheckpoint").floatValue = 50f;
            rmSo.FindProperty("upsideDownRespawnTime").floatValue = 2.5f;

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

        private static void ApplyLevelWeatherAndAtmosphere(int levelNum, Light dirLight)
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;

            switch (levelNum)
            {
                case 1: // Level 1 - Bright Sunny Midday (Training Road)
                    if (dirLight != null)
                    {
                        dirLight.color = new Color(1.0f, 0.98f, 0.92f);
                        dirLight.intensity = 1.3f;
                        dirLight.transform.rotation = Quaternion.Euler(55f, -30f, 0f);
                    }
                    RenderSettings.fogColor = new Color(0.72f, 0.85f, 0.98f);
                    RenderSettings.fogDensity = 0.0015f;
                    RenderSettings.ambientSkyColor = new Color(0.75f, 0.85f, 0.98f);
                    RenderSettings.ambientEquatorColor = new Color(0.6f, 0.7f, 0.8f);
                    RenderSettings.ambientGroundColor = new Color(0.35f, 0.4f, 0.35f);
                    break;

                case 2: // Level 2 - City Dawn (Cool Morning Golden Sunlight)
                    if (dirLight != null)
                    {
                        dirLight.color = new Color(1.0f, 0.88f, 0.75f);
                        dirLight.intensity = 1.25f;
                        dirLight.transform.rotation = Quaternion.Euler(28f, -45f, 0f);
                    }
                    RenderSettings.fogColor = new Color(0.80f, 0.82f, 0.92f);
                    RenderSettings.fogDensity = 0.0025f;
                    RenderSettings.ambientSkyColor = new Color(0.7f, 0.78f, 0.92f);
                    RenderSettings.ambientEquatorColor = new Color(0.65f, 0.65f, 0.75f);
                    RenderSettings.ambientGroundColor = new Color(0.3f, 0.35f, 0.4f);
                    break;

                case 3: // Level 3 - Mountain Mist (Moody Overcast Mountain Fog)
                    if (dirLight != null)
                    {
                        dirLight.color = new Color(0.85f, 0.90f, 0.98f);
                        dirLight.intensity = 1.05f;
                        dirLight.transform.rotation = Quaternion.Euler(60f, -20f, 0f);
                    }
                    RenderSettings.fogColor = new Color(0.76f, 0.82f, 0.88f);
                    RenderSettings.fogDensity = 0.0055f;
                    RenderSettings.ambientSkyColor = new Color(0.68f, 0.75f, 0.82f);
                    RenderSettings.ambientEquatorColor = new Color(0.55f, 0.6f, 0.68f);
                    RenderSettings.ambientGroundColor = new Color(0.28f, 0.32f, 0.35f);
                    break;

                case 4: // Level 4 - Desert Hazard (Warm Amber Heat Haze & Dust)
                    if (dirLight != null)
                    {
                        dirLight.color = new Color(1.0f, 0.82f, 0.55f);
                        dirLight.intensity = 1.35f;
                        dirLight.transform.rotation = Quaternion.Euler(45f, -60f, 0f);
                    }
                    RenderSettings.fogColor = new Color(0.92f, 0.78f, 0.58f);
                    RenderSettings.fogDensity = 0.004f;
                    RenderSettings.ambientSkyColor = new Color(0.95f, 0.82f, 0.6f);
                    RenderSettings.ambientEquatorColor = new Color(0.8f, 0.65f, 0.45f);
                    RenderSettings.ambientGroundColor = new Color(0.45f, 0.35f, 0.25f);
                    break;

                case 5: // Level 5 - Sunset Speedway (Vibrant Golden Twilight)
                    if (dirLight != null)
                    {
                        dirLight.color = new Color(1.0f, 0.62f, 0.32f);
                        dirLight.intensity = 1.4f;
                        dirLight.transform.rotation = Quaternion.Euler(20f, -55f, 0f);
                    }
                    RenderSettings.fogColor = new Color(0.92f, 0.52f, 0.35f);
                    RenderSettings.fogDensity = 0.0035f;
                    RenderSettings.ambientSkyColor = new Color(0.95f, 0.55f, 0.45f);
                    RenderSettings.ambientEquatorColor = new Color(0.65f, 0.4f, 0.55f);
                    RenderSettings.ambientGroundColor = new Color(0.3f, 0.2f, 0.25f);
                    break;

                case 6: // Level 6 - Scorched Canyon (Harsh High Sun Scorcher)
                    if (dirLight != null)
                    {
                        dirLight.color = new Color(1.0f, 0.75f, 0.45f);
                        dirLight.intensity = 1.45f;
                        dirLight.transform.rotation = Quaternion.Euler(70f, -40f, 0f);
                    }
                    RenderSettings.fogColor = new Color(0.88f, 0.65f, 0.45f);
                    RenderSettings.fogDensity = 0.005f;
                    RenderSettings.ambientSkyColor = new Color(0.9f, 0.7f, 0.5f);
                    RenderSettings.ambientEquatorColor = new Color(0.7f, 0.5f, 0.35f);
                    RenderSettings.ambientGroundColor = new Color(0.4f, 0.3f, 0.2f);
                    break;

                case 7: // Level 7 - Stormy Ridge (Tempest Rain Clouds & Heavy Storm Fog)
                    if (dirLight != null)
                    {
                        dirLight.color = new Color(0.55f, 0.65f, 0.78f);
                        dirLight.intensity = 0.8f;
                        dirLight.transform.rotation = Quaternion.Euler(40f, -80f, 0f);
                    }
                    RenderSettings.fogColor = new Color(0.38f, 0.45f, 0.55f);
                    RenderSettings.fogDensity = 0.009f;
                    RenderSettings.ambientSkyColor = new Color(0.45f, 0.52f, 0.65f);
                    RenderSettings.ambientEquatorColor = new Color(0.35f, 0.42f, 0.5f);
                    RenderSettings.ambientGroundColor = new Color(0.2f, 0.25f, 0.3f);
                    break;

                case 8: // Level 8 - Cyber Metropolis (Midnight Neon Blue Atmosphere)
                    if (dirLight != null)
                    {
                        dirLight.color = new Color(0.4f, 0.5f, 0.95f);
                        dirLight.intensity = 0.6f;
                        dirLight.transform.rotation = Quaternion.Euler(35f, -110f, 0f);
                    }
                    RenderSettings.fogColor = new Color(0.12f, 0.15f, 0.32f);
                    RenderSettings.fogDensity = 0.006f;
                    RenderSettings.ambientSkyColor = new Color(0.25f, 0.3f, 0.6f);
                    RenderSettings.ambientEquatorColor = new Color(0.18f, 0.15f, 0.35f);
                    RenderSettings.ambientGroundColor = new Color(0.1f, 0.1f, 0.2f);
                    break;

                case 9: // Level 9 - Inferno Circuit (Volcanic Ash & Crimson Ember Sky)
                    if (dirLight != null)
                    {
                        dirLight.color = new Color(1.0f, 0.35f, 0.18f);
                        dirLight.intensity = 1.15f;
                        dirLight.transform.rotation = Quaternion.Euler(30f, -40f, 0f);
                    }
                    RenderSettings.fogColor = new Color(0.48f, 0.22f, 0.15f);
                    RenderSettings.fogDensity = 0.0075f;
                    RenderSettings.ambientSkyColor = new Color(0.6f, 0.25f, 0.18f);
                    RenderSettings.ambientEquatorColor = new Color(0.35f, 0.18f, 0.15f);
                    RenderSettings.ambientGroundColor = new Color(0.2f, 0.1f, 0.1f);
                    break;

                case 10: // Level 10 - Grand Championship (Golden Dusk Arena)
                default:
                    if (dirLight != null)
                    {
                        dirLight.color = new Color(1.0f, 0.88f, 0.65f);
                        dirLight.intensity = 1.35f;
                        dirLight.transform.rotation = Quaternion.Euler(45f, -45f, 0f);
                    }
                    RenderSettings.fogColor = new Color(0.82f, 0.72f, 0.60f);
                    RenderSettings.fogDensity = 0.003f;
                    RenderSettings.ambientSkyColor = new Color(0.85f, 0.75f, 0.65f);
                    RenderSettings.ambientEquatorColor = new Color(0.65f, 0.55f, 0.45f);
                    RenderSettings.ambientGroundColor = new Color(0.3f, 0.25f, 0.2f);
                    break;
            }
        }

        private static void SpawnTrackObstacles(Transform parent, List<Vector3> trackNodes, int level)
        {
            if (level <= 1) return; // Level 1 is obstacle-free training

            GameObject obsRoot = new GameObject("Obstacles");
            obsRoot.transform.SetParent(parent, false);

            int totalNodes = trackNodes.Count - 1;
            // Space obstacles well apart along the circuit so player has clear reaction time
            int obstacleCount = Mathf.Min(3 + level, totalNodes / 4);
            int step = Mathf.Max(4, totalNodes / (obstacleCount + 1));

            int obstacleIndex = 0;
            for (int i = 4; i < totalNodes - 3; i += step)
            {
                Vector3 pA = trackNodes[i];
                Vector3 pB = trackNodes[i + 1];
                Vector3 fwd = (pB - pA).normalized;
                Vector3 right = Vector3.Cross(Vector3.up, fwd).normalized;
                Vector3 mid = (pA + pB) * 0.5f;
                Quaternion rot = Quaternion.LookRotation(fwd, Vector3.up);

                // Strictly ONE obstacle per position, alternating between Left (-2.8f) and Right (+2.8f)
                // Leaves 7+ meters of open, clear, unobstructed roadway on the opposite lane!
                bool isLeft = (obstacleIndex % 2 == 0);
                float sideOffset = isLeft ? -2.8f : 2.8f;
                obstacleIndex++;

                if (level == 2 || level == 3)
                {
                    // Alternating Traffic Cones and Tire Wall barriers
                    if (obstacleIndex % 2 == 0)
                    {
                        ObstacleBuilder.CreateTireWallBarrier(obsRoot.transform, mid, rot, sideOffset);
                    }
                    else
                    {
                        ObstacleBuilder.CreateTrafficCone(obsRoot.transform, mid + right * sideOffset);
                        ObstacleBuilder.CreateTrafficCone(obsRoot.transform, mid + right * (sideOffset + (isLeft ? -0.8f : 0.8f)));
                    }
                }
                else if (level == 4 || level == 5)
                {
                    // Alternating Solid Concrete Barriers and Hazard Barrels
                    if (obstacleIndex % 2 == 0)
                    {
                        ObstacleBuilder.CreateSolidConcreteBarrier(obsRoot.transform, mid, rot, sideOffset, 3.8f);
                    }
                    else
                    {
                        ObstacleBuilder.CreateHazardBarrel(obsRoot.transform, mid + right * sideOffset);
                    }
                }
                else if (level >= 6 && level <= 8)
                {
                    // Alternating Solid Concrete Barriers, RoadBlock Chicanes, and Solid Tire Walls
                    if (obstacleIndex % 3 == 0)
                    {
                        ObstacleBuilder.CreateSolidConcreteBarrier(obsRoot.transform, mid, rot, sideOffset, 4.0f);
                    }
                    else if (obstacleIndex % 3 == 1)
                    {
                        ObstacleBuilder.CreateRoadBlockChicane(obsRoot.transform, mid, rot, sideOffset);
                    }
                    else
                    {
                        ObstacleBuilder.CreateTireWallBarrier(obsRoot.transform, mid, rot, sideOffset);
                    }
                }
                else // Level 9 & 10 Extreme Master Obstacles (one obstacle per location, alternating sides)
                {
                    if (obstacleIndex % 3 == 0)
                    {
                        ObstacleBuilder.CreateSolidConcreteBarrier(obsRoot.transform, mid, rot, sideOffset, 4.2f);
                    }
                    else if (obstacleIndex % 3 == 1)
                    {
                        ObstacleBuilder.CreateRoadBlockChicane(obsRoot.transform, mid, rot, sideOffset);
                    }
                    else
                    {
                        ObstacleBuilder.CreateTireWallBarrier(obsRoot.transform, mid, rot, sideOffset);
                    }
                }
            }
        }

        private static List<Vector3> GenerateTrackNodes(int level)
        {
            List<Vector3> nodes = new List<Vector3>();
            // High segment count gives perfectly smooth, round road curvature without sharp polygonal corners
            int segments = 40 + level * 4;
            float radius = 70f + level * 9f;

            for (int i = 0; i < segments; i++)
            {
                float angle = (float)i / segments * Mathf.PI * 2f;
                float r = radius;
                float y = 0f;

                switch (level)
                {
                    case 1: // Training Road - Smooth Oval Circuit
                        r += Mathf.Sin(angle * 2f) * 12f;
                        y = 0f;
                        break;

                    case 2: // City Dawn - Gentle S-Curves & Straightaways
                        r += Mathf.Sin(angle * 2f) * 15f + Mathf.Cos(angle * 3f) * 8f;
                        y = 0f;
                        break;

                    case 3: // Mountain Mist - Mountain climbs and sweeping hairpins
                        r += Mathf.Sin(angle * 2f) * 18f + Mathf.Cos(angle * 3f) * 10f;
                        y = Mathf.Sin(angle * 2f) * 3.5f + Mathf.Cos(angle) * 2.0f + 3f;
                        break;

                    case 4: // Desert Hazard - Flowing desert chicanes
                        r += Mathf.Sin(angle * 3f) * 20f + Mathf.Cos(angle * 2f) * 12f;
                        y = Mathf.Sin(angle * 2f) * 2.5f + 1.5f;
                        break;

                    case 5: // Sunset Speedway - Banked High-Speed Tri-Oval Speedway
                        r += Mathf.Sin(angle * 3f) * 25f + Mathf.Cos(angle * 2f) * 14f;
                        y = Mathf.Sin(angle * 2f) * 4.0f + Mathf.Cos(angle) * 2.5f + 3.5f;
                        break;

                    case 6: // Scorched Canyon - Fast canyon sweepers and elevation drop
                        r += Mathf.Sin(angle * 3f) * 22f + Mathf.Cos(angle * 4f) * 10f;
                        y = Mathf.Sin(angle * 2f) * 3.5f + Mathf.Cos(angle * 2f) * 2.0f + 3f;
                        break;

                    case 7: // Stormy Ridge - Technical mountain ridge with wide sweeping turns
                        r += Mathf.Sin(angle * 3f) * 24f + Mathf.Cos(angle * 2f) * 14f;
                        y = Mathf.Sin(angle * 2f) * 4.5f + Mathf.Cos(angle) * 2.5f + 4f;
                        break;

                    case 8: // Cyber Metropolis - Grand Prix flowing chicane circuit
                        r += Mathf.Sin(angle * 3f) * 26f + Mathf.Cos(angle * 4f) * 12f;
                        y = Mathf.Sin(angle * 2f) * 3.0f + 2f;
                        break;

                    case 9: // Inferno Circuit - High intensity sweeping roller-coaster
                        r += Mathf.Sin(angle * 4f) * 26f + Mathf.Cos(angle * 2f) * 16f;
                        y = Mathf.Sin(angle * 2f) * 4.5f + Mathf.Cos(angle * 3f) * 2.0f + 4f;
                        break;

                    case 10: // Grand Championship - Ultimate flowing championship track
                    default:
                        r += Mathf.Sin(angle * 4f) * 28f + Mathf.Cos(angle * 2f) * 16f;
                        y = Mathf.Sin(angle * 2f) * 4.0f + Mathf.Cos(angle) * 3.0f + 3.5f;
                        break;
                }

                float x = Mathf.Cos(angle) * r;
                float z = Mathf.Sin(angle) * r;
                nodes.Add(new Vector3(x, y, z));
            }
            // Close loop seamlessly
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
            cpObj.transform.position = pos + Vector3.up * 2.5f;
            cpObj.transform.rotation = rot;

            BoxCollider col = cpObj.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(18f, 10f, 6f);

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
            finishObj.transform.position = pos + Vector3.up * 2.5f;
            finishObj.transform.rotation = rot;

            BoxCollider col = finishObj.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(18f, 10f, 6f);

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

            // Time Up Panel
            GameObject timeUpPanel = CreateMenuPanel(canvasObj.transform, "TimeUpPanel", "TIME EXPIRED!", new Vector2(520, 420));
            Button timeUpRetryBtn = CreateButton(timeUpPanel.transform, "RetryBtn", "RETRY", new Vector2(0, 0), new Color(0.2f, 0.4f, 0.7f));
            Button timeUpMenuBtn = CreateButton(timeUpPanel.transform, "MenuBtn", "LEVEL SELECT", new Vector2(0, -75), new Color(0.3f, 0.35f, 0.45f));

            UnityEventTools.AddPersistentListener(timeUpRetryBtn.onClick, completeUI.OnRetryPressed);
            UnityEventTools.AddPersistentListener(timeUpMenuBtn.onClick, completeUI.OnLevelSelectPressed);

            SerializedObject compSo = new SerializedObject(completeUI);
            compSo.FindProperty("victoryPanel").objectReferenceValue = victoryPanel;
            compSo.FindProperty("timeUpPanel").objectReferenceValue = timeUpPanel;
            compSo.FindProperty("timeResultText").objectReferenceValue = timeResObj.GetComponent<TextMeshProUGUI>();
            compSo.FindProperty("newBestBadge").objectReferenceValue = bestBadge.GetComponent<TextMeshProUGUI>();
            compSo.ApplyModifiedProperties();
            victoryPanel.SetActive(false);
            timeUpPanel.SetActive(false);

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
                "Assets/Scenes/Level5.unity",
                "Assets/Scenes/Level6.unity",
                "Assets/Scenes/Level7.unity",
                "Assets/Scenes/Level8.unity",
                "Assets/Scenes/Level9.unity",
                "Assets/Scenes/Level10.unity"
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
