namespace ProjectZ.Core
{
    /// <summary>Represents the competing teams in Project Z.</summary>
    public enum Team
    {
        None = 0,
        
        // Standard modes (Ranked, Fast & Fight)
        Attacker = 1,
        Defender = 2,
        
        // Duel / Chaos Mode (5 Teams of 2)
        Alpha = 3,
        Bravo = 4,
        Charlie = 5,
        Delta = 6,
        Echo = 7
    }
}
