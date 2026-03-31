using UnityEngine;

namespace ProjectZ.Hero
{
    public enum UltimateAbilityId
    {
        None = 0,
        SiegeBreaker = 1,
        QuantumRewind = 2,
        Panopticon = 3,
        DoomsdayCharge = 4,
        OverdriveCore = 5,
        BloodPact = 6,
        SpiritWolves = 7,
        VoidWalk = 8,
        SystemFailure = 9,
        BladeDance = 10,
        OneWayMirror = 11,
        Echo = 12,
        GrappleStrike = 13
    }

    /// <summary>
    /// GDD Section 8 - Character Roles
    /// </summary>
    public enum HeroRole
    {
        Anchor,     // Defense/Strategy
        Duelist,    // Mobility/Close Combat
        Support,    // Information/Tempo
        Controller, // Area Control
        Hunter,     // Crowd Control
        Stalker,    // Stealth/Flank
        Disruptor,  // Chaos/Global
        Intel,      // Psychological Pressure
        Thief,      // Flexible
        Acrobat     // Initiator
    }

    /// <summary>
    /// ScriptableObject holding immutable base data for a Hero (Agent).
    /// </summary>
    [CreateAssetMenu(fileName = "NewHero", menuName = "ProjectZ/Hero Data")]
    public class HeroData : ScriptableObject
    {
        [Header("Identity")]
        public string heroId;
        public string heroName;
        public string heroTitle;
        public HeroRole role;
        public string gameplayRole;
        public int gddRosterOrder = 1;

        [Header("Ultimate")]
        public UltimateAbilityId ultimateId = UltimateAbilityId.None;
        public string ultimateName;
        [TextArea] public string ultimateDescription;
        public float ultimateChargePerKill = 15f;
        public float ultimateChargePerAssist = 10f;
        
        // This prefab will be instantiated and attached to the player when this hero is selected
        public GameObject ultimateAbilityPrefab;

        public bool HasConfiguredUltimate => ultimateAbilityPrefab != null || ultimateId != UltimateAbilityId.None;
    }
}
