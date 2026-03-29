using FishNet.Object;

namespace ProjectZ.Hero
{
    /// <summary>
    /// Base class for all ultimate abilities (GDD Section 8).
    /// Attached to the player or spawned dynamically based on the HeroData.
    /// Manages network synchronization for activation.
    /// </summary>
    public abstract class UltimateAbility : NetworkBehaviour
    {
        /// <summary>Owning player who controls this ability.</summary>
        protected ProjectZ.Player.PlayerHeroController OwnerController;

        public virtual void Initialize(ProjectZ.Player.PlayerHeroController controller)
        {
            OwnerController = controller;
        }

        /// <summary>
        /// Called when the player presses the ultimate key (X or equivalent) and charge is 100%.
        /// </summary>
        public abstract void Activate();

        /// <summary>
        /// Optional: Called if the ability needs to be cancelled (e.g., interrupted by death).
        /// </summary>
        [Server]
        public virtual void Cancel() { }
    }
}
