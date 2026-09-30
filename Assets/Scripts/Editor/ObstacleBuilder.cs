#if UNITY_EDITOR
using System.IO;
using CarRush.Game;
using UnityEditor;
using UnityEngine;

namespace CarRush.Editor
{
    public static class ObstacleBuilder
    {
        private static Material coneOrangeMat;
        private static Material coneWhiteMat;
        private static Material barrelYellowMat;
        private static Material barrelBlackMat;
        private static Material barrierMat;

        private static void EnsureMaterials()
        {
            if (!Directory.Exists("Assets/Art/Materials"))
            {
                if (!Directory.Exists("Assets/Art")) AssetDatabase.CreateFolder("Assets", "Art");
                AssetDatabase.CreateFolder("Assets/Art", "Materials");
            }

            coneOrangeMat = GetOrCreateMaterial("ConeOrange", new Color(1f, 0.42f, 0.05f), 0.2f);
            coneWhiteMat = GetOrCreateMaterial("ConeWhite", new Color(0.95f, 0.95f, 0.95f), 0.2f);
            barrelYellowMat = GetOrCreateMaterial("BarrelYellow", new Color(0.95f, 0.8f, 0.05f), 0.3f);
            barrelBlackMat = GetOrCreateMaterial("BarrelBlack", new Color(0.12f, 0.12f, 0.14f), 0.3f);
            barrierMat = GetOrCreateMaterial("HazardBarrier", new Color(0.85f, 0.2f, 0.15f), 0.4f);
        }

        private static Material GetOrCreateMaterial(string name, Color color, float smoothness)
        {
            string path = $"Assets/Art/Materials/{name}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                mat = new Material(shader);
                mat.color = color;
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
                mat.SetFloat("_Smoothness", smoothness);
                AssetDatabase.CreateAsset(mat, path);
            }
            return mat;
        }

        public static GameObject CreateTrafficCone(Transform parent, Vector3 pos)
        {
            EnsureMaterials();
            GameObject cone = new GameObject("TrafficCone");
            if (parent != null) cone.transform.SetParent(parent, true);
            cone.transform.position = pos + Vector3.up * 0.1f;

            // Base square
            GameObject baseObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseObj.name = "ConeBase";
            baseObj.transform.SetParent(cone.transform, false);
            baseObj.transform.localPosition = new Vector3(0, 0.04f, 0);
            baseObj.transform.localScale = new Vector3(0.7f, 0.08f, 0.7f);
            baseObj.GetComponent<MeshRenderer>().material = coneOrangeMat;
            Object.DestroyImmediate(baseObj.GetComponent<Collider>());

            // Main Cone body (Cylinder tapered or stacked)
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            body.name = "ConeBody";
            body.transform.SetParent(cone.transform, false);
            body.transform.localPosition = new Vector3(0, 0.45f, 0);
            body.transform.localScale = new Vector3(0.4f, 0.45f, 0.4f);
            body.GetComponent<MeshRenderer>().material = coneOrangeMat;
            Object.DestroyImmediate(body.GetComponent<Collider>());

            // Reflective White stripe
            GameObject stripe = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stripe.name = "WhiteStripe";
            stripe.transform.SetParent(cone.transform, false);
            stripe.transform.localPosition = new Vector3(0, 0.48f, 0);
            stripe.transform.localScale = new Vector3(0.42f, 0.12f, 0.42f);
            stripe.GetComponent<MeshRenderer>().material = coneWhiteMat;
            Object.DestroyImmediate(stripe.GetComponent<Collider>());

            // Physics Collider & Rigidbody
            BoxCollider col = cone.AddComponent<BoxCollider>();
            col.center = new Vector3(0, 0.45f, 0);
            col.size = new Vector3(0.7f, 0.9f, 0.7f);

            Rigidbody rb = cone.AddComponent<Rigidbody>();
            rb.mass = 8f;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            cone.AddComponent<ObstacleHazard>();
            return cone;
        }

        public static GameObject CreateHazardBarrel(Transform parent, Vector3 pos)
        {
            EnsureMaterials();
            GameObject barrel = new GameObject("HazardBarrel");
            if (parent != null) barrel.transform.SetParent(parent, true);
            barrel.transform.position = pos + Vector3.up * 0.1f;

            // Yellow main drum body
            GameObject drum = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            drum.name = "DrumBody";
            drum.transform.SetParent(barrel.transform, false);
            drum.transform.localPosition = new Vector3(0, 0.65f, 0);
            drum.transform.localScale = new Vector3(0.85f, 0.65f, 0.85f);
            drum.GetComponent<MeshRenderer>().material = barrelYellowMat;
            Object.DestroyImmediate(drum.GetComponent<Collider>());

            // Black stripes
            GameObject stripe1 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stripe1.name = "StripeTop";
            stripe1.transform.SetParent(barrel.transform, false);
            stripe1.transform.localPosition = new Vector3(0, 0.9f, 0);
            stripe1.transform.localScale = new Vector3(0.88f, 0.12f, 0.88f);
            stripe1.GetComponent<MeshRenderer>().material = barrelBlackMat;
            Object.DestroyImmediate(stripe1.GetComponent<Collider>());

            GameObject stripe2 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stripe2.name = "StripeBottom";
            stripe2.transform.SetParent(barrel.transform, false);
            stripe2.transform.localPosition = new Vector3(0, 0.4f, 0);
            stripe2.transform.localScale = new Vector3(0.88f, 0.12f, 0.88f);
            stripe2.GetComponent<MeshRenderer>().material = barrelBlackMat;
            Object.DestroyImmediate(stripe2.GetComponent<Collider>());

            // Physics
            CapsuleCollider col = barrel.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0, 0.65f, 0);
            col.radius = 0.45f;
            col.height = 1.3f;

            Rigidbody rb = barrel.AddComponent<Rigidbody>();
            rb.mass = 40f;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            ObstacleHazard haz = barrel.AddComponent<ObstacleHazard>();
            SerializedObject so = new SerializedObject(haz);
            so.FindProperty("obstacleType").enumValueIndex = (int)ObstacleType.HazardBarrel;
            so.FindProperty("speedPenaltyFraction").floatValue = 0.25f;
            so.ApplyModifiedProperties();

            return barrel;
        }

        public static GameObject CreateSolidConcreteBarrier(Transform parent, Vector3 pos, Quaternion rot, float laneOffset, float length = 4.5f)
        {
            EnsureMaterials();
            GameObject barrier = new GameObject("SolidConcreteBarrier");
            if (parent != null) barrier.transform.SetParent(parent, true);
            barrier.transform.position = pos + rot * new Vector3(laneOffset, 0.6f, 0);
            barrier.transform.rotation = rot;

            // Concrete body
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = "ConcreteWall";
            block.transform.SetParent(barrier.transform, false);
            block.transform.localPosition = Vector3.zero;
            block.transform.localScale = new Vector3(length, 1.2f, 0.9f);
            block.GetComponent<MeshRenderer>().material = barrierMat;
            Object.DestroyImmediate(block.GetComponent<Collider>());

            // Yellow hazard caution top
            GameObject topHazard = GameObject.CreatePrimitive(PrimitiveType.Cube);
            topHazard.name = "HazardCautionStripe";
            topHazard.transform.SetParent(barrier.transform, false);
            topHazard.transform.localPosition = new Vector3(0, 0.65f, 0);
            topHazard.transform.localScale = new Vector3(length + 0.1f, 0.25f, 0.95f);
            topHazard.GetComponent<MeshRenderer>().material = barrelYellowMat;
            Object.DestroyImmediate(topHazard.GetComponent<Collider>());

            // Solid impenetrable box collider
            BoxCollider col = barrier.AddComponent<BoxCollider>();
            col.size = new Vector3(length, 1.5f, 1.0f);

            Rigidbody rb = barrier.AddComponent<Rigidbody>();
            rb.isKinematic = true; // Solid stationary barrier (car cannot pass through)

            ObstacleHazard haz = barrier.AddComponent<ObstacleHazard>();
            SerializedObject so = new SerializedObject(haz);
            so.FindProperty("obstacleType").enumValueIndex = (int)ObstacleType.RoadBlockBarrier;
            so.FindProperty("speedPenaltyFraction").floatValue = 0.5f;
            so.ApplyModifiedProperties();

            return barrier;
        }

        public static GameObject CreateTireWallBarrier(Transform parent, Vector3 pos, Quaternion rot, float laneOffset)
        {
            EnsureMaterials();
            GameObject tireWall = new GameObject("SolidTireWall");
            if (parent != null) tireWall.transform.SetParent(parent, true);
            tireWall.transform.position = pos + rot * new Vector3(laneOffset, 0.5f, 0);
            tireWall.transform.rotation = rot;

            // 3 Stacked tire cylinders
            for (int i = -1; i <= 1; i++)
            {
                GameObject tire = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                tire.name = $"TireStack_{i + 1}";
                tire.transform.SetParent(tireWall.transform, false);
                tire.transform.localPosition = new Vector3(i * 1.0f, 0, 0);
                tire.transform.localScale = new Vector3(0.9f, 0.5f, 0.9f);
                tire.GetComponent<MeshRenderer>().material = barrelBlackMat;
                Object.DestroyImmediate(tire.GetComponent<Collider>());
            }

            BoxCollider col = tireWall.AddComponent<BoxCollider>();
            col.size = new Vector3(3.2f, 1.2f, 1.1f);

            Rigidbody rb = tireWall.AddComponent<Rigidbody>();
            rb.isKinematic = true; // Solid impact wall

            ObstacleHazard haz = tireWall.AddComponent<ObstacleHazard>();
            SerializedObject so = new SerializedObject(haz);
            so.FindProperty("obstacleType").enumValueIndex = (int)ObstacleType.HazardBarrel;
            so.FindProperty("speedPenaltyFraction").floatValue = 0.35f;
            so.ApplyModifiedProperties();

            return tireWall;
        }

        public static GameObject CreateRoadBlockChicane(Transform parent, Vector3 pos, Quaternion rot, float laneOffset)
        {
            EnsureMaterials();
            GameObject barrier = new GameObject("RoadBlockBarrier");
            if (parent != null) barrier.transform.SetParent(parent, true);
            barrier.transform.position = pos + rot * new Vector3(laneOffset, 0.6f, 0);
            barrier.transform.rotation = rot;

            // Concrete base block
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = "BarrierBody";
            block.transform.SetParent(barrier.transform, false);
            block.transform.localPosition = Vector3.zero;
            block.transform.localScale = new Vector3(4.2f, 1.1f, 0.8f);
            block.GetComponent<MeshRenderer>().material = barrierMat;
            Object.DestroyImmediate(block.GetComponent<Collider>());

            // Hazard striped top rail
            GameObject topRail = GameObject.CreatePrimitive(PrimitiveType.Cube);
            topRail.name = "HazardRail";
            topRail.transform.SetParent(barrier.transform, false);
            topRail.transform.localPosition = new Vector3(0, 0.65f, 0);
            topRail.transform.localScale = new Vector3(4.4f, 0.25f, 0.85f);
            topRail.GetComponent<MeshRenderer>().material = barrelYellowMat;
            Object.DestroyImmediate(topRail.GetComponent<Collider>());

            // Solid heavy box collider
            BoxCollider col = barrier.AddComponent<BoxCollider>();
            col.size = new Vector3(4.4f, 1.4f, 0.85f);

            Rigidbody rb = barrier.AddComponent<Rigidbody>();
            rb.mass = 500f;
            rb.isKinematic = true; // Sturdy road roadblock

            ObstacleHazard haz = barrier.AddComponent<ObstacleHazard>();
            SerializedObject so = new SerializedObject(haz);
            so.FindProperty("obstacleType").enumValueIndex = (int)ObstacleType.RoadBlockBarrier;
            so.FindProperty("speedPenaltyFraction").floatValue = 0.4f;
            so.ApplyModifiedProperties();

            return barrier;
        }
    }
}
#endif
