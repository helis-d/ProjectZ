using FishNet.Object;
using ProjectZ.Core;
using UnityEngine;

namespace ProjectZ.Map
{
    /// <summary>
    /// Invisible wall active during BuyPhase and disabled during ActionPhase.
    /// Subscribes to GameEvents instead of polling RoundManager every frame.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class BarrierSystem : NetworkBehaviour
    {
        private BoxCollider _collider;
        private MeshRenderer _meshRenderer;

        private void Awake()
        {
            _collider = GetComponent<BoxCollider>();
            _meshRenderer = GetComponent<MeshRenderer>();
            if (_meshRenderer != null)
                _meshRenderer.enabled = false;
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            GameEvents.OnRoundStart      += OnRoundStart;
            GameEvents.OnBuyPhaseStart   += OnBuyPhaseStart;
            GameEvents.OnActionPhaseStart += OnActionPhaseStart;
        }

        public override void OnStopServer()
        {
            base.OnStopServer();
            GameEvents.OnRoundStart      -= OnRoundStart;
            GameEvents.OnBuyPhaseStart   -= OnBuyPhaseStart;
            GameEvents.OnActionPhaseStart -= OnActionPhaseStart;
        }

        // Enable barriers when a new round begins (buy phase is about to start).
        private void OnRoundStart(int _) => SetActiveState(true);

        // Redundant safety: re-enable barriers when buy phase timer fires.
        private void OnBuyPhaseStart(float _) => SetActiveState(true);

        // Disable barriers when the action phase begins.
        private void OnActionPhaseStart() => SetActiveState(false);

        private void SetActiveState(bool isActive)
        {
            if (_collider.enabled == isActive)
                return;

            _collider.enabled = isActive;
            RpcSetBarrierVisuals(isActive);
        }

        [ObserversRpc]
        private void RpcSetBarrierVisuals(bool isActive)
        {
            if (_meshRenderer != null)
                _meshRenderer.enabled = isActive;
        }
    }
}


