using UnityEngine;
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
        /// Pushes the saved graphics and strict engine rules (like FPS Limits) to Unity.
        /// </summary>
        public void ApplyGraphicsAndEngineSettings()
        {
            var gfx = Current.graphics;

            // Frame Rate
            QualitySettings.vSyncCount = gfx.vSync;
            Application.targetFrameRate = gfx.vSync == 0 ? gfx.fpsLimit : -1;

            // Screen
            FullScreenMode mode = gfx.fullScreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
            
            // If resolution index is provided and valid, apply it. Otherwise keep native.
            if (gfx.resolutionIndex >= 0 && gfx.resolutionIndex < Screen.resolutions.Length)
            {
                var targetRes = Screen.resolutions[gfx.resolutionIndex];
                Screen.SetResolution(targetRes.width, targetRes.height, mode);
            }
            else
            {
                Screen.fullScreenMode = mode; // Just apply windowed/fullscreen
            }
            
            // Audio Mixer volumes would ideally be set here via an AudioMixer reference
            // e.g. mixer.SetFloat("Master", Mathf.Log10(Current.audio.masterVolume) * 20);
        }
    }
}
