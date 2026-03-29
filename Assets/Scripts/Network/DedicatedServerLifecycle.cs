using FishNet.Object;
using ProjectZ.Core;
using UnityEngine;
using System.Collections;
using FishNet.Managing.Server;

namespace ProjectZ.Network
{
    /// <summary>
    /// Headless Server instance logic.
    /// Runs only when the game is built via Linux Server Headless Build.
    /// Handles Game End condition and container termination (Application.Quit).
    /// </summary>
    public class DedicatedServerLifecycle : NetworkBehaviour
    {
        private ServerManager _serverManager;

        private void Start()
        {
            // If we are NOT running in batch mode as a server, destroy this object
            if (!Application.isBatchMode)
            {
                Destroy(gameObject);
                return;
            }

            Debug.Log("[Dedicated] Headless Server initialized. Waiting for logic trigger...");
            
            _serverManager = GameNetworkManager.Instance.GetComponentInChildren<ServerManager>();
            if (_serverManager != null)
            {
                _serverManager.OnRemoteConnectionState += OnClientConnected;
            }
        }

        private void OnDestroy()
        {
            if (_serverManager != null)
                _serverManager.OnRemoteConnectionState -= OnClientConnected;
        }

        private void OnClientConnected(FishNet.Connection.NetworkConnection conn, FishNet.Transporting.RemoteConnectionStateArgs args)
        {
            if (args.ConnectionState == FishNet.Transporting.RemoteConnectionState.Stopped)
            {
                CheckEmptyLobby();
            }
        }

        /// <summary>
        /// Shut down the container if everyone leaves.
        /// Match orchestrator (Agones/Edgegap) will recycle the container slot.
        /// </summary>
        private void CheckEmptyLobby()
        {
            if (_serverManager.Clients.Count == 0)
            {
                Debug.LogWarning("[Dedicated] All players disconnected. Shutting down container in 5 seconds...");
                StartCoroutine(ShutdownRoutine());
            }
        }

        /// <summary>
        /// Triggered manually at the end of a match after sending Elo/XP results to Nakama REST API.
        /// </summary>
        public void MatchDidEndAndResultsSent()
        {
            Debug.Log("[Dedicated] Match completed successfully. Terminating container...");
            StartCoroutine(ShutdownRoutine());
        }

        private IEnumerator ShutdownRoutine()
        {
            yield return new WaitForSeconds(5f);
            
            GameNetworkManager.Instance.StopConnection();
            Application.Quit();
        }
    }
}
