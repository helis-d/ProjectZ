using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ProjectZ.Settings.UI
{
    /// <summary>
    /// Binds UI Dropdowns and Toggles to SettingsData.graphics.
    /// Manages Resolution enumeration dynamically.
    /// </summary>
    public class GraphicsSettingsUI : MonoBehaviour
    {
        [Header("Display")]
        [SerializeField] private TMP_Dropdown _resolutionDropdown;
        [SerializeField] private Toggle _fullscreenToggle;

        [Header("Performance")]
        [SerializeField] private TMP_Dropdown _fpsLimitDropdown; // Options: 60, 120, 144, 240, Unlimited
        [SerializeField] private Toggle _vSyncToggle;

        private Resolution[] _systemResolutions;
        private readonly System.Collections.Generic.List<string> _resolutionOptions = new(); // [FIX] BUG-27: class field, cleared not re-allocated

        private void OnEnable()
        {
            if (SettingsManager.Instance == null) return;

            PopulateResolutions();

            if (_fullscreenToggle != null)
            {
                _fullscreenToggle.isOn = SettingsManager.Instance.Current.graphics.fullScreen;
                // [FIX] BUG-08: capture manager ref, not struct copy — mutations reach the real object
                _fullscreenToggle.onValueChanged.AddListener(val =>
                    SettingsManager.Instance.Current.graphics.fullScreen = val);
            }

            if (_vSyncToggle != null)
            {
                _vSyncToggle.isOn = SettingsManager.Instance.Current.graphics.vSync > 0;
                _vSyncToggle.onValueChanged.AddListener(val => // [FIX] BUG-08
                    SettingsManager.Instance.Current.graphics.vSync = val ? 1 : 0);
            }

            if (_fpsLimitDropdown != null)
            {
                _fpsLimitDropdown.value = MapFpsToIndex(SettingsManager.Instance.Current.graphics.fpsLimit);
                _fpsLimitDropdown.onValueChanged.AddListener(OnFpsChanged);
            }
        }

        private void OnDisable()
        {
            if (_resolutionDropdown != null) _resolutionDropdown.onValueChanged.RemoveAllListeners();
            if (_fullscreenToggle != null) _fullscreenToggle.onValueChanged.RemoveAllListeners();
            if (_vSyncToggle != null) _vSyncToggle.onValueChanged.RemoveAllListeners();
            if (_fpsLimitDropdown != null) _fpsLimitDropdown.onValueChanged.RemoveAllListeners();
        }

        private void PopulateResolutions()
        {
            if (_resolutionDropdown == null) return;

            _systemResolutions = Screen.resolutions;
            _resolutionDropdown.ClearOptions();

            int currentResIndex = 0;
            _resolutionOptions.Clear(); // [FIX] BUG-27: reuse field, no allocation
            int savedIndex = SettingsManager.Instance.Current.graphics.resolutionIndex;

            for (int i = 0; i < _systemResolutions.Length; i++)
            {
                string option = $"{_systemResolutions[i].width} x {_systemResolutions[i].height} @ {_systemResolutions[i].refreshRateRatio.value:F0}Hz";
                _resolutionOptions.Add(option);

                // If saved index matches OR we match current screen W/H in first setup
                if (savedIndex == i ||
                    (savedIndex == -1 && _systemResolutions[i].width == Screen.width && _systemResolutions[i].height == Screen.height))
                {
                    currentResIndex = i;
                }
            }

            _resolutionDropdown.AddOptions(_resolutionOptions);
            _resolutionDropdown.value = currentResIndex;
            _resolutionDropdown.RefreshShownValue();

            // [FIX] BUG-08: capture manager directly, not a struct copy
            _resolutionDropdown.onValueChanged.AddListener(idx =>
                SettingsManager.Instance.Current.graphics.resolutionIndex = idx);
        }

        private void OnFpsChanged(int index)
        {
            int fps = index switch
            {
                0 => 60,
                1 => 120,
                2 => 144,
                3 => 240,
                4 => -1, // Unlimited
                _ => 144
            };
            SettingsManager.Instance.Current.graphics.fpsLimit = fps;
        }

        private int MapFpsToIndex(int fps) => fps switch
        {
            60 => 0,
            120 => 1,
            144 => 2,
            240 => 3,
            -1 => 4,
            _ => 2
        };
    }
}
