using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CarRush.UI
{
    /// <summary>
    /// Interactive settings panel controller for volume, sound effects, and graphics quality.
    /// Saves preferences automatically to PlayerPrefs.
    /// </summary>
    public class SettingsUI : MonoBehaviour
    {
        [Header("Audio Settings")]
        [SerializeField] private Slider volumeSlider;
        [SerializeField] private TextMeshProUGUI volumeValueText;
        [SerializeField] private Toggle sfxToggle;

        [Header("Graphics Settings")]
        [SerializeField] private TMP_Dropdown qualityDropdown;

        [Header("Close Button")]
        [SerializeField] private Button closeButton;

        private const string VolumePrefKey = "CarRush.MasterVolume";
        private const string SfxPrefKey = "CarRush.SfxEnabled";
        private const string QualityPrefKey = "CarRush.QualityLevel";

        private void Awake()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(CloseSettings);

            if (volumeSlider != null)
                volumeSlider.onValueChanged.AddListener(SetVolume);

            if (sfxToggle != null)
                sfxToggle.onValueChanged.AddListener(SetSfx);

            if (qualityDropdown != null)
                qualityDropdown.onValueChanged.AddListener(SetQuality);
        }

        private void OnEnable()
        {
            LoadSavedSettings();
        }

        private void LoadSavedSettings()
        {
            // Volume
            float savedVol = PlayerPrefs.GetFloat(VolumePrefKey, 0.8f);
            if (volumeSlider != null)
            {
                volumeSlider.value = savedVol;
                UpdateVolumeLabel(savedVol);
            }
            AudioListener.volume = savedVol;

            // SFX Toggle
            bool sfxOn = PlayerPrefs.GetInt(SfxPrefKey, 1) == 1;
            if (sfxToggle != null)
                sfxToggle.isOn = sfxOn;

            // Quality
            int savedQuality = PlayerPrefs.GetInt(QualityPrefKey, QualitySettings.GetQualityLevel());
            if (qualityDropdown != null)
                qualityDropdown.value = savedQuality;
        }

        public void SetVolume(float volume)
        {
            AudioListener.volume = volume;
            PlayerPrefs.SetFloat(VolumePrefKey, volume);
            PlayerPrefs.Save();
            UpdateVolumeLabel(volume);
        }

        private void UpdateVolumeLabel(float volume)
        {
            if (volumeValueText != null)
                volumeValueText.text = $"{Mathf.RoundToInt(volume * 100)}%";
        }

        public void SetSfx(bool enabled)
        {
            PlayerPrefs.SetInt(SfxPrefKey, enabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void SetQuality(int level)
        {
            QualitySettings.SetQualityLevel(level, true);
            PlayerPrefs.SetInt(QualityPrefKey, level);
            PlayerPrefs.Save();
        }

        public void CloseSettings()
        {
            gameObject.SetActive(false);
        }
    }
}
