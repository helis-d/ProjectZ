using FishNet.Object;
using ProjectZ.Core;
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
            float finalDamage = baseDamage * zoneMultiplier * wallbangMult;

            HitboxZone zone = hitscanResult.HitboxResult.Zone;
            bool isHeadshot = zone == HitboxZone.Head || zone == HitboxZone.Neck;
            bool isWallbang = hitscanResult.WallsPenetrated > 0;

            PlayerHealth targetHealth = resolvedTarget.GetComponent<PlayerHealth>() ?? resolvedTarget.GetComponentInParent<PlayerHealth>();
            if (targetHealth == null)
                return;

            int victimConnId = targetHealth.OwnerId;

            // Record damage for assist tracking
            DamageAssistRegistry.RecordDamage(shooterConnId, victimConnId, finalDamage);

            targetHealth.TakeDamage(finalDamage, shooterConnId);
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

        public void ProcessVictimDeath(int victimConnId, bool killedByHeadshot)
        {
            WeaponMasteryManager victimMastery = GetPlayerMastery(victimConnId);
            if (victimMastery == null)
                return;

            if (!ServerManager.Clients.TryGetValue(victimConnId, out var conn) || conn.FirstObject == null)
                return;

            WeaponManager wm = conn.FirstObject.GetComponent<WeaponManager>();
            if (wm == null) return;
            
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
    }
}
