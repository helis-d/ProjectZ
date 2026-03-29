using FishNet.Object;
using ProjectZ.GameMode;
using ProjectZ.Weapon;
using UnityEngine;

namespace ProjectZ.Hero.Kant
{
    /// <summary>
    /// Kant's Ultimate: Adrenaline Rush (GDD Section 8)
    /// Grants a temporary fire-rate buff to self and nearby allies.
    /// </summary>
    public class AdrenalineRush : UltimateAbility
    {
        [Header("Settings")]
        [SerializeField] private float _buffRadius = 15.0f;
        [SerializeField] private float _fireRateMultiplier = 1.5f; // +50% Fire Rate
        [SerializeField] private float _duration = 8.0f;

        [Server]
        public override void Activate()
        {
            if (OwnerController == null) return;

            TeamManager tm = TeamManager.Instance;
            if (tm == null) return;

            ProjectZ.Core.Team myTeam = tm.GetTeam(OwnerController.OwnerId);

            // Find all players in the server
            foreach (var client in FishNet.Managing.NetworkManager.Instances[0].ClientManager.Clients.Values)
            {
                if (client.FirstObject == null) continue;

                // Check Team
                ProjectZ.Core.Team targetTeam = tm.GetTeam(client.ClientId);
                if (targetTeam == myTeam || targetTeam == ProjectZ.Core.Team.None) // None check for Solo modes
                {
                    // Check Distance
                    float distance = Vector3.Distance(OwnerController.transform.position, client.FirstObject.transform.position);
                    if (distance <= _buffRadius)
                    {
                        // Target is an ally within radius (or self)
                        WeaponManager wm = client.FirstObject.GetComponent<WeaponManager>();
                        if (wm != null)
                        {
                            // Send RPC so the client applies the buff on their end visually/mechanically
                            TargetApplyBuff(client, wm.gameObject, _fireRateMultiplier, _duration);
                        }
                    }
                }
            }

            // Cleanup the cast object over network
            Despawn(gameObject);
        }

        [TargetRpc]
        private void TargetApplyBuff(FishNet.Connection.NetworkConnection conn, GameObject weaponObj, float multiplier, float duration)
        {
            WeaponManager wm = weaponObj.GetComponent<WeaponManager>();
            if (wm != null)
            {
                var weapon = wm.GetActiveWeapon();
                if (weapon != null)
                {
                    weapon.ApplyTemporaryFireRateBuff(multiplier, duration);
                    Debug.Log($"[AdrenalineRush] Kant buffed your fire rate by {multiplier}x for {duration}s!");
                }
            }
        }
    }
}

