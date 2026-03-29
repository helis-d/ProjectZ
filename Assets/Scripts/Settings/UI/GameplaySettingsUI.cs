using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ProjectZ.Settings.UI
{
    /// <summary>
    /// Binds UI Sliders and Toggles to SettingsData.gameplay
    /// </summary>
    public class GameplaySettingsUI : MonoBehaviour
    {
        [Header("Mouse Sensing")]
        [SerializeField] private Slider _mouseSensSlider;
        [SerializeField] private TextMeshProUGUI _mouseSensText;
        [SerializeField] private Toggle _invertYToggle;

        [Header("Movement Options")]
        [SerializeField] private Toggle _toggleSprintToggle;
        [SerializeField] private Toggle _toggleCrouchToggle;

        private void OnEnable()
        {
            if (SettingsManager.Instance == null) return;
            
            var data = SettingsManager.Instance.Current.gameplay;

            // Load values to UI
            if (_mouseSensSlider != null)
            {
                _mouseSensSlider.value = data.mouseSensitivity;
                _mouseSensSlider.onValueChanged.AddListener(OnSensChanged);
                if (_mouseSensText != null) _mouseSensText.text = data.mouseSensitivity.ToString("0.00");
            }

            if (_invertYToggle != null)
            {
                _invertYToggle.isOn = data.invertY;
                _invertYToggle.onValueChanged.AddListener(val => data.invertY = val);
            }

            if (_toggleSprintToggle != null)
            {
                _toggleSprintToggle.isOn = data.toggleSprint;
                _toggleSprintToggle.onValueChanged.AddListener(val => data.toggleSprint = val);
            }

            if (_toggleCrouchToggle != null)
            {
                _toggleCrouchToggle.isOn = data.toggleCrouch;
                _toggleCrouchToggle.onValueChanged.AddListener(val => data.toggleCrouch = val);
            }
        }

        private void OnDisable()
        {
            if (_mouseSensSlider != null) _mouseSensSlider.onValueChanged.RemoveAllListeners();
            if (_invertYToggle != null) _invertYToggle.onValueChanged.RemoveAllListeners();
            if (_toggleSprintToggle != null) _toggleSprintToggle.onValueChanged.RemoveAllListeners();
            if (_toggleCrouchToggle != null) _toggleCrouchToggle.onValueChanged.RemoveAllListeners();
        }

        private void OnSensChanged(float value)
        {
            SettingsManager.Instance.Current.gameplay.mouseSensitivity = value;
            if (_mouseSensText != null) _mouseSensText.text = value.ToString("0.00");
        }
    }
}
