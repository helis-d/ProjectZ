using System;
using UnityEngine;

namespace ProjectZ.Weapon
{
    /// <summary>
    /// Per-level multipliers for a single weapon class.
    /// Values follow GDD Section 2 JSON config convention:
    ///   Time-based (lower is better): NewValue = Base × Modifier  (modifier &lt; 1)
    ///   Speed-based (higher is better): NewValue = Base × Modifier (modifier &gt; 1)
    /// </summary>
    [Serializable]
    public struct LevelMultipliers
    {
        [Tooltip("ADS time multiplier (e.g. 0.90 = 10% faster)")]
        public float ads;

        [Tooltip("Reload time multiplier")]
        public float reload;

        [Tooltip("Movement speed multiplier (e.g. 1.05 = 5% faster)")]
        public float move;

        [Tooltip("Fire rate multiplier")]
        public float fireRate;

        [Tooltip("Weapon draw time multiplier")]
        public float draw;

        /// <summary>Identity/Level-1 defaults: all 1.0.</summary>
        public static LevelMultipliers Default => new()
        {
            ads = 1f, reload = 1f, move = 1f, fireRate = 1f, draw = 1f
        };

        /// <summary>
        /// Pulls handling bonuses toward neutral (1.0). strength=1 preserves m; strength=0 returns Default.
        /// Use for competitive integrity tuning without editing per-level config assets.
        /// </summary>
        public static LevelMultipliers BlendTowardIdentity(LevelMultipliers m, float strength)
        {
            strength = Mathf.Clamp01(strength);
            return new LevelMultipliers
            {
                ads = 1f + (m.ads - 1f) * strength,
                reload = 1f + (m.reload - 1f) * strength,
                move = 1f + (m.move - 1f) * strength,
                fireRate = 1f + (m.fireRate - 1f) * strength,
                draw = 1f + (m.draw - 1f) * strength
            };
        }
    }

    /// <summary>
    /// ScriptableObject that maps a WeaponType → Level 1-5 buff multipliers.
    /// Create via Assets → Create → ProjectZ → Weapon Class Buff Config.
    ///
    /// GDD Section 2 examples:
    ///   AR Level 2: ads=0.90, reload=1.0, move=1.0
    ///   AR Level 5: ads=0.85, reload=0.80, move=1.05
    /// </summary>
    [CreateAssetMenu(fileName = "NewBuffConfig", menuName = "ProjectZ/Weapon Class Buff Config")]
    public class WeaponTypeBuffConfig : ScriptableObject
    {
        public WeaponType weaponClass;

        [Tooltip("Index 0 = Level 1 (always 1.0), Index 4 = Level 5")]
        public LevelMultipliers[] levels = new LevelMultipliers[5]
        {
            LevelMultipliers.Default, // Level 1
            LevelMultipliers.Default, // Level 2
            LevelMultipliers.Default, // Level 3
            LevelMultipliers.Default, // Level 4
            LevelMultipliers.Default  // Level 5
        };

        /// <summary>
        /// Returns the multipliers for the given mastery level (1-5).
        /// Clamps to valid range.
        /// </summary>
        public LevelMultipliers GetMultipliers(int level)
        {
            int index = Mathf.Clamp(level, 1, 5) - 1;
            return levels[index];
        }
    }
}
