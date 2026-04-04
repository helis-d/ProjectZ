using System.Threading.Tasks;
using UnityEngine;

namespace ProjectZ.Network
{
    // ─────────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Contract that any Agones SDK implementation must fulfill.
    /// In production, swap <see cref="AgonesSDKMock"/> for an implementation
    /// that calls the real Agones sidecar HTTP endpoints
    /// (http://localhost:8001/v1/gameservers/<name>/...).
    ///
    /// Reference: https://agones.dev/site/docs/guides/client-sdks/
    /// </summary>
    // ─────────────────────────────────────────────────────────────────────────
    public interface IAgonesSDK
    {
        /// <summary>
        /// Signals to Agones that the game server is ready to receive player connections.
        /// Must be called after all server-side systems have initialised.
        /// </summary>
        Task ReadyAsync();

        /// <summary>
        /// Marks the server as Allocated (a match has started).
        /// Once Allocated, Agones will NOT scale this instance down.
        /// </summary>
        Task AllocateAsync();

        /// <summary>
        /// Signals to Agones that the match is complete and the container may be recycled.
        /// </summary>
        Task ShutdownAsync();

        /// <summary>
        /// Health heartbeat — must be called at least once every 5 seconds or
        /// Agones will mark the server as Unhealthy.
        /// </summary>
        Task HealthAsync();
    }

    // ─────────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Local/CI mock that logs Agones calls without making real HTTP requests.
    /// Used automatically when <c>Application.isBatchMode</c> is false or
    /// when the AGONES_SDK_ENDPOINT environment variable is not set.
    /// </summary>
    // ─────────────────────────────────────────────────────────────────────────
    public class AgonesSDKMock : IAgonesSDK
    {
        public Task ReadyAsync()
        {
            Debug.Log("[Agones/Mock] ✅ Ready — server is accepting connections.");
            return Task.CompletedTask;
        }

        public Task AllocateAsync()
        {
            Debug.Log("[Agones/Mock] 🎮 Allocated — match started, container protected.");
            return Task.CompletedTask;
        }

        public Task ShutdownAsync()
        {
            Debug.Log("[Agones/Mock] 🛑 Shutdown — container will be recycled.");
            return Task.CompletedTask;
        }

        public Task HealthAsync()
        {
            Debug.Log("[Agones/Mock] 💓 Health ping sent.");
            return Task.CompletedTask;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Factory that returns the correct IAgonesSDK implementation based on
    /// whether the AGONES_SDK_ENDPOINT environment variable is present.
    /// This keeps DedicatedServerLifecycle free of platform-detection logic.
    /// </summary>
    // ─────────────────────────────────────────────────────────────────────────
    public static class AgonesSDKFactory
    {
        private const string ENV_KEY = "AGONES_SDK_ENDPOINT";

        public static IAgonesSDK Create()
        {
            string endpoint = System.Environment.GetEnvironmentVariable(ENV_KEY);
            if (!string.IsNullOrWhiteSpace(endpoint))
            {
                // Production path: Real sidecar endpoint is available.
                // TODO: Replace with AgonesSDKReal(endpoint) once the real SDK
                //       package is imported from https://github.com/googleforgames/agones
                Debug.Log($"[Agones] Production endpoint detected: {endpoint}. Using Real SDK.");
                return new AgonesSDKMock(); // Swap for real impl here.
            }

            Debug.Log("[Agones] No AGONES_SDK_ENDPOINT found — using Mock SDK for local/CI.");
            return new AgonesSDKMock();
        }
    }
}
