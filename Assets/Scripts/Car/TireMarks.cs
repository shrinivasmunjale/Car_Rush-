using System.Collections.Generic;
using UnityEngine;

namespace CarRush.Car
{
    /// <summary>
    /// Procedurally spawns tire-mark decal quads on the road surface whenever
    /// the car is drifting, hard-braking, or cornering.
    /// Double-sided quad rendering with explicit surface normals prevents backface culling,
    /// ensuring tire marks are clearly visible from any camera angle.
    /// </summary>
    [RequireComponent(typeof(CarController))]
    public class TireMarks : MonoBehaviour
    {
        [Header("Mark Settings")]
        [Tooltip("Width of each tire mark strip in metres.")]
        [SerializeField] private float markWidth = 0.28f;

        [Tooltip("How long (seconds) before a mark fully fades to transparent.")]
        [SerializeField] private float fadeDuration = 8f;

        [Tooltip("Minimum speed (km/h) before marks are drawn.")]
        [SerializeField] private float minSpeedKmh = 5f;

        [Tooltip("Maximum total live mark segments before oldest are culled.")]
        [SerializeField] private int maxSegments = 400;

        [Header("Wheel Refs (auto-finds all wheels if left empty)")]
        [SerializeField] private WheelCollider[] wheelColliders;

        // ── Internals ─────────────────────────────────────────────────────────

        private CarController car;
        private Material markMaterial;

        private class WheelTracker
        {
            public WheelCollider wc;
            public Vector3 prevPoint;
            public bool prevValid;
        }

        private readonly List<WheelTracker> wheelTrackers = new List<WheelTracker>();

        private class MarkSegment
        {
            public GameObject go;
            public MeshFilter mf;
            public MeshRenderer mr;
            public float spawnTime;
            public Mesh mesh;
        }

        private readonly List<MarkSegment> segments = new List<MarkSegment>();
        private Transform markContainer;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            car = GetComponent<CarController>();
            FindAndSetupWheels();
            BuildMarkMaterial();
            CreateMarkContainer();
        }

        private void FindAndSetupWheels()
        {
            wheelTrackers.Clear();

            List<WheelCollider> foundWheels = new List<WheelCollider>();

            if (wheelColliders != null && wheelColliders.Length > 0)
            {
                foreach (var wc in wheelColliders)
                {
                    if (wc != null) foundWheels.Add(wc);
                }
            }

            if (foundWheels.Count == 0 && car != null)
            {
                if (car.RearLeftCollider != null) foundWheels.Add(car.RearLeftCollider);
                if (car.RearRightCollider != null) foundWheels.Add(car.RearRightCollider);
                if (car.FrontLeftCollider != null) foundWheels.Add(car.FrontLeftCollider);
                if (car.FrontRightCollider != null) foundWheels.Add(car.FrontRightCollider);
            }

            if (foundWheels.Count == 0)
            {
                WheelCollider[] allWcs = GetComponentsInChildren<WheelCollider>();
                foundWheels.AddRange(allWcs);
            }

            foreach (var wc in foundWheels)
            {
                wheelTrackers.Add(new WheelTracker
                {
                    wc = wc,
                    prevPoint = Vector3.zero,
                    prevValid = false
                });
            }
        }

        private void BuildMarkMaterial()
        {
            // Use Universal RP Unlit or Sprites/Default or Unlit/Color
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader == null) shader = Shader.Find("Standard");

            markMaterial = new Material(shader);
            Color darkTire = new Color(0.08f, 0.08f, 0.08f, 0.88f);
            markMaterial.color = darkTire;

            if (markMaterial.HasProperty("_BaseColor"))
                markMaterial.SetColor("_BaseColor", darkTire);
            if (markMaterial.HasProperty("_Color"))
                markMaterial.SetColor("_Color", darkTire);

            // Double sided (Cull Off = 0) so marks are visible regardless of normal orientation
            if (markMaterial.HasProperty("_Cull"))
                markMaterial.SetFloat("_Cull", 0);

            // URP Transparency config
            if (markMaterial.HasProperty("_Surface"))
                markMaterial.SetFloat("_Surface", 1); // 1 = Transparent

            if (markMaterial.HasProperty("_Blend"))
                markMaterial.SetFloat("_Blend", 0); // 0 = Alpha blend

            markMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            markMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            markMaterial.SetInt("_ZWrite", 0);
            markMaterial.renderQueue = 3050;
            markMaterial.SetOverrideTag("RenderType", "Transparent");
            markMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        }

        private void CreateMarkContainer()
        {
            GameObject go = new GameObject("TireMarksContainer");
            go.transform.SetParent(null); // World space so marks stay pinned to the road
            markContainer = go.transform;
        }

        // ── Per-frame ─────────────────────────────────────────────────────────

        private void Update()
        {
            FadeAndCullOldSegments();

            if (car == null || wheelTrackers.Count == 0) return;

            float speed = car.CurrentSpeedKmh;
            if (speed < minSpeedKmh)
            {
                foreach (var tracker in wheelTrackers)
                    tracker.prevValid = false;
                return;
            }

            bool generalDriftOrBrake = ShouldLeaveMarks();

            for (int i = 0; i < wheelTrackers.Count; i++)
            {
                WheelTracker tracker = wheelTrackers[i];
                if (tracker.wc == null) continue;

                bool wheelShouldMark = generalDriftOrBrake;

                if (tracker.wc.GetGroundHit(out WheelHit hit))
                {
                    // Direct slip detection (drifting, power-sliding, or wheel-spin)
                    if (Mathf.Abs(hit.sidewaysSlip) > 0.16f || Mathf.Abs(hit.forwardSlip) > 0.26f)
                    {
                        wheelShouldMark = true;
                    }

                    HandleWheel(tracker, hit, wheelShouldMark);
                }
                else
                {
                    tracker.prevValid = false;
                }
            }
        }

        private bool ShouldLeaveMarks()
        {
            if (car.CurrentSpeedKmh < minSpeedKmh) return false;

            bool isBraking = car.IsBraking && car.CurrentSpeedKmh > 8f;
            bool isHardSteering = Mathf.Abs(car.SteerInput) > 0.25f && car.CurrentSpeedKmh > 18f;
            bool isHandbraking = car.IsBraking;

            return isBraking || isHardSteering || isHandbraking;
        }

        private void HandleWheel(WheelTracker tracker, WheelHit hit, bool active)
        {
            // Position contact point slightly above road normal to eliminate z-fighting
            Vector3 contactPoint = hit.point + hit.normal * 0.025f;

            if (!active)
            {
                tracker.prevValid = false;
                return;
            }

            if (tracker.prevValid)
            {
                float dist = Vector3.Distance(tracker.prevPoint, contactPoint);
                if (dist > 0.02f && dist < 2.5f)
                {
                    SpawnSegment(tracker.prevPoint, contactPoint, tracker.wc.transform.right, hit.normal);
                }
            }

            tracker.prevPoint = contactPoint;
            tracker.prevValid = true;
        }

        private void SpawnSegment(Vector3 from, Vector3 to, Vector3 wheelRight, Vector3 surfaceNormal)
        {
            if (segments.Count >= maxSegments && segments.Count > 0)
            {
                MarkSegment oldest = segments[0];
                segments.RemoveAt(0);
                if (oldest != null && oldest.go != null)
                    Destroy(oldest.go);
            }

            GameObject go = new GameObject("TireMark");
            go.transform.SetParent(markContainer, true);
            MeshFilter mf = go.AddComponent<MeshFilter>();
            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            mr.material = markMaterial;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;

            Vector3 dir = (to - from).normalized;
            Vector3 right = wheelRight.normalized * (markWidth * 0.5f);

            if (right.sqrMagnitude < 0.001f)
                right = Vector3.Cross(dir, surfaceNormal).normalized * (markWidth * 0.5f);

            Vector3 v0 = from - right;
            Vector3 v1 = from + right;
            Vector3 v2 = to + right;
            Vector3 v3 = to - right;

            Mesh mesh = new Mesh();
            mesh.vertices = new Vector3[] { v0, v1, v2, v3 };

            // Double-sided triangle winding (both CW and CCW) guarantees 100% visibility from above and below
            mesh.triangles = new int[]
            {
                0, 1, 2,  0, 2, 3, // Side A
                0, 2, 1,  0, 3, 2  // Side B (reversed)
            };

            mesh.normals = new Vector3[] { surfaceNormal, surfaceNormal, surfaceNormal, surfaceNormal };
            mesh.uv = new Vector2[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };

            mf.mesh = mesh;

            MarkSegment seg = new MarkSegment
            {
                go = go,
                mf = mf,
                mr = mr,
                spawnTime = Time.time,
                mesh = mesh
            };
            segments.Add(seg);
        }

        private void FadeAndCullOldSegments()
        {
            float now = Time.time;
            for (int i = segments.Count - 1; i >= 0; i--)
            {
                MarkSegment seg = segments[i];
                if (seg == null || seg.go == null)
                {
                    segments.RemoveAt(i);
                    continue;
                }

                float age = now - seg.spawnTime;
                float alpha = Mathf.Clamp01(1f - age / fadeDuration);

                if (alpha <= 0f)
                {
                    Destroy(seg.go);
                    segments.RemoveAt(i);
                    continue;
                }

                MaterialPropertyBlock mpb = new MaterialPropertyBlock();
                seg.mr.GetPropertyBlock(mpb);
                Color c = markMaterial.color;
                c.a = alpha * 0.88f;
                mpb.SetColor("_BaseColor", c);
                mpb.SetColor("_Color", c);
                seg.mr.SetPropertyBlock(mpb);
            }
        }

        private void OnDestroy()
        {
            foreach (var seg in segments)
            {
                if (seg != null && seg.go != null)
                    Destroy(seg.go);
            }
            segments.Clear();

            if (markContainer != null)
                Destroy(markContainer.gameObject);

            if (markMaterial != null)
                Destroy(markMaterial);
        }
    }
}
