using FishNet.Object;
using Newtonsoft.Json;
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
        public string SyncedDisplayName { get; private set; }
        public string SyncedUserId { get; private set; }

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
        private void CmdSyncProfile(string userId, string displayName, string primaryId, string secondaryId, string meleeId, string selectedHeroId)
        {
            SyncedUserId = string.IsNullOrWhiteSpace(userId) ? string.Empty : userId.Trim();
            SyncedDisplayName = string.IsNullOrWhiteSpace(displayName) ? $"player_{OwnerId}" : displayName.Trim();

            // Update inventory
            if (_inventory != null)
            {
                // Force spawn weapons through PlayerInventory
                var pmData = Weapon.WeaponCatalog.Resolve(primaryId);
                if (pmData != null) _inventory.PickUpWeapon(pmData);

                var secData = Weapon.WeaponCatalog.Resolve(secondaryId);
                if (secData != null) _inventory.PickUpWeapon(secData);

                var meleeData = Weapon.WeaponCatalog.Resolve(meleeId);
                if (meleeData != null) _inventory.PickUpWeapon(meleeData);
            }

            if (_heroController != null)
            {
                HeroData heroData = HeroCatalog.Instance.GetById(selectedHeroId);
                if (heroData != null)
                    _heroController.EquipHero(heroData);
            }

            // Sync the name (if we have a PlayerName component, set it here)
            Debug.Log($"[Server] Profile synced for client {OwnerId}: Name={SyncedDisplayName}");
        }

        [TargetRpc]
        private void TargetReceiveAuthoritativeMatchResult(FishNet.Connection.NetworkConnection conn, string serializedPayload)
        {
            if (string.IsNullOrWhiteSpace(serializedPayload))
                return;

            try
            {
                AuthoritativeMatchResultPayload payload = JsonConvert.DeserializeObject<AuthoritativeMatchResultPayload>(serializedPayload);
                if (!AuthoritativeMatchResultSigning.HasValidBasics(payload))
                {
                    Debug.LogWarning("[ProfileSyncer] Ignored malformed signed match result payload.");
                    return;
                }

                NakamaManager.Instance?.QueueAuthoritativeMatchResult(payload);
            }
            catch (JsonException exception)
            {
                Debug.LogWarning($"[ProfileSyncer] Failed to deserialize signed match result payload: {exception.Message}");
            }
        }

        [Server]
        public void DeliverAuthoritativeMatchResult(FishNet.Connection.NetworkConnection ownerConnection, string serializedPayload)
        {
            if (ownerConnection == null || !ownerConnection.IsValid || string.IsNullOrWhiteSpace(serializedPayload))
                return;

            TargetReceiveAuthoritativeMatchResult(ownerConnection, serializedPayload);
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

            NakamaManager manager = NakamaManager.Instance;
            string userId = manager != null ? manager.UserId : null;
            if (string.IsNullOrWhiteSpace(userId))
            {
                Debug.LogWarning("[ProfileSyncer] Cannot sync profile before Nakama user id is available.");
                return;
            }

            _hasSyncedProfile = true;

            manager.OnProfileLoaded -= HandleProfileLoaded;

            Debug.Log(
                $"[ProfileSyncer] Syncing Nakama profile to server: {profile.displayName} | Hero={profile.selectedHero} | " +
                $"{profile.primaryWeaponId} / {profile.secondaryWeaponId} / {profile.meleeWeaponId}");

            CmdSyncProfile(
                userId,
                profile.displayName,
                profile.primaryWeaponId,
                profile.secondaryWeaponId,
                profile.meleeWeaponId,
                profile.selectedHero);
        }
    }
}
