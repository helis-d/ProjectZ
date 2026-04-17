using FishNet.Object;
using FishNet.Object.Synchronizing;
using ProjectZ.Core;
using UnityEngine;

namespace ProjectZ.Player
{
    /// <summary>
    /// Server-authoritative health and armor component.
    /// Handles damage pipeline logic: damage reduces armor first, then HP.
    /// When HP <= 0, triggers global death event via GameEvents.
    /// </summary>
    public class PlayerHealth : NetworkBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float _maxHealth = 100f;
        [SerializeField] private float _maxArmor  = 50f;

        // ─── Synced State ─────────────────────────────────────────────────
        public readonly SyncVar<float> CurrentHealth = new();
        public readonly SyncVar<float> CurrentArmor  = new();
        public readonly SyncVar<bool>  IsDead        = new();

        public float MaxHealth => _maxHealth;

        public override void OnStartServer()
        {
            base.OnStartServer();
            ResetHealth();
        }

        // ─── Public API (Server Only) ─────────────────────────────────────
        /// <summary>
        /// Apply damage to this player. Damage hits armor first, then bleeds to HP.
        /// </summary>
        /// <param name="amount">Total damage to apply.</param>
        /// <param name="instigatorConnId">Connection ID of the player who dealt the damage.</param>
        [Server]
        public void TakeDamage(float amount, int instigatorConnId)
        {
            if (IsDead.Value || amount <= 0f) return;

            if (CurrentArmor.Value > 0f)
            {
                if (amount <= CurrentArmor.Value)
                {
                    CurrentArmor.Value -= amount;
                    amount = 0f;
                }
                else
                {
                    amount -= CurrentArmor.Value;
                    CurrentArmor.Value = 0f;
                }
            }

            if (amount > 0f)
            {
                CurrentHealth.Value -= amount;
                if (CurrentHealth.Value <= 0f)
                {
                    CurrentHealth.Value = 0f;
                    Die(instigatorConnId);
                }
            }
        }

        [Server]
        public void AddArmor(float amount)
        {
            if (IsDead.Value) return;
            CurrentArmor.Value = Mathf.Clamp(CurrentArmor.Value + amount, 0f, _maxArmor);
        }

        [Server]
        public void AddHealth(float amount)
        {
            if (IsDead.Value) return;
            CurrentHealth.Value = Mathf.Clamp(CurrentHealth.Value + amount, 0f, _maxHealth);
        }

        /// <summary>
        /// Adds HP that can exceed MaxHealth up to overhealCeiling.
        /// Used exclusively by BloodPact (GDD: "+50hp per kill with overheal").
        /// </summary>
        [Server]
        public void AddHealthOverheal(float amount, float overhealCeiling) // [FIX] BUG-18
        {
            if (IsDead.Value) return;
            float ceiling = Mathf.Max(_maxHealth, overhealCeiling);
            CurrentHealth.Value = Mathf.Clamp(CurrentHealth.Value + amount, 0f, ceiling);
        }

        [Server]
        public void ResetHealth()
        {
            CurrentHealth.Value = _maxHealth;
            // Armor is typically bought, so we don't reset it to max automatically
            // But for testing/initial spawn we can leave it or clear it based on GameMode rules.
            CurrentArmor.Value  = 0f;
            IsDead.Value        = false;
        }

        // ─── Internal ─────────────────────────────────────────────────────
        private void Die(int killerConnId)
        {
            IsDead.Value = true;
            Debug.Log($"[PlayerHealth] Player {OwnerId} died. Killed by {killerConnId}.");
            GameEvents.InvokePlayerDeath(OwnerId, killerConnId);
        }
    }
}
