using FishNet.Object;
using FishNet.Object.Synchronizing;
using ProjectZ.Core;
using UnityEngine;

namespace ProjectZ.Player
{
    /// <summary>
    /// Server-authoritative equipment separate from weapons (GDD Section 7 Sphere: 7.0s defuse, 3.5s with kit; Buy Menu Section 10 Equipment).
    /// Defuse kit is purchased during Buy Phase and consumed after a successful defuse.
    /// </summary>
    public class PlayerEquipment : NetworkBehaviour
    {
        public readonly SyncVar<bool> HasDefuseKit = new(false);

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

        [Server]
        private void HandleRoundStart(int _)
        {
            HasDefuseKit.Value = false;
        }

        /// <summary>Called by SphereManager after a completed defuse (kit is single-use per GDD-style tactical loop).</summary>
        [Server]
        public void ConsumeDefuseKitAfterSuccessfulDefuse()
        {
            HasDefuseKit.Value = false;
        }

        [Server]
        public void GrantDefuseKit()
        {
            HasDefuseKit.Value = true;
        }
    }
}
