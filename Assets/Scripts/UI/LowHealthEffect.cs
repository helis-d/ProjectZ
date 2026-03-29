using UnityEngine;
using UnityEngine.UI;

namespace ProjectZ.UI
{
    /// <summary>
    /// GDD Section 10: Low health visual effect.
    /// Below 20% HP: red vignette at screen edges with pulse effect.
    /// </summary>
    public class LowHealthEffect : MonoBehaviour
    {
        [Header("Vignette")]
        [SerializeField] private Image _vignetteImage;
        [SerializeField] private float _hpThreshold = 20f;
        [SerializeField] private float _pulseSpeed = 2.5f;
        [SerializeField] private float _minAlpha = 0.15f;
        [SerializeField] private float _maxAlpha = 0.55f;

        [Header("References")]
        [SerializeField] private Player.PlayerHealth _playerHealth;

        private bool _isActive;

        private void Update()
        {
            if (_vignetteImage == null) return;

            float hp = _playerHealth != null ? _playerHealth.CurrentHealth.Value : 100f;
            float maxHp = _playerHealth != null ? _playerHealth.MaxHealth : 100f;
            float hpPercent = (hp / maxHp) * 100f;

            _isActive = hpPercent <= _hpThreshold && hpPercent > 0f;

            if (_isActive)
            {
                // Pulse effect: oscillate alpha
                float pulse = Mathf.Lerp(_minAlpha, _maxAlpha,
                    (Mathf.Sin(Time.time * _pulseSpeed) + 1f) * 0.5f);

                // Intensity increases as HP drops
                float intensity = 1f - (hpPercent / _hpThreshold);
                float alpha = pulse * (0.5f + intensity * 0.5f);

                Color c = _vignetteImage.color;
                c.a = alpha;
                _vignetteImage.color = c;
                _vignetteImage.enabled = true;
            }
            else
            {
                _vignetteImage.enabled = false;
            }
        }
    }
}

