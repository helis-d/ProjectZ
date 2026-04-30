using UnityEngine;
using UnityEngine.Audio;
using System.IO;

namespace ProjectZ.Settings
{
    /// <summary>
    /// Singleton Manager tracking all game settings.
    /// Saves to and loads from device JSON automatically.
    /// Provides an event for decoupled systems to listen to setting changes.
    /// </summary>
    public class SettingsManager : MonoBehaviour
    {
        public static SettingsManager Instance { get; private set; }

        public SettingsData Current { get; private set; }

        private string _savePath;

        /// <summary>
        /// Optional AudioMixer reference. Assign the project's Master AudioMixer in the Inspector.
        /// When assigned, SaveSettings/ApplyGraphicsAndEngineSettings will push all volume
        /// parameters using the standard Unity decibel formula:
        ///   dB = Mathf.Log10(normalizedVolume) * 20
        /// Exposed parameter names must match: "Master", "Music", "SFX", "VoiceChat", "Footstep", "UI".
        /// </summary>
        [Header("Audio Mixer (Optional)")]
        [SerializeField] private AudioMixer _audioMixer;

        public delegate void OnSettingsAppliedHandler();
        public event OnSettingsAppliedHandler OnSettingsApplied;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            _savePath = Path.Combine(Application.persistentDataPath, "UserPreferences.json");
            LoadSettings();
        }

        private void Start()
        {
            ApplyGraphicsAndEngineSettings();
        }

        public void LoadSettings()
        {
            if (File.Exists(_savePath))
            {
                try
                {
                    string json = File.ReadAllText(_savePath);
                    Current = JsonUtility.FromJson<SettingsData>(json);
                    Debug.Log("[Settings] Load successful from JSON.");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[Settings] Failed to parse JSON: {e.Message}");
                    Current = new SettingsData(); // Fallback to defaults
                }
            }
            else
            {
                Debug.Log("[Settings] No save found. Creating defaults.");
                Current = new SettingsData(); // First time play
            }
        }

        public void SaveSettings()
        {
            if (Current == null) return;
            try
            {
                string json = JsonUtility.ToJson(Current, true);
                File.WriteAllText(_savePath, json);
                Debug.Log($"[Settings] Saved to {_savePath}");
                
                ApplyGraphicsAndEngineSettings();
                OnSettingsApplied?.Invoke(); // Alert listeners
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Settings] Failed to save JSON: {e.Message}");
            }
        }

        /// <summary>
        /// Pushes the saved graphics / engine settings and audio mixer volumes to Unity.
        /// </summary>
        public void ApplyGraphicsAndEngineSettings()
        {
            var gfx = Current.graphics;

            // Frame Rate
            QualitySettings.vSyncCount   = gfx.vSync;
            Application.targetFrameRate  = gfx.vSync == 0 ? gfx.fpsLimit : -1;

            // Screen
            FullScreenMode mode = gfx.fullScreen
                ? FullScreenMode.FullScreenWindow
                : FullScreenMode.Windowed;

            if (gfx.resolutionIndex >= 0 && gfx.resolutionIndex < Screen.resolutions.Length)
            {
                var targetRes = Screen.resolutions[gfx.resolutionIndex];
                Screen.SetResolution(targetRes.width, targetRes.height, mode);
            }
            else
            {
                Screen.fullScreenMode = mode;
            }

            // Audio Mixer volumes
            ApplyAudioMixerVolumes();
        }

        /// <summary>
        /// Pushes all audio volume fields from SettingsData.audio to the AudioMixer.
        /// Safe no-op when _audioMixer is not assigned.
        /// </summary>
        private void ApplyAudioMixerVolumes()
        {
            if (_audioMixer == null)
                return;

            var audio = Current.audio;

            SetMixerVolume("Master",    audio.masterVolume);
            SetMixerVolume("Music",     audio.musicVolume);
            SetMixerVolume("SFX",       audio.sfxVolume);
            SetMixerVolume("VoiceChat", audio.voiceChatVolume);
            SetMixerVolume("Footstep",  audio.footstepVolume);
            SetMixerVolume("UI",        audio.uiVolume);
        }

        /// <summary>
        /// Converts a linear [0,1] volume to decibels and sets the named AudioMixer parameter.
        /// Clamps to -80 dB when the linear value is zero to avoid log(0).
        /// </summary>
        private void SetMixerVolume(string parameterName, float linearVolume)
        {
            float db = linearVolume > 0.0001f
                ? Mathf.Log10(linearVolume) * 20f
                : -80f;

            if (!_audioMixer.SetFloat(parameterName, db))
                Debug.LogWarning($"[Settings] AudioMixer parameter '{parameterName}' not found.");
        }
    }
}
