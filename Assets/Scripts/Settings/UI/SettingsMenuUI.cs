using UnityEngine;

namespace ProjectZ.Settings.UI
{
    /// <summary>
    /// Master UI Controller for the Settings Menu (typically opened with ESC).
    /// Manages switching between tabs (Gameplay, Audio, Graphics, Controls)
    /// and invokes SaveSettings() when closing.
    /// </summary>
    public class SettingsMenuUI : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject _mainMenuPanel;
        
        [Header("Tabs")]
        [SerializeField] private GameObject _gameplayTab;
        [SerializeField] private GameObject _audioTab;
        [SerializeField] private GameObject _graphicsTab;
        [SerializeField] private GameObject _controlsTab;

        private void Update()
        {
            // Toggle menu with ESC
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                ToggleMenu();
            }
        }

        public void ToggleMenu()
        {
            if (_mainMenuPanel == null) return;
            
            bool isActive = _mainMenuPanel.activeSelf;
            
            if (isActive)
            {
                // Closing menu -> Save data
                CloseAndSave();
            }
            else
            {
                // Opening menu
                _mainMenuPanel.SetActive(true);
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                
                // Open default tab
                OpenTab(_gameplayTab);
            }
        }

        public void CloseAndSave()
        {
            if (SettingsManager.Instance != null)
            {
                SettingsManager.Instance.SaveSettings();
            }
            
            if (_mainMenuPanel != null) _mainMenuPanel.SetActive(false);
            
            // [FIX] BUG-09: Unconditional cursor lock
            // Only lock cursor if we're actually in the gameplay scene
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "SampleScene")
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        // ─── Tab Navigation Callbacks (Linked to UI Buttons) ───────────────

        public void ShowGameplayTab() => OpenTab(_gameplayTab);
        public void ShowAudioTab()    => OpenTab(_audioTab);
        public void ShowGraphicsTab() => OpenTab(_graphicsTab);
        public void ShowControlsTab() => OpenTab(_controlsTab);

        private void OpenTab(GameObject activeTab)
        {
            if (_gameplayTab != null) _gameplayTab.SetActive(_gameplayTab == activeTab);
            if (_audioTab != null)    _audioTab.SetActive(_audioTab == activeTab);
            if (_graphicsTab != null) _graphicsTab.SetActive(_graphicsTab == activeTab);
            if (_controlsTab != null) _controlsTab.SetActive(_controlsTab == activeTab);
        }
    }
}
