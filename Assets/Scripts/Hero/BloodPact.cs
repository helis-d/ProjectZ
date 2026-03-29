using FishNet.Object;
using ProjectZ.Core;
using ProjectZ.Player;
using ProjectZ.Weapon;
using UnityEngine;

namespace ProjectZ.Hero.Samuel
{
    /// <summary>
    /// Samuel's ultimate: Blood Pact (GDD Section 8).
    /// Bullets cost HP (-5 per shot), kills restore HP (+50, overheal possible).
    /// Critical state: HP ≤ 30 gives 1.3× damage multiplier.
    /// </summary>
    public class BloodPact : UltimateAbility
    {
        [Header("Blood Pact")]
        [SerializeField] private float _hpCostPerShot = 5f;
        [SerializeField] private float _hpPerKill = 50f;
        [SerializeField] private float _criticalHpThreshold = 30f;
        [SerializeField] private float _criticalDamageMultiplier = 1.3f;
        [SerializeField] private float _duration = 10f;

        private bool _isActive;
        private float _timer;
        private PlayerHealth _ownerHealth;
        private PlayerCombatController _combatController;

        [Server]
        public override void Activate()
        {
            if (!IsServerInitialized) return;

            _ownerHealth = GetComponent<PlayerHealth>();
            _combatController = GetComponent<PlayerCombatController>();
            if (_combatController != null)
                _combatController.OnServerFired += HandleWeaponFired;

            _isActive = true;
            _timer = _duration;

            GameEvents.OnPlayerDeath += HandleKill;
            Debug.Log("[BloodPact] Activated! Bullets now cost HP.");
        }

        private void HandleWeaponFired()
        {
            if (_isActive && _ownerHealth != null)
            {
                _ownerHealth.TakeDamage(_hpCostPerShot, -1); // Self-damage, no killer
            }
        }

        private void Update()
        {
            if (!IsServerInitialized || !_isActive) return;

            _timer -= Time.deltaTime;
            if (_timer <= 0f)
            {
                Deactivate();
                return;
            }
        }

        /// <summary>
        /// Returns the damage multiplier for this player. Called by DamageProcessor.
        /// </summary>
        public float GetDamageMultiplier()
        {
            if (!_isActive) return 1f;
            if (_ownerHealth != null && _ownerHealth.CurrentHealth.Value <= _criticalHpThreshold)
                return _criticalDamageMultiplier;
            return 1f;
        }

        private void HandleKill(int victimId, int killerId)
        {
            if (!_isActive || killerId != OwnerId) return;

            if (_ownerHealth != null)
            {
                // Overheal possible — add HP beyond max
                _ownerHealth.AddHealth(_hpPerKill);
                Debug.Log($"[BloodPact] Kill! +{_hpPerKill} HP (current: {_ownerHealth.CurrentHealth})");
            }
        }

        [Server]
        private void Deactivate()
        {
            _isActive = false;
            GameEvents.OnPlayerDeath -= HandleKill;
            if (_combatController != null)
                _combatController.OnServerFired -= HandleWeaponFired;
                
            Debug.Log("[BloodPact] Deactivated.");
        }

        private void OnDestroy()
        {
            if (_isActive) GameEvents.OnPlayerDeath -= HandleKill;
            if (_combatController != null)
                _combatController.OnServerFired -= HandleWeaponFired;
        }
    }
}
