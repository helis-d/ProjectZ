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
            var data = SettingsManager.Instance.Current.audio;

            if (_masterSlider != null)
            {
                _masterSlider.value = data.masterVolume;
                _masterSlider.onValueChanged.AddListener(val => data.masterVolume = val);
            }
            if (_musicSlider != null)
            {
                _musicSlider.value = data.musicVolume;
                _musicSlider.onValueChanged.AddListener(val => data.musicVolume = val);
            }
            if (_sfxSlider != null)
            {
                _sfxSlider.value = data.sfxVolume;
                _sfxSlider.onValueChanged.AddListener(val => data.sfxVolume = val);
            }
            if (_voiceChatSlider != null)
            {
                _voiceChatSlider.value = data.voiceChatVolume;
                _voiceChatSlider.onValueChanged.AddListener(val => data.voiceChatVolume = val);
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
