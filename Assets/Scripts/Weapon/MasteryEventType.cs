namespace ProjectZ.Weapon
{
    /// <summary>
    /// All mastery XP events from GDD Section 1.
    /// Each event has a fixed XP value (positive = gain, negative = penalty).
    /// </summary>
    public enum MasteryEventType
    {
        KillBody,           // +50 XP
        KillHead,           // +100 XP
        AssistHP,           // +25 XP  (enemy had no armor)
        AssistArmorHP,      // +50 XP  (damaged both armor + HP)
        UltiCast,           // +50 XP  (added to most expensive weapon)
        DeathBody,          // -25 XP
        DeathHead,          // -40 XP  (killed by headshot)
        DeathColdStreak,    // -60 XP  (0 kills in last 3 rounds)
        PenaltyToxic        // -85 XP  (deducted from highest level weapon)
    }

    /// <summary>
    /// Maps MasteryEventType → XP value.
    /// </summary>
    public static class MasteryXPTable
    {
        public static int GetXP(MasteryEventType evt) => evt switch
        {
            MasteryEventType.KillBody        =>  50,
            MasteryEventType.KillHead        =>  100,
            MasteryEventType.AssistHP        =>  25,
            MasteryEventType.AssistArmorHP   =>  50,
            MasteryEventType.UltiCast        =>  50,
            MasteryEventType.DeathBody       => -25,
            MasteryEventType.DeathHead       => -40,
            MasteryEventType.DeathColdStreak => -60,
            MasteryEventType.PenaltyToxic    => -85,
            _ => 0
        };
    }
}
