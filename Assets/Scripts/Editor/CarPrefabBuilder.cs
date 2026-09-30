#if UNITY_EDITOR
using System.IO;
using CarRush.Car;
using UnityEditor;
using UnityEngine;

namespace CarRush.Editor
{
    public static class CarPrefabBuilder
    {
        public const string CarPrefabPath = "Assets/Prefabs/PlayerCar.prefab";

        [MenuItem("CarRush/Build Player Car Prefab", false, 10)]
        public static GameObject CreatePlayerCarPrefab()
        {
            if (!Directory.Exists("Assets/Prefabs"))
                AssetDatabase.CreateFolder("Assets", "Prefabs");

            // Root Car GameObject
            GameObject carRoot = new GameObject("PlayerCar");
            carRoot.tag = "Player";

            Rigidbody rb = carRoot.AddComponent<Rigidbody>();
            rb.mass = 1200f;
            rb.linearDamping = 0.05f;
            rb.angularDamping = 2f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            // Materials
            Material bodyMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            bodyMat.color = new Color(0.9f, 0.15f, 0.1f); // Vibrant Racing Red
            bodyMat.SetFloat("_Smoothness", 0.85f);
            if (!Directory.Exists("Assets/Art/Materials"))
            {
                if (!Directory.Exists("Assets/Art")) AssetDatabase.CreateFolder("Assets", "Art");
                AssetDatabase.CreateFolder("Assets/Art", "Materials");
            }
            AssetDatabase.CreateAsset(bodyMat, "Assets/Art/Materials/CarBodyRed.mat");

            Material wheelMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            wheelMat.color = new Color(0.12f, 0.12f, 0.14f);
            AssetDatabase.CreateAsset(wheelMat, "Assets/Art/Materials/CarWheel.mat");

            Material glassMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            glassMat.color = new Color(0.1f, 0.2f, 0.35f, 0.8f);
            AssetDatabase.CreateAsset(glassMat, "Assets/Art/Materials/CarGlass.mat");

            // 1. Car Body Visuals
            GameObject bodyObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bodyObj.name = "BodyMesh";
            bodyObj.transform.SetParent(carRoot.transform, false);
            bodyObj.transform.localPosition = new Vector3(0, 0.45f, 0);
            bodyObj.transform.localScale = new Vector3(1.8f, 0.55f, 4.0f);
            bodyObj.GetComponent<MeshRenderer>().material = bodyMat;
            Object.DestroyImmediate(bodyObj.GetComponent<Collider>());

            // Cabin / Roof
            GameObject cabinObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cabinObj.name = "CabinMesh";
            cabinObj.transform.SetParent(carRoot.transform, false);
            cabinObj.transform.localPosition = new Vector3(0, 0.85f, -0.3f);
            cabinObj.transform.localScale = new Vector3(1.4f, 0.45f, 2.0f);
            cabinObj.GetComponent<MeshRenderer>().material = glassMat;
            Object.DestroyImmediate(cabinObj.GetComponent<Collider>());

            // Spoiler
            GameObject spoilerObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            spoilerObj.name = "Spoiler";
            spoilerObj.transform.SetParent(carRoot.transform, false);
            spoilerObj.transform.localPosition = new Vector3(0, 0.95f, -1.85f);
            spoilerObj.transform.localScale = new Vector3(1.6f, 0.08f, 0.4f);
            spoilerObj.GetComponent<MeshRenderer>().material = wheelMat;
            Object.DestroyImmediate(spoilerObj.GetComponent<Collider>());

            // Main Box Collider
            BoxCollider col = carRoot.AddComponent<BoxCollider>();
            col.center = new Vector3(0, 0.55f, 0);
            col.size = new Vector3(1.8f, 0.8f, 4.0f);

            // 2. Wheels
            GameObject wheelsRoot = new GameObject("Wheels");
            wheelsRoot.transform.SetParent(carRoot.transform, false);

            WheelCollider flCol = CustomCarRigger.CreateWheelWithXRim(wheelsRoot.transform, "FL_Wheel", new Vector3(-0.95f, 0.35f, 1.3f), true, wheelMat, out Transform flMesh);
            WheelCollider frCol = CustomCarRigger.CreateWheelWithXRim(wheelsRoot.transform, "FR_Wheel", new Vector3(0.95f, 0.35f, 1.3f), false, wheelMat, out Transform frMesh);
            WheelCollider rlCol = CustomCarRigger.CreateWheelWithXRim(wheelsRoot.transform, "RL_Wheel", new Vector3(-0.95f, 0.35f, -1.3f), true, wheelMat, out Transform rlMesh);
            WheelCollider rrCol = CustomCarRigger.CreateWheelWithXRim(wheelsRoot.transform, "RR_Wheel", new Vector3(0.95f, 0.35f, -1.3f), false, wheelMat, out Transform rrMesh);

            // 3. CarController Component & Wiring
            CarController controller = carRoot.AddComponent<CarController>();
            SerializedObject so = new SerializedObject(controller);

            SetWheelInfo(so, "frontLeftWheel", flCol, flMesh, true, true);
            SetWheelInfo(so, "frontRightWheel", frCol, frMesh, true, true);
            SetWheelInfo(so, "rearLeftWheel", rlCol, rlMesh, true, false);
            SetWheelInfo(so, "rearRightWheel", rrCol, rrMesh, true, false);
            so.ApplyModifiedProperties();

            // 4. CarAudio Component
            carRoot.AddComponent<CarAudio>();

            // Save Prefab
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(carRoot, CarPrefabPath);
            Object.DestroyImmediate(carRoot);

            Debug.Log("<color=green><b>[CarRush]</b> PlayerCar prefab created at: " + CarPrefabPath + "</color>");
            return prefab;
        }

        private static WheelCollider CreateWheel(Transform parent, string name, Vector3 localPos, Material mat, out Transform visualMesh)
        {
            GameObject wheelObj = new GameObject(name);
            wheelObj.transform.SetParent(parent, false);
            wheelObj.transform.localPosition = localPos;

            WheelCollider wc = wheelObj.AddComponent<WheelCollider>();
            wc.radius = 0.35f;
            wc.suspensionDistance = 0.2f;
            wc.mass = 25f;

            JointSpring spring = wc.suspensionSpring;
            spring.spring = 35000f;
            spring.damper = 4500f;
            spring.targetPosition = 0.5f;
            wc.suspensionSpring = spring;

            WheelFrictionCurve forwardFriction = wc.forwardFriction;
            forwardFriction.stiffness = 1.8f;
            wc.forwardFriction = forwardFriction;

            WheelFrictionCurve sidewaysFriction = wc.sidewaysFriction;
            sidewaysFriction.stiffness = 1.6f;
            wc.sidewaysFriction = sidewaysFriction;

            // Visual Mesh
            GameObject meshObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            meshObj.name = "Mesh";
            meshObj.transform.SetParent(wheelObj.transform, false);
            meshObj.transform.localRotation = Quaternion.Euler(0, 0, 90f);
            meshObj.transform.localScale = new Vector3(0.7f, 0.25f, 0.7f);
            meshObj.GetComponent<MeshRenderer>().material = mat;
            Object.DestroyImmediate(meshObj.GetComponent<Collider>());

            visualMesh = meshObj.transform;
            return wc;
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
