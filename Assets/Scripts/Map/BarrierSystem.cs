using FishNet.Object;
using ProjectZ.Core;
using ProjectZ.GameMode;
using UnityEngine;

namespace ProjectZ.Map
{
    /// <summary>
    /// Invisible wall active during BuyPhase and disabled during ActionPhase.
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
            GameEvents.OnRoundStart += HandleRoundStart;
        }

        public override void OnStopServer()
        {
            base.OnStopServer();
            GameEvents.OnRoundStart -= HandleRoundStart;
        }

        private void Update()
        {
            if (!IsServerInitialized)
                return;

            RoundManager rm = RoundManager.Instance;
            if (rm != null)
                SetActiveState(rm.CurrentState.Value == RoundManager.RoundState.BuyPhase);
        }

        private void HandleRoundStart(int _)
        {
            SetActiveState(true);
        }

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

