using System;

namespace ProjectZ.Settings
{
    [Serializable]
    public class SettingsData
    {
        public GameplaySettings gameplay = new GameplaySettings();
        public AudioSettings audio = new AudioSettings();
        public GraphicsSettings graphics = new GraphicsSettings();
        public ControlsSettings controls = new ControlsSettings();
        public InterfaceSettings ui = new InterfaceSettings();
    }

    [Serializable]
    public class GameplaySettings
    {
        public float mouseSensitivity = 1.0f;
        public float adsSensitivity = 0.8f;
        public bool invertY = false;
        public bool autoReload = true;
        
        // true = toggle (bas-çek), false = hold (basılı tut)
        public bool toggleSprint = false; 
        public bool toggleCrouch = false;
        
        public int crosshairType = 0; // 0=Dot, 1=Cross, 2=Dynamic
        public string crosshairColorHex = "#00FF00"; // Green
        
        public bool showHitmarkers = true;
        public bool showDamageNumbers = false; // Usually False in comp FPS
    }

    [Serializable]
    public class AudioSettings
    {
        public float masterVolume = 1.0f; // 0.0 to 1.0
        public float musicVolume = 0.5f;
        public float sfxVolume = 1.0f;
        public float voiceChatVolume = 0.8f;
        public float footstepVolume = 1.0f;
        public float uiVolume = 0.7f;
        
        // Multiplayer specifics
        public bool pushToTalk = true;
        public string microphoneInputDevice = "Default";
    }

    [Serializable]
    public class GraphicsSettings
    {
        public int resolutionIndex = -1; // -1 means native/max
        public bool fullScreen = true;
        public int vSync = 0; // 0=Off, 1=Every VBlank
        public int fpsLimit = 144;
        
        public int textureQuality = 2; // e.g. 0=Low, 1=Med, 2=High
        public int shadowQuality = 2;
        public int antiAliasing = 2; // e.g. 0=Off, 1=FXAA, 2=SMAA/TAA
        public bool motionBlur = false;
        
        public float fieldOfView = 90f; // Normal range 80-105
    }

    [Serializable]
    public class ControlsSettings
    {
        // For actual keybinding, we rely on Unity's new InputSystem rebind logic.
        // This is a placeholder for custom overrides if needed via JSON.
        // e.g. "MoveForward": "<Keyboard>/w"
        
        public float controllerAimSensitivityX = 50f;
        public float controllerAimSensitivityY = 50f;
        public bool enableControllerAimAssist = false;
    }

    [Serializable]
    public class InterfaceSettings
    {
        public float hudScale = 1.0f;
        public float minimapScale = 1.0f;
        public bool showKillfeed = true;
        public bool showDamageIndicator = true;
        public bool showSubtitles = false;
    }
}
