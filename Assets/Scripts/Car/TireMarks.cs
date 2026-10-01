using System.Collections.Generic;
using UnityEngine;

namespace CarRush.Car
{
    /// <summary>
    /// High-performance tire marks system using dedicated TrailRenderers per wheel.
    /// Completely eliminates per-frame GameObject/Mesh instantiation and GC allocations,
    /// ensuring silky-smooth 60+ FPS even at speeds well above 100-200 km/h.
    /// </summary>
    [RequireComponent(typeof(CarController))]
    public class TireMarks : MonoBehaviour
    {
        [Header("Mark Settings")]
        [Tooltip("Width of each tire mark strip in metres.")]
        [SerializeField] private float markWidth = 0.28f;

        [Tooltip("How long (seconds) before a mark fully fades away.")]
        [SerializeField] private float fadeDuration = 4.5f;

        [Tooltip("Minimum speed (km/h) before marks can be drawn.")]
        [SerializeField] private float minSpeedKmh = 10f;

        [Header("Wheel Refs (auto-finds all wheels if left empty)")]
        [SerializeField] private WheelCollider[] wheelColliders;

        private CarController car;
        private Material markMaterial;

        private class WheelTrailData
        {
            public WheelCollider wc;
            public GameObject trailObj;
            public TrailRenderer trail;
        }

        private readonly List<WheelTrailData> wheelTrails = new List<WheelTrailData>();

        private void Awake()
        {
            car = GetComponent<CarController>();
            BuildMarkMaterial();
            SetupWheelTrails();
        }

        private void SetupWheelTrails()
        {
            wheelTrails.Clear();

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

            for (int i = 0; i < foundWheels.Count; i++)
            {
                WheelCollider wc = foundWheels[i];
                GameObject tObj = new GameObject($"TireTrail_{wc.name}_{i}");
                tObj.transform.SetParent(null); // World space pinned

                TrailRenderer tr = tObj.AddComponent<TrailRenderer>();
                tr.material = markMaterial;
                tr.startWidth = markWidth;
                tr.endWidth = markWidth;
                tr.time = fadeDuration;
                tr.minVertexDistance = 0.18f;
                tr.autodestruct = false;
                tr.emitting = false;
                tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                tr.receiveShadows = false;

                // Smooth fade color gradient
                Gradient grad = new Gradient();
                Color c = markMaterial.color;
                grad.SetKeys(
                    new GradientColorKey[] { new GradientColorKey(c, 0.0f), new GradientColorKey(c, 1.0f) },
                    new GradientAlphaKey[] { new GradientAlphaKey(0.85f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) }
                );
                tr.colorGradient = grad;

                wheelTrails.Add(new WheelTrailData
                {
                    wc = wc,
                    trailObj = tObj,
                    trail = tr
                });
            }
        }

        private void BuildMarkMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit")
                         ?? Shader.Find("Sprites/Default")
                         ?? Shader.Find("Unlit/Color")
                         ?? Shader.Find("Standard");

            markMaterial = new Material(shader);
            Color darkTire = new Color(0.08f, 0.08f, 0.08f, 0.85f);
            markMaterial.color = darkTire;

            if (markMaterial.HasProperty("_BaseColor")) markMaterial.SetColor("_BaseColor", darkTire);
            if (markMaterial.HasProperty("_Color")) markMaterial.SetColor("_Color", darkTire);

            if (markMaterial.HasProperty("_Cull")) markMaterial.SetFloat("_Cull", 0);
            if (markMaterial.HasProperty("_Surface")) markMaterial.SetFloat("_Surface", 1);
            if (markMaterial.HasProperty("_Blend")) markMaterial.SetFloat("_Blend", 0);

            markMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            markMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            markMaterial.SetInt("_ZWrite", 0);
            markMaterial.renderQueue = 3050;
            markMaterial.SetOverrideTag("RenderType", "Transparent");
        }

        private void LateUpdate()
        {
            if (car == null || wheelTrails.Count == 0) return;

            float speed = car.CurrentSpeedKmh;
            if (speed < minSpeedKmh)
            {
                for (int i = 0; i < wheelTrails.Count; i++)
                {
                    if (wheelTrails[i].trail.emitting)
                        wheelTrails[i].trail.emitting = false;
                }
                return;
            }

            for (int i = 0; i < wheelTrails.Count; i++)
            {
                WheelTrailData wt = wheelTrails[i];
                if (wt.wc == null || wt.trail == null) continue;

                if (wt.wc.GetGroundHit(out WheelHit hit))
                {
                    // Detect legitimate skid/drift conditions:
                    // Avoid false positives from straight-line acceleration at high speed (>100 km/h)
                    bool isDrifting = Mathf.Abs(hit.sidewaysSlip) > 0.32f;
                    bool isTireBurnout = Mathf.Abs(hit.forwardSlip) > 0.58f;
                    bool isHardBraking = car.IsBraking && speed > 15f;
                    bool isHardCornering = Mathf.Abs(car.SteerInput) > 0.45f && speed > 35f;

                    bool shouldMark = isDrifting || isTireBurnout || isHardBraking || isHardCornering;

                    if (shouldMark)
                    {
                        // Pin emitter position right at ground contact normal
                        wt.trailObj.transform.position = hit.point + hit.normal * 0.02f;
                        wt.trailObj.transform.rotation = Quaternion.LookRotation(transform.forward, hit.normal);

                        if (!wt.trail.emitting)
                            wt.trail.emitting = true;
                    }
                    else
                    {
                        if (wt.trail.emitting)
                            wt.trail.emitting = false;
                    }
                }
                else
                {
                    if (wt.trail.emitting)
                        wt.trail.emitting = false;
                }
            }
        }

        private void OnDestroy()
        {
            for (int i = 0; i < wheelTrails.Count; i++)
            {
                if (wheelTrails[i].trailObj != null)
                    Destroy(wheelTrails[i].trailObj);
            }
            wheelTrails.Clear();

            if (markMaterial != null)
                Destroy(markMaterial);
        }
    }
}
