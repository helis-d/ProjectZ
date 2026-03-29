using System.Collections.Generic;
using FishNet.Object;
using ProjectZ.Core;
using ProjectZ.GameMode;
using ProjectZ.Player;
using UnityEngine;

namespace ProjectZ.Economy
{
    /// <summary>
    /// Server-side global economy manager.
    /// Listens to kills, round ends, and bomb events, dispatching money
    /// to the relevant PlayerEconomy components.
    /// Implements GDD Section 7 economy rules.
    /// </summary>
    [RequireComponent(typeof(TeamManager))]
    [RequireComponent(typeof(BaseGameMode))]
    public class EconomyManager : NetworkBehaviour
    {
        // ─── Econ Stats ───────────────────────────────────────────────────
        private int _maxMoney;
        private int _killReward;
        private int _roundWinReward;
        private int _roundLossBaseReward;
        private int _sphereReward;
        private float _econMultiplier;

        // ─── State ────────────────────────────────────────────────────────
        private TeamManager _teamManager;
        private int _attackerLossStreak = 0;
        private int _defenderLossStreak = 0;

        // Need reference to all player economies. We look them up via NetworkManager.ServerManager
        // but for simplicity we rely on a registry or FindObjects for this module.
        // In full implementation, keep a Dictionary<int, PlayerEconomy>.

        public override void OnStartServer()
        {
            base.OnStartServer();
            _teamManager = GetComponent<TeamManager>();

            // Setup based on Game Mode type (Ranked vs Fast vs Duel)
            BaseGameMode mode = GetComponent<BaseGameMode>();
            
            if (mode is RankedGameMode ranked)
            {
                _maxMoney            = 9000;
                _killReward          = (int)RankedGameMode.KillReward;
                _roundWinReward      = (int)RankedGameMode.RoundWinReward;
                _roundLossBaseReward = (int)RankedGameMode.RoundLossReward;
                _sphereReward        = (int)RankedGameMode.SphereReward;
                _econMultiplier      = 1.0f;
            }
            else if (mode is FastFightMode fast)
            {
                _maxMoney            = 12000;
                _killReward          = 200;
                _roundWinReward      = 3000;
                _roundLossBaseReward = 1900;
                _sphereReward        = 300;
                _econMultiplier      = 1.5f; // GDD: 1.5x Multiplier
            }
            else
            {
                // Duel / Solo modes have Economy DISABLED (GDD Section 7)
                this.enabled = false;
                return;
            }

            // Apply multiplier
            _killReward          = (int)(_killReward * _econMultiplier);
            _roundWinReward      = (int)(_roundWinReward * _econMultiplier);
            _roundLossBaseReward = (int)(_roundLossBaseReward * _econMultiplier);
            _sphereReward        = (int)(_sphereReward * _econMultiplier);

            // Subscribe
            GameEvents.OnPlayerDeath   += HandlePlayerDeath;
            GameEvents.OnRoundEnd      += HandleRoundEnd;
            GameEvents.OnSpherePlanted += HandleSpherePlanted;
            GameEvents.OnSphereDefused += HandleSphereDefused;
        }

        public override void OnStopServer()
        {
            base.OnStopServer();
            GameEvents.OnPlayerDeath   -= HandlePlayerDeath;
            GameEvents.OnRoundEnd      -= HandleRoundEnd;
            GameEvents.OnSpherePlanted -= HandleSpherePlanted;
            GameEvents.OnSphereDefused -= HandleSphereDefused;
        }

        // ─── Callbacks ────────────────────────────────────────────────────
        private void HandlePlayerDeath(int victimId, int killerId)
        {
            if (victimId == killerId || killerId < 0) return; // Suicide / World kill

            Team killerTeam = _teamManager.GetTeam(killerId);
            Team victimTeam = _teamManager.GetTeam(victimId);

            if (killerTeam == victimTeam && killerTeam != Team.None) return; // Teamkill, no reward (maybe penalty?)

            var killerWallet = GetWallet(killerId);
            if (killerWallet != null)
            {
                killerWallet.AddMoney(_killReward, _maxMoney);
            }
        }

        private void HandleRoundEnd(Team winner, int roundNumber)
        {
            // Update loss streaks
            if (winner == Team.Attacker)
            {
                _attackerLossStreak = 0;
                _defenderLossStreak++;
            }
            else if (winner == Team.Defender)
            {
                _defenderLossStreak = 0;
                _attackerLossStreak++;
            }

            // Calculate loss reward based on streak (+500 per streak, max +1000)
            int atkLossReward = _roundLossBaseReward + Mathf.Min(_attackerLossStreak * 500, 1000);
            int defLossReward = _roundLossBaseReward + Mathf.Min(_defenderLossStreak * 500, 1000);

            // Distribute
            foreach (var atkId in _teamManager.Attackers)
            {
                var wallet = GetWallet(atkId);
                if (wallet != null)
                {
                    int reward = winner == Team.Attacker ? _roundWinReward : atkLossReward;
                    wallet.AddMoney(reward, _maxMoney);
                }
            }

            foreach (var defId in _teamManager.Defenders)
            {
                var wallet = GetWallet(defId);
                if (wallet != null)
                {
                    int reward = winner == Team.Defender ? _roundWinReward : defLossReward;
                    wallet.AddMoney(reward, _maxMoney);
                }
            }

            // Side swap logic: Ranked mode swaps at round 12. Streaks should reset.
            if (roundNumber == 12 && GetComponent<BaseGameMode>() is RankedGameMode)
            {
                _attackerLossStreak = 0;
                _defenderLossStreak = 0;
            }
        }

        private void HandleSpherePlanted(string siteId)
        {
            // Attackers get plant bonus
            foreach (var atkId in _teamManager.Attackers)
            {
                GetWallet(atkId)?.AddMoney(_sphereReward, _maxMoney);
            }
        }

        private void HandleSphereDefused()
        {
            // Defenders get defuse bonus
            foreach (var defId in _teamManager.Defenders)
            {
                GetWallet(defId)?.AddMoney(_sphereReward, _maxMoney);
            }
        }

        // ─── Helpers ──────────────────────────────────────────────────────
        private PlayerEconomy GetWallet(int connId)
        {
            // In a real project, we use ServerManager.Clients to get the connection,
            // then get the FirstObject on that connection.
            if (ServerManager.Clients.TryGetValue(connId, out var conn))
            {
                if (conn.FirstObject != null)
                {
                    return conn.FirstObject.GetComponent<PlayerEconomy>();
                }
            }
            return null;
        }

        /// <summary>Called by RoundManager at the start of pistol rounds (Round 1 + Half-time swap)</summary>
        public void ResetWalletsToStartingMoney(int startingMoney = 800)
        {
            foreach (var client in ServerManager.Clients.Values)
            {
                if (client.FirstObject != null)
                {
                    var wallet = client.FirstObject.GetComponent<PlayerEconomy>();
                    if (wallet != null) wallet.SetMoney(startingMoney);
                }
            }
        }
    }
}
