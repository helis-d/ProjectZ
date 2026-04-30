using System.Collections;
using FishNet.Object;
using ProjectZ.Core;
using ProjectZ.Player;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectZ.UI
{
    /// <summary>
    /// GDD Section 10: Damage visual feedback.
    /// - Lost health red portion slowly decreasing (delayed health bar)
    /// - Directional damage indicator arrows
    ///
    /// Subscribes to GameEvents.OnPlayerDamaged automatically once BindLocalPlayer is called.
    /// </summary>
    public class DamageIndicator : NetworkBehaviour
    {
        [Header("Delayed Health Bar")]
        [SerializeField] private Image _delayedHealthBar;
        [SerializeField] private Image _currentHealthBar;
        [SerializeField] private float _delayBeforeShrink = 0.5f;
        [SerializeField] private float _shrinkSpeed = 2f;

        [Header("Directional Indicators")]
        [SerializeField] private RectTransform _indicatorContainer;
        [SerializeField] private GameObject _indicatorPrefab;
        [SerializeField] private float _indicatorDuration = 2.0f;

        private float _displayedDelayedHP = 1f;
        private float _actualHP = 1f;
        private float _delayCooldown;

        // Connection ID of the local owner — set via BindLocalPlayer.
        private int _localConnId = -1;

        /// <summary>
        /// Call once when the local player spawns. Registers damage event listener.
        /// </summary>
        public void BindLocalPlayer(int connectionId)
        {
            _localConnId = connectionId;
        }

        private void OnEnable()
        {
            GameEvents.OnPlayerDamaged += HandlePlayerDamaged;
        }

        private void OnDisable()
        {
            GameEvents.OnPlayerDamaged -= HandlePlayerDamaged;
        }

        /// <summary>Filters incoming damage events to the locally owned player.</summary>
        private void HandlePlayerDamaged(int victimId, int attackerId, float damage)
        {
            if (_localConnId < 0 || victimId != _localConnId)
                return;

            // Derive a rough world position from the attacker if possible — directional indicator.
            // For now we pass Vector3.zero; callers can extend to pass attacker position.
            OnDamageTaken(Mathf.Clamp01(damage / 100f), Vector3.zero);
        }

        /// <summary>Called when player takes damage. normalizedHP is 0-1.</summary>
        public void OnDamageTaken(float normalizedHP, Vector3 damageSourceWorldPos)
        {
            float previousHP = _actualHP;
            _actualHP = normalizedHP;

            // Reset delay timer so the red bar stays visible briefly
            if (normalizedHP < previousHP)
            {
                _delayCooldown = _delayBeforeShrink;
                ShowDirectionalIndicator(damageSourceWorldPos);
            }

            // Update current health bar immediately
            if (_currentHealthBar != null)
                _currentHealthBar.fillAmount = normalizedHP;
        }

        /// <summary>Called when health is reset (e.g. new round).</summary>
        public void ResetBars(float normalizedHP)
        {
            _actualHP = normalizedHP;
            _displayedDelayedHP = normalizedHP;

            if (_currentHealthBar != null) _currentHealthBar.fillAmount = normalizedHP;
            if (_delayedHealthBar != null) _delayedHealthBar.fillAmount = normalizedHP;
        }

        private void Update()
        {
            if (_delayedHealthBar == null) return;

            if (_delayCooldown > 0f)
            {
                _delayCooldown -= Time.deltaTime;
            }
            else
            {
                // Slowly shrink the delayed bar towards actual HP
                if (_displayedDelayedHP > _actualHP)
                {
                    _displayedDelayedHP -= _shrinkSpeed * Time.deltaTime;
                    if (_displayedDelayedHP < _actualHP)
                        _displayedDelayedHP = _actualHP;
                }
            }

            _delayedHealthBar.fillAmount = _displayedDelayedHP;
        }

        // ─── Directional Indicator ─────────────────────────────────────────
        private void ShowDirectionalIndicator(Vector3 sourceWorld)
        {
            if (_indicatorPrefab == null || _indicatorContainer == null) return;

            Camera mainCamera = Camera.main;
            if (mainCamera == null) return;

            GameObject indicator = Instantiate(_indicatorPrefab, _indicatorContainer);

            // Calculate angle from player to damage source
            Vector3 toSource = sourceWorld - mainCamera.transform.position;
            toSource.y = 0f;
            float angle = Vector3.SignedAngle(mainCamera.transform.forward, toSource, Vector3.up);

            indicator.transform.localRotation = Quaternion.Euler(0f, 0f, -angle);

            StartCoroutine(FadeAndDestroy(indicator));
        }

        private IEnumerator FadeAndDestroy(GameObject indicator)
        {
            CanvasGroup cg = indicator.GetComponent<CanvasGroup>();
            if (cg == null) cg = indicator.AddComponent<CanvasGroup>();

            float elapsed = 0f;
            while (elapsed < _indicatorDuration)
            {
                cg.alpha = 1f - (elapsed / _indicatorDuration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            Destroy(indicator);
        }
    }
}
