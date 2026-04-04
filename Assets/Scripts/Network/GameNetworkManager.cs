using FishNet;
using FishNet.Managing;
using FishNet.Connection;
using FishNet.Transporting;
using FishNet.Transporting.Tugboat;
using ProjectZ.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectZ.Network
{
    /// <summary>
    /// Singleton wrapper around FishNet's NetworkManager.
    /// Handles server/client lifecycle and exposes convenience methods.
    /// </summary>
    public class GameNetworkManager : MonoBehaviour
    {
        public static GameNetworkManager Instance { get; private set; }

        [Header("References")]
        [SerializeField] private NetworkManager _fishNetManager;

        // ─── Unity ───────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (_fishNetManager == null)
                _fishNetManager = GetComponentInChildren<NetworkManager>();
        }

        private void OnEnable()
        {
            if (_fishNetManager == null) return;
            _fishNetManager.ServerManager.OnRemoteConnectionState += OnRemoteConnectionState;
        }

        private void OnDisable()
        {
            if (_fishNetManager == null) return;
            _fishNetManager.ServerManager.OnRemoteConnectionState -= OnRemoteConnectionState;
        }

        private void Update()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (Keyboard.current != null && Keyboard.current.pKey.wasPressedThisFrame)
            {
                if (!IsServer && !IsClient)
                {
                    StartHost();
                }
            }
#endif
        }

        // ─── Public API ───────────────────────────────────────────────────
        /// <summary>Start as host (server + local client).</summary>
        public void StartHost()
        {
            _fishNetManager.ServerManager.StartConnection();
            _fishNetManager.ClientManager.StartConnection();
            Debug.Log("[Network] Started as HOST.");
        }

        /// <summary>Start as dedicated server only.</summary>
        public void StartServer()
        {
            _fishNetManager.ServerManager.StartConnection();
            Debug.Log("[Network] Started as SERVER.");
        }

        /// <summary>Connect as a client to the specified address and port.</summary>
        public void StartClient(string address = "localhost", ushort port = 7770)
        {
            var transport = _fishNetManager.TransportManager.Transport;
            if (transport is Tugboat tugboat)
            {
                tugboat.SetClientAddress(address);
                tugboat.SetPort(port);
            }
            
            _fishNetManager.ClientManager.StartConnection(address);
            Debug.Log($"[Network] Started as CLIENT → {address}:{port}");
        }

        /// <summary>Gracefully stop all connections.</summary>
        public void StopConnection()
        {
            if (_fishNetManager.IsServerStarted)
                _fishNetManager.ServerManager.StopConnection(true);

            if (_fishNetManager.IsClientStarted)
                _fishNetManager.ClientManager.StopConnection();

            Debug.Log("[Network] All connections stopped.");
        }

        public bool IsServer => _fishNetManager != null && _fishNetManager.IsServerStarted;
        public bool IsClient => _fishNetManager != null && _fishNetManager.IsClientStarted;
        public bool IsHost   => IsServer && IsClient;

        // ─── Callbacks ────────────────────────────────────────────────────
        private void OnRemoteConnectionState(NetworkConnection conn, RemoteConnectionStateArgs args)
        {
            if (args.ConnectionState == RemoteConnectionState.Started)
            {
                Debug.Log($"[Network] Player connected — connId: {conn.ClientId}");
                GameEvents.InvokePlayerConnected(conn.ClientId);
            }
            else
            {
                Debug.Log($"[Network] Player disconnected — connId: {conn.ClientId}");
                GameEvents.InvokePlayerDisconnected(conn.ClientId);
            }
        }
    }
}
