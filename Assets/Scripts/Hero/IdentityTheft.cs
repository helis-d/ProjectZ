using FishNet.Object;
using ProjectZ.Core;
using ProjectZ.Player;
using UnityEngine;

namespace ProjectZ.Hero.Kant
{
    /// <summary>
    /// Kant's Ultimate: Identity Theft (Mimic)
    /// Passively listens for kills. If charge is 100% and Kant kills an enemy,
    /// he steals their ultimate prefab and equips it for a single use.
    /// </summary>
    public class IdentityTheft : UltimateAbility
    {
        public override void Initialize(PlayerHeroController controller)
        {
            if (!BindOwner(controller))
                return;

            if (IsServerInitialized)
            {
                GameEvents.OnPlayerDeath += HandlePlayerDeath;
            }
        }

        public override void OnStopServer()
        {
            base.OnStopServer();
            if (IsServerInitialized)
            {
                GameEvents.OnPlayerDeath -= HandlePlayerDeath;
            }
        }

        [Server]
        public override void Activate()
        {
            // Identity Theft is a passive steal. Pressing X before stealing just gives a warning.
            Debug.Log("[IdentityTheft] Kant has no stolen ultimate yet. Kill an enemy while at 100% charge to steal theirs!");
        }

        [Server]
        private void HandlePlayerDeath(int victimId, int killerId)
        {
            if (OwnerController == null) return;

            // If Kant got a kill and his ultimate charge is ready
            if (killerId == OwnerController.OwnerId && OwnerController.IsUltimateReady)
            {
                // [FIX] BUG-19: use NetworkBehaviour.ServerManager — never index NetworkManager.Instances[]
                if (ServerManager.Clients.TryGetValue(victimId, out var victimConn))
                {
                    if (victimConn.FirstObject != null)
                    {
                        PlayerHeroController victimHero = victimConn.FirstObject.GetComponent<PlayerHeroController>();
                        
                        if (victimHero != null && victimHero.Hero != null && victimHero.Hero.ultimateId != UltimateAbilityId.None)
                        {
                            // Steal the ultimate!
                            OwnerController.EquipStolenUltimate(victimHero.Hero.ultimateId);
                            Debug.Log($"[IdentityTheft] Kant stole {victimHero.Hero.ultimateName} from Player {victimId}!");
                            
                            // Unsubscribe to avoid double stealing if multiple kills happen at once
                            GameEvents.OnPlayerDeath -= HandlePlayerDeath;
                        }
                    }
                }
            }
        }
    }
}
