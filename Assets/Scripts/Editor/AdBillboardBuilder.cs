#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CarRush.Editor
{
    public static class AdBillboardBuilder
    {
        private const string AdFolder = "Assets/ad";
        private const string MaterialPath = "Assets/Art/Materials/AdBillboardMaterial.mat";
        private const string PrefabPath = "Assets/Prefabs/AdBillboard.prefab";

        public static Texture2D GetAdTexture()
        {
            if (!Directory.Exists(AdFolder)) return null;

            string[] validExtensions = new[] { ".png", ".jpg", ".jpeg", ".tga", ".bmp", ".webp" };
            string[] files = Directory.GetFiles(AdFolder, "*.*", SearchOption.TopDirectoryOnly);

            string newestImagePath = null;
            System.DateTime newestTime = System.DateTime.MinValue;

            foreach (string f in files)
            {
                string ext = Path.GetExtension(f).ToLowerInvariant();
                if (System.Array.IndexOf(validExtensions, ext) >= 0)
                {
                    System.DateTime writeTime = File.GetLastWriteTimeUtc(f);
                    if (writeTime > newestTime)
                    {
                        newestTime = writeTime;
                        newestImagePath = f.Replace("\\", "/");
                    }
                }
            }

            if (!string.IsNullOrEmpty(newestImagePath))
            {
                AssetDatabase.ImportAsset(newestImagePath, ImportAssetOptions.ForceUpdate);
                return AssetDatabase.LoadAssetAtPath<Texture2D>(newestImagePath);
            }

            return null;
        }

        public static Material GetOrCreateAdMaterial()
        {
            if (!Directory.Exists("Assets/Art/Materials"))
            {
                if (!Directory.Exists("Assets/Art")) AssetDatabase.CreateFolder("Assets", "Art");
                AssetDatabase.CreateFolder("Assets/Art", "Materials");
            }

            Material mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            Texture2D tex = GetAdTexture();

            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit")
                              ?? Shader.Find("Universal Render Pipeline/Lit")
                              ?? Shader.Find("Unlit/Texture")
                              ?? Shader.Find("Standard");

            if (mat == null)
            {
                mat = new Material(unlitShader);
                AssetDatabase.CreateAsset(mat, MaterialPath);
            }
            else
            {
                mat.shader = unlitShader;
            }

            mat.color = Color.white;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", Color.white);

            if (tex != null)
            {
                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
                if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
                mat.mainTexture = tex;
            }

            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();
            return mat;
        }

        public static GameObject CreateBillboardObject(Transform parent, Vector3 position, Quaternion rotation)
        {
            Material adMat = GetOrCreateAdMaterial();
            Material frameMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/BarrierMetal.mat");
            if (frameMat == null)
            {
                frameMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                frameMat.color = new Color(0.18f, 0.2f, 0.22f);
            }

            Texture2D tex = GetAdTexture();
            float aspect = (tex != null && tex.height > 0) ? (float)tex.width / tex.height : 1.77f;
            aspect = Mathf.Clamp(aspect, 0.8f, 2.4f);

            float boardHeight = 4.2f;
            float boardWidth = boardHeight * aspect;
            float boardThickness = 0.35f;
            float poleHeight = 5.5f;
            float poleRadius = 0.25f;
            float boardCenterY = 5.5f;

            GameObject root = new GameObject("AdBillboard");
            if (parent != null) root.transform.SetParent(parent, true);
            root.transform.position = position;
            root.transform.rotation = rotation;

            // 1. Left Pillar / Pole
            GameObject leftPole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            leftPole.name = "LeftPole";
            leftPole.transform.SetParent(root.transform, false);
            leftPole.transform.localPosition = new Vector3(-boardWidth * 0.38f, poleHeight * 0.5f, 0f);
            leftPole.transform.localScale = new Vector3(poleRadius * 2f, poleHeight * 0.5f, poleRadius * 2f);
            leftPole.GetComponent<MeshRenderer>().material = frameMat;

            // 2. Right Pillar / Pole
            GameObject rightPole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rightPole.name = "RightPole";
            rightPole.transform.SetParent(root.transform, false);
            rightPole.transform.localPosition = new Vector3(boardWidth * 0.38f, poleHeight * 0.5f, 0f);
            rightPole.transform.localScale = new Vector3(poleRadius * 2f, poleHeight * 0.5f, poleRadius * 2f);
            rightPole.GetComponent<MeshRenderer>().material = frameMat;

            // 3. Backing Frame
            GameObject frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frame.name = "BackingFrame";
            frame.transform.SetParent(root.transform, false);
            frame.transform.localPosition = new Vector3(0f, boardCenterY, 0f);
            frame.transform.localScale = new Vector3(boardWidth + 0.4f, boardHeight + 0.4f, boardThickness);
            frame.GetComponent<MeshRenderer>().material = frameMat;

            // 4. Front Ad Display Face (Quad facing towards -Z in local space)
            GameObject frontFace = GameObject.CreatePrimitive(PrimitiveType.Quad);
            frontFace.name = "AdFace_Front";
            frontFace.transform.SetParent(root.transform, false);
            frontFace.transform.localPosition = new Vector3(0f, boardCenterY, -(boardThickness * 0.5f + 0.04f));
            frontFace.transform.localRotation = Quaternion.identity; // Unity Quad front faces -Z by default
            frontFace.transform.localScale = new Vector3(boardWidth, boardHeight, 1f);
            frontFace.GetComponent<MeshRenderer>().material = adMat;
            Object.DestroyImmediate(frontFace.GetComponent<Collider>());

            // 5. Back Ad Display Face (Quad facing towards +Z in local space)
            GameObject backFace = GameObject.CreatePrimitive(PrimitiveType.Quad);
            backFace.name = "AdFace_Back";
            backFace.transform.SetParent(root.transform, false);
            backFace.transform.localPosition = new Vector3(0f, boardCenterY, (boardThickness * 0.5f + 0.04f));
            backFace.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            backFace.transform.localScale = new Vector3(boardWidth, boardHeight, 1f);
            backFace.GetComponent<MeshRenderer>().material = adMat;
            Object.DestroyImmediate(backFace.GetComponent<Collider>());

            // 6. Overhead Light Fixture
            GameObject lightBar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lightBar.name = "LightFixture";
            lightBar.transform.SetParent(root.transform, false);
            lightBar.transform.localPosition = new Vector3(0f, boardCenterY + boardHeight * 0.5f + 0.15f, 0f);
            lightBar.transform.localScale = new Vector3(boardWidth * 0.9f, 0.15f, boardThickness + 0.6f);
            lightBar.GetComponent<MeshRenderer>().material = frameMat;
            Object.DestroyImmediate(lightBar.GetComponent<Collider>());

            return root;
        }

        [MenuItem("CarRush/Place Ad Billboards in All Levels", false, 4)]
        public static void PlaceAdBillboardsInAllLevels()
        {
            int totalPlaced = 0;
            for (int i = 1; i <= 20; i++)
            {
                string scenePath = $"Assets/Scenes/Level{i}.unity";
                if (!File.Exists(scenePath)) continue;

                Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                int count = PlaceBillboardsInOpenScene();
                totalPlaced += count;

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"<color=green><b>[CarRush]</b> Successfully placed {totalPlaced} advertisement billboards across all 20 level tracks!</color>");
            EditorUtility.DisplayDialog("Ad Billboards Placed", $"Successfully added advertisement boards outside the road across all 20 levels!\n\nImage source: Assets/ad folder", "Great!");
        }

        [MenuItem("CarRush/Place Ad Billboard in Active Scene", false, 5)]
        public static int PlaceBillboardsInOpenScene()
        {
            GameObject trackObj = GameObject.Find("Track");
            if (trackObj == null)
            {
                Debug.LogWarning("[CarRush] No 'Track' object found in active scene.");
                return 0;
            }

            // Remove existing ad billboards first to avoid duplicates
            Transform existingAds = trackObj.transform.Find("AdBillboards");
            if (existingAds != null)
            {
                Object.DestroyImmediate(existingAds.gameObject);
            }

            GameObject adsGroup = new GameObject("AdBillboards");
            adsGroup.transform.SetParent(trackObj.transform, false);

            // Find road segments
            Transform[] allChildren = trackObj.GetComponentsInChildren<Transform>();
            System.Collections.Generic.List<Transform> roadSegments = new System.Collections.Generic.List<Transform>();
            foreach (Transform t in allChildren)
            {
                if (t.name == "RoadSegment")
                {
                    roadSegments.Add(t);
                }
            }

            if (roadSegments.Count == 0) return 0;

            // Place billboards at strategic intervals outside the road (e.g. every 4-6 segments)
            int placed = 0;
            int step = Mathf.Max(3, roadSegments.Count / 4);

            for (int i = 1; i < roadSegments.Count; i += step)
            {
                Transform seg = roadSegments[i];
                Vector3 segPos = seg.position;
                Vector3 forward = seg.forward;
                Vector3 right = seg.right;

                // Alternate between left side and right side outside the road (12-14 meters from track center)
                bool placeOnRight = (placed % 2 == 0);
                float sideOffset = placeOnRight ? 13.5f : -13.5f;

                Vector3 billboardPos = segPos + right * sideOffset;
                // Keep ground height
                billboardPos.y = segPos.y;

                // Angle slightly towards oncoming cars (facing opposite forward + slightly tilted towards road center)
                Vector3 faceDir = (-forward + (placeOnRight ? -right : right) * 0.35f).normalized;
                Quaternion billboardRot = Quaternion.LookRotation(faceDir, Vector3.up);

                CreateBillboardObject(adsGroup.transform, billboardPos, billboardRot);
                placed++;
            }

            return placed;
        }
    }
}
#endif
