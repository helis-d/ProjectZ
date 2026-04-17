using FishNet.Object;
using ProjectZ.Core;
using ProjectZ.Hero.Samuel;
using ProjectZ.Player;
using ProjectZ.Weapon;
using UnityEngine;

namespace ProjectZ.Combat
{
    /// <summary>
    /// Server-side damage pipeline.
    /// FinalDamage = BaseDamage * ZoneMultiplier * WallbangMultiplier
    /// </summary>
    public class DamageProcessor : NetworkBehaviour
    {
        public override void OnStartServer()
        {
            base.OnStartServer();
            GameEvents.OnRoundStart += HandleRoundStart;
        }

        public override void OnStopServer()
        {
            base.OnStopServer();
            GameEvents.OnRoundStart -= HandleRoundStart;
        }

        private void HandleRoundStart(int _)
        {
            DamageAssistRegistry.ClearAll();
        }

        public void ProcessDamage(int shooterConnId, WeaponData weaponData, HitscanResult hitscanResult, GameObject targetObject = null)
        {
            if (!IsServerInitialized || weaponData == null)
                return;
            if (!hitscanResult.DidHitPlayer)
                return;

            GameObject resolvedTarget = targetObject ?? hitscanResult.TargetObject;
            if (resolvedTarget == null)
                return;

            float baseDamage = weaponData.baseDamage;
            float zoneMultiplier = hitscanResult.HitboxResult.DamageMultiplier;
            float wallbangMult = hitscanResult.DamageMultiplier;
            float bloodPactMult = GetBloodPactMultiplier(shooterConnId);
            float finalDamage = baseDamage * zoneMultiplier * wallbangMult * bloodPactMult;

            HitboxZone zone = hitscanResult.HitboxResult.Zone;
            bool isHeadshot = zone == HitboxZone.Head || zone == HitboxZone.Neck;
            bool isWallbang = hitscanResult.WallsPenetrated > 0;

            PlayerHealth targetHealth = resolvedTarget.GetComponent<PlayerHealth>() ?? resolvedTarget.GetComponentInParent<PlayerHealth>();
            if (targetHealth == null)
                return;

            int victimConnId = targetHealth.OwnerId;

            // Record damage for assist tracking
            DamageAssistRegistry.RecordDamage(shooterConnId, victimConnId, finalDamage);
            if (!isHeadshot && !targetHealth.IsDead.Value)
                DamageAssistRegistry.BodyDamageVictimsThisRound.Add(victimConnId); // [FIX] BUG-17

            targetHealth.TakeDamage(finalDamage, shooterConnId);
            GameEvents.InvokePlayerDamaged(victimConnId, shooterConnId, finalDamage); // [FIX] BUG-16

            bool isKill = targetHealth.IsDead.Value;

            WeaponMasteryManager shooterMastery = GetPlayerMastery(shooterConnId);
            if (shooterMastery != null && isKill)
            {
                MasteryEventType killEvt = isHeadshot ? MasteryEventType.KillHead : MasteryEventType.KillBody;
                shooterMastery.ProcessMasteryEvent(killEvt, weaponData.weaponId);
            }

            if (isKill)
            {
                ProcessVictimDeath(victimConnId, isHeadshot);

                // Resolve assists for all other damage contributors
                DamageAssistRegistry.ResolveAssists(victimConnId, shooterConnId);

                // Fire extended kill details for killfeed
                GameEvents.InvokeKillDetails(shooterConnId, victimConnId, weaponData.weaponId, isHeadshot, isWallbang);
            }

            BroadcastHitInfo(shooterConnId, zone, isHeadshot, isWallbang, finalDamage);
        }

        public void ProcessAbilityDamage(int attackerConnId, float damage, PlayerHealth targetHealth, string damageSourceId)
        {
            if (!IsServerInitialized || targetHealth == null || targetHealth.IsDead.Value || damage <= 0f)
                return;

            int victimConnId = targetHealth.OwnerId;
            if (attackerConnId >= 0)
            {
                DamageAssistRegistry.RecordDamage(attackerConnId, victimConnId, damage);
                DamageAssistRegistry.BodyDamageVictimsThisRound.Add(victimConnId); // Using abilities counts as body damage for SpiritWolves
            }

            targetHealth.TakeDamage(damage, attackerConnId);
            GameEvents.InvokePlayerDamaged(victimConnId, attackerConnId, damage); // [FIX] BUG-16

            bool isKill = targetHealth.IsDead.Value;

            if (isKill)
            {
                ProcessVictimDeath(victimConnId, false);

                if (attackerConnId >= 0)
                {
                    DamageAssistRegistry.ResolveAssists(victimConnId, attackerConnId);
                    string sourceId = string.IsNullOrEmpty(damageSourceId) ? "ability" : damageSourceId;
                    GameEvents.InvokeKillDetails(attackerConnId, victimConnId, sourceId, false, false);
                }
                else
                {
                    DamageAssistRegistry.ClearVictim(victimConnId);
                }
            }

            BroadcastHitInfo(attackerConnId, HitboxZone.UpperChest, false, false, damage);
        }

        public void ProcessEnvironmentalDamage(float damage, PlayerHealth targetHealth, string damageSourceId = "environment")
        {
            ProcessAbilityDamage(-1, damage, targetHealth, damageSourceId);
        }

        public void ProcessVictimDeath(int victimConnId, bool killedByHeadshot)
        {
            if (!IsServerInitialized) return; // [FIX] BUG-02: prevent client invocation

            WeaponMasteryManager victimMastery = GetPlayerMastery(victimConnId);
            if (victimMastery == null)
                return;

            if (!ServerManager.Clients.TryGetValue(victimConnId, out var conn) || conn.FirstObject == null)
                return;

            WeaponManager wm = conn.FirstObject.GetComponent<WeaponManager>();
            if (wm == null)
            {
                Debug.LogWarning($"[DamageProcessor] BUG-21: WeaponManager null for victim {victimConnId}. Death XP penalty skipped."); // [FIX] BUG-21
                return;
            }
            
            BaseWeapon victimWeapon = wm.GetActiveWeapon();
            if (victimWeapon == null || victimWeapon.data == null)
                return;

            MasteryEventType evt = killedByHeadshot ? MasteryEventType.DeathHead : MasteryEventType.DeathBody;
            victimMastery.ProcessMasteryEvent(evt, victimWeapon.data.weaponId);
        }

        [ObserversRpc]
        private void BroadcastHitInfo(int shooterId, HitboxZone zone, bool headshot, bool wallbang, float damage)
        {
            Debug.Log($"[HitInfo] Shooter:{shooterId} Zone:{zone} HS:{headshot} WB:{wallbang} Dmg:{damage:F0}");
        }

        private WeaponMasteryManager GetPlayerMastery(int connId)
        {
            if (!ServerManager.Clients.TryGetValue(connId, out var conn) || conn.FirstObject == null)
                return null;

            return conn.FirstObject.GetComponent<WeaponMasteryManager>();
        }

        private float GetBloodPactMultiplier(int connId)
        {
            if (!ServerManager.Clients.TryGetValue(connId, out var conn) || conn.FirstObject == null)
                return 1f;

            BloodPact bloodPact = conn.FirstObject.GetComponent<BloodPact>();
            return bloodPact != null ? bloodPact.GetDamageMultiplier() : 1f;
        }
    }
}
