using UnityEngine;
using UnityEngine.InputSystem;
using FishNet.Object;

namespace ProjectZ.Player
{
    /// <summary>
    /// FPS camera controller with head bob.
    /// Attaches the Main Camera to the player's eye level, handles mouse look,
    /// and adds a walking/running head bob effect.
    /// </summary>
    public class PlayerCamera : NetworkBehaviour
    {
        [Header("Mouse Settings")]
        [SerializeField] private float _mouseSensitivity = 2f;
        [SerializeField] private Vector3 _cameraOffset = new Vector3(0f, 1.6f, 0f);

        [Header("Head Bob - Yürüme Sallanması")]
        [SerializeField] private float _bobFrequency = 10f;
        [SerializeField] private float _bobVerticalAmount = 0.05f;
        [SerializeField] private float _bobHorizontalAmount = 0.03f;
        [SerializeField] private float _sprintBobMultiplier = 1.6f;

        private Transform _cameraTransform;
        private float _xRotation;
        private PlayerInputHandler _input;
        private PlayerMovement _movement;
        private float _bobTimer;
        private float _defaultYPos;

        public override void OnStartClient()
        {
            base.OnStartClient();

            if (!IsOwner)
            {
                enabled = false;
                return;
            }

            _input = GetComponent<PlayerInputHandler>();
            _movement = GetComponent<PlayerMovement>();

            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                _cameraTransform = mainCam.transform;
                _cameraTransform.SetParent(transform);
                _cameraTransform.localPosition = _cameraOffset;
                _cameraTransform.localRotation = Quaternion.identity;
                _defaultYPos = _cameraOffset.y;
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            HideOwnModel();
        }

        private void HideOwnModel()
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
            }
        }

        private void LateUpdate()
        {
            if (!IsOwner || _cameraTransform == null) return;

            HandleMouseLook();
            HandleHeadBob();
        }

        private void HandleMouseLook()
        {
            Vector2 lookDelta = Vector2.zero;
            if (_input != null)
            {
                lookDelta = _input.LookInput;
            }
            else if (Mouse.current != null)
            {
                lookDelta = Mouse.current.delta.ReadValue();
            }

            float mouseX = lookDelta.x * _mouseSensitivity * 0.1f;
            float mouseY = lookDelta.y * _mouseSensitivity * 0.1f;

            _xRotation -= mouseY;
            _xRotation = Mathf.Clamp(_xRotation, -89f, 89f);
            _cameraTransform.localRotation = Quaternion.Euler(_xRotation, 0f, 0f);

            transform.Rotate(Vector3.up * mouseX);
        }

        private void HandleHeadBob()
        {
            if (_input == null) return;

            float speed = _input.MoveInput.magnitude;

            if (speed > 0.1f)
            {
                // Koşma kontrolü
                bool isSprinting = _movement != null && _movement.CurrentSpeed > 3f;
                float mult = isSprinting ? _sprintBobMultiplier : 1f;

                _bobTimer += Time.deltaTime * _bobFrequency * mult;

                // Dikey sallanma (yukarı-aşağı)
                float bobY = Mathf.Sin(_bobTimer) * _bobVerticalAmount * mult;
                // Yatay sallanma (sağa-sola)
                float bobX = Mathf.Cos(_bobTimer * 0.5f) * _bobHorizontalAmount * mult;

                Vector3 targetPos = new Vector3(
                    _cameraOffset.x + bobX,
                    _defaultYPos + bobY,
                    _cameraOffset.z
                );

                _cameraTransform.localPosition = Vector3.Lerp(
                    _cameraTransform.localPosition, targetPos, Time.deltaTime * 15f);
            }
            else
            {
                // Duruyorken kamerayı sabit pozisyona döndür
                _bobTimer = 0f;
                _cameraTransform.localPosition = Vector3.Lerp(
                    _cameraTransform.localPosition, _cameraOffset, Time.deltaTime * 10f);
            }
        }

        private void OnDestroy()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
