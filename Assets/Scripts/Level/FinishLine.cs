using UnityEngine;
using System;
using System.Collections;

namespace CarRush.Level
{
    /// <summary>
    /// Track finish line trigger with a premium animated design.
    /// Procedurally creates a chequered-flag-style glowing banner arch with
    /// a celebration flash effect when crossed. No external assets required.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class FinishLine : MonoBehaviour
    {
        public event Action<Collider> OnCarCrossFinishLine;

        [Header("Design Colors")]
        [SerializeField] private Color bannerColorA = new Color(1.00f, 0.85f, 0.00f, 0.90f); // gold
        [SerializeField] private Color bannerColorB = new Color(1.00f, 1.00f, 1.00f, 0.90f); // white
        [SerializeField] private Color poleColor    = new Color(0.90f, 0.90f, 0.90f, 1.00f); // silver

        [Header("Animation")]
        [SerializeField] private float bannerScrollSpeed  = 0.8f; // banner shimmer speed
        [SerializeField] private float archPulseSpeed     = 1.5f;
        [SerializeField] private float archPulseAmount    = 0.12f;

        private bool hasBeenCrossed = false;

        // Procedural visuals
        private MeshRenderer leftPole;
        private MeshRenderer rightPole;
        private MeshRenderer topArch;
        private MeshRenderer[] bannerSegments;
        private int bannerCount = 10;
        private float bannerTimer = 0f;

        private void Awake()
        {
            BuildFinishLineVisuals();
        }

        private void Update()
        {
            if (!hasBeenCrossed)
            {
                bannerTimer += Time.deltaTime * bannerScrollSpeed;
                UpdateBannerAnimation();
                PulseArch();
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
                OnCarCrossFinishLine?.Invoke(other);
                if (!hasBeenCrossed)
                {
                    hasBeenCrossed = true;
                    StartCoroutine(CelebrationFlash());
                }
            }
        }

        // ── Visual Helpers ────────────────────────────────────────────────────

        private void BuildFinishLineVisuals()
        {
            // Only build if not already set up by scene designer
            if (GetComponentsInChildren<MeshRenderer>().Length > 1) return;

            BoxCollider bc = GetComponent<BoxCollider>();
            float width  = bc != null ? bc.size.x * 0.92f : 10f;
            float height = bc != null ? bc.size.y * 0.90f : 6f;
            float depth  = 0.28f;

            GameObject root = new GameObject("FinishLineArch");
            root.transform.SetParent(transform, false);
            root.transform.localPosition = Vector3.zero;

            // Two silver poles
            leftPole  = CreateBlock("LeftPole",  root.transform,
                new Vector3(-width * 0.5f + 0.3f, height * 0.5f, 0),
                new Vector3(0.5f, height, depth), poleColor);

            rightPole = CreateBlock("RightPole", root.transform,
                new Vector3( width * 0.5f - 0.3f, height * 0.5f, 0),
                new Vector3(0.5f, height, depth), poleColor);

            // Top arch
            topArch = CreateBlock("TopArch", root.transform,
                new Vector3(0, height - 0.25f, 0),
                new Vector3(width, 0.5f, depth), bannerColorA);

            // Chequered banner segments (alternating gold/white blocks under the arch)
            bannerSegments = new MeshRenderer[bannerCount];
            float segWidth = width / bannerCount;
            for (int i = 0; i < bannerCount; i++)
            {
                float xPos = -width * 0.5f + segWidth * i + segWidth * 0.5f;
                Color c = (i % 2 == 0) ? bannerColorA : bannerColorB;
                bannerSegments[i] = CreateBlock($"BannerSeg{i}", root.transform,
                    new Vector3(xPos, height - 0.9f, 0),
                    new Vector3(segWidth - 0.04f, 0.55f, depth * 0.8f), c);
            }

            // "FINISH" text using a second row of segments as a decorative stripe
            CreateBlock("FinishStripe", root.transform,
                new Vector3(0, height * 0.15f, 0),
                new Vector3(width, 0.2f, depth), bannerColorA);
        }

        private void UpdateBannerAnimation()
        {
            // Scroll chequered colors like a marquee
            for (int i = 0; i < bannerSegments.Length; i++)
            {
                if (bannerSegments[i] == null) continue;
                float phase  = bannerTimer + i * 0.2f;
                bool  isEven = Mathf.FloorToInt(phase) % 2 == 0;
                Color c = isEven ? bannerColorA : bannerColorB;
                // Add mild emission shimmer
                c *= 1f + Mathf.Sin(Time.time * 3f + i * 0.5f) * 0.08f;
                SetColor(bannerSegments[i], c);
            }
        }

        private void PulseArch()
        {
            float pulse = 1f + Mathf.Sin(Time.time * archPulseSpeed) * archPulseAmount;
            Color c = bannerColorA * pulse;
            if (topArch != null) SetColor(topArch, c);
        }

        private IEnumerator CelebrationFlash()
        {
            // Flash gold-white rapidly 6 times
            Color gold  = new Color(1f, 0.85f, 0f, 1f);
            Color white = Color.white;

            for (int f = 0; f < 6; f++)
            {
                Color c = (f % 2 == 0) ? gold : white;
                foreach (var seg in bannerSegments)
                    if (seg != null) SetColor(seg, c);
                if (topArch   != null) SetColor(topArch,   c);
                if (leftPole  != null) SetColor(leftPole,  c);
                if (rightPole != null) SetColor(rightPole, c);
                yield return new WaitForSeconds(0.12f);
            }

            // Settle on gold after celebration
            Color finalGold = new Color(1f, 0.85f, 0f, 0.95f);
            foreach (var seg in bannerSegments)
                if (seg != null) SetColor(seg, finalGold);
            if (topArch   != null) SetColor(topArch,   finalGold);
            if (leftPole  != null) SetColor(leftPole,  finalGold);
            if (rightPole != null) SetColor(rightPole, finalGold);
        }

        // ── Primitive factories ───────────────────────────────────────────────

        private MeshRenderer CreateBlock(string objName, Transform parent, Vector3 localPos, Vector3 scale, Color color)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = objName;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = localPos;
            obj.transform.localScale    = scale;

            Collider c = obj.GetComponent<Collider>();
            if (c != null) Destroy(c);

            MeshRenderer mr = obj.GetComponent<MeshRenderer>();

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            Material mat = new Material(shader);
            mat.EnableKeyword("_EMISSION");
            mat.color = color;
            mr.material = mat;

            SetColor(mr, color);
            return mr;
        }

        private static void SetColor(MeshRenderer mr, Color c)
        {
            if (mr == null) return;
            MaterialPropertyBlock mpb = new MaterialPropertyBlock();
            mr.GetPropertyBlock(mpb);
            mpb.SetColor("_BaseColor", c);
            mpb.SetColor("_Color", c);
            mpb.SetColor("_EmissionColor", c * 1.8f);
            mr.SetPropertyBlock(mpb);
        }
    }
}
