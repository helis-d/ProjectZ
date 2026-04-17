using System.Collections;
using FishNet.Object;
using ProjectZ.Combat;
using ProjectZ.GameMode;
using ProjectZ.Player;
using UnityEngine;

namespace ProjectZ.Hero.Sector
{
    /// <summary>
    /// Sector's ultimate: Doomsday Charge (GDD Section 8).
    /// Sticky bomb that explodes after 2 seconds.
    /// Damage: 150 (0-2m), linear falloff (2-8m).
    /// Crouch bonus: damage reduced by 50%.
    /// </summary>
    public class DoomsdayCharge : UltimateAbility
    {
        [Header("Doomsday Charge")]
        [SerializeField] private float _fuseTime = 2.0f;
        [SerializeField] private float _maxDamage = 150f;
        [SerializeField] private float _innerRadius = 2.0f;
        [SerializeField] private float _outerRadius = 8.0f;
        [SerializeField] private GameObject _chargePrefab;
        [SerializeField] private LayerMask _playerLayer;

        // [FIX] BUG-12: pre-allocated buffer — OverlapSphere allocs a new Collider[] per call
        private readonly Collider[] _overlapBuffer = new Collider[64];

        [Server]
        public override void Activate()
        {
            if (!IsServerInitialized) return;

            // Spawn projectile in front of player
            Vector3 spawnPos = CasterTransform.position + CasterTransform.forward * 1.5f + Vector3.up * 0.5f;
            Vector3 throwDir = CasterTransform.forward;

            if (_chargePrefab != null)
            {
                GameObject charge = Instantiate(_chargePrefab, spawnPos, Quaternion.identity);
                ServerManager.Spawn(charge);

                var rb = charge.GetComponent<Rigidbody>();
                if (rb != null)
                    rb.AddForce(throwDir * 15f, ForceMode.Impulse);

                StartCoroutine(DetonateRoutine(charge));
            }
            else
            {
                // Instant placement fallback
                StartCoroutine(DetonateAtPosition(spawnPos));
            }
        }

        [Server]
        private IEnumerator DetonateRoutine(GameObject charge)
        {
            yield return new WaitForSeconds(_fuseTime);

            if (charge != null)
            {
                Vector3 pos = charge.transform.position;
                ServerManager.Despawn(charge);
                ApplyExplosionDamage(pos);
            }
        }

        [Server]
        private IEnumerator DetonateAtPosition(Vector3 pos)
        {
            yield return new WaitForSeconds(_fuseTime);
            ApplyExplosionDamage(pos);
        }

        [Server]
        private void ApplyExplosionDamage(Vector3 center)
        {
            // [FIX] BUG-12: NonAlloc variant — reuse pre-allocated buffer
            int hitCount = Physics.OverlapSphereNonAlloc(center, _outerRadius, _overlapBuffer, ResolveLayerMask(_playerLayer));
            TeamManager tm = TeamManager.Instance;

            for (int i = 0; i < hitCount; i++)
            {
                Collider hit = _overlapBuffer[i];
                PlayerHealth health = hit.GetComponentInParent<PlayerHealth>();
                if (health == null || health.IsDead.Value) continue;

                float dist = Vector3.Distance(center, hit.transform.position);
                if (dist > _outerRadius) continue;

                float damage;
                if (dist <= _innerRadius)
                    damage = _maxDamage;
                else
                    damage = _maxDamage * (1f - (dist - _innerRadius) / (_outerRadius - _innerRadius));

                // GDD: Crouch bonus — damage reduced by 50%
                CharacterController cc = hit.GetComponentInParent<CharacterController>();
                if (cc != null && cc.height < 1.5f) // crouching
                    damage *= 0.5f;

                DamageProcessor damageProcessor = health.GetComponent<DamageProcessor>();
                if (damageProcessor != null)
                {
                    damageProcessor.ProcessAbilityDamage(OwnerConnectionId, damage, health, "ultimate_doomsday_charge");
                    Debug.Log($"[DoomsdayCharge] {health.OwnerId} took {damage:F0} dmg (dist: {dist:F1}m)");
                }
            }

            RpcPlayExplosionEffect(center);
        }

        [ObserversRpc]
        private void RpcPlayExplosionEffect(Vector3 position)
        {
            Debug.Log($"[DoomsdayCharge] Explosion VFX at {position}");
        }
    }
}
