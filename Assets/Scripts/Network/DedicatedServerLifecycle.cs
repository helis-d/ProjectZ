using FishNet.Object;
using FishNet.Managing.Server;
using ProjectZ.Core;
using System.Collections;
using UnityEngine;

namespace ProjectZ.Network
{
    /// <summary>
    /// Headless dedicated-server lifecycle manager.
    ///
    /// Integrates with the Agones game-server orchestration framework:
    ///   1. OnStartServer  → Agones.Ready()   (accepting connections)
    ///   2. First player   → Agones.Allocate() (match locked; no scale-down)
    ///   3. Match end      → Agones.Shutdown() (container recycled)
    ///
    /// Uses <see cref="AgonesSDKFactory"/> so the real Agones sidecar is not
    /// required locally or in CI — the Mock implementation is used automatically
    /// when the AGONES_SDK_ENDPOINT environment variable is absent.
    ///
    /// Runs only in -batchmode (Linux Headless Server build).
    /// </summary>
    public class DedicatedServerLifecycle : NetworkBehaviour
    {
        [Header("Agones Health")]
        [SerializeField, Tooltip("Interval in seconds between Agones health pings. Must be < 5s.")]
        private float _healthPingInterval = 2f;

        // ─── Internal State ───────────────────────────────────────────────────
        private ServerManager  _serverManager;
        private IAgonesSDK     _agones;
        private Coroutine      _healthRoutine;
        private bool           _allocated;

        // ─────────────────────────────────────────────────────────────────────
        #region Unity / FishNet Lifecycle

        private void Start()
        {
            if (!Application.isBatchMode)
            {
                // Client build — this component is for dedicated servers only.
                Destroy(gameObject);
                return;
            }

            _agones = AgonesSDKFactory.Create();
            Debug.Log("[Dedicated] Headless server starting — Agones SDK acquired.");

            _serverManager = GameNetworkManager.Instance != null
                ? GameNetworkManager.Instance.GetComponentInChildren<ServerManager>()
                : null;

            if (_serverManager != null)
                _serverManager.OnRemoteConnectionState += OnRemoteConnectionStateChanged;
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            if (!Application.isBatchMode) return;

            // Signal Agones: server is ready to receive players.
            _ = _agones.ReadyAsync();
            Debug.Log("[Dedicated] Agones → Ready signal sent.");

            // Begin health heartbeat (Agones requires pings < every 5 seconds).
            _healthRoutine = StartCoroutine(HealthHeartbeatRoutine());
        }

        public override void OnStopServer()
        {
            base.OnStopServer();
            StopHealthHeartbeat();
        }

        private void OnDestroy()
        {
            StopHealthHeartbeat();

            if (_serverManager != null)
                _serverManager.OnRemoteConnectionState -= OnRemoteConnectionStateChanged;
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        #region Connection Callbacks

        private void OnRemoteConnectionStateChanged(
            FishNet.Connection.NetworkConnection conn,
            FishNet.Transporting.RemoteConnectionStateArgs args)
        {
            switch (args.ConnectionState)
            {
                case FishNet.Transporting.RemoteConnectionState.Started:
                    HandlePlayerConnected();
                    break;

                case FishNet.Transporting.RemoteConnectionState.Stopped:
                    CheckEmptyLobby();
                    break;
            }
        }

        private void HandlePlayerConnected()
        {
            if (_allocated) return;      // Already allocated from a previous connection.

            _allocated = true;

            // Signal Agones: match has started — protect this instance from scale-down.
            _ = _agones.AllocateAsync();
            Debug.Log("[Dedicated] Agones → Allocate signal sent. Container is now match-locked.");
        }

        private void CheckEmptyLobby()
        {
            if (_serverManager == null || _serverManager.Clients.Count > 0) return;

            Debug.LogWarning("[Dedicated] All players disconnected. Initiating graceful shutdown...");
            StartCoroutine(GracefulShutdownRoutine());
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        #region Public API

        /// <summary>
        /// Called by the game-mode layer once match results have been persisted
        /// to Nakama (Elo updates, XP, etc.). Triggers Agones shutdown so the
        /// container is recycled for the next match.
        /// </summary>
        public void MatchDidEndAndResultsSent()
        {
            Debug.Log("[Dedicated] Match results committed — initiating container shutdown.");
            StartCoroutine(GracefulShutdownRoutine());
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        #region Agones Heartbeat & Shutdown

        /// <summary>
        /// Sends a health ping to Agones every <see cref="_healthPingInterval"/> seconds.
        /// Agones marks a server Unhealthy if no ping is received for > 5 seconds.
        /// </summary>
        private IEnumerator HealthHeartbeatRoutine()
        {
            var interval = new WaitForSeconds(_healthPingInterval);
            while (true)
            {
                yield return interval;
                _ = _agones.HealthAsync();
            }
        }

        private void StopHealthHeartbeat()
        {
            if (_healthRoutine != null)
            {
                StopCoroutine(_healthRoutine);
                _healthRoutine = null;
            }
        }

        private IEnumerator GracefulShutdownRoutine()
        {
            StopHealthHeartbeat();

            // Brief pause so any last RPC/network messages can flush.
            yield return new WaitForSeconds(3f);

            // Signal Agones: container may be recycled.
            _ = _agones.ShutdownAsync();

            yield return new WaitForSeconds(2f);    // Allow Agones to ACK shutdown.

            if (GameNetworkManager.Instance != null)
                GameNetworkManager.Instance.StopConnection();

            Application.Quit();
        }

        #endregion
    }
}

