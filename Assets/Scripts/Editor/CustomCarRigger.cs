#if UNITY_EDITOR
using System.IO;
using CarRush.Car;
using UnityEditor;
using UnityEngine;

namespace CarRush.Editor
{
    public static class CustomCarRigger
    {
        private const string FbxPath = "Assets/Art/Cars/car correct.fbx";
        private const string WheelFbxPath = "Assets/Art/wheel/XRim.fbx";
        private const string WheelAlbedoPath = "Assets/Art/wheel/FADE505ALBEDO1k.png";
        private const string WheelNormalPath = "Assets/Art/wheel/505NormalMap5.jpg";
        private const string WheelMetallicPath = "Assets/Art/wheel/505Metallic.png";
        private const string WheelRoughnessPath = "Assets/Art/wheel/505Roughness2.png";
        private const string WheelMaterialPath = "Assets/Art/Materials/WheelMaterial.mat";
        private const string OutputPrefabPath = "Assets/Prefabs/PlayerCar.prefab";

        [MenuItem("CarRush/🚗 Rig Imported Custom Car with XRim Wheels", false, 20)]
        public static void RigCustomCar()
        {
            RigCarWithRotation(new Vector3(-90f, 0f, 0f));
        }

        [MenuItem("CarRush/🛞 Replace Wheels with XRim Models", false, 21)]
        public static void ReplaceWheelsOnExistingPrefab()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(OutputPrefabPath);
            if (prefab == null)
            {
                Debug.LogWarning("[CustomCarRigger] PlayerCar.prefab not found. Rigging full car instead.");
                RigCustomCar();
                return;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            if (instance == null)
            {
                Debug.LogError("[CustomCarRigger] Failed to instantiate PlayerCar prefab.");
                return;
            }

            // Ensure material exists
            Material wheelMat = EnsureWheelMaterial();

            // Hide old static wheels from Model3D if present
            Transform model3D = instance.transform.Find("Model3D");
            if (model3D != null)
            {
                HideStaticCarWheels(model3D);
            }

            // Remove old Wheels transform
            Transform oldWheels = instance.transform.Find("Wheels");
            if (oldWheels != null)
            {
                Object.DestroyImmediate(oldWheels.gameObject);
            }

            // Create new Wheels hierarchy
            GameObject wheelsRoot = new GameObject("Wheels");
            wheelsRoot.transform.SetParent(instance.transform, false);

            WheelCollider flCol = CreateWheelWithXRim(wheelsRoot.transform, "FL_Wheel", new Vector3(-0.90f, 0.35f, 1.35f), true, wheelMat, out Transform flMesh);
            WheelCollider frCol = CreateWheelWithXRim(wheelsRoot.transform, "FR_Wheel", new Vector3(0.90f, 0.35f, 1.35f), false, wheelMat, out Transform frMesh);
            WheelCollider rlCol = CreateWheelWithXRim(wheelsRoot.transform, "RL_Wheel", new Vector3(-0.90f, 0.35f, -1.35f), true, wheelMat, out Transform rlMesh);
            WheelCollider rrCol = CreateWheelWithXRim(wheelsRoot.transform, "RR_Wheel", new Vector3(0.90f, 0.35f, -1.35f), false, wheelMat, out Transform rrMesh);

            // Re-wire CarController
            CarController controller = instance.GetComponent<CarController>();
            if (controller == null) controller = instance.AddComponent<CarController>();
            SerializedObject so = new SerializedObject(controller);
            SetWheelInfo(so, "frontLeftWheel", flCol, flMesh, true, true);
            SetWheelInfo(so, "frontRightWheel", frCol, frMesh, true, true);
            SetWheelInfo(so, "rearLeftWheel", rlCol, rlMesh, true, false);
            SetWheelInfo(so, "rearRightWheel", rrCol, rrMesh, true, false);
            so.ApplyModifiedProperties();

            PrefabUtility.SaveAsPrefabAsset(instance, OutputPrefabPath);
            Object.DestroyImmediate(instance);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("<color=green><b>[CustomCarRigger]</b> Perfectly scaled & centered XRim 3D wheels installed on PlayerCar.prefab!</color>");
            EditorUtility.DisplayDialog("Wheels Updated!",
                "Custom XRim 3D wheels scaled to standard car size (0.70m) and aligned!\n\n" +
                "✓ Scaled perfectly to fit WheelCollider radius\n" +
                "✓ Position centered at wheel axle\n" +
                "✓ Vertical alignment & left/right outward mirror applied\n" +
                "✓ Material & textures connected\n\n" +
                "Press Play (▶) to test!", "Awesome! 🏎️");
        }

        [MenuItem("CarRush/Car Orientation/Rotate Pitch (+90 X)", false, 50)]
        public static void RotatePitchPlus()
        {
            AdjustModelRotation(new Vector3(90f, 0f, 0f));
        }

        [MenuItem("CarRush/Car Orientation/Rotate Pitch (-90 X)", false, 51)]
        public static void RotatePitchMinus()
        {
            AdjustModelRotation(new Vector3(-90f, 0f, 0f));
        }

        [MenuItem("CarRush/Car Orientation/Rotate Yaw (180 Y Flip Front-Back)", false, 52)]
        public static void RotateYaw180()
        {
            AdjustModelRotation(new Vector3(0f, 180f, 0f));
        }

        [MenuItem("CarRush/🛞 Wheel Orientation/Rotate Roll (+90° Z)", false, 60)]
        public static void RotateWheelsRollPlus90()
        {
            AdjustWheelRotatorRotation(new Vector3(0f, 0f, 90f));
        }

        [MenuItem("CarRush/🛞 Wheel Orientation/Rotate Roll (-90° Z)", false, 61)]
        public static void RotateWheelsRollMinus90()
        {
            AdjustWheelRotatorRotation(new Vector3(0f, 0f, -90f));
        }

        [MenuItem("CarRush/🛞 Wheel Orientation/Rotate Pitch (+90° X)", false, 62)]
        public static void RotateWheelsPitchPlus90()
        {
            AdjustWheelRotatorRotation(new Vector3(90f, 0f, 0f));
        }

        [MenuItem("CarRush/🛞 Wheel Orientation/Rotate Yaw (+90° Y)", false, 63)]
        public static void RotateWheelsYawPlus90()
        {
            AdjustWheelRotatorRotation(new Vector3(0f, 90f, 0f));
        }

        [MenuItem("CarRush/🛞 Wheel Orientation/Flip Left-Right 180° (Yaw)", false, 64)]
        public static void FlipWheelsLeftRight()
        {
            AdjustWheelRotatorRotation(new Vector3(0f, 180f, 0f));
        }

        [MenuItem("CarRush/🛞 Wheel Height/Move Wheels Down (-0.05m)", false, 70)]
        public static void MoveWheelsDown()
        {
            AdjustWheelModelYOffset(-0.05f);
        }

        [MenuItem("CarRush/🛞 Wheel Height/Move Wheels Up (+0.05m)", false, 71)]
        public static void MoveWheelsUp()
        {
            AdjustWheelModelYOffset(0.05f);
        }

        [MenuItem("CarRush/🛞 Wheel Scale/Increase Size (+10%)", false, 80)]
        public static void ScaleWheelsUp()
        {
            AdjustWheelModelScale(1.10f);
        }

        [MenuItem("CarRush/🛞 Wheel Scale/Decrease Size (-10%)", false, 81)]
        public static void ScaleWheelsDown()
        {
            AdjustWheelModelScale(0.90f);
        }

        [MenuItem("CarRush/🛞 Wheel Track/Widen Wheels (+0.05m)", false, 90)]
        public static void WidenWheels()
        {
            AdjustWheelTrackOffset(0.05f);
        }

        [MenuItem("CarRush/🛞 Wheel Track/Narrow Wheels (-0.05m)", false, 91)]
        public static void NarrowWheels()
        {
            AdjustWheelTrackOffset(-0.05f);
        }

        public static void RigCarWithRotation(Vector3 modelEulerRotation)
        {
            GameObject fbxAsset = AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath);
            if (fbxAsset == null)
            {
                Debug.LogError("[CustomCarRigger] Could not find car FBX at: " + FbxPath);
                EditorUtility.DisplayDialog("Error", "Could not find 'car correct.fbx' in Assets/Art/Cars/", "OK");
                return;
            }

            // Create temporary instance in scene
            GameObject carRoot = new GameObject("PlayerCar");
            carRoot.tag = "Player";

            // Instantiate FBX model as child
            GameObject modelInstance = Object.Instantiate(fbxAsset, carRoot.transform);
            modelInstance.name = "Model3D";
            modelInstance.transform.localPosition = Vector3.zero;
            modelInstance.transform.localRotation = Quaternion.Euler(modelEulerRotation);
            modelInstance.transform.localScale = Vector3.one;

            // 1. Fix Materials for URP (Universal Render Pipeline)
            Shader urpLitShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Renderer[] renderers = modelInstance.GetComponentsInChildren<Renderer>(true);

            foreach (Renderer rend in renderers)
            {
                rend.enabled = true;
                Material[] mats = rend.sharedMaterials;
                for (int m = 0; m < mats.Length; m++)
                {
                    if (mats[m] == null || mats[m].shader == null || mats[m].shader.name.Contains("InternalErrorShader") || mats[m].shader.name.Contains("Standard"))
                    {
                        Material newMat = new Material(urpLitShader);
                        if (mats[m] != null)
                        {
                            if (mats[m].HasProperty("_Color")) newMat.color = mats[m].color;
                            if (mats[m].HasProperty("_MainTex") && mats[m].mainTexture != null) newMat.mainTexture = mats[m].mainTexture;
                        }
                        else
                        {
                            newMat.color = new Color(0.85f, 0.15f, 0.15f);
                        }
                        newMat.SetFloat("_Smoothness", 0.8f);
                        mats[m] = newMat;
                    }
                    else
                    {
                        if (mats[m].HasProperty("_Color") && mats[m].color.a < 0.1f)
                        {
                            Color c = mats[m].color;
                            c.a = 1f;
                            mats[m].color = c;
                        }
                    }
                }
                rend.sharedMaterials = mats;
            }

            // 2. Hide static car wheels embedded in car body model
            HideStaticCarWheels(modelInstance.transform);

            // 3. Normalize Scale and Center
            Bounds combinedBounds = new Bounds(Vector3.zero, Vector3.zero);
            bool hasBounds = false;
            foreach (Renderer r in renderers)
            {
                if (!r.enabled) continue;
                if (r is MeshRenderer || r is SkinnedMeshRenderer)
                {
                    if (!hasBounds)
                    {
                        combinedBounds = r.bounds;
                        hasBounds = true;
                    }
                    else
                    {
                        combinedBounds.Encapsulate(r.bounds);
                    }
                }
            }

            float maxDim = Mathf.Max(combinedBounds.size.x, combinedBounds.size.y, combinedBounds.size.z);
            if (maxDim > 0.001f)
            {
                if (maxDim > 10f || maxDim < 1.5f)
                {
                    float targetScale = 4.5f / maxDim;
                    modelInstance.transform.localScale = Vector3.one * targetScale;
                }
            }

            // Recompute bounds after scale
            combinedBounds = new Bounds(Vector3.zero, Vector3.zero);
            hasBounds = false;
            foreach (Renderer r in renderers)
            {
                if (!r.enabled) continue;
                if (r is MeshRenderer || r is SkinnedMeshRenderer)
                {
                    if (!hasBounds)
                    {
                        combinedBounds = r.bounds;
                        hasBounds = true;
                    }
                    else
                    {
                        combinedBounds.Encapsulate(r.bounds);
                    }
                }
            }

            // Center model
            modelInstance.transform.position = carRoot.transform.position - combinedBounds.center + Vector3.up * (combinedBounds.extents.y + 0.1f);

            // Rigidbody
            Rigidbody rb = carRoot.AddComponent<Rigidbody>();
            rb.mass = 1250f;
            rb.linearDamping = 0.06f;
            rb.angularDamping = 2.5f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            // Main Chassis BoxCollider
            BoxCollider col = carRoot.AddComponent<BoxCollider>();
            col.center = new Vector3(0, 0.65f, 0);
            col.size = new Vector3(1.8f, 0.75f, 4.0f);

            // Remove internal colliders from model
            Collider[] childCols = modelInstance.GetComponentsInChildren<Collider>();
            foreach (var c in childCols) Object.DestroyImmediate(c);

            // 4. Create Custom XRim Wheels
            Material wheelMat = EnsureWheelMaterial();

            GameObject wheelsRoot = new GameObject("Wheels");
            wheelsRoot.transform.SetParent(carRoot.transform, false);

            WheelCollider flCol = CreateWheelWithXRim(wheelsRoot.transform, "FL_Wheel", new Vector3(-0.90f, 0.35f, 1.35f), true, wheelMat, out Transform flMesh);
            WheelCollider frCol = CreateWheelWithXRim(wheelsRoot.transform, "FR_Wheel", new Vector3(0.90f, 0.35f, 1.35f), false, wheelMat, out Transform frMesh);
            WheelCollider rlCol = CreateWheelWithXRim(wheelsRoot.transform, "RL_Wheel", new Vector3(-0.90f, 0.35f, -1.35f), true, wheelMat, out Transform rlMesh);
            WheelCollider rrCol = CreateWheelWithXRim(wheelsRoot.transform, "RR_Wheel", new Vector3(0.90f, 0.35f, -1.35f), false, wheelMat, out Transform rrMesh);

            // Add CarController & Wire WheelInfo
            CarController controller = carRoot.AddComponent<CarController>();
            SerializedObject so = new SerializedObject(controller);
            SetWheelInfo(so, "frontLeftWheel", flCol, flMesh, true, true);
            SetWheelInfo(so, "frontRightWheel", frCol, frMesh, true, true);
            SetWheelInfo(so, "rearLeftWheel", rlCol, rlMesh, true, false);
            SetWheelInfo(so, "rearRightWheel", rrCol, rrMesh, true, false);
            so.ApplyModifiedProperties();

            // Add CarAudio Synthesizer
            carRoot.AddComponent<CarAudio>();

            // Save Prefab
            if (!Directory.Exists("Assets/Prefabs")) AssetDatabase.CreateFolder("Assets", "Prefabs");
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(carRoot, OutputPrefabPath);
            Object.DestroyImmediate(carRoot);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("<color=green><b>[CustomCarRigger]</b> Custom car successfully rigged with vertical XRim wheels and saved to: " + OutputPrefabPath + "</color>");
            EditorUtility.DisplayDialog("Custom Car Rigged!",
                "Your custom 3D car model has been re-oriented and rigged with custom XRim wheels!\n\n" +
                "✓ Car model centered flat on ground\n" +
                "✓ Custom XRim wheels scaled to 0.70m and positioned at wheel hub\n" +
                "✓ 4 WheelColliders aligned with proper left/right outward orientations\n" +
                "✓ CarController + Physics updated\n\n" +
                "Press Play (▶) to test!", "Awesome! 🏎️");
        }

        public static Material EnsureWheelMaterial()
        {
            if (!Directory.Exists("Assets/Art/Materials"))
            {
                if (!Directory.Exists("Assets/Art")) AssetDatabase.CreateFolder("Assets", "Art");
                AssetDatabase.CreateFolder("Assets/Art", "Materials");
            }

            // Ensure Normal Map texture is imported properly
            if (File.Exists(WheelNormalPath))
            {
                TextureImporter normalImporter = AssetImporter.GetAtPath(WheelNormalPath) as TextureImporter;
                if (normalImporter != null && normalImporter.textureType != TextureImporterType.NormalMap)
                {
                    normalImporter.textureType = TextureImporterType.NormalMap;
                    normalImporter.SaveAndReimport();
                }
            }

            Texture2D albedoTex = AssetDatabase.LoadAssetAtPath<Texture2D>(WheelAlbedoPath);
            Texture2D normalTex = AssetDatabase.LoadAssetAtPath<Texture2D>(WheelNormalPath);
            Texture2D metallicTex = AssetDatabase.LoadAssetAtPath<Texture2D>(WheelMetallicPath);

            Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material wheelMat = AssetDatabase.LoadAssetAtPath<Material>(WheelMaterialPath);

            if (wheelMat == null)
            {
                wheelMat = new Material(litShader);
                AssetDatabase.CreateAsset(wheelMat, WheelMaterialPath);
            }
            else
            {
                wheelMat.shader = litShader;
            }

            if (albedoTex != null)
            {
                wheelMat.SetTexture("_BaseMap", albedoTex);
                wheelMat.SetTexture("_MainTex", albedoTex);
            }
            if (normalTex != null)
            {
                wheelMat.SetTexture("_BumpMap", normalTex);
                wheelMat.EnableKeyword("_NORMALMAP");
            }
            if (metallicTex != null)
            {
                wheelMat.SetTexture("_MetallicGlossMap", metallicTex);
                wheelMat.EnableKeyword("_METALLICSPECGLOSSMAP");
            }

            wheelMat.SetFloat("_Metallic", 0.9f);
            wheelMat.SetFloat("_Smoothness", 0.82f);
            wheelMat.color = Color.white;

            EditorUtility.SetDirty(wheelMat);

            // Also update CarWheel.mat for consistency
            Material legacyCarWheelMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/CarWheel.mat");
            if (legacyCarWheelMat != null)
            {
                legacyCarWheelMat.shader = litShader;
                if (albedoTex != null)
                {
                    legacyCarWheelMat.SetTexture("_BaseMap", albedoTex);
                    legacyCarWheelMat.SetTexture("_MainTex", albedoTex);
                }
                if (normalTex != null)
                {
                    legacyCarWheelMat.SetTexture("_BumpMap", normalTex);
                    legacyCarWheelMat.EnableKeyword("_NORMALMAP");
                }
                if (metallicTex != null)
                {
                    legacyCarWheelMat.SetTexture("_MetallicGlossMap", metallicTex);
                }
                legacyCarWheelMat.SetFloat("_Metallic", 0.9f);
                legacyCarWheelMat.SetFloat("_Smoothness", 0.82f);
                legacyCarWheelMat.color = Color.white;
                EditorUtility.SetDirty(legacyCarWheelMat);
            }

            AssetDatabase.SaveAssets();
            return wheelMat;
        }

        public static WheelCollider CreateWheelWithXRim(Transform parent, string name, Vector3 localPos, bool isLeftWheel, Material mat, out Transform visualPivotTransform)
        {
            GameObject wheelObj = new GameObject(name);
            wheelObj.transform.SetParent(parent, false);
            wheelObj.transform.localPosition = localPos;

            WheelCollider wc = wheelObj.AddComponent<WheelCollider>();
            wc.radius = 0.35f;
            wc.suspensionDistance = 0.18f;
            wc.mass = 25f;

            JointSpring spring = wc.suspensionSpring;
            spring.spring = 36000f;
            spring.damper = 4500f;
            spring.targetPosition = 0.5f;
            wc.suspensionSpring = spring;

            WheelFrictionCurve forwardFriction = wc.forwardFriction;
            forwardFriction.stiffness = 1.9f;
            wc.forwardFriction = forwardFriction;

            WheelFrictionCurve sidewaysFriction = wc.sidewaysFriction;
            sidewaysFriction.stiffness = 1.7f;
            wc.sidewaysFriction = sidewaysFriction;

            // Visual Pivot: Position & rotation driven each frame by CarController.UpdateWheelMesh
            GameObject pivotObj = new GameObject("VisualPivot");
            pivotObj.transform.SetParent(wheelObj.transform, false);
            pivotObj.transform.localPosition = Vector3.zero;
            pivotObj.transform.localRotation = Quaternion.identity;
            pivotObj.transform.localScale = Vector3.one;
            visualPivotTransform = pivotObj.transform;

            // Load XRim model
            GameObject xRimAsset = AssetDatabase.LoadAssetAtPath<GameObject>(WheelFbxPath);
            if (xRimAsset != null)
            {
                // Intermediate Rotator node for orientation
                GameObject rotatorObj = new GameObject("WheelRotator");
                rotatorObj.transform.SetParent(pivotObj.transform, false);
                rotatorObj.transform.localPosition = Vector3.zero;
                rotatorObj.transform.localRotation = Quaternion.identity;
                rotatorObj.transform.localScale = Vector3.one;

                // Instantiate raw FBX under rotator
                GameObject wheelModel = Object.Instantiate(xRimAsset, rotatorObj.transform);
                wheelModel.name = "XRimMesh";
                wheelModel.transform.localPosition = Vector3.zero;
                wheelModel.transform.localRotation = Quaternion.identity;
                wheelModel.transform.localScale = Vector3.one;

                // Remove colliders/cameras if any in FBX
                Collider[] cols = wheelModel.GetComponentsInChildren<Collider>(true);
                foreach (var c in cols) Object.DestroyImmediate(c);

                // Apply Wheel Material
                Renderer[] rends = wheelModel.GetComponentsInChildren<Renderer>(true);
                foreach (var r in rends)
                {
                    r.material = mat;
                }

                // Compute exact composite bounding box of vertices in rotator space
                Bounds localBounds = new Bounds(Vector3.zero, Vector3.zero);
                bool hasBounds = false;

                MeshFilter[] allMfs = wheelModel.GetComponentsInChildren<MeshFilter>(true);
                foreach (var mf in allMfs)
                {
                    if (mf.sharedMesh == null) continue;
                    Matrix4x4 localMat = rotatorObj.transform.worldToLocalMatrix * mf.transform.localToWorldMatrix;
                    Vector3[] vertices = mf.sharedMesh.vertices;
                    int step = Mathf.Max(1, vertices.Length / 120);
                    for (int v = 0; v < vertices.Length; v += step)
                    {
                        Vector3 p = localMat.MultiplyPoint3x4(vertices[v]);
                        if (!hasBounds)
                        {
                            localBounds = new Bounds(p, Vector3.zero);
                            hasBounds = true;
                        }
                        else
                        {
                            localBounds.Encapsulate(p);
                        }
                    }
                }

                float sizeX = localBounds.size.x;
                float sizeY = localBounds.size.y;
                float sizeZ = localBounds.size.z;

                // Target wheel diameter is 0.70m (matching 0.35m radius)
                float currentDiameter = Mathf.Max(sizeX, sizeY, sizeZ);
                float targetDiameter = 0.70f;
                float scaleFactor = (currentDiameter > 0.0001f) ? (targetDiameter / currentDiameter) : 1.0f;

                // Center and scale the raw mesh relative to rotator origin
                wheelModel.transform.localScale = Vector3.one * scaleFactor;
                wheelModel.transform.localPosition = -localBounds.center * scaleFactor;

                // Set rotator orientation so circular rim stands vertically and faces outwards:
                Vector3 baseRotation;
                if (sizeY <= sizeX && sizeY <= sizeZ)
                {
                    // Normal is Y (flat on floor): Rotate Z by 90 to face -X (Left) or -90 to face +X (Right)
                    baseRotation = isLeftWheel ? new Vector3(0f, 0f, 90f) : new Vector3(0f, 180f, 90f);
                }
                else if (sizeZ <= sizeX && sizeZ <= sizeY)
                {
                    // Normal is Z: Rotate Y by -90 for Left or +90 for Right
                    baseRotation = isLeftWheel ? new Vector3(0f, -90f, 0f) : new Vector3(0f, 90f, 0f);
                }
                else
                {
                    // Normal is X:
                    baseRotation = isLeftWheel ? new Vector3(0f, 0f, 0f) : new Vector3(0f, 180f, 0f);
                }

                rotatorObj.transform.localRotation = Quaternion.Euler(baseRotation);
            }
            else
            {
                // Fallback procedural cylinder wheel
                GameObject fallback = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                fallback.name = "FallbackWheelMesh";
                fallback.transform.SetParent(pivotObj.transform, false);
                fallback.transform.localRotation = Quaternion.Euler(0, 0, 90f);
                fallback.transform.localScale = new Vector3(0.7f, 0.22f, 0.7f);
                fallback.GetComponent<MeshRenderer>().material = mat;
                Object.DestroyImmediate(fallback.GetComponent<Collider>());
            }

            return wc;
        }

        private static void HideStaticCarWheels(Transform modelRoot)
        {
            if (modelRoot == null) return;

            Renderer[] allRends = modelRoot.GetComponentsInChildren<Renderer>(true);
            foreach (var r in allRends)
            {
                string n = r.gameObject.name.ToLower();
                if (n.Contains("rim") || n.Contains("tire") || n.Contains("wheel") || n.Contains("brake"))
                {
                    r.enabled = false;
                    r.gameObject.SetActive(false);
                }
            }
        }

        private static void AdjustModelRotation(Vector3 additionalEuler)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(OutputPrefabPath);
            if (prefab == null)
            {
                RigCustomCar();
                return;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Transform model = instance.transform.Find("Model3D");
            if (model != null)
            {
                model.Rotate(additionalEuler, Space.Self);
                PrefabUtility.SaveAsPrefabAsset(instance, OutputPrefabPath);
                Debug.Log($"[CustomCarRigger] Adjusted model rotation by {additionalEuler}.");
            }
            Object.DestroyImmediate(instance);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void AdjustWheelRotatorRotation(Vector3 additionalEuler)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(OutputPrefabPath);
            if (prefab == null) return;

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Transform wheels = instance.transform.Find("Wheels");
            if (wheels != null)
            {
                for (int i = 0; i < wheels.childCount; i++)
                {
                    Transform w = wheels.GetChild(i);
                    Transform pivot = w.Find("VisualPivot");
                    if (pivot != null)
                    {
                        Transform rotator = pivot.Find("WheelRotator") ?? (pivot.childCount > 0 ? pivot.GetChild(0) : null);
                        if (rotator != null)
                        {
                            rotator.Rotate(additionalEuler, Space.Self);
                        }
                    }
                }
                PrefabUtility.SaveAsPrefabAsset(instance, OutputPrefabPath);
                Debug.Log($"[CustomCarRigger] Rotated wheel models by {additionalEuler}.");
            }
            Object.DestroyImmediate(instance);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void AdjustWheelModelYOffset(float deltaY)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(OutputPrefabPath);
            if (prefab == null) return;

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Transform wheels = instance.transform.Find("Wheels");
            if (wheels != null)
            {
                for (int i = 0; i < wheels.childCount; i++)
                {
                    Transform w = wheels.GetChild(i);
                    Transform pivot = w.Find("VisualPivot");
                    if (pivot != null)
                    {
                        Transform rotator = pivot.Find("WheelRotator") ?? (pivot.childCount > 0 ? pivot.GetChild(0) : null);
                        if (rotator != null)
                        {
                            Vector3 pos = rotator.localPosition;
                            pos.y += deltaY;
                            rotator.localPosition = pos;
                        }
                    }
                }
                PrefabUtility.SaveAsPrefabAsset(instance, OutputPrefabPath);
                Debug.Log($"[CustomCarRigger] Adjusted wheel Y height offset by {deltaY:F2}m.");
            }
            Object.DestroyImmediate(instance);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void AdjustWheelModelScale(float scaleFactor)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(OutputPrefabPath);
            if (prefab == null) return;

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Transform wheels = instance.transform.Find("Wheels");
            if (wheels != null)
            {
                for (int i = 0; i < wheels.childCount; i++)
                {
                    Transform w = wheels.GetChild(i);
                    Transform pivot = w.Find("VisualPivot");
                    if (pivot != null)
                    {
                        Transform rotator = pivot.Find("WheelRotator") ?? (pivot.childCount > 0 ? pivot.GetChild(0) : null);
                        if (rotator != null)
                        {
                            rotator.localScale *= scaleFactor;
                        }
                    }
                }
                PrefabUtility.SaveAsPrefabAsset(instance, OutputPrefabPath);
                Debug.Log($"[CustomCarRigger] Scaled wheel models by factor {scaleFactor}.");
            }
            Object.DestroyImmediate(instance);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void AdjustWheelTrackOffset(float deltaX)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(OutputPrefabPath);
            if (prefab == null) return;

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Transform wheels = instance.transform.Find("Wheels");
            if (wheels != null)
            {
                for (int i = 0; i < wheels.childCount; i++)
                {
                    Transform w = wheels.GetChild(i);
                    Vector3 pos = w.localPosition;
                    if (pos.x > 0) pos.x += deltaX;
                    else if (pos.x < 0) pos.x -= deltaX;
                    w.localPosition = pos;
                }
                PrefabUtility.SaveAsPrefabAsset(instance, OutputPrefabPath);
                Debug.Log($"[CustomCarRigger] Adjusted wheel track width by {deltaX * 2}m.");
            }
            Object.DestroyImmediate(instance);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void SetWheelInfo(SerializedObject so, string propertyName, WheelCollider collider, Transform mesh, bool isMotor, bool isSteer)
        {
            SerializedProperty prop = so.FindProperty(propertyName);
            if (prop != null)
            {
                prop.FindPropertyRelative("collider").objectReferenceValue = collider;
                prop.FindPropertyRelative("visualMesh").objectReferenceValue = mesh;
                prop.FindPropertyRelative("isMotor").boolValue = isMotor;
                prop.FindPropertyRelative("isSteer").boolValue = isSteer;
            }
        }
    }
}
#endif
