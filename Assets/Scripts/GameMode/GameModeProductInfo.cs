using UnityEngine;

namespace ProjectZ.GameMode
{
    /// <summary>
    /// Product / shipping labels for game modes. See Docs/DESIGN_PILLARS.md.
    /// </summary>
    public static class GameModeProductInfo
    {
        public enum ShippingTier
        {
            Primary,
            Secondary,
            Experimental
        }

        /// <summary>
        /// Primary = main competitive focus; Secondary = alternate ranked rules; Experimental = not the shipping core loop.
        /// </summary>
        public static ShippingTier GetTier(BaseGameMode mode)
        {
            if (mode == null)
                return ShippingTier.Secondary;

            return mode switch
            {
                RankedGameMode => ShippingTier.Primary,
                FastFightMode => ShippingTier.Secondary,
                DuelChaosMode => ShippingTier.Experimental,
                SoloTournamentMode => ShippingTier.Experimental,
                _ => ShippingTier.Secondary
            };
        }

        public static bool IsExperimental(BaseGameMode mode)
        {
            return GetTier(mode) == ShippingTier.Experimental;
        }
    }
}
