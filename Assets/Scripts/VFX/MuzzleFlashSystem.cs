using UnityEngine;

namespace ProjectZ.VFX
{
    /// <summary>
    /// Prosedürel namlu flaşı sistemi. Dışarıdan hiçbir asset gerekmez.
    /// BaseWeapon.OnWeaponFired eventine abone olur, tetiklendiğinde:
    ///   1. Point Light ile anlık ışık parlaması
    ///   2. ParticleSystem ile kıvılcım/alev efekti
    /// 
    /// KURULUM: Player prefab'ına ekle. Otomatik çalışır.
    /// </summary>
    public class MuzzleFlashSystem : MonoBehaviour
    {
        [Header("Light Flash")]
        [SerializeField] private Color _flashColor = new Color(1f, 0.8f, 0.4f);
        [SerializeField] private float _flashIntensity = 3f;
        [SerializeField] private float _flashRange = 5f;
        [SerializeField] private float _flashDuration = 0.05f;

        [Header("Particle Settings")]
        [SerializeField] private int _sparkCount = 12;
        [SerializeField] private float _sparkSpeed = 8f;
        [SerializeField] private float _sparkLifetime = 0.08f;
        [SerializeField] private float _flashScale = 0.15f;

        // ─── Runtime ──────────────────────────────────────────────────────
        private Light _flashLight;
        private ParticleSystem _sparkParticles;
        private ParticleSystem _coreFlash;
        private float _flashTimer;
        private Transform _currentMuzzle;

        private void Start()
        {
            CreateFlashLight();
            CreateSparkParticles();
            CreateCoreFlash();
        }

        /// <summary>
        /// Dışarıdan çağrılır — BaseWeapon.muzzlePoint'ten ateş efekti tetikler.
        /// </summary>
        public void Fire(Transform muzzlePoint)
        {
            if (muzzlePoint == null) return;
            _currentMuzzle = muzzlePoint;

            // Işık
            _flashLight.transform.position = muzzlePoint.position;
            _flashLight.enabled = true;
            _flashTimer = _flashDuration;

            // Kıvılcım
            _sparkParticles.transform.position = muzzlePoint.position;
            _sparkParticles.transform.rotation = muzzlePoint.rotation;
            _sparkParticles.Emit(_sparkCount);

            // Çekirdek flaş
            _coreFlash.transform.position = muzzlePoint.position;
            _coreFlash.transform.rotation = muzzlePoint.rotation;
            _coreFlash.Emit(1);
        }

        private void Update()
        {
            if (_flashTimer > 0f)
            {
                _flashTimer -= Time.deltaTime;
                float t = Mathf.Clamp01(_flashTimer / _flashDuration);
                _flashLight.intensity = _flashIntensity * t;

                if (_flashTimer <= 0f)
                    _flashLight.enabled = false;
            }
        }

        // ─── Prosedürel bileşen oluşturuculari ────────────────────────────
        private void CreateFlashLight()
        {
            GameObject lightObj = new GameObject("MuzzleFlash_Light");
            lightObj.transform.SetParent(transform);
            _flashLight = lightObj.AddComponent<Light>();
            _flashLight.type = LightType.Point;
            _flashLight.color = _flashColor;
            _flashLight.intensity = 0f;
            _flashLight.range = _flashRange;
            _flashLight.shadows = LightShadows.None;
            _flashLight.enabled = false;
        }

        private void CreateSparkParticles()
        {
            GameObject sparkObj = new GameObject("MuzzleFlash_Sparks");
            sparkObj.transform.SetParent(transform);
            _sparkParticles = sparkObj.AddComponent<ParticleSystem>();

            var main = _sparkParticles.main;
            main.startLifetime = _sparkLifetime;
            main.startSpeed = _sparkSpeed;
            main.startSize = 0.02f;
            main.startColor = new Color(1f, 0.9f, 0.5f);
            main.maxParticles = 50;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;

            var emission = _sparkParticles.emission;
            emission.enabled = false; // Emit() ile elle tetikliyoruz

            var shape = _sparkParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 25f;
            shape.radius = 0.01f;

            var sizeOverLife = _sparkParticles.sizeOverLifetime;
            sizeOverLife.enabled = true;
            sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));

            // Renderer
            var renderer = sparkObj.GetComponent<ParticleSystemRenderer>();
            renderer.material = CreateParticleMaterial(new Color(1f, 0.85f, 0.4f));
        }

        private void CreateCoreFlash()
        {
            GameObject coreObj = new GameObject("MuzzleFlash_Core");
            coreObj.transform.SetParent(transform);
            _coreFlash = coreObj.AddComponent<ParticleSystem>();

            var main = _coreFlash.main;
            main.startLifetime = 0.04f;
            main.startSpeed = 0f;
            main.startSize = _flashScale;
            main.startColor = new Color(1f, 0.95f, 0.8f, 0.9f);
            main.maxParticles = 5;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;

            var emission = _coreFlash.emission;
            emission.enabled = false;

            var sizeOverLife = _coreFlash.sizeOverLifetime;
            sizeOverLife.enabled = true;
            sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));

            var renderer = coreObj.GetComponent<ParticleSystemRenderer>();
            renderer.material = CreateParticleMaterial(new Color(1f, 0.9f, 0.6f));
        }

        private Material CreateParticleMaterial(Color color)
        {
            // URP'de Particles/Standard Unlit shader kullan
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null) shader = Shader.Find("Standard");

            Material mat = new Material(shader);
            mat.color = color;
            mat.SetFloat("_Surface", 1f); // Transparent
            return mat;
        }
    }
}
