using FishNet.Object;
using ProjectZ.Player;
using ProjectZ.Weapon;
using UnityEngine;

namespace ProjectZ.Weapons
{
    /// <summary>
    /// Ground weapon pickup interactable.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class WeaponPickup : NetworkBehaviour
    {
        private WeaponData _weaponData;
        private WeaponRuntimeData _runtimeData;

        public void Initialize(WeaponData data, WeaponRuntimeData runtime)
        {
            _weaponData = data;
            _runtimeData = runtime;
        }

        [ServerRpc(RequireOwnership = false)]
        public void CmdInteract(int playerId)
        {
            if (_weaponData == null)
                return;

            if (ServerManager.Clients.TryGetValue(playerId, out var client) && client.FirstObject != null)
            {
                PlayerInventory inv = client.FirstObject.GetComponent<PlayerInventory>();
                if (inv != null)
                {
                    inv.PickUpWeapon(_weaponData, _runtimeData);
                    ServerManager.Despawn(gameObject);
                }
            }
        }
    }
}
