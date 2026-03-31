using FishNet.Object;
using UnityEngine;

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

        protected Transform CasterTransform => OwnerController != null ? OwnerController.transform : transform;
        protected GameObject CasterObject => OwnerController != null ? OwnerController.gameObject : gameObject;
        protected int OwnerConnectionId => OwnerController != null ? OwnerController.OwnerId : OwnerId;

        public virtual void Initialize(ProjectZ.Player.PlayerHeroController controller)
        {
            BindOwner(controller);
        }

        protected bool BindOwner(ProjectZ.Player.PlayerHeroController controller)
        {
            if (controller == null)
                return false;

            if (OwnerController == controller)
                return false;

            OwnerController = controller;
            return true;
        }

        protected T GetOwnerComponent<T>() where T : Component
        {
            return OwnerController != null ? OwnerController.GetComponent<T>() : GetComponent<T>();
        }

        protected int ResolveLayerMask(LayerMask mask)
        {
            return mask.value == 0 ? Physics.AllLayers : mask.value;
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
