using System.Collections;
using FishNet.Object;
using ProjectZ.Combat;
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
            if (Physics.Raycast(CasterTransform.position, CasterTransform.forward, out RaycastHit hit, _maxRange, ResolveLayerMask(_grappleMask)))
            {
                PlayerHealth targetHealth = hit.collider.GetComponentInParent<PlayerHealth>();

                if (targetHealth != null && !targetHealth.IsDead.Value && targetHealth.OwnerId != OwnerConnectionId)
                {
                    // Hit an enemy
                    DamageProcessor damageProcessor = targetHealth.GetComponent<DamageProcessor>();
                    if (damageProcessor != null)
                        damageProcessor.ProcessAbilityDamage(OwnerConnectionId, _enemyDamage, targetHealth, "ultimate_grapple_strike");

                    var targetNob = hit.collider.GetComponentInParent<FishNet.Object.NetworkObject>();
                    if (targetNob != null && targetNob.Owner.IsValid)
                        RpcApplySlow(targetNob.Owner, targetNob.gameObject, _slowDuration, _slowPercent);

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
            CharacterController cc = GetOwnerComponent<CharacterController>();
            if (cc == null) { _isActive = false; yield break; }

            float dist = Vector3.Distance(CasterTransform.position, target);
            float travelTime = dist / _pullSpeed;
            float elapsed = 0f;
            Vector3 start = CasterTransform.position;

            // [FIX] BUG-04: Use CharacterController.Move instead of manual Transform mutation
            while (elapsed < travelTime)
            {
                Vector3 currentPos = CasterTransform.position;
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / travelTime);
                Vector3 intendedPos = Vector3.Lerp(start, target, t);
                
                cc.Move(intendedPos - currentPos);
                yield return null;
            }

            _isActive = false;
            Debug.Log("[GrappleStrike] Pull complete.");
        }

        [TargetRpc]
        private void RpcApplySlow(FishNet.Connection.NetworkConnection conn, GameObject targetPlayer, float duration, float slowAmount)
        {
            if (targetPlayer != null)
                StartCoroutine(SlowRoutine(targetPlayer, duration, slowAmount));
        }

        private IEnumerator SlowRoutine(GameObject targetPlayer, float duration, float slowAmount)
        {
            PlayerInputHandler input = targetPlayer.GetComponent<PlayerInputHandler>();
            bool originalInputState = input != null && input.enabled;

            if (input != null)
                input.enabled = false;

            Debug.Log($"[GrappleStrike] Heavy slow applied for {duration}s at {slowAmount * 100f:F0}% intensity.");
            yield return new WaitForSeconds(duration);

            if (input != null)
                input.enabled = originalInputState;

            Debug.Log("[GrappleStrike] Slow expired.");
        }

    }
}
