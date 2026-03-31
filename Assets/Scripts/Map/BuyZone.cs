using ProjectZ.Core;
using ProjectZ.GameMode;
using UnityEngine;

namespace ProjectZ.Map
{
    /// <summary>
    /// Team-gated buy zone. Can be used both by local UI checks and server purchase validation.
    /// Falls back gracefully when no zones are configured in the current scene.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class BuyZone : MonoBehaviour
    {
        [SerializeField] private Team _team = Team.None;

        public Team Team => _team;

        private void Awake()
        {
            Collider col = GetComponent<Collider>();
            if (col != null)
                col.isTrigger = true;
        }

        public void Configure(Team team)
        {
            _team = team;
        }

        public bool IsFriendlyTo(int playerId)
        {
            TeamManager teamManager = TeamManager.Instance;
            if (teamManager == null)
                return true;

            Team playerTeam = teamManager.GetTeam(playerId);
            return _team == Team.None || playerTeam == Team.None || _team == playerTeam;
        }

        public bool Contains(GameObject playerObject)
        {
            if (playerObject == null)
                return false;

            Collider zoneCollider = GetComponent<Collider>();
            if (zoneCollider == null)
                return false;

            CharacterController characterController = playerObject.GetComponent<CharacterController>();
            if (characterController != null)
            {
                return zoneCollider.bounds.Intersects(characterController.bounds);
            }

            Collider playerCollider = playerObject.GetComponent<Collider>() ?? playerObject.GetComponentInChildren<Collider>();
            return playerCollider != null && zoneCollider.bounds.Intersects(playerCollider.bounds);
        }

        public static bool HasConfiguredZones()
        {
            return FindObjectsByType<BuyZone>(FindObjectsSortMode.None).Length > 0;
        }

        public static bool IsPlayerInsideFriendlyZone(GameObject playerObject, int playerId)
        {
            return TryGetFriendlyZone(playerObject, playerId, out _);
        }

        public static bool TryGetFriendlyZone(GameObject playerObject, int playerId, out BuyZone friendlyZone)
        {
            friendlyZone = null;
            if (playerObject == null)
                return false;

            BuyZone[] zones = FindObjectsByType<BuyZone>(FindObjectsSortMode.None);
            foreach (BuyZone zone in zones)
            {
                if (zone == null || !zone.isActiveAndEnabled)
                    continue;

                if (!zone.IsFriendlyTo(playerId))
                    continue;

                if (!zone.Contains(playerObject))
                    continue;

                friendlyZone = zone;
                return true;
            }

            return false;
        }
    }
}
