using FishNet.Object;
using ProjectZ.Player;
using UnityEngine;

namespace ProjectZ.Hero.Lagrange
{
    /// <summary>
    /// Lagrange's Ultimate: Temporal Rewind (GDD Section 8)
    /// Teleports the player to their 10-second old position.
    /// </summary>
    public class TemporalRewind : UltimateAbility
    {
        [Server]
        public override void Activate()
        {
            if (OwnerController == null) return;

            // Find the tracker on the owner player
            TemporalTracker tracker = OwnerController.GetComponent<TemporalTracker>();
            if (tracker == null)
            {
                Debug.LogWarning("[TemporalRewind] Lagrange attempted to rewind but no TemporalTracker found on Player!");
                return;
            }

            Vector3 rewindPos = tracker.GetOldestPosition();

            // Perform the safe teleport by briefly disabling the CharacterController
            CharacterController cc = OwnerController.GetComponent<CharacterController>();
            if (cc != null)
            {
                cc.enabled = false;
                OwnerController.transform.position = rewindPos;
                cc.enabled = true;
            }
            else
            {
                // Fallback direct movement
                OwnerController.transform.position = rewindPos;
            }

            // Optional: TargetRpc to owner to play a flashy screen effect
            TargetPlayRewindVFX(OwnerController.Owner);

            Debug.Log("[TemporalRewind] Rewind complete.");
        }

        [TargetRpc]
        private void TargetPlayRewindVFX(FishNet.Connection.NetworkConnection conn)
        {
            // In a full implementation, you'd trigger an FOV warp, color shift, or sound effect here
            Debug.Log("[Lagrange] ZWOOSH! Time Rewound 10 seconds.");
        }
    }
}
