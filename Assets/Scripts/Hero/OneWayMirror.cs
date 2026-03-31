using System.Collections;
using FishNet.Object;
using ProjectZ.Core;
using ProjectZ.Player;
using UnityEngine;

namespace ProjectZ.Hero.Helix
{
    /// <summary>
    /// Helix's ultimate: One-Way Mirror (GDD Section 8).
    /// Creates a 2×1.5m one-way window.
    /// Helix side: clear vision. Enemy side: opaque, wavy energy.
    /// Duration: 12 seconds, +25 HP for kills from behind window.
    /// </summary>
    public class OneWayMirror : UltimateAbility
    {
        [Header("One-Way Mirror")]
        [SerializeField] private float _duration = 12f;
        [SerializeField] private float _width = 2f;
        [SerializeField] private float _height = 1.5f;
        [SerializeField] private float _killBonusHP = 25f;
        [SerializeField] private float _placeDistance = 2f;
        [SerializeField] private GameObject _mirrorPrefab;

        private GameObject _activeMirror;
        private bool _isActive;

        [Server]
        public override void Activate()
        {
            if (!IsServerInitialized) return;

            Vector3 pos = CasterTransform.position + CasterTransform.forward * _placeDistance;
            Quaternion rot = Quaternion.LookRotation(CasterTransform.forward);

            if (_mirrorPrefab != null)
            {
                _activeMirror = Instantiate(_mirrorPrefab, pos, rot);
                _activeMirror.transform.localScale = new Vector3(_width, _height, 0.05f);
                ServerManager.Spawn(_activeMirror);
            }
            else
            {
                _activeMirror = new GameObject("OneWayMirror");
                _activeMirror.transform.position = pos;
                _activeMirror.transform.rotation = rot;
                _activeMirror.transform.localScale = new Vector3(_width, _height, 0.05f);

                var col = _activeMirror.AddComponent<BoxCollider>();
                col.isTrigger = false; // Blocks bullets from enemy side
            }

            _isActive = true;
            GameEvents.OnPlayerDeath += HandleKillBehindMirror;
            StartCoroutine(MirrorLifetime());

            FishNet.Object.NetworkObject mirrorNetworkObject = _activeMirror.GetComponent<FishNet.Object.NetworkObject>();
            if (mirrorNetworkObject != null)
                RpcSetupMirrorVisuals(_activeMirror, OwnerConnectionId);
            Debug.Log($"[OneWayMirror] Placed at {pos}, {_width}×{_height}m, {_duration}s");
        }

        [Server]
        private IEnumerator MirrorLifetime()
        {
            yield return new WaitForSeconds(_duration);

            _isActive = false;
            GameEvents.OnPlayerDeath -= HandleKillBehindMirror;

            if (_activeMirror != null)
            {
                var nob = _activeMirror.GetComponent<FishNet.Object.NetworkObject>();
                if (nob != null && nob.IsSpawned)
                    ServerManager.Despawn(_activeMirror);
                else
                    Destroy(_activeMirror);
            }

            Debug.Log("[OneWayMirror] Expired.");
        }

        private void HandleKillBehindMirror(int victimId, int killerId)
        {
            if (!_isActive || killerId != OwnerConnectionId || _activeMirror == null) return;

            // Check if owner is behind the mirror
            Vector3 mirrorForward = _activeMirror.transform.forward;
            Vector3 ownerToMirror = _activeMirror.transform.position - CasterTransform.position;

            if (Vector3.Dot(mirrorForward, ownerToMirror) > 0f)
            {
                // Owner is behind the mirror (Helix side)
                PlayerHealth health = GetOwnerComponent<PlayerHealth>();
                if (health != null)
                {
                    health.AddHealth(_killBonusHP);
                    Debug.Log($"[OneWayMirror] Kill from behind mirror! +{_killBonusHP} HP");
                }
            }
        }

        [ObserversRpc]
        private void RpcSetupMirrorVisuals(GameObject mirror, int ownerId)
        {
            if (mirror == null) return;

            // The mirror material should be one-way:
            // Owner side: transparent/clear
            // Enemy side: opaque wavy energy shader
            var renderer = mirror.GetComponent<Renderer>();
            if (renderer != null)
            {
                // In a full implementation, this would use a custom shader
                // that checks camera position vs mirror normal
                Debug.Log($"[OneWayMirror] Visual setup for client (owner: {ownerId})");
            }
        }

        private void OnDestroy()
        {
            if (_isActive) GameEvents.OnPlayerDeath -= HandleKillBehindMirror;
        }
    }
}
