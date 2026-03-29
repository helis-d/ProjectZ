using FishNet.Object;
using ProjectZ.Core;
using ProjectZ.Economy;
using ProjectZ.Player;
using ProjectZ.Weapon;
using UnityEngine;

namespace ProjectZ.GameMode
{
    /// <summary>
    /// Abstract base class for all game modes (GDD Section 7).
    /// Rule enforcement runs on server.
    /// </summary>
    public abstract class BaseGameMode : NetworkBehaviour
    {
        [Header("Round Configuration")]
        [SerializeField] protected float roundTimeLimit = 105f; // seconds
        [SerializeField] protected int maxRounds = 13;

        [Header("Feature Flags")]
        [SerializeField] protected bool enableMastery = true;
        [SerializeField] protected bool enableAbilities = true;
        [SerializeField] protected bool enableEconomy = true;

        public float RoundTimeLimit => roundTimeLimit;
        public int MaxRounds => maxRounds;
        public bool EnableMastery => enableMastery;
        public bool EnableAbilities => enableAbilities;
        public bool EnableEconomy => enableEconomy;

        public override void OnStartServer()
        {
            base.OnStartServer();
            Debug.Log($"[GameMode] {GetType().Name} started on server.");
            GameEvents.OnPlayerDeath += OnPlayerDeath;
        }

        public override void OnStopServer()
        {
            base.OnStopServer();
            GameEvents.OnPlayerDeath -= OnPlayerDeath;
        }

        public virtual void OnRoundStart(int roundNumber)
        {
            Debug.Log($"[GameMode] Round {roundNumber} started.");
        }

        protected virtual void OnPlayerDeath(int victimId, int killerId)
        {
            CheckWinCondition();
        }

        /// <summary>Evaluate whether a team has won the current round.</summary>
        public abstract void CheckWinCondition();

        /// <summary>Called when the round ends.</summary>
        public abstract void OnRoundEnd(Team winner, int roundNumber);

        protected void ApplyPistolRoundRules(bool isPistol)
        {
            foreach (var client in ServerManager.Clients.Values)
            {
                if (client.FirstObject == null)
                    continue;

                PlayerHeroController hero = client.FirstObject.GetComponent<PlayerHeroController>();
                if (hero != null)
                    hero.SetPistolRound(isPistol);

                WeaponMasteryManager mastery = client.FirstObject.GetComponent<WeaponMasteryManager>();
                if (mastery != null)
                    mastery.SetPistolRound(isPistol);
            }
        }

        protected void ResetEconomyForPistolRound(int startingMoney)
        {
            EconomyManager economy = GetComponent<EconomyManager>();
            if (economy != null)
                economy.ResetWalletsToStartingMoney(startingMoney);
        }
    }
}
