using System.Collections.Generic;
using ProjectZ.Core;
using UnityEngine;

namespace ProjectZ.Combat
{
    /// <summary>
    /// Static registry that tracks damage dealt by each player to each victim.
    /// Used to determine assists when a kill occurs (GDD Section 1/8).
    /// </summary>
    public static class DamageAssistRegistry
    {
        // victimId -> (attackerId -> totalDamage)
        private static readonly Dictionary<int, Dictionary<int, float>> _damageMap = new();

        /// <summary>Record damage dealt by an attacker to a victim.</summary>
        public static void RecordDamage(int attackerId, int victimId, float damage)
        {
            if (attackerId == victimId || damage <= 0f)
                return;

            if (!_damageMap.TryGetValue(victimId, out var attackers))
            {
                attackers = new Dictionary<int, float>();
                _damageMap[victimId] = attackers;
            }

            if (attackers.ContainsKey(attackerId))
                attackers[attackerId] += damage;
            else
                attackers[attackerId] = damage;
        }

        /// <summary>
        /// Resolve assists for a killed victim. Returns all player IDs that dealt damage
        /// but are not the killer. Fires OnPlayerAssist for each assister.
        /// </summary>
        public static void ResolveAssists(int victimId, int killerId)
        {
            if (!_damageMap.TryGetValue(victimId, out var attackers))
                return;

            foreach (var kvp in attackers)
            {
                int attackerId = kvp.Key;
                if (attackerId != killerId && attackerId != victimId)
                {
                    GameEvents.InvokePlayerAssist(attackerId, victimId);
                    Debug.Log($"[Assist] Player {attackerId} assisted in killing Player {victimId} ({kvp.Value:F0} dmg)");
                }
            }

            // Clear this victim's damage records
            _damageMap.Remove(victimId);
        }

        /// <summary>Clear all records (called at round start).</summary>
        public static void ClearAll()
        {
            _damageMap.Clear();
        }

        /// <summary>Clear records for a specific victim (e.g. on respawn).</summary>
        public static void ClearVictim(int victimId)
        {
            _damageMap.Remove(victimId);
        }
    }
}
