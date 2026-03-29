namespace ProjectZ.Combat
{
    /// <summary>
    /// Body zone enumeration matching GDD Section 6 hitbox table.
    /// Each zone has a fixed damage multiplier.
    /// </summary>
    public enum HitboxZone
    {
        Head,           // 4.0×  —  Head_Bone, radius 12 cm
        Neck,           // 4.0×  —  Neck_Bone, radius 8 cm
        UpperChest,     // 1.0×  —  Spine_03, radius 25 cm
        Stomach,        // 1.0×  —  Spine_01, radius 22 cm
        Arms,           // 0.85× —  LowerArm_L/R, radius 9 cm
        Legs            // 0.85× —  Calf_L/R, radius 11 cm
    }

    /// <summary>
    /// Static lookup for zone → damage multiplier (GDD Section 6).
    /// </summary>
    public static class HitboxZoneData
    {
        public static float GetMultiplier(HitboxZone zone) => zone switch
        {
            HitboxZone.Head       => 4.0f,
            HitboxZone.Neck       => 4.0f,
            HitboxZone.UpperChest => 1.0f,
            HitboxZone.Stomach    => 1.0f,
            HitboxZone.Arms       => 0.85f,
            HitboxZone.Legs       => 0.85f,
            _ => 1.0f
        };

        public static float GetDefaultRadius(HitboxZone zone) => zone switch
        {
            HitboxZone.Head       => 0.12f,  // 12 cm → 0.12 m
            HitboxZone.Neck       => 0.08f,
            HitboxZone.UpperChest => 0.25f,
            HitboxZone.Stomach    => 0.22f,
            HitboxZone.Arms       => 0.09f,
            HitboxZone.Legs       => 0.11f,
            _ => 0.10f
        };
    }
}
