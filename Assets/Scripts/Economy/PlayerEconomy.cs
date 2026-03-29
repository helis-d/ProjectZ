using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

namespace ProjectZ.Economy
{
    /// <summary>
    /// Tracks the money belonging to a single player.
    /// Synchronised to the owning client so they can see their balance.
    /// Updates only happen on the server.
    /// </summary>
    public class PlayerEconomy : NetworkBehaviour
    {
        // ─── Synced State ─────────────────────────────────────────────────
        public readonly SyncVar<int> CurrentMoney = new();

        // ─── Server API ───────────────────────────────────────────────────
        [Server]
        public void AddMoney(int amount, int maxLimit = 9000)
        {
            if (amount <= 0) return;
            CurrentMoney.Value = Mathf.Clamp(CurrentMoney.Value + amount, 0, maxLimit);
            Debug.Log($"[Economy] Player {OwnerId} gained ${amount}. Balance: ${CurrentMoney.Value}");
        }

        [Server]
        public bool TrySpendMoney(int amount)
        {
            if (amount <= 0 || CurrentMoney.Value < amount)
                return false;

            CurrentMoney.Value -= amount;
            Debug.Log($"[Economy] Player {OwnerId} spent ${amount}. Balance: ${CurrentMoney.Value}");
            return true;
        }

        [Server]
        public void SetMoney(int amount)
        {
            CurrentMoney.Value = amount;
        }
    }
}
