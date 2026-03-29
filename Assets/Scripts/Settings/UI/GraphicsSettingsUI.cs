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

        private void OnEnable()
        {
            if (SettingsManager.Instance == null) return;
            var data = SettingsManager.Instance.Current.graphics;

            PopulateResolutions(data);

            if (_fullscreenToggle != null)
            {
                _fullscreenToggle.isOn = data.fullScreen;
                _fullscreenToggle.onValueChanged.AddListener(val => data.fullScreen = val);
            }

            if (_vSyncToggle != null)
            {
                _vSyncToggle.isOn = data.vSync > 0;
                _vSyncToggle.onValueChanged.AddListener(val => data.vSync = val ? 1 : 0);
            }

            if (_fpsLimitDropdown != null)
            {
                // Simple hardcoded mapping for the example: 0=60, 1=120, 2=144, 3=240, 4=Uncapped(-1)
                _fpsLimitDropdown.value = MapFpsToIndex(data.fpsLimit);
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

        private void PopulateResolutions(GraphicsSettings data)
        {
            if (_resolutionDropdown == null) return;

            _systemResolutions = Screen.resolutions;
            _resolutionDropdown.ClearOptions();

            int currentResIndex = 0;
            List<string> options = new List<string>();

            for (int i = 0; i < _systemResolutions.Length; i++)
            {
                string option = $"{_systemResolutions[i].width} x {_systemResolutions[i].height} @ {_systemResolutions[i].refreshRateRatio.value:F0}Hz";
                options.Add(option);

                // If saved index matches OR we match current screen W/H in first setup
                if (data.resolutionIndex == i || 
                    (data.resolutionIndex == -1 && _systemResolutions[i].width == Screen.width && _systemResolutions[i].height == Screen.height))
                {
                    currentResIndex = i;
                }
            }

            _resolutionDropdown.AddOptions(options);
            _resolutionDropdown.value = currentResIndex;
            _resolutionDropdown.RefreshShownValue();

            _resolutionDropdown.onValueChanged.AddListener(idx => data.resolutionIndex = idx);
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
