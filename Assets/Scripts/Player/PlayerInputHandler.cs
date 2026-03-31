using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectZ.Player
{
    /// <summary>
    /// Reads raw input from the Unity Input System and exposes
    /// clean, frame-stable values for other player components.
    /// Attach this alongside the generated InputSystem_Actions class.
    /// </summary>
    [RequireComponent(typeof(PlayerInput))]
    public class PlayerInputHandler : MonoBehaviour
    {
        // ─── Exposed Input State ──────────────────────────────────────────
        /// <summary>WASD / left-stick movement in local X/Z space.</summary>
        public Vector2 MoveInput   { get; private set; }

        /// <summary>Mouse delta / right-stick aiming delta.</summary>
        public Vector2 LookInput   { get; private set; }

        /// <summary>True while the sprint key is held.</summary>
        public bool IsSprinting    { get; private set; }

        /// <summary>True while the crouch key is held.</summary>
        public bool IsCrouching    { get; private set; }

        /// <summary>True on the frame the primary fire button is pressed.</summary>
        public bool FirePressed    { get; private set; }

        /// <summary>True while the primary fire button is held.</summary>
        public bool FireHeld       { get; private set; }

        /// <summary>True on the frame the reload key is pressed.</summary>
        public bool ReloadPressed  { get; private set; }

        /// <summary>True while the interact key is held.</summary>
        public bool InteractHeld   { get; private set; }

        /// <summary>True on the frame the jump key is pressed.</summary>
        public bool JumpPressed    { get; private set; }

        /// <summary>True on the frame the Drop (G) key is pressed.</summary>
        public bool DropPressed    { get; private set; }

        /// <summary>True on the frame the Ultimate (X) key is pressed.</summary>
        public bool UltimatePressed { get; private set; }

        /// <summary>True on the frame a slot key (1, 2, 3) is pressed.</summary>
        public int SlotAlphaPressed { get; private set; } = -1;

        // ─── Internals ────────────────────────────────────────────────────
        private InputSystem_Actions _actions;

        private void Awake()
        {
            _actions = new InputSystem_Actions();
        }

        private void OnEnable()
        {
            _actions.Enable();
        }

        private void OnDisable()
        {
            _actions.Disable();
        }

        private void Update()
        {
            // Read per-frame values
            MoveInput     = _actions.Player.Move.ReadValue<Vector2>();
            LookInput     = _actions.Player.Look.ReadValue<Vector2>();
            IsSprinting   = _actions.Player.Sprint.IsPressed();
            IsCrouching   = _actions.Player.Crouch.IsPressed();
            FirePressed   = _actions.Player.Attack.WasPressedThisFrame();
            FireHeld      = _actions.Player.Attack.IsPressed();
            ReloadPressed = _actions.Player.Reload.WasPressedThisFrame();
            InteractHeld  = _actions.Player.Interact.IsPressed();
            JumpPressed   = _actions.Player.Jump.WasPressedThisFrame();

            // Reset prototype-only one-frame fallback flags
            DropPressed = false;
            UltimatePressed = false;
            SlotAlphaPressed = -1;

            // Manual Keyboard Fallback for specific unmapped actions in prototype:
            if (Keyboard.current != null)
            {
                if (Keyboard.current.gKey.wasPressedThisFrame) DropPressed = true;
                if (Keyboard.current.xKey.wasPressedThisFrame) UltimatePressed = true; // User requested X instead of Y
                if (Keyboard.current.digit1Key.wasPressedThisFrame) SlotAlphaPressed = 1;
                if (Keyboard.current.digit2Key.wasPressedThisFrame) SlotAlphaPressed = 2;
                if (Keyboard.current.digit3Key.wasPressedThisFrame) SlotAlphaPressed = 3;
            }
        }
    }
}
