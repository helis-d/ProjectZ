using FishNet.Object;
using ProjectZ.Player;
using UnityEngine;

namespace ProjectZ.Network
{
    /// <summary>
    /// Sits on the Player object. When a local player connects, this grabs
    /// their cached Nakama profile (Name, Loadout, Selected Hero) and 
    /// sends a ServerRpc to configure the server-side authoritative components
    /// (PlayerInventory, PlayerStats, etc.).
    /// </summary>
    public class NakamaProfileSyncer : NetworkBehaviour
    {
        [SerializeField] private PlayerInventory _inventory;
        // Optionally add hero/stat references here later if needed

        private void Awake()
        {
            if (_inventory == null)
                _inventory = GetComponent<PlayerInventory>();
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            // Only the owner syncs their own profile to the server
            if (!IsOwner) return;

            // Check if NakamaManager and a cached profile exist
            if (NakamaManager.Instance != null && NakamaManager.Instance.CachedProfile != null)
            {
                var p = NakamaManager.Instance.CachedProfile;
                Debug.Log($"[ProfileSyncer] Syncing Nakama profile to server: {p.displayName} | {p.primaryWeaponId} / {p.secondaryWeaponId} / {p.meleeWeaponId}");
                
                CmdSyncProfile(p.displayName, p.primaryWeaponId, p.secondaryWeaponId, p.meleeWeaponId);
            }
            else
            {
                Debug.LogWarning("[ProfileSyncer] Nakama profile not found natively. Defaults will be used.");
            }
        }

        [ServerRpc]
        private void CmdSyncProfile(string displayName, string primaryId, string secondaryId, string meleeId)
        {
            // Update inventory
            if (_inventory != null)
            {
                // Force spawn weapons through PlayerInventory
                var pmData = Weapon.WeaponCatalog.Instance?.GetById(primaryId);
                if (pmData != null) _inventory.PickUpWeapon(pmData);

                var secData = Weapon.WeaponCatalog.Instance?.GetById(secondaryId);
                if (secData != null) _inventory.PickUpWeapon(secData);

                var meleeData = Weapon.WeaponCatalog.Instance?.GetById(meleeId);
                if (meleeData != null) _inventory.PickUpWeapon(meleeData);
            }

            // Sync the name (if we have a PlayerName component, set it here)
            Debug.Log($"[Server] Profile synced for client {OwnerId}: Name={displayName}");
        }
    }
}
