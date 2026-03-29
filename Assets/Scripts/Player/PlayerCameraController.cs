using UnityEngine;
using ProjectZ.Settings;

namespace ProjectZ.Player
{
    /// <summary>
    /// First-person camera controller.
    /// The camera root transforms with the body; vertical look is applied only
    /// to the camera pivot child so the body doesn't tilt.
    /// </summary>
    public class PlayerCameraController : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The child GameObject whose X rotation drives vertical look.")]
        [SerializeField] private Transform _cameraPivot;

        [Header("Settings")]
        [SerializeField] private float _mouseSensitivity = 0.15f;
        [SerializeField] private float _verticalClamp    = 89f;
        [SerializeField] private bool  _invertY          = false;

        // ─── State ─────────────────────────────────────────────────────────
        private float _xRotation; // accumulated vertical angle
        private PlayerInputHandler _input;

        private void Awake()
        {
            _input = GetComponentInParent<PlayerInputHandler>();
            if (_input == null)
                _input = GetComponent<PlayerInputHandler>();

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;
        }

        private void Update()
        {
            ApplyLook();
        }

        // ─── Public ────────────────────────────────────────────────────────
        /// <summary>Override sensitivity at runtime (e.g. from settings menu).</summary>
        public void SetSensitivity(float value) => _mouseSensitivity = Mathf.Clamp(value, 0.01f, 5f);

        // ─── Private ───────────────────────────────────────────────────────
        private void ApplyLook()
        {
            float sensitivity = SettingsManager.Instance?.Current.gameplay.mouseSensitivity ?? _mouseSensitivity;
            bool invertY = SettingsManager.Instance?.Current.gameplay.invertY ?? _invertY;

            Vector2 look = _input.LookInput * sensitivity;

            // Horizontal: rotate the entire player body
            transform.parent.Rotate(Vector3.up * look.x);

            // Vertical: rotate only the camera pivot
            float verticalDelta = invertY ? -look.y : look.y; // Notice normal is look.y, invert is -look.y based on usual conventions
            _xRotation = Mathf.Clamp(_xRotation - verticalDelta, -_verticalClamp, _verticalClamp);

            if (_cameraPivot != null)
                _cameraPivot.localRotation = Quaternion.Euler(_xRotation, 0f, 0f);
        }
    }
}
