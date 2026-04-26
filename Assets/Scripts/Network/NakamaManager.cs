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
        public event Action<PlayerProfileData> OnProfileLoaded;

        // ─── Player Data Cache ────────────────────────────────────────────
        /// <summary>Cached player profile loaded from Nakama storage.</summary>
        public PlayerProfileData CachedProfile { get; private set; }

        private const string STORAGE_COLLECTION = "player_data";
        private const string STORAGE_KEY_PROFILE = "profile";
        private const string RPC_GET_PROFILE_STATE = "projectz_get_profile_state";
        private const string RPC_SELECT_HERO = "projectz_select_hero";
        private const string RPC_UNLOCK_HERO = "projectz_unlock_hero";
        private const string RPC_PURCHASE_OFFER = "projectz_purchase_offer";
        private const string RPC_SUBMIT_MATCH_TELEMETRY = "projectz_submit_match_telemetry";
        private const string RPC_FINALIZE_SIGNED_MATCH_RESULT = "projectz_finalize_signed_match_result";
        private const float SIGNED_RESULT_WAIT_WINDOW_SECONDS = 5f;
        private const float SIGNED_RESULT_REUSE_WINDOW_SECONDS = 15f;

        private AuthoritativeMatchResultPayload _pendingAuthoritativeMatchResult;
        private Task<BackendSignedMatchResultResponse> _authoritativeMatchResultTask;
        private string _authoritativeMatchResultTaskKey;
        private BackendSignedMatchResultResponse _lastAuthoritativeMatchResultResponse;
        private string _lastAuthoritativeMatchResultKey;
        private float _lastAuthoritativeMatchResultAt;

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
            _ = DisconnectAsync();
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
            if (_socket != null)
                await ResetSocketAsync();

            _socket = _client.NewSocket();
            _socket.Closed += HandleSocketClosed;
            _socket.ReceivedMatchmakerMatched += HandleMatchmakerMatched;

            await _socket.ConnectAsync(_session, true);
            Debug.Log("[Nakama] Socket connected.");
        }

        /// <summary>Gracefully disconnect the socket.</summary>
        public async Task DisconnectAsync()
        {
            await ResetSocketAsync();
        }

        // ─── Storage: Player Profile ──────────────────────────────────────
        /// <summary>
        /// Load the player's profile (loadout, currency, mastery) from Nakama storage.
        /// </summary>
        public async Task<PlayerProfileData> LoadPlayerProfileAsync()
        {
            BackendProfileRpcResponse authoritativeResponse = await CallBackendRpcAsync<BackendProfileRpcResponse>(RPC_GET_PROFILE_STATE, null);
            if (authoritativeResponse?.profile != null)
            {
                ApplyBackendProfile(authoritativeResponse.profile);
                Debug.Log($"[Nakama] Authoritative profile loaded: {CachedProfile.displayName}");
                return CachedProfile;
            }

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
                    OnProfileLoaded?.Invoke(CachedProfile);
                    Debug.Log($"[Nakama] Profile loaded: {CachedProfile.displayName}");
                    return CachedProfile;
                }

                // First time user - create defaults
                Debug.Log("[Nakama] No profile found, creating default...");
                CachedProfile = PlayerProfileData.CreateDefault(_session.Username);
                await SavePlayerProfileAsync(CachedProfile);
                OnProfileLoaded?.Invoke(CachedProfile);
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
                        PermissionRead = 1,
                        PermissionWrite = 1
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
            _ = performance;

            BackendSignedMatchResultResponse signedResponse = await TryAwaitSignedMatchResultAsync();
            if (signedResponse != null && signedResponse.succeeded)
                return CompetitiveRankSystem.BuildProgressionResult(signedResponse.previousRating, signedResponse.newRating);

            if (signedResponse != null)
                Debug.LogWarning($"[Nakama] Signed ranked result rejected: {signedResponse.errorCode} | {signedResponse.message}. Returning unchanged progression snapshot.");
            else
                Debug.LogWarning("[Nakama] Signed ranked result was unavailable. Returning unchanged progression snapshot.");

            return CompetitiveRankSystem.BuildProgressionResult(previousRating, previousRating);
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
            BackendPurchaseRpcResponse response = await CallBackendRpcAsync<BackendPurchaseRpcResponse>(
                RPC_UNLOCK_HERO,
                new BackendHeroRequest { heroId = heroId });

            if (response?.profile != null)
                ApplyBackendProfile(response.profile);

            if (response?.purchase != null)
                return response.purchase.ToPurchaseResult();

            return new MonetizationPurchaseResult(
                MonetizationPurchaseStatus.InvalidProfile,
                $"hero_unlock_{heroId}",
                heroId,
                response != null ? response.message : "Backend hero unlock istegi basarisiz oldu.");
        }

        public async Task<bool> SetSelectedHeroAsync(string heroId)
        {
            EnsureCachedProfile();
            BackendProfileRpcResponse response = await CallBackendRpcAsync<BackendProfileRpcResponse>(
                RPC_SELECT_HERO,
                new BackendHeroRequest { heroId = heroId });

            if (response?.profile != null)
                ApplyBackendProfile(response.profile);

            return response != null && response.succeeded;
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
            BackendPurchaseRpcResponse response = await CallBackendRpcAsync<BackendPurchaseRpcResponse>(
                RPC_PURCHASE_OFFER,
                new BackendOfferRequest { offerId = offerId });

            if (response?.profile != null)
                ApplyBackendProfile(response.profile);

            if (response?.purchase != null)
                return response.purchase.ToPurchaseResult();

            return new MonetizationPurchaseResult(
                MonetizationPurchaseStatus.InvalidOffer,
                offerId,
                offerId,
                response != null ? response.message : "Backend satin alim istegi basarisiz oldu.");
        }

        private void EnsureCachedProfile()
        {
            if (CachedProfile == null)
                CachedProfile = PlayerProfileData.CreateDefault(_session != null ? _session.Username : "NewPlayer");

            CachedProfile.Sanitize();
        }

        private async Task<TResponse> CallBackendRpcAsync<TResponse>(string rpcId, object payload)
            where TResponse : class
        {
            if (!IsAuthenticated || _client == null || _session == null)
            {
                Debug.LogWarning($"[Nakama] RPC skipped because there is no authenticated session: {rpcId}");
                return null;
            }

            try
            {
                string payloadJson = payload != null ? JsonConvert.SerializeObject(payload) : null;
                IApiRpc rpcResponse = string.IsNullOrWhiteSpace(payloadJson)
                    ? await _client.RpcAsync(_session, rpcId)
                    : await _client.RpcAsync(_session, rpcId, payloadJson);

                if (rpcResponse == null || string.IsNullOrWhiteSpace(rpcResponse.Payload))
                {
                    Debug.LogWarning($"[Nakama] RPC returned an empty payload: {rpcId}");
                    return null;
                }

                return JsonConvert.DeserializeObject<TResponse>(rpcResponse.Payload);
            }
            catch (ApiResponseException e)
            {
                Debug.LogError($"[Nakama] RPC {rpcId} failed: {e.Message}");
                return null;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Nakama] RPC {rpcId} failed unexpectedly: {e.Message}");
                return null;
            }
        }

        private void ApplyBackendProfile(PlayerProfileData profile)
        {
            if (profile == null)
                return;

            profile.Sanitize();
            CachedProfile = profile;
            OnProfileLoaded?.Invoke(CachedProfile);
        }

        public void QueueAuthoritativeMatchResult(AuthoritativeMatchResultPayload payload)
        {
            if (!AuthoritativeMatchResultSigning.HasValidBasics(payload))
            {
                Debug.LogWarning("[Nakama] Ignored malformed authoritative match result payload.");
                return;
            }

            if (IsAuthenticated && !IsPayloadForCurrentUser(payload))
            {
                Debug.LogWarning("[Nakama] Ignored authoritative match result for a different Nakama user.");
                return;
            }

            _pendingAuthoritativeMatchResult = payload;

            if (IsAuthenticated)
                _ = EnsureAuthoritativeMatchResultSubmittedAsync(payload);
        }

        private async Task<BackendSignedMatchResultResponse> TryAwaitSignedMatchResultAsync()
        {
            float deadline = Time.unscaledTime + SIGNED_RESULT_WAIT_WINDOW_SECONDS;
            while (Time.unscaledTime <= deadline)
            {
                if (_authoritativeMatchResultTask != null)
                {
                    BackendSignedMatchResultResponse awaitedResponse = await _authoritativeMatchResultTask;
                    if (awaitedResponse != null && awaitedResponse.succeeded)
                        return awaitedResponse;
                }

                if (HasRecentSignedMatchResult())
                    return _lastAuthoritativeMatchResultResponse;

                if (_pendingAuthoritativeMatchResult != null &&
                    string.IsNullOrWhiteSpace(_authoritativeMatchResultTaskKey))
                {
                    BackendSignedMatchResultResponse response = await EnsureAuthoritativeMatchResultSubmittedAsync(_pendingAuthoritativeMatchResult);
                    if (response != null && response.succeeded)
                        return response;
                }

                await Task.Delay(100);
            }

            return HasRecentSignedMatchResult() ? _lastAuthoritativeMatchResultResponse : null;
        }

        private bool HasRecentSignedMatchResult()
        {
            return _lastAuthoritativeMatchResultResponse != null
                && !string.IsNullOrWhiteSpace(_lastAuthoritativeMatchResultKey)
                && (Time.unscaledTime - _lastAuthoritativeMatchResultAt) <= SIGNED_RESULT_REUSE_WINDOW_SECONDS;
        }

        private async Task<BackendSignedMatchResultResponse> EnsureAuthoritativeMatchResultSubmittedAsync(AuthoritativeMatchResultPayload payload)
        {
            if (payload == null || !IsAuthenticated)
                return null;

            if (!IsPayloadForCurrentUser(payload))
            {
                Debug.LogWarning("[Nakama] Refusing to submit authoritative match result for a different Nakama user.");
                return null;
            }

            if (!string.IsNullOrWhiteSpace(_lastAuthoritativeMatchResultKey) &&
                string.Equals(_lastAuthoritativeMatchResultKey, payload.matchKey, StringComparison.OrdinalIgnoreCase) &&
                _lastAuthoritativeMatchResultResponse != null)
            {
                return _lastAuthoritativeMatchResultResponse;
            }

            if (_authoritativeMatchResultTask != null &&
                string.Equals(_authoritativeMatchResultTaskKey, payload.matchKey, StringComparison.OrdinalIgnoreCase))
            {
                return await _authoritativeMatchResultTask;
            }

            _authoritativeMatchResultTaskKey = payload.matchKey;
            _authoritativeMatchResultTask = SubmitAuthoritativeMatchResultInternalAsync(payload);
            return await _authoritativeMatchResultTask;
        }

        private async Task<BackendSignedMatchResultResponse> SubmitAuthoritativeMatchResultInternalAsync(AuthoritativeMatchResultPayload payload)
        {
            try
            {
                BackendSignedMatchResultResponse response = await CallBackendRpcAsync<BackendSignedMatchResultResponse>(
                    RPC_FINALIZE_SIGNED_MATCH_RESULT,
                    payload);

                if (response?.profile != null)
                    ApplyBackendProfile(response.profile);

                if (response != null && response.succeeded)
                {
                    _lastAuthoritativeMatchResultResponse = response;
                    _lastAuthoritativeMatchResultKey = string.IsNullOrWhiteSpace(response.matchKey) ? payload.matchKey : response.matchKey;
                    _lastAuthoritativeMatchResultAt = Time.unscaledTime;
                    _pendingAuthoritativeMatchResult = null;
                }

                return response;
            }
            finally
            {
                _authoritativeMatchResultTask = null;
                _authoritativeMatchResultTaskKey = null;
            }
        }

        private bool IsPayloadForCurrentUser(AuthoritativeMatchResultPayload payload)
        {
            return payload != null
                && !string.IsNullOrWhiteSpace(payload.userId)
                && !string.IsNullOrWhiteSpace(UserId)
                && string.Equals(payload.userId.Trim(), UserId.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        public bool HasPendingMatchToken()
        {
            return !string.IsNullOrWhiteSpace(PendingMatchToken);
        }

        public void ClearPendingMatchToken()
        {
            PendingMatchToken = null;
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
                return FindRankedMatchAsync();

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

                Debug.Log($"[Nakama] SBMM ticket issued — Query: {query} | Min: {minCount} Max: {maxCount} | Ticket: {RedactToken(ticket.Ticket)}");
                return ticket;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Nakama] Matchmaking failed: {e.Message}");
                return null;
            }
        }

        private async Task ResetSocketAsync()
        {
            if (_socket == null)
                return;

            ISocket socket = _socket;
            _socket = null;

            socket.Closed -= HandleSocketClosed;
            socket.ReceivedMatchmakerMatched -= HandleMatchmakerMatched;

            try
            {
                await socket.CloseAsync();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Nakama] Socket close raised while resetting: {e.Message}");
            }

            PendingMatchToken = null;
            _pendingAuthoritativeMatchResult = null;
            _authoritativeMatchResultTask = null;
            _authoritativeMatchResultTaskKey = null;
        }

        private void HandleSocketClosed(string reason)
        {
            Debug.Log($"[Nakama] Socket disconnected. Reason: {reason}");
            PendingMatchToken = null;
            OnDisconnected?.Invoke();
        }

        private void HandleMatchmakerMatched(IMatchmakerMatched matched)
        {
            PendingMatchToken = matched.Token;
            Debug.Log($"[Nakama] Match found! Token: {RedactToken(matched.Token)}");
            OnMatchFound?.Invoke(matched);
        }

        private static string RedactToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return "<empty>";

            int visibleChars = Mathf.Min(8, token.Length);
            return token.Substring(0, visibleChars) + "...";
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

            try
            {
                BackendTelemetryResponse response = await CallBackendRpcAsync<BackendTelemetryResponse>(
                    RPC_SUBMIT_MATCH_TELEMETRY,
                    new BackendTelemetryRequest
                    {
                        mapId = telemetry.mapId,
                        gameMode = telemetry.gameMode,
                        winningTeam = telemetry.winningTeam,
                        matchDurationSeconds = telemetry.matchDurationSeconds,
                        totalRoundsPlayed = telemetry.totalRoundsPlayed,
                        attackerRoundsWon = telemetry.attackerRoundsWon,
                        defenderRoundsWon = telemetry.defenderRoundsWon,
                        kills = telemetry.kills,
                        deaths = telemetry.deaths,
                        assists = telemetry.assists,
                        headshotCount = telemetry.headshotCount,
                        wallbangCount = telemetry.wallbangCount,
                        wasMvp = telemetry.wasMvp,
                        heroId = telemetry.heroId,
                        mostUsedWeaponId = telemetry.mostUsedWeaponId,
                        spherePlantsCount = telemetry.spherePlantsCount,
                        sphereDefusesCount = telemetry.sphereDefusesCount,
                        ultimateActivations = telemetry.ultimateActivations,
                        eloBefore = telemetry.eloBefore,
                        eloDelta = telemetry.eloDelta,
                        peakCreditsThisMatch = telemetry.peakCreditsThisMatch
                    });

                if (response != null && response.succeeded)
                {
                    telemetry.userId = UserId;
                    telemetry.matchKey = response.matchKey;
                    Debug.Log($"[Nakama] Telemetry saved authoritatively - Match: {response.matchKey} | Map: {telemetry.mapId} | Winner: {telemetry.winningTeam}");
                }
                else
                {
                    Debug.LogWarning($"[Nakama] Telemetry RPC returned a failure response: {response?.message}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[Nakama] Failed to save telemetry: {e.Message}");
            }
        }

        [Serializable]
        private sealed class BackendHeroRequest
        {
            public string heroId;
        }

        [Serializable]
        private sealed class BackendOfferRequest
        {
            public string offerId;
        }

        [Serializable]
        private sealed class BackendTelemetryRequest
        {
            public string mapId;
            public string gameMode;
            public string winningTeam;
            public float matchDurationSeconds;
            public int totalRoundsPlayed;
            public int attackerRoundsWon;
            public int defenderRoundsWon;
            public int kills;
            public int deaths;
            public int assists;
            public int headshotCount;
            public int wallbangCount;
            public bool wasMvp;
            public string heroId;
            public string mostUsedWeaponId;
            public int spherePlantsCount;
            public int sphereDefusesCount;
            public int ultimateActivations;
            public int eloBefore;
            public int eloDelta;
            public int peakCreditsThisMatch;
        }

        [Serializable]
        private class BackendProfileRpcResponse
        {
            public bool succeeded;
            public string errorCode;
            public string message;
            public PlayerProfileData profile;
        }

        [Serializable]
        private sealed class BackendPurchaseRpcResponse : BackendProfileRpcResponse
        {
            public BackendPurchasePayload purchase;
        }

        [Serializable]
        private class BackendRankedResultResponse : BackendProfileRpcResponse
        {
            public int previousRating;
            public int newRating;
            public int delta;
        }

        [Serializable]
        private sealed class BackendSignedMatchResultResponse : BackendRankedResultResponse
        {
            public string matchKey;
            public bool telemetrySaved;
            public bool alreadyProcessed;
        }

        [Serializable]
        private sealed class BackendTelemetryResponse
        {
            public bool succeeded;
            public string errorCode;
            public string message;
            public string matchKey;
        }

        [Serializable]
        private sealed class BackendPurchasePayload
        {
            public MonetizationPurchaseStatus status;
            public string offerId;
            public string contentId;
            public string message;
            public MonetizationCurrencyType currencyType;
            public int amountSpent;

            public MonetizationPurchaseResult ToPurchaseResult()
            {
                return new MonetizationPurchaseResult(
                    status,
                    offerId,
                    contentId,
                    message,
                    currencyType,
                    amountSpent);
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
