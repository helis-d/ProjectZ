using UnityEngine;
using FishNet.Object;
using FishNet.Object.Prediction;
using FishNet.Transporting;
using ProjectZ.Settings;

namespace ProjectZ.Player
{
    // ─── CSP Structs ────────────────────────────────────────────────────────
    public struct MoveData : IReplicateData
    {
        public Vector2 Input;
        public bool WantsToSprint;
        public bool WantsToCrouch;
        public Vector3 Forward;
        public Vector3 Right;

        private uint _tick;
        public void Dispose() { }
        public uint GetTick() => _tick;
        public void SetTick(uint value) => _tick = value;
    }

    public struct ReconcileData : IReconcileData
    {
        public Vector3 Position;
        public float VerticalVelocity;
        public float CurrentHeight;

        private uint _tick;
        public void Dispose() { }
        public uint GetTick() => _tick;
        public void SetTick(uint value) => _tick = value;
    }

    /// <summary>
    /// Server-authoritative FPS movement using FishNet Client-Side Prediction (CSP).
    /// Uses native [Replicate] and [Reconcile] for zero-lag feeling but 100% server authority (anti-cheat).
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : NetworkBehaviour
    {
        // ─── GDD Speed Constants (units/s) ───────────────────────────────
        public const float WalkSpeed   = 250f;
        public const float SprintSpeed = 375f;
        public const float CrouchSpeed = 150f;

        [Header("Movement Settings")]
        [SerializeField] private float _gravity = -20f;
        [SerializeField] private float _groundCheckDistance = 0.1f;
        [SerializeField] private LayerMask _groundMask;

        [Header("Crouch Settings")]
        [SerializeField] private float _standHeight  = 1.8f;
        [SerializeField] private float _crouchHeight = 1.1f;
        [SerializeField] private float _crouchTransitionSpeed = 10f;

        // ─── Components ───────────────────────────────────────────────────
        private CharacterController _cc;
        private PlayerInputHandler  _input;

        // ─── State (Will be Reconciled) ───────────────────────────────────
        private float _verticalVelocity;
        private float _currentHeight;

        // ─── Input Caching (Toggles) ──────────────────────────────────────
        private bool _sprintToggledOn;
        private bool _crouchToggledOn;
        private bool _prevSprintInput;
        private bool _prevCrouchInput;

        public float CurrentSpeed { get; private set; }

        private void Awake()
        {
            _cc    = GetComponent<CharacterController>();
            _input = GetComponent<PlayerInputHandler>();

            _cc.height = _standHeight;
            _currentHeight = _standHeight;
        }

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            TimeManager.OnTick += TimeManager_OnTick;
        }

        public override void OnStopNetwork()
        {
            base.OnStopNetwork();
            if (TimeManager != null)
                TimeManager.OnTick -= TimeManager_OnTick;
        }

        private void TimeManager_OnTick()
        {
            if (IsOwner)
            {
                Reconciliation(default);
                CheckInput(out MoveData md);
                Move(md);
            }
            if (IsServerInitialized)
            {
                Move(default);
                // CreateReconcile() automatically handles the reconcile data creation now!
            }
        }

        public override void CreateReconcile()
        {
            // Server automatically creates and sends reconcile data.
            ReconcileData rd = new ReconcileData
            {
                Position = transform.position,
                VerticalVelocity = _verticalVelocity,
                CurrentHeight = _currentHeight
            };
            Reconciliation(rd);
        }

        // ─── Input Gathering ──────────────────────────────────────────────
        private void CheckInput(out MoveData md)
        {
            md = new MoveData
            {
                Input = Vector2.zero,
                WantsToSprint = false,
                WantsToCrouch = false,
                Forward = transform.forward,
                Right = transform.right
            };

            if (_input == null) return;

            // Handle Toggles
            bool isToggleCrouch = SettingsManager.Instance?.Current.gameplay.toggleCrouch ?? false;
            bool crouchInput = _input.IsCrouching;
            if (isToggleCrouch)
            {
                if (crouchInput && !_prevCrouchInput) _crouchToggledOn = !_crouchToggledOn;
                _prevCrouchInput = crouchInput;
                md.WantsToCrouch = _crouchToggledOn;
            }
            else
            {
                md.WantsToCrouch = crouchInput;
            }

            bool isToggleSprint = SettingsManager.Instance?.Current.gameplay.toggleSprint ?? false;
            bool sprintInput = _input.IsSprinting;
            if (isToggleSprint)
            {
                if (sprintInput && !_prevSprintInput) _sprintToggledOn = !_sprintToggledOn;
                _prevSprintInput = sprintInput;
                md.WantsToSprint = _sprintToggledOn;
            }
            else
            {
                md.WantsToSprint = sprintInput;
            }

            md.Input = _input.MoveInput;
        }

        // ─── REPLICATE (Movement Logic) ───────────────────────────────────
        [Replicate]
        private void Move(MoveData md, ReplicateState state = ReplicateState.Invalid, Channel channel = Channel.Unreliable)
        {
            // 0. a16z STANDARDI: Anti-Speedhack (Girdiyi Sınırla)
            md.Input = Vector2.ClampMagnitude(md.Input, 1.0f);

            // 1. Determine Target Speed
            bool isCrouching = _currentHeight < _standHeight - 0.1f;
            float targetSpeed;
            if (isCrouching)          targetSpeed = CrouchSpeed;
            else if (md.WantsToSprint)targetSpeed = WalkSpeed; // Normal is sprint, shift is walk
            else                      targetSpeed = SprintSpeed;

            float speed = targetSpeed * 0.01f;
            CurrentSpeed = speed;

            // 2. Horizontal Movement
            Vector3 move = md.Right * md.Input.x + md.Forward * md.Input.y;
            Vector3 horizontalVelocity = move * speed;

            // 3. Gravity & Jump (Ground check via CharacterController)
            bool isGrounded = _cc.isGrounded;
            if (isGrounded && _verticalVelocity < 0f)
                _verticalVelocity = -2f; // Stick to ground
                
            _verticalVelocity += _gravity * (float)TimeManager.TickDelta;

            // Combine velocities
            Vector3 finalVelocity = horizontalVelocity + (Vector3.up * _verticalVelocity);

            // 4. Handle Crouch Height
            float targetHeight = md.WantsToCrouch ? _crouchHeight : _standHeight;
            _currentHeight = Mathf.Lerp(_currentHeight, targetHeight, (float)TimeManager.TickDelta * _crouchTransitionSpeed);
            _cc.height = _currentHeight;

            // 5. Apply Move
            _cc.Move(finalVelocity * (float)TimeManager.TickDelta);
        }

        // ─── RECONCILE (Rubberbanding/State Correction) ───────────────────
        [Reconcile]
        private void Reconciliation(ReconcileData rd, Channel channel = Channel.Unreliable)
        {
            // If the server tells us our state is wrong, snap to the server's state.
            transform.position = rd.Position;
            _verticalVelocity  = rd.VerticalVelocity;
            _currentHeight     = rd.CurrentHeight;
            _cc.height         = rd.CurrentHeight;
        }

        // Update can be used for smooth visual interpolation if needed in the future
        // But the physics runs in TimeManager_OnTick for CSP.
    }
}
