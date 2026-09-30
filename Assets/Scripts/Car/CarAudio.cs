using UnityEngine;

namespace CarRush.Car
{
    /// <summary>
    /// Procedural / synthesizer audio controller for engine roar and tire screeches.
    /// Creates synthetic tones so no external audio assets are required!
    /// </summary>
    [RequireComponent(typeof(CarController))]
    public class CarAudio : MonoBehaviour
    {
        [Header("Audio Sources")]
        private AudioSource engineSource;
        private AudioSource screechSource;

        [Header("Engine Tuning")]
        [SerializeField] private float minPitch = 0.6f;
        [SerializeField] private float maxPitch = 2.4f;
        [SerializeField] private float maxSpeed = 160f;

        private CarController car;

        private void Awake()
        {
            car = GetComponent<CarController>();
            SetupEngineAudio();
            SetupScreechAudio();
        }

        private void SetupEngineAudio()
        {
            engineSource = gameObject.AddComponent<AudioSource>();
            engineSource.loop = true;
            engineSource.playOnAwake = true;
            engineSource.volume = 0.45f;
            engineSource.pitch = minPitch;
            engineSource.spatialBlend = 0.5f;

            // Generate procedural continuous engine waveform
            int sampleRate = 44100;
            int samplesCount = sampleRate / 2; // 0.5 sec loop
            AudioClip clip = AudioClip.Create("EngineWave", samplesCount, 1, sampleRate, false);
            float[] data = new float[samplesCount];
            for (int i = 0; i < samplesCount; i++)
            {
                float t = (float)i / sampleRate;
                // Sawtooth + harmonic mixture for engine rumble
                float wave1 = (t * 80f % 1f) * 2f - 1f;
                float wave2 = Mathf.Sin(2f * Mathf.PI * 160f * t) * 0.5f;
                float wave3 = Mathf.Sin(2f * Mathf.PI * 40f * t) * 0.4f;
                data[i] = (wave1 + wave2 + wave3) * 0.3f;
            }
            clip.SetData(data, 0);
            engineSource.clip = clip;
            engineSource.Play();
        }

        private void SetupScreechAudio()
        {
            screechSource = gameObject.AddComponent<AudioSource>();
            screechSource.loop = true;
            screechSource.playOnAwake = false;
            screechSource.volume = 0f;
            screechSource.pitch = 1.1f;

            // Generate tire screech / white-noise tone
            int sampleRate = 44100;
            int samplesCount = sampleRate / 2;
            AudioClip clip = AudioClip.Create("ScreechWave", samplesCount, 1, sampleRate, false);
            float[] data = new float[samplesCount];
            System.Random rand = new System.Random(42);
            for (int i = 0; i < samplesCount; i++)
            {
                float noise = (float)(rand.NextDouble() * 2.0 - 1.0) * 0.2f;
                float tone = Mathf.Sin(2f * Mathf.PI * 1200f * (float)i / sampleRate) * 0.15f;
                data[i] = noise + tone;
            }
            clip.SetData(data, 0);
            screechSource.clip = clip;
            screechSource.Play();
        }

        private void Update()
        {
            if (car == null) return;

            float speedRatio = Mathf.Clamp01(car.CurrentSpeedKmh / maxSpeed);
            float throttleBoost = Mathf.Abs(car.ThrottleInput) * 0.3f;
            engineSource.pitch = Mathf.Lerp(minPitch, maxPitch, speedRatio + throttleBoost);
            engineSource.volume = Mathf.Lerp(0.25f, 0.6f, speedRatio + throttleBoost);

            // Screech when braking hard or sharp steering at speed
            bool isScreeching = (car.IsBraking && car.CurrentSpeedKmh > 15f) ||
                               (Mathf.Abs(car.SteerInput) > 0.6f && car.CurrentSpeedKmh > 45f);

            screechSource.volume = Mathf.Lerp(screechSource.volume, isScreeching ? 0.35f : 0f, Time.deltaTime * 10f);
        }
    }
}
