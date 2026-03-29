using System.Collections.Generic;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using ProjectZ.Core;
using ProjectZ.Hero;
using ProjectZ.Weapon;
using UnityEngine;

namespace ProjectZ.Player
{
    /// <summary>
    /// Manages hero selection and ultimate charge (0-100%).
    /// Charge gain: Kill +15, Assist +10 (assist wiring pending).
    /// </summary>
    public class PlayerHeroController : NetworkBehaviour
    {
        [Header("Settings")]
        [SerializeField] private HeroData _selectedHero;

        public readonly SyncVar<float> UltimateCharge = new(0f);
        public readonly SyncVar<bool> _isPistolRound = new();

        private UltimateAbility _activeUltimate;
        private PlayerInputHandler _input;
        private GameObject _originalUltimatePrefab;
        private bool _hasStolenUltimate;

        public HeroData Hero => _selectedHero;
        public bool IsUltimateReady => UltimateCharge.Value >= 100f;

        private void Awake()
        {
            _input = GetComponent<PlayerInputHandler>();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            if (_selectedHero != null)
                EquipHero(_selectedHero);

            GameEvents.OnPlayerDeath += HandlePlayerDeath;
            GameEvents.OnPlayerAssist += HandlePlayerAssist;
        }

        public override void OnStopServer()
        {
            base.OnStopServer();
            GameEvents.OnPlayerDeath -= HandlePlayerDeath;
            GameEvents.OnPlayerAssist -= HandlePlayerAssist;
        }

        private void Update()
        {
            if (!IsOwner || _input == null)
                return;

            if (_input.UltimatePressed && IsUltimateReady)
                CmdTryActivateUltimate();
        }

        [ServerRpc]
        private void CmdTryActivateUltimate()
        {
            TryActivateUltimate();
        }

        [Server]
        public void EquipHero(HeroData hero)
        {
            _selectedHero = hero;
            UltimateCharge.Value = 0f;
            _originalUltimatePrefab = hero.ultimateAbilityPrefab;
            _hasStolenUltimate = false;

            EquipUltimatePrefab(_originalUltimatePrefab);
        }

        [Server]
        public void EquipStolenUltimate(GameObject stolenPrefab)
        {
            _hasStolenUltimate = true;
            EquipUltimatePrefab(stolenPrefab);
        }

        [Server]
        public void SetPistolRound(bool isPistol)
        {
            _isPistolRound.Value = isPistol;
        }

        [Server]
        private void EquipUltimatePrefab(GameObject prefab)
        {
            if (_activeUltimate != null)
            {
                if (_activeUltimate.IsSpawned)
                    Despawn(_activeUltimate.gameObject);
                Destroy(_activeUltimate.gameObject);
            }

            if (prefab == null)
                return;

            GameObject obj = Instantiate(prefab, transform);
            ServerManager.Spawn(obj, Owner);

            _activeUltimate = obj.GetComponent<UltimateAbility>();
            if (_activeUltimate != null)
                _activeUltimate.Initialize(this);
        }

        [Server]
        public void AddCharge(float amount)
        {
            if (_isPistolRound.Value) // Check pistol round here
                return;

            if (UltimateCharge.Value < 100f)
            {
                UltimateCharge.Value = Mathf.Clamp(UltimateCharge.Value + amount, 0f, 100f);
                if (UltimateCharge.Value >= 100f)
                    Debug.Log($"[{gameObject.name}] Ultimate Ready!");
            }
        }

        [Server]
        public void SpendUltimate()
        {
            UltimateCharge.Value = 0f;
        }

        [Server]
        public bool TryActivateUltimate()
        {
            if (_activeUltimate == null)
                return false;

            if (!_hasStolenUltimate && _activeUltimate.GetType().Name == "IdentityTheft")
            {
                _activeUltimate.Activate();
                return false;
            }

            if (UltimateCharge.Value < 100f) return false; // Check if ultimate is ready
            if (_isPistolRound.Value) return false; // Check pistol round

            SpendUltimate(); // Use the new method
            _activeUltimate.Activate();

            if (_hasStolenUltimate)
            {
                _hasStolenUltimate = false;
                EquipUltimatePrefab(_originalUltimatePrefab);
            }

            WeaponMasteryManager mastery = GetComponent<WeaponMasteryManager>();
            if (mastery != null)
            {
                WeaponManager wm = GetComponent<WeaponManager>();
                if (wm != null)
                {
                    BaseWeapon activeWeapon = wm.GetActiveWeapon();
                    if (activeWeapon != null)
                        mastery.ProcessUltimateCast(new System.Collections.Generic.List<BaseWeapon> { activeWeapon });
                }
            }

            return true;
        }

        private void HandlePlayerDeath(int victimId, int killerId)
        {
            if (!IsServerInitialized || victimId == OwnerId)
                return;

            if (killerId == OwnerId)
            {
                AddCharge(15f);
            }
        }

        private void HandlePlayerAssist(int assisterId, int victimId)
        {
            if (!IsServerInitialized) return;

            if (assisterId == OwnerId)
            {
                AddCharge(10f); // GDD Section 8: Assist +10% charge
            }
        }
    }
}
