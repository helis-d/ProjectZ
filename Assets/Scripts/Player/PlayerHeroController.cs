using FishNet.Object;
using FishNet.Object.Synchronizing;
using ProjectZ.Core;
using ProjectZ.Hero.Helix;
using ProjectZ.Hero.Jacob;
using ProjectZ.Hero.Jielda;
using ProjectZ.Hero.Kant;
using ProjectZ.Hero.Lagrange;
using ProjectZ.Hero.Marcus;
using ProjectZ.Hero.Sai;
using ProjectZ.Hero.Samuel;
using ProjectZ.Hero.Sector;
using ProjectZ.Hero.Silvia;
using ProjectZ.Hero.Volt;
using ProjectZ.Hero.Zauhll;
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
        public readonly SyncVar<string> SelectedHeroId = new(string.Empty);

        private UltimateAbility _activeUltimate;
        private PlayerInputHandler _input;
        private bool _hasStolenUltimate;
        private UltimateAbilityId _baseUltimateId;

        public HeroData Hero => _selectedHero;
        public bool IsUltimateReady => UltimateCharge.Value >= 100f;
        public bool IsPistolRound => _isPistolRound.Value;

        private void Awake()
        {
            _input = GetComponent<PlayerInputHandler>();
            SelectedHeroId.OnChange += HandleSelectedHeroIdChanged;
        }

        private void OnDestroy()
        {
            SelectedHeroId.OnChange -= HandleSelectedHeroIdChanged;
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            if (_selectedHero != null)
                EquipHero(_selectedHero);

            GameEvents.OnPlayerDeath += HandlePlayerDeath;
            GameEvents.OnPlayerAssist += HandlePlayerAssist;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            if (!string.IsNullOrWhiteSpace(SelectedHeroId.Value))
                ApplyResolvedHeroData(SelectedHeroId.Value);
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
            if (hero == null)
                return;

            _selectedHero = hero;
            SelectedHeroId.Value = string.IsNullOrWhiteSpace(hero.heroId) ? string.Empty : hero.heroId.Trim().ToLowerInvariant();
            UltimateCharge.Value = 0f;
            _baseUltimateId = hero.ultimateId;
            _hasStolenUltimate = false;

            EquipUltimate(hero.ultimateId);
        }

        [Server]
        public void EquipStolenUltimate(UltimateAbilityId stolenUltimateId)
        {
            if (stolenUltimateId == UltimateAbilityId.None)
                return;

            _hasStolenUltimate = true;
            EquipUltimate(stolenUltimateId);
        }

        [Server]
        public void SetPistolRound(bool isPistol)
        {
            _isPistolRound.Value = isPistol;
        }

        [Server]
        private void EquipUltimate(UltimateAbilityId ultimateId)
        {
            _activeUltimate = ResolveAttachedUltimate(ultimateId);
            if (_activeUltimate == null)
            {
                if (ultimateId != UltimateAbilityId.None)
                {
                    Debug.LogWarning($"[PlayerHeroController] No runtime ultimate component is attached for {ultimateId} on {gameObject.name}.");
                }
                return;
            }

            _activeUltimate.Initialize(this);
        }

        [Server]
        public void AddCharge(float amount)
        {
            if (RoundRuleGuards.SuppressProgressionForPistolRound(_isPistolRound.Value))
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
            {
                if (_selectedHero != null && _selectedHero.ultimateId != UltimateAbilityId.None)
                {
                    Debug.LogWarning($"[PlayerHeroController] {_selectedHero.heroName} has canonical ultimate data but no matching runtime component is attached to the player prefab.");
                }
                return false;
            }

            if (!_hasStolenUltimate && _activeUltimate.GetType().Name == "IdentityTheft")
            {
                _activeUltimate.Activate();
                return false;
            }

            if (UltimateCharge.Value < 100f) return false; // Check if ultimate is ready
            if (RoundRuleGuards.SuppressProgressionForPistolRound(_isPistolRound.Value)) return false;

            SpendUltimate(); // Use the new method
            _activeUltimate.Activate();

            if (_hasStolenUltimate)
            {
                _hasStolenUltimate = false;
                EquipUltimate(_baseUltimateId);
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

        private UltimateAbility ResolveAttachedUltimate(UltimateAbilityId ultimateId)
        {
            return ultimateId switch
            {
                UltimateAbilityId.SiegeBreaker => GetComponent<SiegeBreaker>(),
                UltimateAbilityId.QuantumRewind => GetComponent<TemporalRewind>(),
                UltimateAbilityId.Panopticon => GetComponent<PanopticonTotem>(),
                UltimateAbilityId.DoomsdayCharge => GetComponent<DoomsdayCharge>(),
                UltimateAbilityId.OverdriveCore => GetComponent<OverdriveCore>(),
                UltimateAbilityId.BloodPact => GetComponent<BloodPact>(),
                UltimateAbilityId.SpiritWolves => GetComponent<SpiritWolves>(),
                UltimateAbilityId.VoidWalk => GetComponent<VoidWalk>(),
                UltimateAbilityId.SystemFailure => GetComponent<SystemFailure>(),
                UltimateAbilityId.BladeDance => GetComponent<BladeDance>(),
                UltimateAbilityId.OneWayMirror => GetComponent<OneWayMirror>(),
                UltimateAbilityId.Echo => GetComponent<IdentityTheft>(),
                UltimateAbilityId.GrappleStrike => GetComponent<GrappleStrike>(),
                _ => null
            };
        }

        private void HandleSelectedHeroIdChanged(string previousHeroId, string nextHeroId, bool asServer)
        {
            if (!string.IsNullOrWhiteSpace(nextHeroId))
                ApplyResolvedHeroData(nextHeroId);
        }

        private void ApplyResolvedHeroData(string heroId)
        {
            HeroData resolvedHero = HeroCatalog.Instance.GetById(heroId);
            if (resolvedHero != null)
                _selectedHero = resolvedHero;
        }

        private void HandlePlayerDeath(int victimId, int killerId)
        {
            if (!IsServerInitialized || victimId == OwnerId)
                return;

            if (killerId == OwnerId)
            {
                float chargePerKill = _selectedHero != null ? _selectedHero.ultimateChargePerKill : 15f;
                AddCharge(chargePerKill);
            }
        }

        private void HandlePlayerAssist(int assisterId, int victimId)
        {
            if (!IsServerInitialized) return;

            if (assisterId == OwnerId)
            {
                float chargePerAssist = _selectedHero != null ? _selectedHero.ultimateChargePerAssist : 10f;
                AddCharge(chargePerAssist);
            }
        }
    }
}
