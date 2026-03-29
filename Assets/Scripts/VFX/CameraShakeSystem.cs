using UnityEngine;

namespace ProjectZ.VFX
{
    /// <summary>
    /// Prosedürel kamera sarsıntı sistemi. Singleton olarak çalışır.
    /// Herhangi bir yerden CameraShakeSystem.Instance.Shake() çağrılabilir.
    /// Perlin Noise tabanlı organik titreşim üretir.
    /// 
    /// KURULUM: Player prefab'ına veya sahneye ekle. Singleton: DontDestroyOnLoad.
    /// </summary>
    public class CameraShakeSystem : MonoBehaviour
    {
        public static CameraShakeSystem Instance { get; private set; }

        [Header("Shake Limits")]
        [SerializeField] private float _maxOffset = 0.15f;
        [SerializeField] private float _maxRotation = 3f;

        private float _trauma; // 0-1 arası, kare ile çarpılarak shake üretilir
        private float _decayRate = 2.5f;
        private Camera _cam;
        private Vector3 _originalLocalPos;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            _cam = Camera.main;
            if (_cam != null)
                _originalLocalPos = _cam.transform.localPosition;
        }

        /// <summary>
        /// Sarsıntı ekle. amount: 0-1 arası (0.2 = hafif, 0.5 = orta, 1.0 = çok sert).
        /// </summary>
        public void Shake(float amount)
        {
            _trauma = Mathf.Clamp01(_trauma + amount);
        }

        /// <summary>
        /// Ateş etme sarsıntısı (silah geri tepmesiyle uyumlu hafif shake).
        /// </summary>
        public void FireShake()
        {
            Shake(0.08f);
        }

        /// <summary>
        /// Hasar alma sarsıntısı. Hasara orantılı.
        /// </summary>
        public void DamageShake(float damageAmount)
        {
            float intensity = Mathf.Clamp01(damageAmount / 100f) * 0.4f;
            Shake(intensity);
        }

        /// <summary>
        /// Patlama/yetenek sarsıntısı (güçlü).
        /// </summary>
        public void ExplosionShake()
        {
            Shake(0.6f);
        }

        private void LateUpdate()
        {
            if (_cam == null)
            {
                _cam = Camera.main;
                if (_cam != null)
                    _originalLocalPos = _cam.transform.localPosition;
                return;
            }

            if (_trauma <= 0f) return;

            // Perlin noise ile organik hareket
            float shake = _trauma * _trauma; // Kare = daha doğal his
            float seed = Time.time * 25f;

            float offsetX = _maxOffset * shake * (Mathf.PerlinNoise(seed, 0f) * 2f - 1f);
            float offsetY = _maxOffset * shake * (Mathf.PerlinNoise(0f, seed) * 2f - 1f);
            float rotZ = _maxRotation * shake * (Mathf.PerlinNoise(seed, seed) * 2f - 1f);

            _cam.transform.localPosition = _originalLocalPos + new Vector3(offsetX, offsetY, 0f);
            _cam.transform.localRotation *= Quaternion.Euler(0f, 0f, rotZ);

            // Zamanla azalt
            _trauma = Mathf.Max(0f, _trauma - _decayRate * Time.deltaTime);

            if (_trauma <= 0f)
                _cam.transform.localPosition = _originalLocalPos;
        }
    }
}
