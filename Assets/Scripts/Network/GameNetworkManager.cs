using FishNet.Connection;
using FishNet.Managing;
using FishNet.Transporting;
using FishNet.Transporting.Tugboat;
using ProjectZ.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace ProjectZ.Network
{
    /// <summary>
    /// Singleton wrapper around FishNet's NetworkManager.
    /// Handles server/client lifecycle, local development shortcuts, and
    /// matched-session client startup once Nakama matchmaking produces a token.
    /// </summary>
    public class GameNetworkManager : MonoBehaviour
    {
        public static GameNetworkManager Instance { get; private set; }

        [Header("References")]
        [SerializeField] private NetworkManager _fishNetManager;

        [Header("Matched Session")]
        [SerializeField] private string _gameplaySceneName = "SampleScene";
        [SerializeField] private string _matchServerAddress = "127.0.0.1";
        [SerializeField] private ushort _matchServerPort = 7770;
        [SerializeField] private bool _autoJoinMatchedSessions = true;

        private string _lastAttemptedMatchToken;

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
            if (_fishNetManager == null)
                return;

            _fishNetManager.ServerManager.OnRemoteConnectionState += OnRemoteConnectionState;
            _fishNetManager.ClientManager.OnClientConnectionState += OnClientConnectionState;
        }

        private void OnDisable()
        {
            if (_fishNetManager == null)
                return;

            _fishNetManager.ServerManager.OnRemoteConnectionState -= OnRemoteConnectionState;
            _fishNetManager.ClientManager.OnClientConnectionState -= OnClientConnectionState;
        }

        private void Update()
        {
            if (_autoJoinMatchedSessions)
            {
                if (NakamaManager.Instance != null && !NakamaManager.Instance.HasPendingMatchToken())
                    _lastAttemptedMatchToken = null;

                TryBeginPendingMatchSession();
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (Keyboard.current != null && Keyboard.current.pKey.wasPressedThisFrame && !IsServer && !IsClient)
                StartHost();
#endif
        }

        /// <summary>Start as host (server + local client).</summary>
        public void StartHost()
        {
            if (_fishNetManager == null)
                return;

            _fishNetManager.ServerManager.StartConnection();
            _fishNetManager.ClientManager.StartConnection();
            Debug.Log("[Network] Started as HOST.");
        }

        /// <summary>Start as dedicated server only.</summary>
        public void StartServer()
        {
            if (_fishNetManager == null)
                return;

            _fishNetManager.ServerManager.StartConnection();
            Debug.Log("[Network] Started as SERVER.");
        }

        /// <summary>Connect as a client to the specified address and port.</summary>
        public void StartClient(string address = "localhost", ushort port = 7770)
        {
            if (_fishNetManager == null)
                return;

            if (IsClient)
                return;

            if (_fishNetManager.TransportManager.Transport is Tugboat tugboat)
            {
                tugboat.SetClientAddress(address);
                tugboat.SetPort(port);
            }

            _fishNetManager.ClientManager.StartConnection(address);
            Debug.Log($"[Network] Started as CLIENT -> {address}:{port}");
        }

        /// <summary>Gracefully stop all connections.</summary>
        public void StopConnection()
        {
            _lastAttemptedMatchToken = null;

            if (_fishNetManager != null && _fishNetManager.IsServerStarted)
                _fishNetManager.ServerManager.StopConnection(true);

            if (_fishNetManager != null && _fishNetManager.IsClientStarted)
                _fishNetManager.ClientManager.StopConnection();

            Debug.Log("[Network] All connections stopped.");
        }

        public bool IsServer => _fishNetManager != null && _fishNetManager.IsServerStarted;
        public bool IsClient => _fishNetManager != null && _fishNetManager.IsClientStarted;
        public bool IsHost => IsServer && IsClient;

        public void TryBeginPendingMatchSession()
        {
            if (_fishNetManager == null || Application.isBatchMode)
                return;

            if (IsServer || IsClient)
                return;

            if (SceneManager.GetActiveScene().name != _gameplaySceneName)
                return;

            NakamaManager nakamaManager = NakamaManager.Instance;
            if (nakamaManager == null || !nakamaManager.HasPendingMatchToken())
                return;

            string token = nakamaManager.PendingMatchToken;
            if (string.IsNullOrWhiteSpace(token))
                return;

            if (string.Equals(token, _lastAttemptedMatchToken, System.StringComparison.Ordinal))
                return;

            _lastAttemptedMatchToken = token;
            Debug.Log($"[Network] Beginning matched session for token {token[..Mathf.Min(8, token.Length)]}.");
            StartClient(_matchServerAddress, _matchServerPort);
        }

        private void OnClientConnectionState(ClientConnectionStateArgs args)
        {
            if (args.ConnectionState == LocalConnectionState.Started)
            {
                NakamaManager.Instance?.ClearPendingMatchToken();
                Debug.Log("[Network] Matched gameplay client connected.");
            }
            else if (args.ConnectionState == LocalConnectionState.Stopped)
            {
                Debug.Log("[Network] Client connection stopped.");
            }
        }

        private void OnRemoteConnectionState(NetworkConnection conn, RemoteConnectionStateArgs args)
        {
            if (args.ConnectionState == RemoteConnectionState.Started)
            {
                Debug.Log($"[Network] Player connected -> connId: {conn.ClientId}");
                GameEvents.InvokePlayerConnected(conn.ClientId);
            }
            else
            {
                Debug.Log($"[Network] Player disconnected -> connId: {conn.ClientId}");
                GameEvents.InvokePlayerDisconnected(conn.ClientId);
            }
        }
    }
}
