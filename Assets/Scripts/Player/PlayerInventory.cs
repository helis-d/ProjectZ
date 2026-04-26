using FishNet.Object;
using FishNet.Object.Synchronizing;
using ProjectZ.Weapon;
using ProjectZ.Weapons;
using UnityEngine;

namespace ProjectZ.Player
{
    /// <summary>
    /// Server-authoritative inventory system.
    /// Slots: Primary(1), Secondary(2), Melee(3).
    /// 
    /// FishNet AudioClip serileştirme hatası yüzünden SyncVar<WeaponData> KULLANILAMAZ.
    /// Bunun yerine SyncVar<string> (weaponId) sync'lenir ve WeaponCatalog'dan çözümlenir.
    /// </summary>
    [RequireComponent(typeof(PlayerInputHandler))]
    public class PlayerInventory : NetworkBehaviour
    {
        [Header("References")]
        [SerializeField] private WeaponManager _weaponManager;
        [SerializeField] private GameObject _weaponPickupPrefab;

        [Header("Starting Loadout")]
        [SerializeField] private WeaponData _defaultSecondary;
        [SerializeField] private WeaponData _defaultMelee;

        // ─── Synced weapon IDs (string, FishNet-safe) ─────────────────────
        public readonly SyncVar<string> _primaryWeaponId   = new(string.Empty);
        public readonly SyncVar<string> _secondaryWeaponId = new(string.Empty);
        public readonly SyncVar<string> _meleeWeaponId     = new(string.Empty);

        private WeaponRuntimeData _primaryRuntime;
        private WeaponRuntimeData _secondaryRuntime;
        private WeaponRuntimeData _meleeRuntime;

        public readonly SyncVar<int> _activeSlot = new(3);

        private PlayerInputHandler _input;
        private WeaponMasteryManager _mastery;

        // ─── Helpers ──────────────────────────────────────────────────────
        private WeaponData GetWeaponData(string id) => WeaponCatalog.Resolve(id);
        public WeaponData ActiveWeaponData => GetWeaponData(GetActiveWeaponId());
        
        private string GetActiveWeaponId()
        {
            return _activeSlot.Value switch
            {
                1 => _primaryWeaponId.Value,
                2 => _secondaryWeaponId.Value,
                3 => _meleeWeaponId.Value,
                _ => string.Empty
            };
        }

        private void Awake()
        {
            _input = GetComponent<PlayerInputHandler>();
            _mastery = GetComponent<WeaponMasteryManager>();

            if (_weaponManager == null)
                _weaponManager = GetComponentInChildren<WeaponManager>();

            _weaponManager = WeaponRuntimeRigBuilder.EnsurePlayerRig(gameObject, _weaponManager);

            _activeSlot.OnChange += OnActiveSlotChanged;
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            if (_defaultSecondary == null)
                _defaultSecondary = WeaponCatalog.Resolve("pistol_classic");

            if (_defaultMelee == null)
                _defaultMelee = WeaponCatalog.Resolve("knife_tactical");

            if (_defaultSecondary != null)
            {
                _secondaryWeaponId.Value = _defaultSecondary.weaponId;
                _secondaryRuntime = new WeaponRuntimeData { WeaponID = _defaultSecondary.weaponId };
            }

            if (_defaultMelee != null)
            {
                _meleeWeaponId.Value = _defaultMelee.weaponId;
                _meleeRuntime = new WeaponRuntimeData { WeaponID = _defaultMelee.weaponId };
            }

            EquipWeapon(3, null); // Start with melee
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            _weaponManager = WeaponRuntimeRigBuilder.EnsurePlayerRig(gameObject, _weaponManager);
            RefreshWeaponController();
        }

        private void Update()
        {
            if (!IsOwner) return;

            // Slot switching
            if (_input.SlotAlphaPressed == 1 && !string.IsNullOrEmpty(_primaryWeaponId.Value)) RequestSwitchSlot(1);
            else if (_input.SlotAlphaPressed == 2 && !string.IsNullOrEmpty(_secondaryWeaponId.Value)) RequestSwitchSlot(2);
            else if (_input.SlotAlphaPressed == 3 && !string.IsNullOrEmpty(_meleeWeaponId.Value)) RequestSwitchSlot(3);

            // Drop weapon
            if (_input.DropPressed)
            {
                if (_activeSlot.Value == 1 && !string.IsNullOrEmpty(_primaryWeaponId.Value)) RequestDropWeapon(1);
                else if (_activeSlot.Value == 2 && !string.IsNullOrEmpty(_secondaryWeaponId.Value)) RequestDropWeapon(2);
            }
        }

        [Server]
        private void EquipWeapon(int slot, WeaponRuntimeData _)
        {
            _activeSlot.Value = slot;
            RefreshWeaponController();
        }

        [ServerRpc]
        private void RequestSwitchSlot(int slot)
        {
            if (slot == _activeSlot.Value) return;

            string id = slot switch
            {
                1 => _primaryWeaponId.Value,
                2 => _secondaryWeaponId.Value,
                3 => _meleeWeaponId.Value,
                _ => string.Empty
            };
            if (string.IsNullOrEmpty(id)) return;

            EquipWeapon(slot, null);
        }

        [ServerRpc]
        private void RequestDropWeapon(int slot) => DropWeaponInternal(slot);

        // [FIX] BUG-23: Server logic separated from ServerRpc to prevent server calling Rpc directly
        [Server]
        private void DropWeaponInternal(int slot)
        {
            if (_weaponPickupPrefab == null)
            {
                Debug.LogWarning("[Inventory] No pickup prefab configured. Drop request ignored.");
                return;
            }

            string dropId = string.Empty;

            if (slot == 1)
            {
                dropId = _primaryWeaponId.Value;
                _primaryWeaponId.Value = string.Empty;
                _primaryRuntime = null;
            }
            else if (slot == 2)
            {
                dropId = _secondaryWeaponId.Value;
                _secondaryWeaponId.Value = string.Empty;
                _secondaryRuntime = null;
            }

            if (string.IsNullOrEmpty(dropId)) return;

            WeaponData dropData = GetWeaponData(dropId);
            if (_mastery != null)
                _mastery.OnWeaponDropped(dropId);

            Vector3 dropPos = transform.position + transform.forward * 1.5f + Vector3.up;
            GameObject pickupObj = Instantiate(_weaponPickupPrefab, dropPos, Quaternion.identity);

            WeaponPickup pickup = pickupObj.GetComponent<WeaponPickup>();
            if (pickup != null && dropData != null)
                pickup.Initialize(dropData, new WeaponRuntimeData { WeaponID = dropId });

            ServerManager.Spawn(pickupObj);

            // Fallback to next available slot
            if (!string.IsNullOrEmpty(_primaryWeaponId.Value))
                _activeSlot.Value = 1;
            else if (!string.IsNullOrEmpty(_secondaryWeaponId.Value))
                _activeSlot.Value = 2;
            else
                _activeSlot.Value = 3;

            RefreshWeaponController();
        }

        [Server]
        public void PickUpWeapon(WeaponData data, WeaponRuntimeData runtime = null)
        {
            if (data == null) return;

            int targetSlot = data.weaponType == WeaponType.Pistol ? 2 : 1;

            if (targetSlot == 1)
            {
                if (!string.IsNullOrEmpty(_primaryWeaponId.Value))
                    DropWeaponInternal(1); // [FIX] BUG-23

                _primaryWeaponId.Value = data.weaponId;
                _primaryRuntime = runtime ?? new WeaponRuntimeData { WeaponID = data.weaponId };
                _activeSlot.Value = 1;
            }
            else
            {
                if (!string.IsNullOrEmpty(_secondaryWeaponId.Value))
                    DropWeaponInternal(2); // [FIX] BUG-23

                _secondaryWeaponId.Value = data.weaponId;
                _secondaryRuntime = runtime ?? new WeaponRuntimeData { WeaponID = data.weaponId };
                _activeSlot.Value = 2;
            }

            RefreshWeaponController();
        }

        private void OnActiveSlotChanged(int oldSlot, int newSlot, bool asServer)
        {
            RefreshWeaponController();
        }

        private void RefreshWeaponController()
        {
            if (_weaponManager == null)
                _weaponManager = WeaponRuntimeRigBuilder.EnsurePlayerRig(gameObject, _weaponManager);

            if (_weaponManager == null)
                return;

            string activeId = GetActiveWeaponId();
            WeaponData activeData = GetWeaponData(activeId);

            if (activeData == null)
            {
                _mastery?.SetEquippedWeapon(null);
                return;
            }

            _weaponManager.SwitchToSlot(_activeSlot.Value - 1);
            BaseWeapon activeWeapon = _weaponManager.GetActiveWeapon();

            _mastery?.SetEquippedWeapon(activeWeapon);
            Debug.Log($"[Inventory] Switched to slot {_activeSlot.Value}: {activeData.weaponName}");
        }
    }
}
