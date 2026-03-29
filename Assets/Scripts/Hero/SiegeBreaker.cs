using FishNet.Object;
using ProjectZ.Combat;
using ProjectZ.Core;
using ProjectZ.GameMode;
using UnityEngine;


namespace ProjectZ.Hero.Jacob
{
    /// <summary>
    /// Jacob's ultimate: Siege Breaker (GDD Section 8).
    /// Selects a 3×3m wall area where bullets lose no damage when penetrating.
    /// Duration: Current round + next round.
    /// </summary>
    public class SiegeBreaker : UltimateAbility
    {
        [Header("Siege Breaker")]
        [SerializeField] private float _zoneSize = 3.0f;
        [SerializeField] private GameObject _siegeZonePrefab;

        private GameObject _activeZone;
        private int _roundsRemaining;

        public override void Initialize(ProjectZ.Player.PlayerHeroController controller)
        {
            base.Initialize(controller);
            Core.GameEvents.OnRoundEnd += HandleRoundEnd;
        }

        private void OnDestroy()
        {
            Core.GameEvents.OnRoundEnd -= HandleRoundEnd;
        }

        [Server]
        public override void Activate()
        {
            if (!IsServerInitialized) return;

            // Place zone in front of the player, on the nearest wall via raycast
            Transform ownerTransform = transform;
            if (Physics.Raycast(ownerTransform.position, ownerTransform.forward, out RaycastHit hit, 20f))
            {
                Vector3 zonePos = hit.point;
                Quaternion zoneRot = Quaternion.LookRotation(hit.normal);

                if (_siegeZonePrefab != null)
                {
                    _activeZone = Instantiate(_siegeZonePrefab, zonePos, zoneRot);
                    _activeZone.transform.localScale = new Vector3(_zoneSize, _zoneSize, 0.5f);
                    ServerManager.Spawn(_activeZone);
                }
                else
                {
                    // Fallback: create a trigger box
                    _activeZone = new GameObject("SiegeBreakerZone");
                    _activeZone.transform.position = zonePos;
                    _activeZone.transform.rotation = zoneRot;

                    var col = _activeZone.AddComponent<BoxCollider>();
                    col.isTrigger = true;
                    col.size = new Vector3(_zoneSize, _zoneSize, 0.5f);

                    _activeZone.AddComponent<SiegeBreakerZone>();
                }

                _roundsRemaining = 2; // Current round + next round
                Debug.Log($"[SiegeBreaker] Zone placed at {zonePos}. Active for {_roundsRemaining} rounds.");
            }
        }

        private void HandleRoundEnd(Team winner, int roundNumber)
        {
            if (_activeZone == null) return;

            _roundsRemaining--;
            if (_roundsRemaining <= 0)
            {
                if (_activeZone.GetComponent<FishNet.Object.NetworkObject>() != null)
                    ServerManager.Despawn(_activeZone);
                else
                    Destroy(_activeZone);

                _activeZone = null;
                Debug.Log("[SiegeBreaker] Zone expired.");
            }
        }
    }

    /// <summary>
    /// Trigger zone component: bullets passing through this zone lose no damage.
    /// HitscanShooter should check for this zone and skip damage reduction.
    /// </summary>
    public class SiegeBreakerZone : MonoBehaviour
    {
        /// <summary>Returns true if a ray passes through any active SiegeBreakerZone.</summary>
        public static bool IsPointInSiegeZone(Vector3 point)
        {
            foreach (var zone in FindObjectsByType<SiegeBreakerZone>(FindObjectsSortMode.None))
            {
                Collider col = zone.GetComponent<Collider>();
                if (col != null && col.bounds.Contains(point))
                    return true;
            }
            return false;
        }
    }
}
