using FishNet.Object;
using FishNet.Object.Prediction;
using FishNet.Transporting;
using ProjectZ.Core;
using UnityEngine;

namespace ProjectZ.Player
{
    /// <summary>
    /// FishNet NetworkBehaviour that drives the networked player.
    /// Implements client-side prediction and server reconciliation.
    ///
    /// Architecture:
    ///   - Owner sends input each tick via ServerRpc.
    ///   - Server moves the player and broadcasts state via ObserverRpc.
    ///   - Non-owners interpolate received positions.
    ///   - Owner predicts locally without waiting for server confirmation.
    /// </summary>
    [RequireComponent(typeof(PlayerMovement))]
    [RequireComponent(typeof(PlayerInputHandler))]
    [RequireComponent(typeof(PlayerCombatController))]
    public class NetworkPlayerController : NetworkBehaviour
    {
        [Header("Reconciliation")]
        [Tooltip("Position error threshold (metres) before a reconciliation correction is applied.")]
        [SerializeField] private float _reconcileThreshold = 0.05f;

        [Header("Interpolation (non-owner)")]
        [SerializeField] private float _interpolationSpeed = 25f;

        // â”€â”€â”€ Components â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        private PlayerMovement    _movement;
        private PlayerInputHandler _input;

        // â”€â”€â”€ State â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        private Vector3    _serverPosition;
        private Quaternion _serverRotation;
        private bool       _hasReceivedState;

        // â”€â”€â”€ Prediction Data Structs â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        public struct InputData
        {
            public Vector2 MoveInput;
            public Vector2 LookInput;
            public bool    IsSprinting;
            public bool    IsCrouching;
        }

        public struct ReconcileData
        {
            public Vector3    Position;
            public Quaternion Rotation;
        }

        // â”€â”€â”€ Unity / FishNet â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        private void Awake()
        {
            _movement = GetComponent<PlayerMovement>();
            _input    = GetComponent<PlayerInputHandler>();
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            // Disable local components for non-owners
            if (!IsOwner)
            {
                _movement.enabled = false;
                _input.enabled    = false;
                if (TryGetComponent<PlayerCameraController>(out var cam))
                    cam.enabled = false;
            }
        }

        private void Update()
        {
            // Non-owner interpolation
            if (!IsOwner && _hasReceivedState)
            {
                transform.position = Vector3.Lerp(
                    transform.position, _serverPosition,
                    Time.deltaTime * _interpolationSpeed);

                transform.rotation = Quaternion.Slerp(
                    transform.rotation, _serverRotation,
                    Time.deltaTime * _interpolationSpeed);
            }
        }

        // Called every network tick for the owner (set tick rate in FishNet settings)
        private void FixedUpdate()
        {
            if (!IsOwner) return;
            SendInputToServer();
        }

        // â”€â”€â”€ RPC Methods â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        /// <summary>Owner â†’ Server: send this tick's input.</summary>
        [ServerRpc(RunLocally = false)]
        private void SendInputToServer(InputData data = default,
                                       Channel channel = Channel.Unreliable)
        {
            // Server applies movement
            MoveWithInput(data);

            // Broadcast corrected state to all observers
            BroadcastState(new ReconcileData
            {
                Position = transform.position,
                Rotation = transform.rotation
            });
        }

        /// <summary>Server â†’ All clients: authoritative position update.</summary>
        [ObserversRpc(RunLocally = false)]
        private void BroadcastState(ReconcileData state, Channel channel = Channel.Unreliable)
        {
            _serverPosition   = state.Position;
            _serverRotation   = state.Rotation;
            _hasReceivedState = true;

            // Owner: reconcile if drift is too large
            if (IsOwner)
            {
                float error = Vector3.Distance(transform.position, state.Position);
                if (error > _reconcileThreshold)
                {
                    transform.position = state.Position;
                    Debug.Log($"[Network] Reconciled position â€” error: {error:F3}m");
                }
            }
        }

        // â”€â”€â”€ Helpers â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        private void SendInputToServer()
        {
            InputData data = new InputData
            {
                MoveInput  = _input.MoveInput,
                LookInput  = _input.LookInput,
                IsSprinting = _input.IsSprinting,
                IsCrouching = _input.IsCrouching
            };

            SendInputToServer(data);
        }

        private void MoveWithInput(InputData data)
        {
            // Server-side authoritative movement replay using the same
            // speed constants as PlayerMovement (GDD Section 1).
            CharacterController cc = GetComponent<CharacterController>();
            if (cc == null) return;

            // Determine target speed (inverted sprint: normal = sprint, shift = walk)
            float targetSpeed;
            if (data.IsCrouching)
                targetSpeed = 150f; // CrouchSpeed
            else if (data.IsSprinting)
                targetSpeed = 250f; // WalkSpeed (Shift = silent walk)
            else
                targetSpeed = 375f; // SprintSpeed (Normal = run)

            // Convert cm/s to m/s (GDD uses cm, Unity uses metres)
            float speed = targetSpeed * 0.01f;

            Vector3 move = transform.right * data.MoveInput.x + transform.forward * data.MoveInput.y;
            cc.Move(move * speed * Time.fixedDeltaTime);
        }
    }
}

