using UnityEngine;
using UnityEngine.UI;

namespace ProjectZ.VFX
{
    /// <summary>
    /// Hasar alındığında ekranın kenarlarında kısa kırmızı flaş efekti.
    /// PlayerHealth.CurrentHealth SyncVar değişimini dinler.
    /// 
    /// KURULUM:
    /// 1. Canvas'a tam ekran bir UI Image ekle (kırmızı radial gradient veya düz kırmızı)
    /// 2. Bu scripti aynı Canvas'a ekle ve _flashImage'a o Image'ı ata
    /// 3. Başlangıçta alpha = 0 olmalı
    /// </summary>
    public class HitFlashEffect : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Image _flashImage;

        [Header("Settings")]
        [SerializeField] private Color _flashColor = new Color(0.8f, 0.05f, 0.05f, 0.45f);
        [SerializeField] private float _flashDuration = 0.15f;
        [SerializeField] private float _fadeSpeed = 8f;

        private float _flashTimer;
        private float _currentAlpha;
        private float _lastKnownHealth = -1f;

        /// <summary>
        /// Yerel oyuncuyu bağla — HP değişimini izlemek için.
        /// </summary>
        public void BindLocalPlayer(Player.PlayerHealth playerHealth)
        {
            if (playerHealth == null) return;
            // İlk değeri kaydet
            _lastKnownHealth = playerHealth.CurrentHealth.Value;

            // SyncVar değişim callback'i
            playerHealth.CurrentHealth.OnChange += OnHealthChanged;
        }

        private void OnHealthChanged(float prev, float next, bool asServer)
        {
            // Sadece hasar aldıysa (değer düştüyse) flash göster
            if (next < prev && next > 0f)
            {
                float damageRatio = (prev - next) / 100f;
                TriggerFlash(Mathf.Clamp(damageRatio * 2f, 0.3f, 1f));
            }
        }

        /// <summary>
        /// Dışarıdan da çağrılabilir: flash yoğunluğunu (0-1) belirle.
        /// </summary>
        public void TriggerFlash(float intensity = 1f)
        {
            _currentAlpha = _flashColor.a * intensity;
            _flashTimer = _flashDuration;

            // Kamera sarsıntısı da tetikle
            if (CameraShakeSystem.Instance != null)
                CameraShakeSystem.Instance.DamageShake(intensity * 50f);
        }

        private void Update()
        {
            if (_flashImage == null) return;

            if (_flashTimer > 0f)
            {
                _flashTimer -= Time.deltaTime;
            }
            else
            {
                _currentAlpha = Mathf.Lerp(_currentAlpha, 0f, Time.deltaTime * _fadeSpeed);
            }

            Color c = _flashColor;
            c.a = _currentAlpha;
            _flashImage.color = c;
            _flashImage.enabled = _currentAlpha > 0.01f;
        }
    }
}
