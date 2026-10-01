using UnityEngine;

namespace CarRush.Car
{
    /// <summary>
    /// Bugatti-style W16 engine audio using procedural synthesis.
    /// Produces a deep growling idle, powerful mid-range roar, and high-pitched
    /// supercharger whine at top speed. No external audio files required.
    /// </summary>
    [RequireComponent(typeof(CarController))]
    public class CarAudio : MonoBehaviour
    {
        [Header("Engine Tuning")]
        [SerializeField] private float minPitch   = 0.55f;
        [SerializeField] private float maxPitch   = 2.8f;
        [SerializeField] private float maxSpeed   = 160f;

        [Header("Volume")]
        [SerializeField] private float idleVolume   = 0.55f;
        [SerializeField] private float maxVolume    = 0.80f;
        [SerializeField] private float screechMaxVol = 0.40f;

        // Audio sources
        private AudioSource engineSource;    // main W16 roar
        private AudioSource idleSource;      // low thump at idle
        private AudioSource whineSource;     // supercharger whine layer
        private AudioSource screechSource;   // tire screech

        private CarController car;

        private void Awake()
        {
            car = GetComponent<CarController>();
            BuildEngineAudio();
            BuildIdleAudio();
            BuildWhineAudio();
            BuildScreechAudio();
        }

        // ── Synthesis helpers ─────────────────────────────────────────────────

        /// <summary>Creates a W16-style growling waveform: sawtooth stack + harmonics.</summary>
        private void BuildEngineAudio()
        {
            engineSource = CreateSource(loop: true, volume: idleVolume, pitch: minPitch, spatial: 0.45f);

            int sampleRate = 44100;
            int len = sampleRate;  // 1-second loop
            AudioClip clip = AudioClip.Create("BugattiEngine", len, 1, sampleRate, false);
            float[] data = new float[len];

            for (int i = 0; i < len; i++)
            {
                float t = (float)i / sampleRate;

                // Fundamental W16 cylinder fire pattern: 4 groups × 4 cylinders
                // Base frequency ~55 Hz (deep growl, like Veyron idle)
                float f0 = 55f;

                // Sawtooth at fundamental (cylinder pulse)
                float saw0 = Sawtooth(f0, t);
                // 2nd harmonic (first overtone)
                float saw1 = Sawtooth(f0 * 2f, t) * 0.55f;
                // 3rd harmonic (adds mid-range body)
                float saw2 = Sawtooth(f0 * 3f, t) * 0.35f;
                // 4th harmonic (edge/bite)
                float saw3 = Sawtooth(f0 * 4f, t) * 0.20f;

                // Low sub-bass thud (cylinder ignition thump)
                float sub = Mathf.Sin(2f * Mathf.PI * 27.5f * t) * 0.45f;

                // Mid growl modulated slightly (engine "wobble" at idle)
                float wobble = 1f + Mathf.Sin(2f * Mathf.PI * 7f * t) * 0.07f;

                float sample = (saw0 + saw1 + saw2 + saw3 + sub) * wobble * 0.28f;
                data[i] = Mathf.Clamp(sample, -1f, 1f);
            }

            clip.SetData(data, 0);
            engineSource.clip = clip;
            engineSource.Play();
        }

        /// <summary>Deep low-frequency idle thud (valvetrain/exhaust pulse).</summary>
        private void BuildIdleAudio()
        {
            idleSource = CreateSource(loop: true, volume: 0.30f, pitch: 1.0f, spatial: 0.5f);

            int sampleRate = 44100;
            int len = sampleRate / 4; // 0.25s loop
            AudioClip clip = AudioClip.Create("BugattiIdle", len, 1, sampleRate, false);
            float[] data = new float[len];

            for (int i = 0; i < len; i++)
            {
                float t = (float)i / sampleRate;
                // Short thumping decay pulse (simulates exhaust pop)
                float pulse = Mathf.Exp(-t * 14f) * Mathf.Sin(2f * Mathf.PI * 38f * t);
                data[i] = Mathf.Clamp(pulse * 0.9f, -1f, 1f);
            }

            clip.SetData(data, 0);
            idleSource.clip = clip;
            idleSource.Play();
        }

        /// <summary>Supercharger/turbo whine that rises with RPM (Bugatti signature sound).</summary>
        private void BuildWhineAudio()
        {
            whineSource = CreateSource(loop: true, volume: 0f, pitch: 0.5f, spatial: 0.3f);

            int sampleRate = 44100;
            int len = sampleRate;
            AudioClip clip = AudioClip.Create("BugattiWhine", len, 1, sampleRate, false);
            float[] data = new float[len];

            for (int i = 0; i < len; i++)
            {
                float t = (float)i / sampleRate;
                // Rising sine tone — pitch is controlled at runtime
                float tone = Mathf.Sin(2f * Mathf.PI * 1800f * t) * 0.4f;
                // Slight harmonic shimmer
                float shimmer = Mathf.Sin(2f * Mathf.PI * 3600f * t) * 0.15f;
                // High-freq whistle (boost whistle)
                float whistle = Mathf.Sin(2f * Mathf.PI * 5400f * t) * 0.08f;
                data[i] = Mathf.Clamp((tone + shimmer + whistle) * 0.6f, -1f, 1f);
            }

            clip.SetData(data, 0);
            whineSource.clip = clip;
            whineSource.Play();
        }

        /// <summary>Tire screech / slide noise.</summary>
        private void BuildScreechAudio()
        {
            screechSource = CreateSource(loop: true, volume: 0f, pitch: 1.1f, spatial: 0.6f);

            int sampleRate = 44100;
            int len = sampleRate / 2;
            AudioClip clip = AudioClip.Create("TireScreech", len, 1, sampleRate, false);
            float[] data = new float[len];
            System.Random rand = new System.Random(7);

            for (int i = 0; i < len; i++)
            {
                float t  = (float)i / sampleRate;
                float noise = (float)(rand.NextDouble() * 2.0 - 1.0) * 0.25f;
                float tone  = Mathf.Sin(2f * Mathf.PI * 900f * t) * 0.12f;
                float tone2 = Mathf.Sin(2f * Mathf.PI * 1500f * t) * 0.08f;
                data[i] = Mathf.Clamp(noise + tone + tone2, -1f, 1f);
            }

            clip.SetData(data, 0);
            screechSource.clip = clip;
            screechSource.Play();
        }

        // ── Runtime update ────────────────────────────────────────────────────

        private void Update()
        {
            if (car == null) return;

            // Pause all car sounds when game is paused
            if (Time.timeScale <= 0.001f)
            {
                if (engineSource != null && engineSource.isPlaying) engineSource.Pause();
                if (idleSource != null && idleSource.isPlaying) idleSource.Pause();
                if (whineSource != null && whineSource.isPlaying) whineSource.Pause();
                if (screechSource != null && screechSource.isPlaying) screechSource.Pause();
                return;
            }
            else
            {
                if (engineSource != null && !engineSource.isPlaying) engineSource.UnPause();
                if (idleSource != null && !idleSource.isPlaying) idleSource.UnPause();
                if (whineSource != null && !whineSource.isPlaying) whineSource.UnPause();
                if (screechSource != null && !screechSource.isPlaying) screechSource.UnPause();
            }

            float speedRatio   = Mathf.Clamp01(car.CurrentSpeedKmh / maxSpeed);
            float throttleAbs  = Mathf.Abs(car.ThrottleInput);

            // Engine pitch rises aggressively with speed and throttle (Bugatti feel)
            float rawPitch  = Mathf.Lerp(minPitch, maxPitch, speedRatio);
            float boostPitch = rawPitch + throttleAbs * 0.35f * (1f - speedRatio); // blip under throttle
            engineSource.pitch  = Mathf.Lerp(engineSource.pitch, Mathf.Clamp(boostPitch, minPitch, maxPitch + 0.2f), Time.deltaTime * 6f);
            engineSource.volume = Mathf.Lerp(idleVolume, maxVolume, speedRatio + throttleAbs * 0.2f);

            // Idle thud fades out as speed rises (not audible at high speed)
            idleSource.pitch  = Mathf.Lerp(0.8f, 2.2f, speedRatio);
            idleSource.volume = Mathf.Lerp(0.30f, 0.05f, speedRatio);

            // Supercharger whine scales with speed/throttle (most prominent above 50 km/h)
            float whineFactor = Mathf.Clamp01((car.CurrentSpeedKmh - 30f) / (maxSpeed - 30f));
            float whineTarget = whineFactor * 0.35f + throttleAbs * 0.1f;
            whineSource.pitch  = Mathf.Lerp(0.5f, 2.4f, speedRatio);
            whineSource.volume = Mathf.Lerp(whineSource.volume, whineTarget, Time.deltaTime * 5f);

            // Tire screech: braking hard or cornering fast
            bool isScreeching = (car.IsBraking && car.CurrentSpeedKmh > 20f) ||
                                 (Mathf.Abs(car.SteerInput) > 0.55f && car.CurrentSpeedKmh > 40f);
            float screechTarget = isScreeching ? screechMaxVol * Mathf.Clamp01(car.CurrentSpeedKmh / 80f) : 0f;
            screechSource.volume = Mathf.Lerp(screechSource.volume, screechTarget, Time.deltaTime * 12f);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private AudioSource CreateSource(bool loop, float volume, float pitch, float spatial)
        {
            AudioSource src = gameObject.AddComponent<AudioSource>();
            src.loop         = loop;
            src.playOnAwake  = false;
            src.volume       = volume;
            src.pitch        = pitch;
            src.spatialBlend = spatial;
            return src;
        }

        private static float Sawtooth(float freq, float t)
        {
            return (t * freq % 1f) * 2f - 1f;
        }
    }
}
