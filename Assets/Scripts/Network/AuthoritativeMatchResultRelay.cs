using System;
using FishNet.Connection;
using FishNet.Object;
using Newtonsoft.Json;
using ProjectZ.Core;
using ProjectZ.Economy;
using ProjectZ.GameMode;
using ProjectZ.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectZ.Network
{
    /// <summary>
    /// Builds a server-authored match summary for each connected player and signs
    /// it before relaying it to the owner client.
    /// </summary>
    [RequireComponent(typeof(TeamManager))]
    public class AuthoritativeMatchResultRelay : NetworkBehaviour
    {
        private string _matchKey;
        private float _matchStartTime;
        private bool _resultsRelayed;
        private RankedGameMode _rankedMode;
        private FastFightMode _fastFightMode;
        private TeamManager _teamManager;

        public override void OnStartServer()
        {
            base.OnStartServer();
            _teamManager = GetComponent<TeamManager>();
            _rankedMode = GetComponent<RankedGameMode>();
            _fastFightMode = GetComponent<FastFightMode>();
            _matchKey = Guid.NewGuid().ToString("N");
            _matchStartTime = Time.unscaledTime;
            _resultsRelayed = false;
            GameEvents.OnMatchEnd += HandleMatchEnd;
        }

        public override void OnStopServer()
        {
            base.OnStopServer();
            GameEvents.OnMatchEnd -= HandleMatchEnd;
        }

        [Server]
        private void HandleMatchEnd(Team winningTeam)
        {
            if (_resultsRelayed)
                return;

            _resultsRelayed = true;

            int bestScore = int.MinValue;
            int mvpOwnerId = -1;
            foreach (NetworkConnection conn in ServerManager.Clients.Values)
            {
                if (conn?.FirstObject == null)
                    continue;

                PlayerStats stats = conn.FirstObject.GetComponent<PlayerStats>();
                if (stats == null)
                    continue;

                int score = CalculateMvpScore(stats);
                if (score > bestScore)
                {
                    bestScore = score;
                    mvpOwnerId = conn.ClientId;
                }
            }

            foreach (NetworkConnection conn in ServerManager.Clients.Values)
            {
                if (conn?.FirstObject == null)
                    continue;

                NakamaProfileSyncer syncer = conn.FirstObject.GetComponent<NakamaProfileSyncer>();
                if (syncer == null || string.IsNullOrWhiteSpace(syncer.SyncedUserId))
                    continue;

                PlayerStats stats = conn.FirstObject.GetComponent<PlayerStats>();
                PlayerHeroController heroController = conn.FirstObject.GetComponent<PlayerHeroController>();
                PlayerEconomy economy = conn.FirstObject.GetComponent<PlayerEconomy>();
                Team playerTeam = _teamManager != null ? _teamManager.GetTeam(conn.ClientId) : Team.None;

                AuthoritativeMatchResultPayload payload = new AuthoritativeMatchResultPayload
                {
                    version = 1,
                    matchKey = _matchKey,
                    issuedAtUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    userId = syncer.SyncedUserId,
                    mapId = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.ToLowerInvariant(),
                    gameMode = ResolveGameMode(),
                    playerTeam = NormalizeTeam(playerTeam),
                    winningTeam = NormalizeTeam(winningTeam),
                    won = playerTeam != Team.None && playerTeam == winningTeam,
                    attackerRoundsWon = ResolveAttackerRoundsWon(),
                    defenderRoundsWon = ResolveDefenderRoundsWon(),
                    kills = stats != null ? stats.Kills.Value : 0,
                    deaths = stats != null ? stats.Deaths.Value : 0,
                    assists = stats != null ? stats.Assists.Value : 0,
                    wasMvp = conn.ClientId == mvpOwnerId,
                    heroId = ResolveHeroId(heroController),
                    matchDurationSeconds = Mathf.Max(1, Mathf.RoundToInt(Time.unscaledTime - _matchStartTime)),
                    headshotCount = 0,
                    wallbangCount = 0,
                    spherePlantsCount = 0,
                    sphereDefusesCount = 0,
                    ultimateActivations = 0,
                    peakCreditsThisMatch = economy != null ? Mathf.Max(0, economy.CurrentMoney.Value) : 0,
                    mostUsedWeaponId = string.Empty
                };

                payload.signature = AuthoritativeMatchResultSigning.ComputeSignature(payload);
                syncer.DeliverAuthoritativeMatchResult(conn, JsonConvert.SerializeObject(payload));
            }

            Debug.Log($"[SignedMatchResultRelay] Relayed signed results for match {_matchKey}.");
        }

        private int ResolveAttackerRoundsWon()
        {
            if (_rankedMode != null)
                return _rankedMode.AttackerRoundWins;

            return _fastFightMode != null ? _fastFightMode.AttackerRoundWins : 0;
        }

        private int ResolveDefenderRoundsWon()
        {
            if (_rankedMode != null)
                return _rankedMode.DefenderRoundWins;

            return _fastFightMode != null ? _fastFightMode.DefenderRoundWins : 0;
        }

        private string ResolveGameMode()
        {
            if (_rankedMode != null)
                return "ranked";

            return _fastFightMode != null ? "fastfight" : "unknown";
        }

        private static int CalculateMvpScore(PlayerStats stats)
        {
            return (stats.Kills.Value * 3) + stats.Assists.Value - stats.Deaths.Value;
        }

        private static string ResolveHeroId(PlayerHeroController heroController)
        {
            if (heroController == null)
                return "volt";

            if (!string.IsNullOrWhiteSpace(heroController.SelectedHeroId.Value))
                return heroController.SelectedHeroId.Value;

            return heroController.Hero != null && !string.IsNullOrWhiteSpace(heroController.Hero.heroId)
                ? heroController.Hero.heroId
                : "volt";
        }

        private static string NormalizeTeam(Team team)
        {
            return team switch
            {
                Team.Attacker => "attacker",
                Team.Defender => "defender",
                _ => "none"
            };
        }
    }
}
