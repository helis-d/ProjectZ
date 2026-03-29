using System;
using UnityEngine;

namespace ProjectZ.Weapon
{
    /// <summary>
    /// Runtime XP/level data for a single weapon instance.
    /// Implements the GDD Section 1 WeaponRuntimeData class exactly:
    ///
    ///   Level thresholds: each 1000 XP = 1 level (max 5)
    ///   XP can never drop below 0
    ///   Level can dynamically decrease when XP drops
    /// </summary>
    [Serializable]
    public class WeaponRuntimeData
    {
        public string WeaponID;
        public int CurrentXP    = 0;
        public int CurrentLevel = 1;
        public int KillsInMatch = 0;

        /// <summary>Fired when mastery level changes. Args: old level, new level.</summary>
        public event Action<int, int> OnLevelChanged;

        /// <summary>
        /// GDD AddXP implementation:
        ///   CurrentXP += amount
        ///   Clamp to >= 0
        ///   newLevel = (XP / 1000) + 1, clamped 1-5
        ///   If level changed → RecalculateStats
        /// </summary>
        public void AddXP(int amount)
        {
            int oldLevel = CurrentLevel;
            CurrentXP += amount;

            // Minimum XP protection (GDD rule)
            if (CurrentXP < 0)
                CurrentXP = 0;

            // Calculate new level
            int newLevel = (CurrentXP / 1000) + 1;
            newLevel = Mathf.Clamp(newLevel, 1, 5);

            if (newLevel != CurrentLevel)
            {
                CurrentLevel = newLevel;
                OnLevelChanged?.Invoke(oldLevel, newLevel);

                Debug.Log($"[Mastery] {WeaponID}: Level {oldLevel} → {newLevel} (XP: {CurrentXP})");
            }
        }

        /// <summary>Reset all mastery data (GDD: dropped weapon resets to Level 1).</summary>
        public void Reset()
        {
            int oldLevel = CurrentLevel;
            CurrentXP    = 0;
            CurrentLevel = 1;
            KillsInMatch = 0;

            if (oldLevel != 1)
                OnLevelChanged?.Invoke(oldLevel, 1);
        }
    }
}
