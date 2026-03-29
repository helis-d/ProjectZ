using UnityEngine;

namespace ProjectZ.Hero
{
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
        public HeroRole role;

        [Header("Ultimate")]
        public string ultimateName;
        [TextArea] public string ultimateDescription;
        
        // This prefab will be instantiated and attached to the player when this hero is selected
        public GameObject ultimateAbilityPrefab;
    }
}
