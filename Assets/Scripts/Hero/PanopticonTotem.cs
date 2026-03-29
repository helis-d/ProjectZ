using FishNet.Object;
using UnityEngine;

namespace ProjectZ.Hero.Sector
{
    /// <summary>
    /// Sector's Ultimate: Panopticon (GDD Section 8)
    /// Spawns a physical Radar Totem at the player's position.
    /// </summary>
    public class PanopticonTotem : UltimateAbility
    {
        [Header("Settings")]
        [SerializeField] private GameObject _totemPrefab;
        [SerializeField] private float _spawnForwardOffset = 1.0f;

        [Server]
        public override void Activate()
        {
            if (_totemPrefab == null || OwnerController == null) return;

            // Calculate spawn position (slightly in front of the player, on the ground)
            Vector3 spawnPos = OwnerController.transform.position + OwnerController.transform.forward * _spawnForwardOffset;
            
            // Adjust to ground level if necessary via Raycast, but assuming transform.position is roughly feet level for now.

            GameObject totemObj = Instantiate(_totemPrefab, spawnPos, Quaternion.identity);
            
            // Re-assign ownership to the caster so the Totem knows who to report sightings to
            ServerManager.Spawn(totemObj, OwnerController.Owner);

            TotemBehaviour behaviour = totemObj.GetComponent<TotemBehaviour>();
            if (behaviour != null)
            {
                behaviour.Initialize(OwnerController.OwnerId);
            }

            // Cleanup the ability controller shell
            Despawn(gameObject);
        }
    }
}
