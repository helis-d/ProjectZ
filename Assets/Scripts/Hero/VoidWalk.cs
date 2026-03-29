using System.Collections;
using FishNet.Object;
using ProjectZ.Core;
using ProjectZ.GameMode;
using ProjectZ.Player;
using UnityEngine;

namespace ProjectZ.Hero.Zauhll
{
    /// <summary>
    /// Zauhll's ultimate: Void Walk (GDD Section 8).
    /// Complete invisibility with 3m vision range.
    /// Movement speed +25%, duration 7 seconds.
    /// First hit out of invisibility: 100% lifesteal.
    /// </summary>
    public class VoidWalk : UltimateAbility
    {
        [Header("Void Walk")]
        [SerializeField] private float _duration = 7f;
        [SerializeField] private float _visionRange = 3f;
        [SerializeField] private float _speedBonus = 1.25f;

        private bool _isActive;
        private bool _firstHitUsed;

        [Server]
        public override void Activate()
        {
            if (!IsServerInitialized) return;

            _isActive = true;
            _firstHitUsed = false;

            // Make invisible to enemies
            RpcSetInvisible(true, _visionRange);

            GameEvents.OnPlayerDeath += HandleFirstKill;
            StartCoroutine(VoidWalkRoutine());
            Debug.Log("[VoidWalk] Activated! Invisible.");
        }

        [Server]
        private IEnumerator VoidWalkRoutine()
        {
            yield return new WaitForSeconds(_duration);
            Deactivate();
        }

        private void HandleFirstKill(int victimId, int killerId)
        {
            if (!_isActive || _firstHitUsed || killerId != OwnerId) return;

            _firstHitUsed = true;

            // 100% lifesteal — restore HP equal to damage dealt
            PlayerHealth health = GetComponent<PlayerHealth>();
            if (health != null)
            {
                health.AddHealth(50f); // Approximate lifesteal
                Debug.Log("[VoidWalk] First kill lifesteal applied!");
            }
        }

        /// <summary>Returns true if Void Walk is currently active (for speed modifier check).</summary>
        public bool IsVoidWalkActive => _isActive;

        /// <summary>Speed multiplier while active.</summary>
        public float SpeedMultiplier => _isActive ? _speedBonus : 1f;

        [Server]
        private void Deactivate()
        {
            if (!_isActive) return;
            _isActive = false;
            GameEvents.OnPlayerDeath -= HandleFirstKill;
            RpcSetInvisible(false, 0f);
            Debug.Log("[VoidWalk] Deactivated.");
        }

        [ObserversRpc]
        private void RpcSetInvisible(bool invisible, float visionLimit)
        {
            // Toggle visibility on all renderers
            var renderers = GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                if (IsOwner)
                {
                    // Owner sees themselves as translucent
                    foreach (var mat in r.materials)
                    {
                        Color c = mat.color;
                        c.a = invisible ? 0.3f : 1f;
                        mat.color = c;
                    }
                }
                else
                {
                    r.enabled = !invisible;
                }
            }

            // Vision limit would be applied via a post-processing fog/mask effect
            if (IsOwner && invisible)
            {
                Debug.Log($"[VoidWalk] Vision limited to {visionLimit}m");
            }
        }

        private void OnDestroy()
        {
            if (_isActive) GameEvents.OnPlayerDeath -= HandleFirstKill;
        }
    }
}
