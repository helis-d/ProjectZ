using System.Collections;
using System.Collections.Generic;
using FishNet.Object;
using ProjectZ.Combat;
using ProjectZ.Core;
using ProjectZ.GameMode;
using ProjectZ.Player;
using UnityEngine;

namespace ProjectZ.Hero.Jielda
{
    /// <summary>
    /// Jielda's ultimate: Spirit Wolves (GDD Section 8).
    /// Sends wolves to the first 3 damaged enemies.
    /// Wolf effect: 1.5 second stun (movement and firing blocked).
    /// </summary>
    public class SpiritWolves : UltimateAbility
    {
        [Header("Spirit Wolves")]
        [SerializeField] private int _maxTargets = 3;
        [SerializeField] private float _stunDuration = 1.5f;
        [SerializeField] private float _wolfTravelSpeed = 25f;
        [SerializeField] private GameObject _wolfPrefab;

        [Server]
        public override void Activate()
        {
            if (!IsServerInitialized) return;

            TeamManager tm = TeamManager.Instance;
            if (tm == null) return;

            Team ownerTeam = tm.GetTeam(OwnerConnectionId);
            List<int> targets = new List<int>();

            // Find enemies that have been damaged (using DamageAssistRegistry concept)
            foreach (var client in ServerManager.Clients.Values)
            {
                if (client.FirstObject == null) continue;

                int targetId = client.ClientId;
                Team targetTeam = tm.GetTeam(targetId);

                if (targetTeam == ownerTeam || targetTeam == Team.None) continue;

                PlayerHealth health = client.FirstObject.GetComponent<PlayerHealth>();
                if (health == null || health.IsDead.Value) continue;

                // Prioritize damaged enemies (not full HP)
                if (health.CurrentHealth.Value < health.MaxHealth)
                    targets.Insert(0, targetId); // damaged first
                else
                    targets.Add(targetId);

                if (targets.Count >= _maxTargets) break;
            }

            // Trim to max
            if (targets.Count > _maxTargets)
                targets.RemoveRange(_maxTargets, targets.Count - _maxTargets);

            foreach (int targetId in targets)
            {
                StartCoroutine(SendWolf(targetId));
            }

            Debug.Log($"[SpiritWolves] Sent {targets.Count} wolves.");
        }

        [Server]
        private IEnumerator SendWolf(int targetId)
        {
            if (!ServerManager.Clients.TryGetValue(targetId, out var conn) || conn.FirstObject == null)
                yield break;

            // Spawn wolf visual
            GameObject wolf = null;
            if (_wolfPrefab != null)
            {
                wolf = Instantiate(_wolfPrefab, CasterTransform.position, Quaternion.identity);
                ServerManager.Spawn(wolf);
            }

            // Simulate wolf travel time
            float dist = Vector3.Distance(CasterTransform.position, conn.FirstObject.transform.position);
            float travelTime = dist / _wolfTravelSpeed;

            if (wolf != null)
            {
                float elapsed = 0f;
                Vector3 start = CasterTransform.position;
                while (elapsed < travelTime)
                {
                    if (conn.FirstObject != null)
                        wolf.transform.position = Vector3.Lerp(start, conn.FirstObject.transform.position, elapsed / travelTime);
                    elapsed += Time.deltaTime;
                    yield return null;
                }
                ServerManager.Despawn(wolf);
            }
            else
            {
                yield return new WaitForSeconds(travelTime);
            }

            // Apply stun
            if (conn.FirstObject != null)
            {
                RpcApplyStun(conn, conn.FirstObject.gameObject, _stunDuration);
                Debug.Log($"[SpiritWolves] Player {targetId} stunned for {_stunDuration}s");
            }
        }

        [TargetRpc]
        private void RpcApplyStun(FishNet.Connection.NetworkConnection conn, GameObject targetPlayer, float duration)
        {
            if (targetPlayer == null)
                return;

            PlayerInputHandler input = targetPlayer.GetComponent<PlayerInputHandler>();
            if (input != null)
                StartCoroutine(StunRoutine(input, duration));
        }

        private IEnumerator StunRoutine(PlayerInputHandler input, float duration)
        {
            input.enabled = false;
            yield return new WaitForSeconds(duration);
            input.enabled = true;
        }
    }
}
