using UnityEngine;

namespace ProjectZ.Weapon
{
    /// <summary>
    /// GDD Section 3: Level color codes for mastery UI/VFX.
    /// Level 1: White | Level 2: Green | Level 3: Blue | Level 4: Purple | Level 5: Gold/Orange
    /// </summary>
    public static class MasteryLevelColors
    {
        private static readonly Color[] _colors = new Color[]
        {
            new Color(1.0f,  1.0f,  1.0f),    // Level 1: White
            new Color(0.2f,  0.9f,  0.3f),    // Level 2: Green
            new Color(0.3f,  0.5f,  1.0f),    // Level 3: Blue
            new Color(0.7f,  0.2f,  0.9f),    // Level 4: Purple
            new Color(1.0f,  0.75f, 0.1f),    // Level 5: Gold/Orange
        };

        /// <summary>Returns the color for a mastery level (1-5).</summary>
        public static Color GetColor(int level)
        {
            int index = Mathf.Clamp(level, 1, 5) - 1;
            return _colors[index];
        }

        /// <summary>Returns a Roman numeral string for a mastery level (1-5).</summary>
        public static string GetRomanNumeral(int level) => level switch
        {
            1 => "I",
            2 => "II",
            3 => "III",
            4 => "IV",
            5 => "V",
            _ => "I"
        };
    }
}
