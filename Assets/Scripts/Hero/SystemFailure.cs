using System.Collections;
using FishNet.Object;
using ProjectZ.GameMode;
using ProjectZ.UI;
using UnityEngine;

namespace ProjectZ.Hero.Volt
{
    /// <summary>
    /// Volt's Ultimate: System Failure (GDD Section 8)
    /// Disables all enemy HUDs and maps for 5 seconds.
    /// </summary>
    public class SystemFailure : UltimateAbility
    {
        [Header("Settings")]
        [SerializeField] private float _duration = 5.0f;

        [Server]
        public override void Activate()
        {
            var tm = TeamManager.Instance;
            if (tm == null) return;

            ProjectZ.Core.Team myTeam = tm.GetTeam(OwnerController.OwnerId);

            // Find all connected clients that are not on my team
            foreach (var client in FishNet.Managing.NetworkManager.Instances[0].ClientManager.Clients.Values)
            {
                if (client.FirstObject == null) continue;

                ProjectZ.Core.Team targetTeam = tm.GetTeam(client.ClientId);
                if (targetTeam != myTeam && targetTeam != ProjectZ.Core.Team.None)
                {
                    // Target is an enemy. Send them the blackout RPC.
                    TargetApplyBlackout(client, _duration);
                }
            }

            // Cleanup or play local sfx...
            Debug.Log("[SystemFailure] Enemy HUD blackout pulse sent.");
        }

        [TargetRpc]
        private void TargetApplyBlackout(FishNet.Connection.NetworkConnection conn, float duration)
        {
            // This runs only on the targeted enemy's client
            if (HUDManager.Instance != null)
            {
                HUDManager.Instance.ShowSystemFailure(duration);
            }
        }
    }
}
