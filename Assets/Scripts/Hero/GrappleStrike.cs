using System.Collections;
using FishNet.Object;
using ProjectZ.Player;
using UnityEngine;

namespace ProjectZ.Hero.Marcus
{
    /// <summary>
    /// Marcus 2.0's ultimate: Grapple Strike (GDD Section 8).
    /// Physics-based grappling hook with 25m range.
    /// On Wall: Pulls player to that point.
    /// On Enemy: 25 damage, 40% slow for 3 seconds.
    /// Passive: No fall damage while ultimate is active.
    /// </summary>
    public class GrappleStrike : UltimateAbility
    {
        [Header("Grapple Strike")]
        [SerializeField] private float _maxRange = 25f;
        [SerializeField] private float _pullSpeed = 30f;
        [SerializeField] private float _enemyDamage = 25f;
        [SerializeField] private float _slowPercent = 0.4f;
        [SerializeField] private float _slowDuration = 3f;
        [SerializeField] private LayerMask _grappleMask;

        private bool _isActive;

        /// <summary>Returns true while grapple is active (no fall damage).</summary>
        public bool IsGrappleActive => _isActive;

        [Server]
        public override void Activate()
        {
            if (!IsServerInitialized) return;

            _isActive = true;

            // Raycast to find grapple target
            if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, _maxRange, _grappleMask))
            {
                PlayerHealth targetHealth = hit.collider.GetComponentInParent<PlayerHealth>();

                if (targetHealth != null && !targetHealth.IsDead.Value && targetHealth.OwnerId != OwnerId)
                {
                    // Hit an enemy
                    targetHealth.TakeDamage(_enemyDamage, OwnerId);

                    var targetNob = hit.collider.GetComponentInParent<FishNet.Object.NetworkObject>();
                    if (targetNob != null && targetNob.Owner.IsValid)
                        RpcApplySlow(targetNob.Owner, _slowDuration, _slowPercent);

                    Debug.Log($"[GrappleStrike] Hit enemy {targetHealth.OwnerId}: {_enemyDamage} dmg + {_slowPercent * 100}% slow");
                }
                else
                {
                    // Hit a wall — pull player to that point
                    StartCoroutine(PullToPoint(hit.point));
                    Debug.Log($"[GrappleStrike] Pulling to wall at {hit.point}");
                    return; // Don't deactivate until pull completes
                }
            }
            else
            {
                Debug.Log("[GrappleStrike] No target in range.");
            }

            _isActive = false;
        }

        [Server]
        private IEnumerator PullToPoint(Vector3 target)
        {
            CharacterController cc = GetComponent<CharacterController>();
            if (cc == null) { _isActive = false; yield break; }

            cc.enabled = false;

            float dist = Vector3.Distance(transform.position, target);
            float travelTime = dist / _pullSpeed;
            float elapsed = 0f;
            Vector3 start = transform.position;

            while (elapsed < travelTime)
            {
                transform.position = Vector3.Lerp(start, target, elapsed / travelTime);
                elapsed += Time.deltaTime;

                // Broadcast position
                RpcSyncPosition(transform.position);
                yield return null;
            }

            transform.position = target;
            cc.enabled = true;
            _isActive = false;
            Debug.Log("[GrappleStrike] Pull complete.");
        }

        [TargetRpc]
        private void RpcApplySlow(FishNet.Connection.NetworkConnection conn, float duration, float slowAmount)
        {
            StartCoroutine(SlowRoutine(duration));
        }

        private IEnumerator SlowRoutine(float duration)
        {
            // Reduce movement speed on the client
            var movement = GetComponent<PlayerMovement>();
            // Slow effect would ideally be a multiplier on PlayerMovement
            // For now we log it
            Debug.Log($"[GrappleStrike] Slowed for {duration}s");
            yield return new WaitForSeconds(duration);
            Debug.Log("[GrappleStrike] Slow expired.");
        }

        [ObserversRpc]
        private void RpcSyncPosition(Vector3 pos)
        {
            if (!IsOwner && !IsServerInitialized)
                transform.position = pos;
        }
    }
}
