namespace ProjectZ.Sphere
{
    /// <summary>
    /// GDD Section 7 - Sphere State Machine
    /// </summary>
    public enum SphereState
    {
        Idle,       // Carried by player or dropped on ground
        Planting,   // Currently being planted by an Attacker
        Active,     // Planted and ticking down (45s)
        Defusing,   // Currently being defused by a Defender
        Exploded,   // Detonated (Attackers win)
        Defused     // Defused (Defenders win)
    }
}
