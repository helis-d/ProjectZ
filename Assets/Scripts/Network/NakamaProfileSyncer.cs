using FishNet.Object;
using ProjectZ.Hero;
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
        [SerializeField] private PlayerHeroController _heroController;

        private bool _hasSyncedProfile;

        private void Awake()
        {
            if (_inventory == null)
                _inventory = GetComponent<PlayerInventory>();

            if (_heroController == null)
                _heroController = GetComponent<PlayerHeroController>();
        }

        private void OnDestroy()
        {
            if (NakamaManager.Instance != null)
                NakamaManager.Instance.OnProfileLoaded -= HandleProfileLoaded;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            // Only the owner syncs their own profile to the server
            if (!IsOwner) return;

            TrySyncProfileOrSubscribe();
        }

        [ServerRpc]
        private void CmdSyncProfile(string displayName, string primaryId, string secondaryId, string meleeId, string selectedHeroId)
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

            if (_heroController != null)
            {
                HeroData heroData = HeroCatalog.Instance.GetById(selectedHeroId);
                if (heroData != null)
                    _heroController.EquipHero(heroData);
            }

            // Sync the name (if we have a PlayerName component, set it here)
            Debug.Log($"[Server] Profile synced for client {OwnerId}: Name={displayName}");
        }

        private void TrySyncProfileOrSubscribe()
        {
            NakamaManager manager = NakamaManager.Instance;
            if (manager == null)
            {
                Debug.LogWarning("[ProfileSyncer] NakamaManager not available yet. Waiting for profile load.");
                return;
            }

            manager.OnProfileLoaded -= HandleProfileLoaded;
            manager.OnProfileLoaded += HandleProfileLoaded;

            if (manager.CachedProfile != null)
                SyncProfile(manager.CachedProfile);
            else
                Debug.Log("[ProfileSyncer] Cached profile is not ready yet. Waiting for load callback.");
        }

        private void HandleProfileLoaded(PlayerProfileData profile)
        {
            SyncProfile(profile);
        }

        private void SyncProfile(PlayerProfileData profile)
        {
            if (!IsOwner || _hasSyncedProfile || profile == null)
                return;

            _hasSyncedProfile = true;

            if (NakamaManager.Instance != null)
                NakamaManager.Instance.OnProfileLoaded -= HandleProfileLoaded;

            Debug.Log(
                $"[ProfileSyncer] Syncing Nakama profile to server: {profile.displayName} | Hero={profile.selectedHero} | " +
                $"{profile.primaryWeaponId} / {profile.secondaryWeaponId} / {profile.meleeWeaponId}");

            CmdSyncProfile(
                profile.displayName,
                profile.primaryWeaponId,
                profile.secondaryWeaponId,
                profile.meleeWeaponId,
                profile.selectedHero);
        }
    }
}
