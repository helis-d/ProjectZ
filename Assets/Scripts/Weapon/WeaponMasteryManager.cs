using System.Collections.Generic;
using System.Linq;
using FishNet.Object;
using UnityEngine;

namespace ProjectZ.Weapon
{
    /// <summary>
    /// Per-player Weapon Mastery manager.
    /// Handles XP progression, cold streak checks, pistol round exceptions, and buff application.
    /// </summary>
    public class WeaponMasteryManager : NetworkBehaviour
    {
        [Header("Buff Configurations")]
        [Tooltip("Assign one config per weapon class.")]
        [SerializeField] private WeaponTypeBuffConfig[] _buffConfigs;

        [Header("Competitive balance")]
        [Tooltip("1 = full GDD handling buffs. Lower (e.g. 0.35) pulls ADS/reload/move/fire-rate buffs toward neutral for ranked integrity. See Docs/COMPETITIVE_INTEGRITY_PASS.md.")]
        [SerializeField] [Range(0f, 1f)] private float _masteryHandlingStrength = 1f;

        private readonly Dictionary<string, WeaponRuntimeData> _weaponData = new();
        private readonly Queue<int> _killsPerRound = new();
        private readonly Dictionary<WeaponType, WeaponTypeBuffConfig> _buffLookup = new();

        private int _killsThisRound;
        private bool _isPistolRound;
        private BaseWeapon _equippedWeapon;

        public override void OnStartServer()
        {
            base.OnStartServer();

            foreach (WeaponTypeBuffConfig cfg in _buffConfigs)
            {
                if (cfg != null)
                    _buffLookup[cfg.weaponClass] = cfg;
            }

            Core.GameEvents.OnRoundStart += HandleRoundStart;
            Core.GameEvents.OnRoundEnd += HandleRoundEnd;
        }

        public override void OnStopServer()
        {
            base.OnStopServer();
            Core.GameEvents.OnRoundStart -= HandleRoundStart;
            Core.GameEvents.OnRoundEnd -= HandleRoundEnd;
        }

        public WeaponRuntimeData RegisterWeapon(string weaponId)
        {
            if (_weaponData.TryGetValue(weaponId, out WeaponRuntimeData existing))
                return existing;

            WeaponRuntimeData data = new WeaponRuntimeData { WeaponID = weaponId };
            data.OnLevelChanged += (_, _) => RecalculateBuffs(weaponId);
            _weaponData[weaponId] = data;
            return data;
        }

        public void SetEquippedWeapon(BaseWeapon weapon)
        {
            _equippedWeapon = weapon;
            if (weapon != null && weapon.data != null)
                RecalculateBuffs(weapon.data.weaponId);
        }

        public void ProcessMasteryEvent(MasteryEventType evt, string weaponId)
        {
            if (!IsServerInitialized)
                return;

            bool isKillEvent = evt == MasteryEventType.KillBody || evt == MasteryEventType.KillHead;
            if (isKillEvent)
                _killsThisRound++;

            // GDD: no XP in pistol rounds, but kill counter must continue for cold streak tracking.
            if (_isPistolRound)
                return;

            WeaponRuntimeData data = GetOrCreate(weaponId);
            int xp = MasteryXPTable.GetXP(evt);
            data.AddXP(xp);

            if (isKillEvent)
                data.KillsInMatch++;
        }

        public void ProcessUltimateCast(List<BaseWeapon> inventory)
        {
            if (!IsServerInitialized || _isPistolRound)
                return;
            if (inventory == null || inventory.Count == 0)
                return;

            BaseWeapon target = inventory
                .Where(w => w != null && w.data != null)
                .OrderByDescending(w => w.data.price)
                .FirstOrDefault();

            if (target != null)
                ProcessMasteryEvent(MasteryEventType.UltiCast, target.data.weaponId);
        }

        public void ProcessToxicPenalty()
        {
            if (!IsServerInitialized)
                return;

            WeaponRuntimeData highest = _weaponData.Values
                .OrderByDescending(d => d.CurrentLevel)
                .ThenByDescending(d => d.CurrentXP)
                .FirstOrDefault();

            if (highest != null)
                highest.AddXP(MasteryXPTable.GetXP(MasteryEventType.PenaltyToxic));
        }

        public void OnWeaponDropped(string weaponId)
        {
            if (_weaponData.TryGetValue(weaponId, out WeaponRuntimeData data))
            {
                data.Reset();
                _weaponData.Remove(weaponId);
            }
        }

        public int GetLevel(string weaponId)
        {
            return _weaponData.TryGetValue(weaponId, out WeaponRuntimeData data) ? data.CurrentLevel : 1;
        }

        public int GetXP(string weaponId)
        {
            return _weaponData.TryGetValue(weaponId, out WeaponRuntimeData data) ? data.CurrentXP : 0;
        }

        public void SetPistolRound(bool isPistol)
        {
            _isPistolRound = isPistol;
        }

        private void HandleRoundStart(int _)
        {
            _killsThisRound = 0;
        }

        private void HandleRoundEnd(Core.Team winner, int roundNumber)
        {
            _killsPerRound.Enqueue(_killsThisRound);
            if (_killsPerRound.Count > 3)
                _killsPerRound.Dequeue();

            if (_killsPerRound.Count >= 3 && _killsPerRound.All(k => k == 0))
            {
                foreach (WeaponRuntimeData data in _weaponData.Values)
                    data.AddXP(MasteryXPTable.GetXP(MasteryEventType.DeathColdStreak));

                Debug.Log("[Mastery] Cold streak triggered. -60 XP applied.");
            }
        }

        private WeaponRuntimeData GetOrCreate(string weaponId)
        {
            if (!_weaponData.TryGetValue(weaponId, out WeaponRuntimeData data))
                data = RegisterWeapon(weaponId);

            return data;
        }

        private void RecalculateBuffs(string weaponId)
        {
            if (_equippedWeapon == null || _equippedWeapon.data == null)
                return;
            if (_equippedWeapon.data.weaponId != weaponId)
                return;

            WeaponRuntimeData data = GetOrCreate(weaponId);
            WeaponType wClass = _equippedWeapon.data.weaponClass;

            if (_buffLookup.TryGetValue(wClass, out WeaponTypeBuffConfig config))
            {
                LevelMultipliers multipliers = config.GetMultipliers(data.CurrentLevel);
                multipliers = LevelMultipliers.BlendTowardIdentity(multipliers, _masteryHandlingStrength);
                _equippedWeapon.ApplyBuffMultipliers(multipliers);
            }
        }
    }
}
