using System.Collections.Generic;
using FishNet.Managing.Server;
using FishNet.Object;
using ProjectZ.Core;
using UnityEngine;

namespace ProjectZ.Network
{
    // ─────────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Server-side heuristic anti-cheat layer.
    ///
    /// Detects and responds to:
    ///   • Speedhack / teleport   — position delta vs. allowed speed budget
    ///   • Violation accumulation — soft warn → hard kick → Nakama ban flag
    ///
    /// NOTE: Fire-rate and ammo cheats are already handled in
    ///       <see cref="ProjectZ.Player.PlayerCombatController"/> (CmdFire).
    ///       This component covers *movement* integrity exclusively.
    ///
    /// USAGE: Attach to the same GameObject as the FishNet NetworkManager.
    ///        Ticks per server send at 20TPS are sufficient for detection.
    /// </summary>
    // ─────────────────────────────────────────────────────────────────────────
    public class ServerAntiCheatHeuristics : NetworkBehaviour
    {
        // ─── Tuning ──────────────────────────────────────────────────────────
        [Header("Speed Validation")]
        [Tooltip("Tolerance multiplier over the GDD max speed (SprintSpeed). " +
                 "1.20 = 20% leeway for network jitter / lag compensation.")]
        [SerializeField] private float _speedToleranceMultiplier = 1.20f;

        [Tooltip("Maximum distance a player may teleport in a single server tick. " +
                 "Larger values are needed on high-latency connections.")]
        [SerializeField] private float _maxTeleportDistance = 8f;

        [Tooltip("Seconds between re-evaluation of each player's violation score.")]
        [SerializeField] private float _samplingInterval = 0.5f;

        [Header("Violation Thresholds")]
        [SerializeField] private int _softWarnThreshold  = 5;   // Log only
        [SerializeField] private int _hardKickThreshold  = 15;  // Disconnect
        [SerializeField] private int _banFlagThreshold   = 25;  // Nakama ban flag

        // ─── Runtime State ───────────────────────────────────────────────────
        private ServerManager _serverManager;
        private float         _nextSampleTime;

        // Per-connection tracking: connId → snapshot data
        private readonly Dictionary<int, PlayerSnapshot> _snapshots = new();
        private readonly Dictionary<int, int>            _violations = new();

        // Max allowed units/second (SprintSpeed in PlayerMovement is 375 units/s × 0.01 scale)
        // = 3.75 world-units/s. We validate against the raw scaled value.
        private const float MAX_SPEED_UPS = Player.PlayerMovement.SprintSpeed * 0.01f; // 3.75 u/s

        // ─────────────────────────────────────────────────────────────────────
        #region FishNet Lifecycle

        public override void OnStartServer()
        {
            base.OnStartServer();
            _serverManager = ServerManager;
            _serverManager.OnRemoteConnectionState += OnRemoteConnectionState;
            Debug.Log("[AntiCheat] Server-side movement heuristics active.");
        }

        public override void OnStopServer()
        {
            base.OnStopServer();
            if (_serverManager != null)
                _serverManager.OnRemoteConnectionState -= OnRemoteConnectionState;
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        #region Connection Tracking

        private void OnRemoteConnectionState(
            FishNet.Connection.NetworkConnection conn,
            FishNet.Transporting.RemoteConnectionStateArgs args)
        {
            if (args.ConnectionState == FishNet.Transporting.RemoteConnectionState.Started)
            {
                _snapshots[conn.ClientId]  = new PlayerSnapshot();
                _violations[conn.ClientId] = 0;
                Debug.Log($"[AntiCheat] Tracking player {conn.ClientId}.");
            }
            else if (args.ConnectionState == FishNet.Transporting.RemoteConnectionState.Stopped)
            {
                _snapshots.Remove(conn.ClientId);
                _violations.Remove(conn.ClientId);
            }
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        #region Per-Tick Evaluation

        private void Update()
        {
            float now = Time.unscaledTime;
            if (!IsServerInitialized || now < _nextSampleTime) return;
            _nextSampleTime = now + _samplingInterval;

            foreach (var kv in _serverManager.Clients)
            {
                int connId = kv.Key;
                var conn   = kv.Value;

                if (conn.FirstObject == null) continue;

                Transform playerTransform = conn.FirstObject.transform;
                Vector3 currentPos        = playerTransform.position;

                if (!_snapshots.TryGetValue(connId, out PlayerSnapshot snap))
                {
                    _snapshots[connId] = new PlayerSnapshot { LastPosition = currentPos, LastTime = now };
                    continue;
                }

                float elapsed  = now - snap.LastTime;
                if (elapsed <= 0f) continue;

                float distance = Vector3.Distance(currentPos, snap.LastPosition);
                float speed    = distance / elapsed;

                EvaluateSpeedViolation(connId, conn, speed, distance);

                _snapshots[connId] = new PlayerSnapshot { LastPosition = currentPos, LastTime = now };
            }
        }

        private void EvaluateSpeedViolation(
            int connId,
            FishNet.Connection.NetworkConnection conn,
            float speed,
            float distance)
        {
            float allowedSpeed = MAX_SPEED_UPS * _speedToleranceMultiplier;
            bool  isTeleport   = distance > _maxTeleportDistance;
            bool  isSpeedhack  = speed > allowedSpeed && !isTeleport; // teleport checked separately

            if (!isTeleport && !isSpeedhack) return; // Clean frame — no action

            if (!_violations.ContainsKey(connId))
                _violations[connId] = 0;

            _violations[connId]++;
            int vCount = _violations[connId];

            string type = isTeleport ? "TELEPORT" : "SPEEDHACK";
            Debug.LogWarning(
                $"[AntiCheat] {type} detected — Player {connId} " +
                $"| Speed: {speed:F2} u/s (Limit: {allowedSpeed:F2}) " +
                $"| Distance: {distance:F2} m " +
                $"| Violations: {vCount}");

            if (vCount >= _banFlagThreshold)
            {
                FlagForBan(connId, conn, type);
            }
            else if (vCount >= _hardKickThreshold)
            {
                KickPlayer(connId, conn, type);
            }
            else if (vCount >= _softWarnThreshold)
            {
                Debug.LogWarning($"[AntiCheat] ⚠️  Player {connId} approaching kick threshold ({vCount}/{_hardKickThreshold}).");
            }
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        #region Enforcement

        private void KickPlayer(int connId, FishNet.Connection.NetworkConnection conn, string reason)
        {
            Debug.LogWarning($"[AntiCheat] 🚫 KICK — Player {connId} ({reason}). Violations: {_violations[connId]}.");
            conn.Disconnect(false); // false = immediate, no grace period
        }

        private void FlagForBan(int connId, FishNet.Connection.NetworkConnection conn, string reason)
        {
            Debug.LogError($"[AntiCheat] ☠️  BAN FLAG — Player {connId} ({reason}). Violations: {_violations[connId]}. Flagging in Nakama...");

            // Dispatch to NakamaManager's server-side REST API call.
            // We use a fire-and-forget pattern; the ban status is persisted in Nakama Storage
            // and checked at authentication time on the client via PlayerProfileData.isBanned.
            _ = FlagPlayerBanAsync(connId);

            KickPlayer(connId, conn, reason);
        }

        private async System.Threading.Tasks.Task FlagPlayerBanAsync(int connId)
        {
            // In a real implementation, the dedicated server talks to Nakama's
            // server-to-server REST API with the server key, not the user session.
            // Here we log a structured record that a backend service can pick up.
            // Replace with: await nakamaHttpClient.BanUsersAsync(new[] { userId });
            await System.Threading.Tasks.Task.Delay(0);
            Debug.LogError($"[AntiCheat] Ban record emitted for connId {connId}. " +
                           "TODO: POST to Nakama /v2/console/account/<userId>/ban");
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        /// <summary>Resets violation count for a player. Call after server decides false-positive.</summary>
        [Server]
        public void ClearViolations(int connId)
        {
            if (_violations.ContainsKey(connId))
            {
                _violations[connId] = 0;
                Debug.Log($"[AntiCheat] Violation count cleared for player {connId}.");
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        private struct PlayerSnapshot
        {
            public Vector3 LastPosition;
            public float   LastTime;
        }
    }
}
