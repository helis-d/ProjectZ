using System;
using System.Threading.Tasks;
using Nakama;
using Newtonsoft.Json;
using ProjectZ.GameMode;
using ProjectZ.Monetization;
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
        /// <summary>The latest matchmaker token received from Nakama.</summary>
        public string PendingMatchToken { get; private set; }

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
                PendingMatchToken = null;
                OnDisconnected?.Invoke();
            };

            _socket.ReceivedMatchmakerMatched += matched =>
            {
                PendingMatchToken = matched.Token;
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
                    CachedProfile ??= PlayerProfileData.CreateDefault(_session.Username);
                    CachedProfile.Sanitize();
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
            if (profile == null)
                return;

            profile.Sanitize();
            CachedProfile = profile;

            if (!IsAuthenticated || _client == null || _session == null)
            {
                Debug.LogWarning("[Nakama] Save skipped because there is no authenticated session. Cached profile updated locally.");
                return;
            }

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
                Debug.Log("[Nakama] Profile saved successfully.");
            }
            catch (Exception e)
            {
                Debug.LogError($"[Nakama] Failed to save profile: {e.Message}");
            }
        }

        public RankedProgressionResult PreviewRankedProgression(RankedMatchPerformance performance)
        {
            EnsureCachedProfile();

            int previousRating = CachedProfile.elo;
            int delta = CompetitiveRankSystem.CalculateRatingDelta(performance);
            int newRating = CompetitiveRankSystem.ApplyRatingDelta(previousRating, delta);
            return CompetitiveRankSystem.BuildProgressionResult(previousRating, newRating);
        }

        public async Task<RankedProgressionResult> ApplyRankedMatchResultAsync(RankedMatchPerformance performance)
        {
            EnsureCachedProfile();

            int previousRating = CachedProfile.elo;
            int delta = CompetitiveRankSystem.CalculateRatingDelta(performance);
            int newRating = CompetitiveRankSystem.ApplyRatingDelta(previousRating, delta);

            CachedProfile.elo = newRating;
            CachedProfile.rankedMatchesPlayed++;
            if (performance.Won)
                CachedProfile.rankedWins++;
            else
                CachedProfile.rankedLosses++;

            CachedProfile.peakElo = Mathf.Max(CachedProfile.peakElo, CachedProfile.elo);
            CachedProfile.Sanitize();

            RankedProgressionResult result = CompetitiveRankSystem.BuildProgressionResult(previousRating, newRating);
            await SavePlayerProfileAsync(CachedProfile);
            return result;
        }

        public bool OwnsHero(string heroId)
        {
            EnsureCachedProfile();
            return MonetizationService.OwnsHero(CachedProfile, heroId);
        }

        public bool CanAccessRankedByOwnership()
        {
            EnsureCachedProfile();
            return MonetizationService.CanEnterRanked(CachedProfile);
        }

        public async Task<MonetizationPurchaseResult> TryUnlockHeroAsync(string heroId)
        {
            EnsureCachedProfile();

            MonetizationPurchaseResult result = MonetizationService.TryUnlockHero(CachedProfile, heroId);
            if (result.Succeeded)
                await SavePlayerProfileAsync(CachedProfile);

            return result;
        }

        public async Task<bool> SetSelectedHeroAsync(string heroId)
        {
            EnsureCachedProfile();

            if (!MonetizationService.CanSelectHero(CachedProfile, heroId))
                return false;

            CachedProfile.selectedHero = heroId;
            CachedProfile.Sanitize();
            await SavePlayerProfileAsync(CachedProfile);
            return true;
        }

        public string GetSelectedHeroId()
        {
            EnsureCachedProfile();
            return CachedProfile.selectedHero;
        }

        public async Task<MonetizationPurchaseResult> TryPurchaseOfferAsync(
            string offerId,
            bool alphaEntitlementsEnabled = false,
            bool season2Enabled = false,
            bool eventContentEnabled = false)
        {
            EnsureCachedProfile();

            MonetizationCatalogOffer offer = MonetizationCatalog.Instance.GetById(offerId);
            MonetizationPurchaseResult result = MonetizationService.TryPurchaseOffer(
                CachedProfile,
                offer,
                alphaEntitlementsEnabled,
                season2Enabled,
                eventContentEnabled);

            if (result.Succeeded)
                await SavePlayerProfileAsync(CachedProfile);

            return result;
        }

        private void EnsureCachedProfile()
        {
            if (CachedProfile == null)
                CachedProfile = PlayerProfileData.CreateDefault(_session != null ? _session.Username : "NewPlayer");

            CachedProfile.Sanitize();
        }

        // ─── Matchmaking ──────────────────────────────────────────────────
        // SBMM constants
        private const int SBMM_RANKED_TOLERANCE   = 150;   // ±150 Elo for competitive
        private const int SBMM_CASUAL_TOLERANCE    = 400;   // ±400 Elo for casual
        private const int SBMM_MIN_ELO             = CompetitiveRankSystem.MinimumRating;
        private const int SBMM_MAX_ELO             = 99999; // Open ceiling for Prestij+

        /// <summary>
        /// Competitive ranked matchmaking — places the player into a queue with
        /// tight Elo tolerance (±<see cref="SBMM_RANKED_TOLERANCE"/>).
        /// The player's current Elo and rank band are sent as Nakama numeric
        /// properties so the matchmaker can apply server-side filtering.
        /// Returns null if competitive access requirements are not met.
        /// </summary>
        public async Task<IMatchmakerTicket> FindRankedMatchAsync(int minPlayers = 10, int maxPlayers = 10)
        {
            EnsureCachedProfile();

            if (!CanAccessRankedByOwnership())
            {
                Debug.LogWarning("[Nakama] Ranked matchmaking blocked: hero ownership requirements not met.");
                return null;
            }

            int elo         = Mathf.Max(SBMM_MIN_ELO, CachedProfile.elo);
            int eloFloor    = Mathf.Max(SBMM_MIN_ELO, elo - SBMM_RANKED_TOLERANCE);
            int eloCeiling  = elo + SBMM_RANKED_TOLERANCE;

            // Nakama query syntax: +properties.elo:>=floor +properties.elo:<=ceiling
            string query = $"+properties.elo:>={eloFloor} +properties.elo:<={eloCeiling}";

            var numericProperties = new System.Collections.Generic.Dictionary<string, double>
            {
                { "elo",            elo },
                { "ranked_matches", CachedProfile.rankedMatchesPlayed }
            };

            var stringProperties = new System.Collections.Generic.Dictionary<string, string>
            {
                { "mode", "ranked" }
            };

            return await EnqueueMatchAsync(query, minPlayers, maxPlayers, numericProperties, stringProperties);
        }

        /// <summary>
        /// Casual matchmaking with a wider Elo window (±<see cref="SBMM_CASUAL_TOLERANCE"/>).
        /// No ownership gating — accessible to all authenticated players.
        /// </summary>
        public async Task<IMatchmakerTicket> FindCasualMatchAsync(int minPlayers = 2, int maxPlayers = 10)
        {
            EnsureCachedProfile();

            int elo        = Mathf.Max(SBMM_MIN_ELO, CachedProfile.elo);
            int eloFloor   = Mathf.Max(SBMM_MIN_ELO, elo - SBMM_CASUAL_TOLERANCE);
            int eloCeiling = elo + SBMM_CASUAL_TOLERANCE;

            string query = $"+properties.elo:>={eloFloor} +properties.elo:<={eloCeiling}";

            var numericProperties = new System.Collections.Generic.Dictionary<string, double>
            {
                { "elo", elo }
            };

            var stringProperties = new System.Collections.Generic.Dictionary<string, string>
            {
                { "mode", "casual" }
            };

            return await EnqueueMatchAsync(query, minPlayers, maxPlayers, numericProperties, stringProperties);
        }

        /// <summary>
        /// Legacy overload kept for backwards compatibility.
        /// For new code prefer <see cref="FindRankedMatchAsync"/> or <see cref="FindCasualMatchAsync"/>.
        /// </summary>
        public Task<IMatchmakerTicket> FindMatchAsync(
            int minCount = 2,
            int maxCount = 10,
            string query = "*",
            bool requireCompetitiveAccess = false)
        {
            if (requireCompetitiveAccess)
                return FindRankedMatchAsync(minCount, maxCount);

            return FindCasualMatchAsync(minCount, maxCount);
        }

        /// <summary>
        /// Cancel an active matchmaking ticket.
        /// </summary>
        public async Task CancelMatchAsync(IMatchmakerTicket ticket)
        {
            if (ticket == null) return;
            try
            {
                await _socket.RemoveMatchmakerAsync(ticket);
                PendingMatchToken = null;
                Debug.Log("[Nakama] Matchmaking cancelled.");
            }
            catch (Exception e)
            {
                Debug.LogError($"[Nakama] Failed to cancel matchmaking: {e.Message}");
            }
        }

        // ─── Internal Matchmaker Helper ───────────────────────────────────────
        private async Task<IMatchmakerTicket> EnqueueMatchAsync(
            string query,
            int minCount,
            int maxCount,
            System.Collections.Generic.Dictionary<string, double> numericProps,
            System.Collections.Generic.Dictionary<string, string> stringProps)
        {
            if (!IsAuthenticated || _socket == null)
            {
                Debug.LogError("[Nakama] Cannot matchmake: not authenticated.");
                return null;
            }

            try
            {
                PendingMatchToken = null;

                var ticket = await _socket.AddMatchmakerAsync(
                    query,
                    minCount,
                    maxCount,
                    stringProps,
                    numericProps);

                Debug.Log($"[Nakama] SBMM ticket issued — Query: {query} | Min: {minCount} Max: {maxCount} | Ticket: {ticket.Ticket}");
                return ticket;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Nakama] Matchmaking failed: {e.Message}");
                return null;
            }
        }

        // ─── Match Telemetry ──────────────────────────────────────────
        private const string TELEMETRY_COLLECTION = "match_telemetry";

        /// <summary>
        /// Persists a <see cref="MatchTelemetryData"/> record to the player's
        /// Nakama Storage bucket after a match ends.
        ///
        /// The server-side Lua/TS hooks inside Nakama can aggregate these records
        /// into leaderboards, balance dashboards, and LiveOps reports.
        ///
        /// Call from the game-mode layer (e.g. RankedGameMode.OnRoundEnd) once
        /// all round results are available.
        /// </summary>
        public async Task SaveMatchTelemetryAsync(MatchTelemetryData telemetry)
        {
            if (telemetry == null || !IsAuthenticated)
            {
                Debug.LogWarning("[Nakama] Telemetry skipped: null data or unauthenticated.");
                return;
            }

            // Stamp caller info before serialising.
            telemetry.userId    = UserId;
            telemetry.timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            telemetry.matchKey  = Guid.NewGuid().ToString("N")[..12]; // Short unique key

            try
            {
                string json = JsonConvert.SerializeObject(telemetry, Formatting.None);

                await _client.WriteStorageObjectsAsync(_session, new WriteStorageObject[]
                {
                    new WriteStorageObject
                    {
                        Collection     = TELEMETRY_COLLECTION,
                        Key            = telemetry.matchKey,
                        Value          = json,
                        PermissionRead  = 2,   // Public read — dev dashboard can query any player
                        PermissionWrite = 1    // Only owner can write (client-authoritative record)
                    }
                });

                Debug.Log($"[Nakama] 📊 Telemetry saved — Match: {telemetry.matchKey} " +
                          $"| Map: {telemetry.mapId} | Duration: {telemetry.matchDurationSeconds:F0}s " +
                          $"| Winner: {telemetry.winningTeam}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[Nakama] Failed to save telemetry: {e.Message}");
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
        public int commandCredits;
        public int zCore;
        public int elo;
        public int peakElo;
        public int rankedMatchesPlayed;
        public int rankedWins;
        public int rankedLosses;
        public string selectedHero;
        public string primaryWeaponId;
        public string secondaryWeaponId;
        public string meleeWeaponId;
        public System.Collections.Generic.List<string> ownedHeroIds = new();
        public System.Collections.Generic.List<string> ownedCosmeticIds = new();
        public System.Collections.Generic.List<string> ownedOfferIds = new();

        // Mastery XP per weapon (weaponId -> xp)
        public System.Collections.Generic.Dictionary<string, int> weaponMastery = new();

        public static PlayerProfileData CreateDefault(string username)
        {
            PlayerProfileData profile = new PlayerProfileData
            {
                displayName = username ?? "NewPlayer",
                currency = MonetizationService.StartingCommandCredits,
                commandCredits = MonetizationService.StartingCommandCredits,
                zCore = MonetizationService.StartingZCore,
                elo = CompetitiveRankSystem.StartingRating,
                peakElo = CompetitiveRankSystem.StartingRating,
                rankedMatchesPlayed = 0,
                rankedWins = 0,
                rankedLosses = 0,
                selectedHero = "volt",
                primaryWeaponId = "vandal",
                secondaryWeaponId = "pistol_classic",
                meleeWeaponId = "knife_tactical",
                ownedHeroIds = new System.Collections.Generic.List<string>(MonetizationService.GetStarterHeroIds()),
                ownedCosmeticIds = new System.Collections.Generic.List<string>(),
                ownedOfferIds = new System.Collections.Generic.List<string>(),
                weaponMastery = new System.Collections.Generic.Dictionary<string, int>()
            };

            profile.Sanitize();
            return profile;
        }

        public void Sanitize()
        {
            displayName = string.IsNullOrWhiteSpace(displayName) ? "NewPlayer" : displayName;
            currency = Mathf.Max(0, currency);
            commandCredits = Mathf.Max(0, commandCredits);
            zCore = Mathf.Max(0, zCore);

            if (elo < CompetitiveRankSystem.MinimumRating)
                elo = CompetitiveRankSystem.StartingRating;

            peakElo = Mathf.Max(peakElo, elo);
            rankedMatchesPlayed = Mathf.Max(0, rankedMatchesPlayed);
            rankedWins = Mathf.Max(0, rankedWins);
            rankedLosses = Mathf.Max(0, rankedLosses);
            rankedMatchesPlayed = Mathf.Max(rankedMatchesPlayed, rankedWins + rankedLosses);

            selectedHero = string.IsNullOrWhiteSpace(selectedHero) ? "volt" : selectedHero;
            primaryWeaponId = string.IsNullOrWhiteSpace(primaryWeaponId) ? "vandal" : primaryWeaponId;
            secondaryWeaponId = string.IsNullOrWhiteSpace(secondaryWeaponId) ? "pistol_classic" : secondaryWeaponId;
            meleeWeaponId = string.IsNullOrWhiteSpace(meleeWeaponId) ? "knife_tactical" : meleeWeaponId;
            ownedHeroIds ??= new System.Collections.Generic.List<string>();
            ownedCosmeticIds ??= new System.Collections.Generic.List<string>();
            ownedOfferIds ??= new System.Collections.Generic.List<string>();
            weaponMastery ??= new System.Collections.Generic.Dictionary<string, int>();

            MonetizationService.NormalizeProfile(this);
        }
    }

    // ─── Telemetry Model ───────────────────────────────────────────
    /// <summary>
    /// Structured match result record stored in Nakama Storage after each game.
    /// Fields are deliberately flat (no nested objects) so they can be indexed
    /// and queried by Nakama's server-runtime or piped to a BI dashboard
    /// (BigQuery / Redshift) via a Nakama webhook.
    ///
    /// USAGE:
    /// <code>
    /// var t = new MatchTelemetryData
    /// {
    ///     mapId              = "map_fragment",
    ///     gameMode           = "ranked",
    ///     winningTeam        = "attacker",
    ///     matchDurationSeconds = 312f,
    ///     totalRoundsPlayed  = 13,
    ///     attackerRoundsWon  = 7,
    ///     defenderRoundsWon  = 6,
    ///     kills              = 18,
    ///     deaths             = 11,
    ///     assists            = 4,
    ///     mostUsedWeaponId   = "vandal",
    ///     spherePlantsCount  = 5,
    ///     heroId             = "volt",
    ///     eloDelta           = +22
    /// };
    /// await NakamaManager.Instance.SaveMatchTelemetryAsync(t);
    /// </code>
    /// </summary>
    [Serializable]
    public class MatchTelemetryData
    {
        // ─ Identity (auto-stamped by SaveMatchTelemetryAsync) ────────────────
        [Newtonsoft.Json.JsonProperty("user_id")]
        public string userId;

        [Newtonsoft.Json.JsonProperty("match_key")]
        public string matchKey;

        [Newtonsoft.Json.JsonProperty("timestamp_unix")]
        public long timestamp;

        // ─ Match Context ────────────────────────────────────────
        [Newtonsoft.Json.JsonProperty("map_id")]
        public string mapId;

        [Newtonsoft.Json.JsonProperty("game_mode")]
        public string gameMode;             // "ranked" | "casual" | "duel"

        [Newtonsoft.Json.JsonProperty("winning_team")]
        public string winningTeam;          // "attacker" | "defender"

        [Newtonsoft.Json.JsonProperty("match_duration_sec")]
        public float matchDurationSeconds;

        [Newtonsoft.Json.JsonProperty("total_rounds")]
        public int totalRoundsPlayed;

        [Newtonsoft.Json.JsonProperty("attacker_rounds_won")]
        public int attackerRoundsWon;

        [Newtonsoft.Json.JsonProperty("defender_rounds_won")]
        public int defenderRoundsWon;

        // ─ Player Performance ───────────────────────────────────
        [Newtonsoft.Json.JsonProperty("kills")]
        public int kills;

        [Newtonsoft.Json.JsonProperty("deaths")]
        public int deaths;

        [Newtonsoft.Json.JsonProperty("assists")]
        public int assists;

        [Newtonsoft.Json.JsonProperty("headshot_count")]
        public int headshotCount;

        [Newtonsoft.Json.JsonProperty("wallbang_count")]
        public int wallbangCount;

        [Newtonsoft.Json.JsonProperty("was_mvp")]
        public bool wasMvp;

        // ─ Weapon & Agent Metrics (Balance Data)──────────────────
        [Newtonsoft.Json.JsonProperty("hero_id")]
        public string heroId;

        [Newtonsoft.Json.JsonProperty("most_used_weapon_id")]
        public string mostUsedWeaponId;

        [Newtonsoft.Json.JsonProperty("sphere_plants")]
        public int spherePlantsCount;

        [Newtonsoft.Json.JsonProperty("sphere_defuses")]
        public int sphereDefusesCount;

        [Newtonsoft.Json.JsonProperty("ultimate_activations")]
        public int ultimateActivations;

        // ─ Economy & Rating ─────────────────────────────────
        [Newtonsoft.Json.JsonProperty("elo_before")]
        public int eloBefore;

        [Newtonsoft.Json.JsonProperty("elo_delta")]
        public int eloDelta;

        [Newtonsoft.Json.JsonProperty("peak_credits_this_match")]
        public int peakCreditsThisMatch;
    }
}
