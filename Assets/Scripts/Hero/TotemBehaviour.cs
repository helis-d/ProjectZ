using System.Collections;
using FishNet.Object;
using ProjectZ.Core;
using ProjectZ.GameMode;
using ProjectZ.Player;
using UnityEngine;

namespace ProjectZ.Hero.Sector
{
    /// <summary>
    /// The physical totem spawned by Sector. Pulse-scans a 30m radius every second.
    /// Commands enemy OutlineControllers to flash red.
    /// </summary>
    public class TotemBehaviour : NetworkBehaviour
    {
        [Header("Radar Settings")]
        [SerializeField] private float _scanRadius = 30.0f;
        [SerializeField] private float _scanInterval = 1.0f;
        [SerializeField] private float _lifeTime = 10.0f;
        [SerializeField] private LayerMask _playerLayer; // Layer that Player Hitboxes or Roots sit on

        private int _ownerId = -1;
        private Team _ownerTeam = Team.None;

        [Server]
        public void Initialize(int ownerId)
        {
            _ownerId = ownerId;
            if (TeamManager.Instance != null)
            {
                _ownerTeam = TeamManager.Instance.GetTeam(_ownerId);
            }

            // Begin scanning cycle
            StartCoroutine(ScanRoutine());

            // Destroy after lifetime
            StartCoroutine(SelfDestructRoutine());
        }

        [Server]
        private IEnumerator ScanRoutine()
        {
            while (true)
            {
                ScanPulse();
                yield return new WaitForSeconds(_scanInterval);
            }
        }

        [Server]
        private IEnumerator SelfDestructRoutine()
        {
            yield return new WaitForSeconds(_lifeTime);
            if (IsSpawned) Despawn(gameObject);
        }

        [Server]
        private void ScanPulse()
        {
            if (TeamManager.Instance == null) return;
            if (!ServerManager.Clients.TryGetValue(_ownerId, out var ownerConn))
                return;

            int playerMask = _playerLayer.value == 0 ? Physics.AllLayers : _playerLayer.value;
            Collider[] hits = Physics.OverlapSphere(transform.position, _scanRadius, playerMask);
            foreach (Collider hit in hits)
            {
                // Find root network object
                NetworkObject netObj = hit.GetComponentInParent<NetworkObject>();
                if (netObj != null && netObj.Owner.IsValid)
                {
                    Team targetTeam = TeamManager.Instance.GetTeam(netObj.OwnerId);

                    // Check if it's an enemy
                    if (targetTeam != _ownerTeam && targetTeam != Team.None)
                    {
                        OutlineController outline = netObj.GetComponent<OutlineController>();
                        if (outline != null)
                        {
                            // Target is enemy inside the radius: Send RPC to make them glow!
                            // The duration of the glow should be roughly the pulse interval (e.g. 1 sec)
                            outline.TargetShowOutline(ownerConn, _scanInterval);
                        }
                    }
                }
            }
            
            // Optional: Send a ClientRPC to Owner to draw a radar ping on their minimap
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0, 1, 1, 0.2f);
            Gizmos.DrawSphere(transform.position, _scanRadius);
        }
    }
}
