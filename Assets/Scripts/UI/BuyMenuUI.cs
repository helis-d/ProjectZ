using FishNet.Object;
using ProjectZ.Core;
using ProjectZ.Economy;
using ProjectZ.GameMode;
using ProjectZ.Map;
using ProjectZ.Player;
using ProjectZ.Weapon;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace ProjectZ.UI
{
    /// <summary>
    /// Buy menu opened with B.
    /// Only accessible during BuyPhase.
    /// </summary>
    public class BuyMenuUI : NetworkBehaviour
    {
        [SerializeField] private GameObject _buyMenuPanel;

        [Header("Weapon Registry")]
        [Tooltip("All purchasable weapons. Server uses this to look up WeaponData by ID.")]
        [SerializeField] private WeaponData[] _weaponCatalog;

        [Header("Equipment (GDD Section 10 — Buy Menu Equipment)")]
        [Tooltip("Credits for defuse kit. Align with the published GDD buy grid when available.")]
        [SerializeField] private int _defuseKitPriceCredits = 400;

        private RoundManager _roundManager;
        private PlayerEconomy _localEconomy;

        public override void OnStartClient()
        {
            base.OnStartClient();
            if (!IsOwner)
                enabled = false;

            _roundManager = RoundManager.Instance ?? FindFirstObjectByType<RoundManager>();
            _localEconomy = GetComponent<PlayerEconomy>();
        }

        private void Update()
        {
            if (_buyMenuPanel != null && _buyMenuPanel.activeSelf && !CanUseBuyMenu())
                CloseMenu();

#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.bKey.wasPressedThisFrame)
                ToggleMenu();
#else
            if (Input.GetKeyDown(KeyCode.B))
                ToggleMenu();
#endif
        }

        private void ToggleMenu()
        {
            if (_buyMenuPanel == null)
                return;

            bool isOpening = !_buyMenuPanel.activeSelf;
            if (isOpening && !CanUseBuyMenu())
            {
                Debug.LogWarning("[BuyMenu] Buy menu requires Buy Phase and a friendly buy zone.");
                return;
            }

            _buyMenuPanel.SetActive(isOpening);
            Cursor.lockState = isOpening ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = isOpening;
        }

        public void RequestBuyWeapon(string weaponId, int price)
        {
            if (_localEconomy != null && _localEconomy.CurrentMoney.Value < price)
            {
                Debug.LogWarning("[BuyMenu] Insufficient funds.");
                return;
            }

            CmdBuyWeapon(weaponId, price);
        }

        /// <summary>Purchase defuse kit (defenders only, buy phase, GDD Section 7 timings 7.0s / 3.5s with kit).</summary>
        public void RequestBuyDefuseKit()
        {
            if (_localEconomy != null && _localEconomy.CurrentMoney.Value < _defuseKitPriceCredits)
            {
                Debug.LogWarning("[BuyMenu] Insufficient funds for defuse kit.");
                return;
            }

            CmdBuyDefuseKit(_defuseKitPriceCredits);
        }

        [ServerRpc]
        private void CmdBuyWeapon(string weaponId, int requestedPrice)
        {
            if (_roundManager == null)
                _roundManager = RoundManager.Instance ?? FindFirstObjectByType<RoundManager>();

            if (_roundManager == null || _roundManager.CurrentState.Value != RoundManager.RoundState.BuyPhase)
                return;

            if (TryGetCurrentMode(out BaseGameMode mode) && !mode.EnableEconomy)
                return;

            if (BuyZone.HasConfiguredZones() && !BuyZone.IsPlayerInsideFriendlyZone(gameObject, OwnerId))
                return;

            WeaponData purchasedWeapon = FindWeaponById(weaponId);
            if (purchasedWeapon == null)
            {
                Debug.LogError($"[BuyMenu] Unknown weapon ID: {weaponId}");
                return;
            }

            if (purchasedWeapon.price != requestedPrice)
            {
                Debug.LogWarning($"[BuyMenu] Price mismatch for {weaponId}. Purchase rejected.");
                return;
            }

            PlayerEconomy economy = GetComponent<PlayerEconomy>();
            if (economy == null || !economy.TrySpendMoney(purchasedWeapon.price))
                return;

            // Add weapon to player inventory
            PlayerInventory inventory = GetComponent<PlayerInventory>();
            if (inventory == null)
            {
                Debug.LogError($"[BuyMenu] Player {OwnerId} has no PlayerInventory. Refunding {purchasedWeapon.price} credits.");
                economy.AddMoney(purchasedWeapon.price);
                return;
            }

            inventory.PickUpWeapon(purchasedWeapon);
            Debug.Log($"[Server] Player {OwnerId} bought {purchasedWeapon.weaponName} for ${purchasedWeapon.price}");
        }

        [ServerRpc]
        private void CmdBuyDefuseKit(int requestedPrice)
        {
            if (requestedPrice != _defuseKitPriceCredits)
                return;

            if (_roundManager == null)
                _roundManager = RoundManager.Instance ?? FindFirstObjectByType<RoundManager>();

            if (_roundManager == null || _roundManager.CurrentState.Value != RoundManager.RoundState.BuyPhase)
                return;

            if (TryGetCurrentMode(out BaseGameMode mode) && !mode.EnableEconomy)
                return;

            if (BuyZone.HasConfiguredZones() && !BuyZone.IsPlayerInsideFriendlyZone(gameObject, OwnerId))
                return;

            if (TeamManager.Instance == null || TeamManager.Instance.GetTeam(OwnerId) != Team.Defender)
                return;

            PlayerEquipment equipment = GetComponent<PlayerEquipment>();
            if (equipment == null || equipment.HasDefuseKit.Value)
                return;

            PlayerEconomy economy = GetComponent<PlayerEconomy>();
            if (economy == null || !economy.TrySpendMoney(requestedPrice))
                return;

            equipment.GrantDefuseKit();
            Debug.Log($"[Server] Player {OwnerId} bought defuse kit for ${requestedPrice}.");
        }

        private bool CanUseBuyMenu()
        {
            if (_roundManager == null)
                _roundManager = RoundManager.Instance ?? FindFirstObjectByType<RoundManager>();

            if (_roundManager != null && _roundManager.CurrentState.Value != RoundManager.RoundState.BuyPhase)
                return false;

            if (TryGetCurrentMode(out BaseGameMode mode) && !mode.EnableEconomy)
                return false;

            if (!BuyZone.HasConfiguredZones())
                return true;

            return BuyZone.IsPlayerInsideFriendlyZone(gameObject, OwnerId);
        }

        private void CloseMenu()
        {
            if (_buyMenuPanel == null || !_buyMenuPanel.activeSelf)
                return;

            _buyMenuPanel.SetActive(false);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private WeaponData FindWeaponById(string weaponId)
        {
            if (_weaponCatalog != null)
            {
                foreach (var w in _weaponCatalog)
                {
                    if (w != null && w.weaponId == weaponId)
                        return w;
                }
            }

            return WeaponCatalog.Instance?.GetById(weaponId);
        }

        private bool TryGetCurrentMode(out BaseGameMode mode)
        {
            mode = null;

            if (_roundManager != null && _roundManager.TryGetComponent(out mode))
                return true;

            if (RoundManager.Instance != null && RoundManager.Instance.TryGetComponent(out mode))
                return true;

            if (TeamManager.Instance != null && TeamManager.Instance.TryGetComponent(out mode))
                return true;

            mode = FindFirstObjectByType<BaseGameMode>();
            return mode != null;
        }
    }
}
