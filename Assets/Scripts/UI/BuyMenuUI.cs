using FishNet.Object;
using ProjectZ.Economy;
using ProjectZ.GameMode;
using ProjectZ.Player;
using ProjectZ.Weapon;
using UnityEngine;

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

        private RoundManager _roundManager;
        private PlayerEconomy _localEconomy;

        public override void OnStartClient()
        {
            base.OnStartClient();
            if (!IsOwner)
                enabled = false;

            _roundManager = FindFirstObjectByType<RoundManager>();
            _localEconomy = GetComponent<PlayerEconomy>();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.B))
                ToggleMenu();
        }

        private void ToggleMenu()
        {
            if (_buyMenuPanel == null)
                return;

            bool isOpening = !_buyMenuPanel.activeSelf;
            if (isOpening && _roundManager != null && _roundManager.CurrentState.Value != RoundManager.RoundState.BuyPhase)
            {
                Debug.LogWarning("[BuyMenu] Can only open during Buy Phase.");
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

        [ServerRpc]
        private void CmdBuyWeapon(string weaponId, int requestedPrice)
        {
            if (_roundManager == null || _roundManager.CurrentState.Value != RoundManager.RoundState.BuyPhase)
                return;

            PlayerEconomy economy = GetComponent<PlayerEconomy>();
            if (economy == null || !economy.TrySpendMoney(requestedPrice))
                return;

            // Look up WeaponData from the server-side catalog
            WeaponData purchasedWeapon = FindWeaponById(weaponId);
            if (purchasedWeapon == null)
            {
                Debug.LogError($"[BuyMenu] Unknown weapon ID: {weaponId}");
                economy.AddMoney(requestedPrice); // Refund
                return;
            }

            // Validate price matches catalog
            if (purchasedWeapon.price != requestedPrice)
            {
                Debug.LogWarning($"[BuyMenu] Price mismatch for {weaponId}. Refunding.");
                economy.AddMoney(requestedPrice);
                return;
            }

            // Add weapon to player inventory
            PlayerInventory inventory = GetComponent<PlayerInventory>();
            if (inventory != null)
            {
                inventory.PickUpWeapon(purchasedWeapon);
                Debug.Log($"[Server] Player {OwnerId} bought {purchasedWeapon.weaponName} for ${requestedPrice}");
            }
        }

        private WeaponData FindWeaponById(string weaponId)
        {
            if (_weaponCatalog == null) return null;
            foreach (var w in _weaponCatalog)
            {
                if (w != null && w.weaponId == weaponId)
                    return w;
            }
            return null;
        }
    }
}

