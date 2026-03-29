using System.Collections.Generic;
using ProjectZ.Core;
using ProjectZ.Player;
using UnityEngine;

namespace ProjectZ.GameMode
{
    /// <summary>
    /// Solo / Tournament mode (GDD Section 7).
    /// 5v5 lobby where players duel 1v1 while others spectate.
    /// Mastery/abilities/economy disabled.
    /// </summary>
    public class SoloTournamentMode : BaseGameMode
    {
        [Header("Tournament Rules")]
        [SerializeField] private Transform _arenaSpawn1;
        [SerializeField] private Transform _arenaSpawn2;
        [SerializeField] private Transform _spectatorArea;

        private readonly Queue<int> _attackerQueue = new();
        private readonly Queue<int> _defenderQueue = new();

        private int _currentCombatantA = -1;
        private int _currentCombatantB = -1;
        private int _attackerDuelWins;
        private int _defenderDuelWins;
        private RoundManager _roundManager;

        public override void OnStartServer()
        {
            roundTimeLimit = 60f;
            maxRounds = 10;
            enableMastery = false;
            enableAbilities = false;
            enableEconomy = false;

            base.OnStartServer();

            _roundManager = GetComponent<RoundManager>();

            TeamManager tm = TeamManager.Instance;
            if (tm != null)
            {
                foreach (int atk in tm.Attackers)
                    _attackerQueue.Enqueue(atk);

                foreach (int def in tm.Defenders)
                    _defenderQueue.Enqueue(def);
            }
        }

        public override void OnRoundStart(int roundNumber)
        {
            base.OnRoundStart(roundNumber);

            _currentCombatantA = _attackerQueue.Count > 0 ? _attackerQueue.Dequeue() : -1;
            _currentCombatantB = _defenderQueue.Count > 0 ? _defenderQueue.Dequeue() : -1;

            TeleportPlayers();
        }

        public override void CheckWinCondition()
        {
            if (!IsServerInitialized || _roundManager == null)
                return;

            // Check which combatant died
            bool combatantADead = IsCombatantDead(_currentCombatantA);
            bool combatantBDead = IsCombatantDead(_currentCombatantB);

            if (combatantADead && !combatantBDead)
            {
                // Attacker lost this duel → Defender wins
                _roundManager.ForceEndRound(Team.Defender);
            }
            else if (combatantBDead && !combatantADead)
            {
                // Defender lost this duel → Attacker wins
                _roundManager.ForceEndRound(Team.Attacker);
            }
            else if (combatantADead && combatantBDead)
            {
                // Both dead (simultaneous) → draw, defender advantage
                _roundManager.ForceEndRound(Team.Defender);
            }
        }

        public override void OnRoundEnd(Team winner, int roundNumber)
        {
            if (winner == Team.Attacker)
                _attackerDuelWins++;
            else if (winner == Team.Defender)
                _defenderDuelWins++;

            Debug.Log($"[Solo] Duel Score -> ATK {_attackerDuelWins} : DEF {_defenderDuelWins}");

            // Re-queue combatants for future rounds
            if (_currentCombatantA >= 0) _attackerQueue.Enqueue(_currentCombatantA);
            if (_currentCombatantB >= 0) _defenderQueue.Enqueue(_currentCombatantB);

            // GDD: Team with most round wins at end of 10 rounds
            int winsNeeded = (maxRounds / 2) + 1; // 6 wins for 10 rounds
            if (_attackerDuelWins >= winsNeeded)
                GameEvents.InvokeMatchEnd(Team.Attacker);
            else if (_defenderDuelWins >= winsNeeded)
                GameEvents.InvokeMatchEnd(Team.Defender);
        }

        private bool IsCombatantDead(int connId)
        {
            if (connId < 0) return true;
            if (!ServerManager.Clients.TryGetValue(connId, out var conn) || conn.FirstObject == null)
                return true;

            PlayerHealth health = conn.FirstObject.GetComponent<PlayerHealth>();
            return health != null && health.IsDead.Value;
        }

        private void TeleportPlayers()
        {
            foreach (var client in ServerManager.Clients.Values)
            {
                if (client.FirstObject == null)
                    continue;

                CharacterController cc = client.FirstObject.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;

                if (client.ClientId == _currentCombatantA && _arenaSpawn1 != null)
                {
                    client.FirstObject.transform.position = _arenaSpawn1.position;
                }
                else if (client.ClientId == _currentCombatantB && _arenaSpawn2 != null)
                {
                    client.FirstObject.transform.position = _arenaSpawn2.position;
                }
                else if (_spectatorArea != null)
                {
                    client.FirstObject.transform.position = _spectatorArea.position;
                }

                if (cc != null) cc.enabled = true;
            }
        }
    }
}
