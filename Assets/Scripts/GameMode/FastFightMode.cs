using ProjectZ.Core;
using ProjectZ.Player;
using ProjectZ.Sphere;
using UnityEngine;

namespace ProjectZ.GameMode
{
    /// <summary>
    /// Fast & Fight mode (GDD Section 7):
    /// 1:30 round time, side swap after round 9, and pistol rounds on rounds 1 and 10.
    /// </summary>
    public class FastFightMode : BaseGameMode
    {
        public const int RegulationRoundCount = 10;
        public const int HalfTimeRound = 9;
        public const int SecondPistolRound = 10;
        public const int StartingMoney = 2000;
        private const int WinsRequired = 6;

        private int _attackerRoundWins;
        private int _defenderRoundWins;
        private RoundManager _roundManager;

        public override void OnStartServer()
        {
            roundTimeLimit = 90f;
            maxRounds = RegulationRoundCount;
            enableMastery = true;
            enableAbilities = true;
            enableEconomy = true;

            base.OnStartServer();

            _roundManager = GetComponent<RoundManager>();
            GameEvents.OnSphereDetonated += HandleBombDetonation;
            GameEvents.OnSphereDefused += HandleBombDefusal;
        }

        public override void OnStopServer()
        {
            base.OnStopServer();
            GameEvents.OnSphereDetonated -= HandleBombDetonation;
            GameEvents.OnSphereDefused -= HandleBombDefusal;
        }

        public override void OnRoundStart(int roundNumber)
        {
            base.OnRoundStart(roundNumber);

            bool pistolRound = IsPistolRound(roundNumber);
            ApplyPistolRoundRules(pistolRound);

            if (pistolRound)
                ResetEconomyForPistolRound(StartingMoney);
        }

        public override void CheckWinCondition()
        {
            if (!IsServerInitialized || _roundManager == null)
                return;

            TeamManager tm = TeamManager.Instance;
            if (tm == null)
                return;

            int atkAlive = CountAlive(tm.Attackers);
            int defAlive = CountAlive(tm.Defenders);

            bool spherePlanted = false;
            SphereManager sm = FindFirstObjectByType<SphereManager>();
            if (sm != null && (sm.CurrentState.Value == SphereState.Active || sm.CurrentState.Value == SphereState.Defusing))
                spherePlanted = true;

            if (atkAlive == 0 && !spherePlanted)
                _roundManager.ForceEndRound(Team.Defender);
            else if (defAlive == 0 && !spherePlanted)
                _roundManager.ForceEndRound(Team.Attacker);
        }

        public override void OnRoundEnd(Team winner, int roundNumber)
        {
            if (winner == Team.Attacker)
                _attackerRoundWins++;
            else if (winner == Team.Defender)
                _defenderRoundWins++;

            if (roundNumber == HalfTimeRound && TeamManager.Instance != null)
                TeamManager.Instance.SwapTeams();

            if (_attackerRoundWins >= WinsRequired)
                GameEvents.InvokeMatchEnd(Team.Attacker);
            else if (_defenderRoundWins >= WinsRequired)
                GameEvents.InvokeMatchEnd(Team.Defender);
        }

        private bool IsPistolRound(int roundNumber)
        {
            return roundNumber == 1 || roundNumber == SecondPistolRound;
        }

        private int CountAlive(System.Collections.Generic.IReadOnlyList<int> ids)
        {
            int alive = 0;
            foreach (int id in ids)
            {
                if (!ServerManager.Clients.TryGetValue(id, out var conn) || conn.FirstObject == null)
                    continue;

                PlayerHealth health = conn.FirstObject.GetComponent<PlayerHealth>();
                if (health != null && !health.IsDead.Value)
                    alive++;
            }
            return alive;
        }

        private void HandleBombDetonation()
        {
            if (_roundManager != null)
                _roundManager.ForceEndRound(Team.Attacker);
        }

        private void HandleBombDefusal()
        {
            if (_roundManager != null)
                _roundManager.ForceEndRound(Team.Defender);
        }
    }
}
