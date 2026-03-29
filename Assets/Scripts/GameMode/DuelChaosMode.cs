using System.Collections;
using ProjectZ.Core;
using UnityEngine;

namespace ProjectZ.GameMode
{
    /// <summary>
    /// Duel (Chaos) mode (GDD Section 7).
    /// 5 teams x 2 players, first to 100 kills, 10 minute cap.
    /// Mastery/abilities/economy disabled.
    /// </summary>
    public class DuelChaosMode : BaseGameMode
    {
        [Header("Duel Rules")]
        [SerializeField] private int _targetKills = 100;
        [SerializeField] private float _respawnTime = 3.0f;
        [SerializeField] private float _timeLimitSeconds = 600f;

        private readonly int[] _teamScores = new int[8];

        public override void OnStartServer()
        {
            roundTimeLimit = _timeLimitSeconds;
            maxRounds = 1;
            enableMastery = false;
            enableAbilities = false;
            enableEconomy = false;

            base.OnStartServer();
            GameEvents.OnPlayerDeath += HandleDeath;
        }

        public override void OnStopServer()
        {
            base.OnStopServer();
            GameEvents.OnPlayerDeath -= HandleDeath;
        }

        public override void CheckWinCondition()
        {
            // Continuous score-based mode, win condition handled in HandleDeath.
        }

        public override void OnRoundEnd(Team winner, int roundNumber)
        {
            // No classic round scoring in this mode.
        }

        private void HandleDeath(int victimId, int killerId)
        {
            if (!IsServerInitialized) return;
            if (victimId == killerId || killerId < 0) return;

            TeamManager tm = TeamManager.Instance;
            if (tm == null) return;

            Team killerTeam = tm.GetTeam(killerId);
            Team victimTeam = tm.GetTeam(victimId);

            if (killerTeam != victimTeam && killerTeam != Team.None)
            {
                _teamScores[(int)killerTeam]++;
                if (_teamScores[(int)killerTeam] >= _targetKills)
                    GameEvents.InvokeMatchEnd(killerTeam);
            }

            StartCoroutine(RespawnRoutine(victimId));
        }

        private IEnumerator RespawnRoutine(int victimId)
        {
            yield return new WaitForSeconds(_respawnTime);

            if (!ServerManager.Clients.TryGetValue(victimId, out var conn) || conn.FirstObject == null)
                yield break;

            // Reset health
            Player.PlayerHealth health = conn.FirstObject.GetComponent<Player.PlayerHealth>();
            if (health != null)
                health.ResetHealth();

            // Clear damage records for this player (fresh slate after respawn)
            Combat.DamageAssistRegistry.ClearVictim(victimId);

            // Teleport to a safe spawn point
            TeamManager tm = TeamManager.Instance;
            if (tm != null)
            {
                Team team = tm.GetTeam(victimId);
                Transform spawnPoint = tm.GetSpawnPoint(team);

                if (spawnPoint != null)
                {
                    CharacterController cc = conn.FirstObject.GetComponent<CharacterController>();
                    if (cc != null) cc.enabled = false;
                    conn.FirstObject.transform.position = spawnPoint.position;
                    conn.FirstObject.transform.rotation = spawnPoint.rotation;
                    if (cc != null) cc.enabled = true;
                }
            }

            Debug.Log($"[Duel] Player {victimId} respawned.");
        }
    }
}
