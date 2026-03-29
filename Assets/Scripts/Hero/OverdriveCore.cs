using System.Collections;
using FishNet.Object;
using ProjectZ.Core;
using ProjectZ.GameMode;
using ProjectZ.Player;
using ProjectZ.Weapon;
using UnityEngine;

namespace ProjectZ.Hero.Silvia
{
    /// <summary>
    /// Silvia's ultimate: Overdrive Core (GDD Section 8).
    /// Creates a 60m long energy tunnel.
    /// Allies inside: movement speed ×1.3, fire rate ×1.15
    /// Enemies inside: movement speed ×0.6
    /// Duration: 8 seconds (+2s per kill during ultimate)
    /// </summary>
    public class OverdriveCore : UltimateAbility
    {
        [Header("Overdrive Core")]
        [SerializeField] private float _tunnelLength = 60f;
        [SerializeField] private float _tunnelWidth = 4f;
        [SerializeField] private float _baseDuration = 8f;
        [SerializeField] private float _killBonusTime = 2f;
        [SerializeField] private LayerMask _playerLayer;

        private float _remainingTime;
        private bool _isActive;
        private Vector3 _tunnelStart;
        private Vector3 _tunnelEnd;
        private Vector3 _tunnelCenter;

        [Server]
        public override void Activate()
        {
            if (!IsServerInitialized) return;

            _tunnelStart = transform.position;
            _tunnelEnd = transform.position + transform.forward * _tunnelLength;
            _tunnelCenter = (_tunnelStart + _tunnelEnd) * 0.5f;
            _remainingTime = _baseDuration;
            _isActive = true;

            Core.GameEvents.OnPlayerDeath += HandleKillDuringUlt;
            StartCoroutine(OverdriveRoutine());
            RpcShowTunnelEffect(_tunnelStart, _tunnelEnd, _tunnelWidth);

            Debug.Log($"[OverdriveCore] Tunnel active for {_baseDuration}s");
        }

        [Server]
        private IEnumerator OverdriveRoutine()
        {
            while (_remainingTime > 0f)
            {
                ApplyEffects();
                _remainingTime -= Time.deltaTime;
                yield return null;
            }

            _isActive = false;
            Core.GameEvents.OnPlayerDeath -= HandleKillDuringUlt;
            RemoveAllEffects();
            Debug.Log("[OverdriveCore] Tunnel expired.");
        }

        [Server]
        private void ApplyEffects()
        {
            TeamManager tm = TeamManager.Instance;
            if (tm == null) return;

            Team ownerTeam = tm.GetTeam(OwnerId);

            foreach (var client in ServerManager.Clients.Values)
            {
                if (client.FirstObject == null) continue;
                Vector3 pos = client.FirstObject.transform.position;

                if (!IsInsideTunnel(pos)) continue;

                Team playerTeam = tm.GetTeam(client.ClientId);

                if (playerTeam == ownerTeam)
                {
                    // Ally buff: speed ×1.3, fire rate ×1.15
                    var wm = client.FirstObject.GetComponent<WeaponManager>();
                    if (wm != null)
                    {
                        var weapon = wm.GetActiveWeapon();
                        if (weapon != null) weapon.ApplyTemporaryFireRateBuff(1.15f, Time.deltaTime + 0.1f);
                    }
                }
                // Enemy debuff applied via movement speed modifier would go here
            }
        }

        private bool IsInsideTunnel(Vector3 point)
        {
            Vector3 dir = (_tunnelEnd - _tunnelStart).normalized;
            Vector3 toPoint = point - _tunnelStart;
            float projection = Vector3.Dot(toPoint, dir);

            if (projection < 0f || projection > _tunnelLength) return false;

            Vector3 closestOnLine = _tunnelStart + dir * projection;
            float dist = Vector3.Distance(point, closestOnLine);
            return dist <= _tunnelWidth * 0.5f;
        }

        private void HandleKillDuringUlt(int victimId, int killerId)
        {
            if (!_isActive || killerId != OwnerId) return;
            _remainingTime += _killBonusTime;
            Debug.Log($"[OverdriveCore] Kill bonus! +{_killBonusTime}s (remaining: {_remainingTime:F1}s)");
        }

        [Server]
        private void RemoveAllEffects()
        {
            // Buffs are time-limited and auto-expire
        }

        [ObserversRpc]
        private void RpcShowTunnelEffect(Vector3 start, Vector3 end, float width)
        {
            Debug.Log($"[OverdriveCore] Tunnel VFX: {start} -> {end}, width {width}m");
        }
    }
}
