using ProjectZ.Core;
using ProjectZ.Player;
using ProjectZ.Sphere;
using UnityEngine;

namespace ProjectZ.GameMode
{
    /// <summary>
    /// Ranked mode (GDD Section 7):
    /// 13 rounds, 1:45 round time, side swap after round 12.
    /// </summary>
    public class RankedGameMode : BaseGameMode
    {
        public const int WinsRequired = 7;
        public const int StartingMoney = 800;
        public const int KillReward = 200;
        public const int RoundWinReward = 3000;
        public const int RoundLossReward = 1900;
        public const int SphereReward = 300;

        private int _attackerRoundWins;
        private int _defenderRoundWins;
        private RoundManager _roundManager;

        public override void OnStartServer()
        {
            roundTimeLimit = 105f;
            maxRounds = 13;
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

            if (!TryGetComponent(out TeamManager tm))
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

            Debug.Log($"[Ranked] Score -> ATK {_attackerRoundWins} : DEF {_defenderRoundWins}");

            if (roundNumber == 12 && TryGetComponent(out TeamManager tm))
                tm.SwapTeams();

            if (_attackerRoundWins >= WinsRequired)
                GameEvents.InvokeMatchEnd(Team.Attacker);
            else if (_defenderRoundWins >= WinsRequired)
                GameEvents.InvokeMatchEnd(Team.Defender);
        }

        public bool IsPistolRound(int roundNumber)
        {
            return roundNumber == 1 || roundNumber == maxRounds;
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
