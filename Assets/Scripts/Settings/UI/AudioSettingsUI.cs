using UnityEngine;
using UnityEngine.UI;

namespace ProjectZ.Settings.UI
{
    /// <summary>
    /// Binds audio sliders to SettingsData.audio.
    /// In a full implementation, these would also push to an AudioMixer instantly.
    /// </summary>
    public class AudioSettingsUI : MonoBehaviour
    {
        [Header("Volumes")]
        [SerializeField] private Slider _masterSlider;
        [SerializeField] private Slider _musicSlider;
        [SerializeField] private Slider _sfxSlider;
        [SerializeField] private Slider _voiceChatSlider;

        private void OnEnable()
        {
            if (SettingsManager.Instance == null) return;

            // [FIX] same struct-capture bug as BUG-08: capture manager, not a struct copy
            if (_masterSlider != null)
            {
                _masterSlider.value = SettingsManager.Instance.Current.audio.masterVolume;
                _masterSlider.onValueChanged.AddListener(val =>
                    SettingsManager.Instance.Current.audio.masterVolume = val);
            }
            if (_musicSlider != null)
            {
                _musicSlider.value = SettingsManager.Instance.Current.audio.musicVolume;
                _musicSlider.onValueChanged.AddListener(val =>
                    SettingsManager.Instance.Current.audio.musicVolume = val);
            }
            if (_sfxSlider != null)
            {
                _sfxSlider.value = SettingsManager.Instance.Current.audio.sfxVolume;
                _sfxSlider.onValueChanged.AddListener(val =>
                    SettingsManager.Instance.Current.audio.sfxVolume = val);
            }
            if (_voiceChatSlider != null)
            {
                _voiceChatSlider.value = SettingsManager.Instance.Current.audio.voiceChatVolume;
                _voiceChatSlider.onValueChanged.AddListener(val =>
                    SettingsManager.Instance.Current.audio.voiceChatVolume = val);
            }
        }

        private void OnDisable()
        {
            if (_masterSlider != null) _masterSlider.onValueChanged.RemoveAllListeners();
            if (_musicSlider != null) _musicSlider.onValueChanged.RemoveAllListeners();
            if (_sfxSlider != null) _sfxSlider.onValueChanged.RemoveAllListeners();
            if (_voiceChatSlider != null) _voiceChatSlider.onValueChanged.RemoveAllListeners();
        }
    }
}
