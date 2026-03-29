using FishNet.Object;
using UnityEngine;

namespace ProjectZ.Network
{
    /// <summary>
    /// Registers and tracks all network-spawnable prefabs.
    /// Assign the Player prefab (and future prefabs) in the Inspector.
    /// FishNet's DefaultPrefabObjects asset handles the actual registration;
    /// this component acts as a convenient runtime reference holder.
    /// </summary>
    public class NetworkPrefabManager : MonoBehaviour
    {
        public static NetworkPrefabManager Instance { get; private set; }

        [Header("Spawnable Prefabs")]
        [Tooltip("The networked player prefab to spawn on connection.")]
        [SerializeField] private NetworkObject _playerPrefab;

        public NetworkObject PlayerPrefab => _playerPrefab;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }
}
