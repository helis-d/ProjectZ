using System.Collections;
using FishNet.Object;
using ProjectZ.Player;
using UnityEngine;

namespace ProjectZ.Hero.Sai
{
    /// <summary>
    /// Sai's ultimate: Blade Dance (GDD Section 8).
    /// 3 sword strikes state machine:
    /// Strikes 1-2: 4m range, 75 damage, blocks incoming bullets
    /// Strike 3: 6m range, 3 second root, reveals position
    /// </summary>
    public class BladeDance : UltimateAbility
    {
        [Header("Blade Dance")]
        [SerializeField] private float _strike12Range = 4f;
        [SerializeField] private float _strike12Damage = 75f;
        [SerializeField] private float _strike3Range = 6f;
        [SerializeField] private float _rootDuration = 3f;
        [SerializeField] private float _strikeCooldown = 0.6f;
        [SerializeField] private LayerMask _playerLayer;

        private int _currentStrike;
        private bool _isActive;

        [Server]
        public override void Activate()
        {
            if (!IsServerInitialized) return;

            _currentStrike = 0;
            _isActive = true;
            StartCoroutine(BladeDanceRoutine());
            Debug.Log("[BladeDance] Activated!");
        }

        [Server]
        private IEnumerator BladeDanceRoutine()
        {
            for (int i = 1; i <= 3; i++)
            {
                _currentStrike = i;

                if (i <= 2)
                    ExecuteStrike12();
                else
                    ExecuteStrike3();

                RpcPlayStrikeAnimation(i);
                yield return new WaitForSeconds(_strikeCooldown);
            }

            _isActive = false;
            _currentStrike = 0;
            Debug.Log("[BladeDance] Complete.");
        }

        [Server]
        private void ExecuteStrike12()
        {
            // Cone/sphere damage in front of player
            Collider[] hits = Physics.OverlapSphere(CasterTransform.position, _strike12Range, ResolveLayerMask(_playerLayer));
            foreach (Collider hit in hits)
            {
                // Only hit enemies in front hemisphere
                Vector3 toTarget = (hit.transform.position - CasterTransform.position).normalized;
                if (Vector3.Dot(CasterTransform.forward, toTarget) < 0.3f) continue;

                PlayerHealth health = hit.GetComponentInParent<PlayerHealth>();
                if (health != null && !health.IsDead.Value && health.OwnerId != OwnerConnectionId)
                {
                    health.TakeDamage(_strike12Damage, OwnerConnectionId);
                    Debug.Log($"[BladeDance] Strike {_currentStrike} hit player {health.OwnerId} for {_strike12Damage}");
                }
            }
        }

        [Server]
        private void ExecuteStrike3()
        {
            // Extended range + root
            Collider[] hits = Physics.OverlapSphere(CasterTransform.position, _strike3Range, ResolveLayerMask(_playerLayer));
            foreach (Collider hit in hits)
            {
                Vector3 toTarget = (hit.transform.position - CasterTransform.position).normalized;
                if (Vector3.Dot(CasterTransform.forward, toTarget) < 0.2f) continue;

                PlayerHealth health = hit.GetComponentInParent<PlayerHealth>();
                if (health != null && !health.IsDead.Value && health.OwnerId != OwnerConnectionId)
                {
                    // Root: disable movement for duration
                    var nob = hit.GetComponentInParent<FishNet.Object.NetworkObject>();
                    if (nob != null && nob.Owner.IsValid)
                    {
                        RpcApplyRoot(nob.Owner, nob.gameObject, _rootDuration);
                    }

                    // Reveal position via outline
                    var outline = hit.GetComponentInParent<OutlineController>();
                    if (outline != null && OwnerController != null)
                        outline.TargetShowOutline(OwnerController.Owner, _rootDuration);

                    Debug.Log($"[BladeDance] Strike 3 rooted player {health.OwnerId} for {_rootDuration}s");
                }
            }
        }

        /// <summary>Returns true during strikes 1-2 (bullet blocking active).</summary>
        public bool IsBlockingBullets => _isActive && _currentStrike >= 1 && _currentStrike <= 2;

        [TargetRpc]
        private void RpcApplyRoot(FishNet.Connection.NetworkConnection conn, GameObject targetPlayer, float duration)
        {
            if (targetPlayer != null)
                StartCoroutine(RootRoutine(targetPlayer, duration));
        }

        private IEnumerator RootRoutine(GameObject targetPlayer, float duration)
        {
            PlayerMovement movement = targetPlayer.GetComponent<PlayerMovement>();
            PlayerInputHandler input = targetPlayer.GetComponent<PlayerInputHandler>();
            if (movement != null) movement.enabled = false;
            if (input != null) input.enabled = false;

            yield return new WaitForSeconds(duration);

            if (movement != null) movement.enabled = true;
            if (input != null) input.enabled = true;
        }

        [ObserversRpc]
        private void RpcPlayStrikeAnimation(int strikeNumber)
        {
            Debug.Log($"[BladeDance] Strike {strikeNumber} animation");
        }
    }
}

