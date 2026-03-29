using FishNet.Object;
using FishNet.Object.Synchronizing;
using ProjectZ.Core;
using UnityEngine;

namespace ProjectZ.Player
{
    /// <summary>
    /// Per-player K/D/A statistics tracker.
    /// Synced to all clients for scoreboard display (GDD Section 10).
    /// </summary>
    public class PlayerStats : NetworkBehaviour
    {
        public readonly SyncVar<int> Kills = new();
        public readonly SyncVar<int> Deaths = new();
        public readonly SyncVar<int> Assists = new();

        public override void OnStartServer()
        {
            base.OnStartServer();
            GameEvents.OnPlayerDeath  += HandlePlayerDeath;
            GameEvents.OnPlayerAssist += HandlePlayerAssist;
        }

        public override void OnStopServer()
        {
            base.OnStopServer();
            GameEvents.OnPlayerDeath  -= HandlePlayerDeath;
            GameEvents.OnPlayerAssist -= HandlePlayerAssist;
        }

        private void HandlePlayerDeath(int victimId, int killerId)
        {
            if (!IsServerInitialized) return;

            if (victimId == OwnerId)
            {
                Deaths.Value++;
            }

            if (killerId == OwnerId && killerId != victimId)
            {
                Kills.Value++;
            }
        }

        private void HandlePlayerAssist(int assisterId, int victimId)
        {
            if (!IsServerInitialized) return;

            if (assisterId == OwnerId)
            {
                Assists.Value++;
            }
        }

        /// <summary>Reset stats (e.g. new match).</summary>
        [Server]
        public void ResetStats()
        {
            Kills.Value = 0;
            Deaths.Value = 0;
            Assists.Value = 0;
        }
    }
}
