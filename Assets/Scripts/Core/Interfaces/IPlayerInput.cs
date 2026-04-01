using UnityEngine;

namespace ProjectZ.Core.Interfaces
{
    /// <summary>
    /// Core interface for player or AI inputs.
    /// Ensures loose coupling between movement/combat components and the source of input.
    /// This allows seamless AI integration and better testability.
    /// </summary>
    public interface IPlayerInput
    {
        Vector2 MoveInput { get; }
        Vector2 LookInput { get; }
        bool IsSprinting { get; }
        bool IsCrouching { get; }
        bool FirePressed { get; }
        bool FireHeld { get; }
        bool ReloadPressed { get; }
        bool InteractHeld { get; }
        bool JumpPressed { get; }
        bool DropPressed { get; }
        bool UltimatePressed { get; }
        int SlotAlphaPressed { get; }
    }
}
