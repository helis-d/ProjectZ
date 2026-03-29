using System;
using System.Threading.Tasks;
using Nakama;
using Newtonsoft.Json;
using UnityEngine;

namespace ProjectZ.Network
{
    /// <summary>
    /// Singleton that manages the lifecycle of the Nakama connection.
    /// Handles Device-based Authentication, Session persistence, and
    /// Storage read/write for player loadouts and metagame data.
    /// 
    /// USAGE: Place this on a persistent GameObject in the Lobby/Main Menu scene.
    /// </summary>
    public class NakamaManager : MonoBehaviour
    {
        public static NakamaManager Instance { get; private set; }

        [Header("Nakama Server Settings")]
        [Tooltip("Scheme (http or https)")]
        [SerializeField] private string _scheme = "http";
        [Tooltip("Nakama server host address")]
        [SerializeField] private string _host = "127.0.0.1";
        [Tooltip("Nakama server port")]
        [SerializeField] private int _port = 7350;
        [Tooltip("Nakama server key (default: defaultkey)")]
        [SerializeField] private string _serverKey = "defaultkey";

        // ─── Nakama Core Objects ──────────────────────────────────────────
        private IClient _client;
        private ISession _session;
        private ISocket _socket;

        /// <summary>True if authenticated and session is valid.</summary>
        public bool IsAuthenticated => _session != null && !_session.IsExpired;
        /// <summary>The authenticated user's Nakama User ID.</summary>
        public string UserId => _session?.UserId;
        /// <summary>The authenticated user's username.</summary>
        public string Username => _session?.Username;
        /// <summary>The active Nakama socket for real-time features (matchmaking, chat).</summary>
        public ISocket Socket => _socket;
        /// <summary>The active Nakama client for REST API calls.</summary>
        public IClient Client => _client;
        /// <summary>The active session.</summary>
        public ISession Session => _session;

        // ─── Events ───────────────────────────────────────────────────────
        public event Action OnAuthenticationSuccess;
        public event Action<string> OnAuthenticationFailed;
        public event Action OnDisconnected;
        public event Action<IMatchmakerMatched> OnMatchFound;

        // ─── Player Data Cache ────────────────────────────────────────────
        /// <summary>Cached player profile loaded from Nakama storage.</summary>
        public PlayerProfileData CachedProfile { get; private set; }

        private const string STORAGE_COLLECTION = "player_data";
        private const string STORAGE_KEY_PROFILE = "profile";

        // ─── Unity Lifecycle ──────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Create the Nakama client (does NOT connect yet)
            _client = new Nakama.Client(_scheme, _host, _port, _serverKey);
        }

        private void OnDestroy()
        {
            DisconnectAsync();
        }

        // ─── Authentication ───────────────────────────────────────────────
        /// <summary>
        /// Authenticate using the device's unique identifier (silent login).
        /// Creates a new account if one doesn't exist.
        /// </summary>
        public async Task<bool> AuthenticateWithDeviceAsync()
        {
            try
            {
                string deviceId = SystemInfo.deviceUniqueIdentifier;
                if (deviceId == SystemInfo.unsupportedIdentifier)
                    deviceId = Guid.NewGuid().ToString(); // Fallback for unsupported platforms

                // Check stored session first
                string storedToken = PlayerPrefs.GetString("nakama_token", null);
                string storedRefreshToken = PlayerPrefs.GetString("nakama_refresh_token", null);

                if (!string.IsNullOrEmpty(storedToken))
                {
                    var restoredSession = Nakama.Session.Restore(storedToken, storedRefreshToken);
                    if (!restoredSession.IsExpired)
                    {
                        _session = restoredSession;
                        Debug.Log($"[Nakama] Session restored for user: {_session.Username}");
                    }
                }

                // If no valid session, authenticate fresh
                if (_session == null || _session.IsExpired)
                {
                    _session = await _client.AuthenticateDeviceAsync(deviceId, null, true);
                    Debug.Log($"[Nakama] Authenticated as: {_session.Username} (ID: {_session.UserId})");

                    // Persist tokens
                    PlayerPrefs.SetString("nakama_token", _session.AuthToken);
                    PlayerPrefs.SetString("nakama_refresh_token", _session.RefreshToken);
                    PlayerPrefs.Save();
                }

                // Connect socket for real-time features
                await ConnectSocketAsync();

                OnAuthenticationSuccess?.Invoke();
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Nakama] Authentication failed: {e.Message}");
                OnAuthenticationFailed?.Invoke(e.Message);
                return false;
            }
        }

        // ─── Socket ───────────────────────────────────────────────────────
        private async Task ConnectSocketAsync()
        {
            _socket = _client.NewSocket();

            _socket.Closed += (sender) =>
            {
                Debug.Log("[Nakama] Socket disconnected.");
                OnDisconnected?.Invoke();
            };

            _socket.ReceivedMatchmakerMatched += matched =>
            {
                Debug.Log($"[Nakama] Match found! Token: {matched.Token}");
                OnMatchFound?.Invoke(matched);
            };

            await _socket.ConnectAsync(_session, true);
            Debug.Log("[Nakama] Socket connected.");
        }

        /// <summary>Gracefully disconnect the socket.</summary>
        public async void DisconnectAsync()
        {
            if (_socket != null)
            {
                await _socket.CloseAsync();
                _socket = null;
            }
        }

        // ─── Storage: Player Profile ──────────────────────────────────────
        /// <summary>
        /// Load the player's profile (loadout, currency, mastery) from Nakama storage.
        /// </summary>
        public async Task<PlayerProfileData> LoadPlayerProfileAsync()
        {
            try
            {
                var result = await _client.ReadStorageObjectsAsync(_session, new IApiReadStorageObjectId[]
                {
                    new StorageObjectId
                    {
                        Collection = STORAGE_COLLECTION,
                        Key = STORAGE_KEY_PROFILE,
                        UserId = _session.UserId
                    }
                });

                foreach (var obj in result.Objects)
                {
                    CachedProfile = JsonConvert.DeserializeObject<PlayerProfileData>(obj.Value);
                    Debug.Log($"[Nakama] Profile loaded: {CachedProfile.displayName}");
                    return CachedProfile;
                }

                // First time user - create defaults
                Debug.Log("[Nakama] No profile found, creating default...");
                CachedProfile = PlayerProfileData.CreateDefault(_session.Username);
                await SavePlayerProfileAsync(CachedProfile);
                return CachedProfile;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Nakama] Failed to load profile: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Save the player's profile back to Nakama storage.
        /// </summary>
        public async Task SavePlayerProfileAsync(PlayerProfileData profile)
        {
            try
            {
                string json = JsonConvert.SerializeObject(profile);

                await _client.WriteStorageObjectsAsync(_session, new WriteStorageObject[]
                {
                    new WriteStorageObject
                    {
                        Collection = STORAGE_COLLECTION,
                        Key = STORAGE_KEY_PROFILE,
                        Value = json,
                        PermissionRead = 1,  // Owner can read
                        PermissionWrite = 1  // Owner can write
                    }
                });

                CachedProfile = profile;
                Debug.Log("[Nakama] Profile saved successfully.");
            }
            catch (Exception e)
            {
                Debug.LogError($"[Nakama] Failed to save profile: {e.Message}");
            }
        }

        // ─── Matchmaking ──────────────────────────────────────────────────
        /// <summary>
        /// Add this player to the matchmaking queue. 
        /// Returns a matchmaker ticket that can be used to cancel the search.
        /// </summary>
        public async Task<IMatchmakerTicket> FindMatchAsync(int minCount = 2, int maxCount = 10, string query = "*")
        {
            try
            {
                var ticket = await _socket.AddMatchmakerAsync(query, minCount, maxCount);
                Debug.Log($"[Nakama] Matchmaking ticket: {ticket.Ticket}");
                return ticket;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Nakama] Matchmaking failed: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Cancel an active matchmaking ticket.
        /// </summary>
        public async Task CancelMatchAsync(IMatchmakerTicket ticket)
        {
            try
            {
                await _socket.RemoveMatchmakerAsync(ticket);
                Debug.Log("[Nakama] Matchmaking cancelled.");
            }
            catch (Exception e)
            {
                Debug.LogError($"[Nakama] Failed to cancel matchmaking: {e.Message}");
            }
        }
    }

    // ─── Data Models ──────────────────────────────────────────────────────
    /// <summary>
    /// Serializable player profile stored in Nakama's Storage Engine.
    /// </summary>
    [Serializable]
    public class PlayerProfileData
    {
        public string displayName;
        public int currency;
        public int elo;
        public string selectedHero;
        public string primaryWeaponId;
        public string secondaryWeaponId;
        public string meleeWeaponId;

        // Mastery XP per weapon (weaponId -> xp)
        public System.Collections.Generic.Dictionary<string, int> weaponMastery = new();

        public static PlayerProfileData CreateDefault(string username)
        {
            return new PlayerProfileData
            {
                displayName = username ?? "NewPlayer",
                currency = 1000,
                elo = 1000,
                selectedHero = "volt",
                primaryWeaponId = "vandal",
                secondaryWeaponId = "pistol_classic",
                meleeWeaponId = "knife_tactical",
                weaponMastery = new System.Collections.Generic.Dictionary<string, int>()
            };
        }
    }
}
