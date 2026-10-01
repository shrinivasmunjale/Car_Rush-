using UnityEngine;
using System;

namespace CarRush.Level
{
    /// <summary>
    /// Track checkpoint trigger with a premium animated visual design.
    /// Shows a glowing gate arch with animated pole beams that turn green when passed.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class Checkpoint : MonoBehaviour
    {
        public int CheckpointIndex { get; set; }

        public event Action<Checkpoint, Collider> OnCarEnterCheckpoint;

        [Header("Visuals")]
        [SerializeField] private MeshRenderer visualIndicator;

        [Header("Design Colors")]
        [SerializeField] private Color normalColor  = new Color(0.15f, 0.55f, 1.00f, 0.75f);
        [SerializeField] private Color passedColor  = new Color(0.10f, 0.90f, 0.30f, 0.85f);
        [SerializeField] private Color glowColor    = new Color(0.20f, 0.70f, 1.00f, 1.00f);

        [Header("Animation")]
        [Tooltip("Pulse cycle speed when not yet passed.")]
        [SerializeField] private float pulseSpeed   = 2.0f;
        [Tooltip("Pulse amplitude (0 = no pulse).")]
        [SerializeField] private float pulseAmount  = 0.18f;

        private bool hasPassed = false;
        private float baseAlpha;
        // Keep a cached material instance so we don't modify shared material
        private Material matInstance;

        // Procedurally spawned arch visuals
        private GameObject archRoot;
        private MeshRenderer archRenderer;
        private MeshRenderer leftPole;
        private MeshRenderer rightPole;
        private MeshRenderer topBeam;

        private void Awake()
        {
            // Cache the base alpha from the inspector-assigned normal color
            baseAlpha = normalColor.a;

            if (visualIndicator != null)
            {
                matInstance = new Material(visualIndicator.material);
                visualIndicator.material = matInstance;
            }

            BuildArchVisuals();
        }

        private void Update()
        {
            if (!hasPassed)
            {
                // Animated pulse for active checkpoints
                float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
                Color animated = normalColor;
                animated.a *= pulse;

                ApplyColor(animated);
                ApplyArchColor(glowColor * pulse, false);
            }
        }

        // ── Trigger ───────────────────────────────────────────────────────────

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player") ||
                other.GetComponentInParent<Car.CarController>() != null ||
                (other.attachedRigidbody != null && other.attachedRigidbody.CompareTag("Player")) ||
                (other.attachedRigidbody != null && other.attachedRigidbody.GetComponent<Car.CarController>() != null))
            {
                OnCarEnterCheckpoint?.Invoke(this, other);
            }
        }

        // ── State ─────────────────────────────────────────────────────────────

        public void SetPassed(bool passed)
        {
            hasPassed = passed;
            Color c = passed ? passedColor : normalColor;
            ApplyColor(c);
            ApplyArchColor(passed ? passedColor : glowColor, passed);
        }

        // ── Visual helpers ────────────────────────────────────────────────────

        private void ApplyColor(Color c)
        {
            if (matInstance != null)
            {
                matInstance.color = c;
                if (matInstance.HasProperty("_EmissionColor"))
                    matInstance.SetColor("_EmissionColor", c * 1.5f);
            }
        }

        private void ApplyArchColor(Color c, bool passed)
        {
            if (leftPole  != null) SetRendererColor(leftPole,  c);
            if (rightPole != null) SetRendererColor(rightPole, c);
            if (topBeam   != null) SetRendererColor(topBeam,   c);
        }

        private void SetRendererColor(MeshRenderer mr, Color c)
        {
            if (mr == null) return;
            // Apply via PropertyBlock to avoid shared material modification
            MaterialPropertyBlock mpb = new MaterialPropertyBlock();
            mr.GetPropertyBlock(mpb);
            mpb.SetColor("_BaseColor", c);
            mpb.SetColor("_Color", c);
            mpb.SetColor("_EmissionColor", c * 2f);
            mr.SetPropertyBlock(mpb);
        }

        /// <summary>
        /// Procedurally builds a gate arch out of primitive quads so the checkpoint
        /// looks like a glowing portal arch without needing external art assets.
        /// </summary>
        private void BuildArchVisuals()
        {
            // Only build if no visual indicator is already assigned AND
            // the GameObject doesn't already have child renderers set up by the scene.
            if (GetComponentsInChildren<MeshRenderer>().Length > 1) return;

            archRoot = new GameObject("CheckpointArch");
            archRoot.transform.SetParent(transform, false);
            archRoot.transform.localPosition = Vector3.zero;

            // Determine the scale from the trigger collider
            BoxCollider bc = GetComponent<BoxCollider>();
            float width  = bc != null ? bc.size.x * 0.9f : 8f;
            float height = bc != null ? bc.size.y * 0.85f : 5f;
            float depth  = 0.25f;

            leftPole  = CreatePole("LeftPole",  archRoot.transform,
                new Vector3(-width * 0.5f + 0.25f, height * 0.5f, 0),
                new Vector3(0.4f, height, depth));

            rightPole = CreatePole("RightPole", archRoot.transform,
                new Vector3( width * 0.5f - 0.25f, height * 0.5f, 0),
                new Vector3(0.4f, height, depth));

            topBeam   = CreatePole("TopBeam",   archRoot.transform,
                new Vector3(0, height - 0.2f, 0),
                new Vector3(width, 0.45f, depth));

            // Initial color
            ApplyArchColor(glowColor, false);
        }

        private MeshRenderer CreatePole(string objName, Transform parent, Vector3 localPos, Vector3 scale)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = objName;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = localPos;
            obj.transform.localScale    = scale;

            // Remove collider from the visual-only primitive
            Collider c = obj.GetComponent<Collider>();
            if (c != null) Destroy(c);

            MeshRenderer mr = obj.GetComponent<MeshRenderer>();

            // Use emissive URP unlit or Standard material
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            Material mat = new Material(shader);
            mat.EnableKeyword("_EMISSION");
            mr.material = mat;

            return mr;
        }

        private void OnDestroy()
        {
            if (matInstance != null)
                Destroy(matInstance);
        }
    }
}
