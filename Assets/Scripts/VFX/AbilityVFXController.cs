using UnityEngine;

namespace ProjectZ.VFX
{
    /// <summary>
    /// Hero yeteneği aktif olduğunda prosedürel VFX efektleri üretir:
    ///   1. Karakter etrafında parçacık patlaması (renk hero'ya göre değişir)
    ///   2. Kamera sarsıntısı
    ///   3. Ekran renk overlay'i (opsiyonel)
    /// 
    /// KURULUM: Player prefab'ına ekle. 
    /// Hero scriptleri Activate() çağırdığında bu sistemi de tetiklesin.
    /// </summary>
    public class AbilityVFXController : MonoBehaviour
    {
        [Header("Burst Particle Settings")]
        [SerializeField] private int _burstCount = 30;
        [SerializeField] private float _burstSpeed = 5f;
        [SerializeField] private float _burstLifetime = 0.6f;

        private ParticleSystem _burstParticles;
        private ParticleSystem _auraParticles;

        // ─── Hero renk paleti ─────────────────────────────────────────────
        public static readonly Color VoltColor = new Color(0.2f, 0.6f, 1f);       // Elektrik mavisi
        public static readonly Color SectorColor = new Color(0.9f, 0.4f, 0.1f);   // Turuncu
        public static readonly Color LagrangeColor = new Color(0.3f, 0.8f, 1f);   // Açık cyan
        public static readonly Color KantColor = new Color(0.7f, 0.2f, 0.9f);     // Mor
        public static readonly Color DefaultColor = new Color(1f, 0.9f, 0.3f);    // Sarı

        private void Start()
        {
            CreateBurstSystem();
            CreateAuraSystem();
        }

        /// <summary>
        /// Yetenek aktive edildiğinde çağır.
        /// heroName: "volt", "sector", "lagrange", "kant" vb.
        /// </summary>
        public void PlayActivation(string heroName)
        {
            Color color = GetHeroColor(heroName);
            EmitBurst(color);
            EmitAura(color);

            // Kamera sarsıntısı
            if (CameraShakeSystem.Instance != null)
                CameraShakeSystem.Instance.ExplosionShake();
        }

        /// <summary>
        /// Sürekli devam eden yetenek efekti (aura).
        /// Yetenek süresi boyunca çağrılabilir.
        /// </summary>
        public void PlaySustainedAura(string heroName)
        {
            Color color = GetHeroColor(heroName);
            var auraMain = _auraParticles.main;
            auraMain.startColor = color;

            if (!_auraParticles.isPlaying)
                _auraParticles.Play();
        }

        /// <summary>
        /// Sürekli aura efektini durdur.
        /// </summary>
        public void StopSustainedAura()
        {
            if (_auraParticles.isPlaying)
                _auraParticles.Stop();
        }

        private Color GetHeroColor(string heroName)
        {
            if (string.IsNullOrEmpty(heroName)) return DefaultColor;
            return heroName.ToLower() switch
            {
                "volt" => VoltColor,
                "sector" => SectorColor,
                "lagrange" => LagrangeColor,
                "kant" => KantColor,
                _ => DefaultColor
            };
        }

        private void EmitBurst(Color color)
        {
            var main = _burstParticles.main;
            main.startColor = color;

            _burstParticles.transform.position = transform.position + Vector3.up * 1f;
            _burstParticles.Emit(_burstCount);
        }

        private void EmitAura(Color color)
        {
            var main = _auraParticles.main;
            main.startColor = new Color(color.r, color.g, color.b, 0.3f);

            _auraParticles.transform.position = transform.position;
            _auraParticles.Emit(15);
        }

        // ─── Prosedürel Particle Oluşturuculari ───────────────────────────
        private void CreateBurstSystem()
        {
            GameObject obj = new GameObject("Ability_Burst");
            obj.transform.SetParent(transform);
            _burstParticles = obj.AddComponent<ParticleSystem>();

            var main = _burstParticles.main;
            main.startLifetime = _burstLifetime;
            main.startSpeed = _burstSpeed;
            main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.08f);
            main.startColor = DefaultColor;
            main.maxParticles = 100;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;
            main.gravityModifier = -0.5f; // Yukarı süzülsün

            var emission = _burstParticles.emission;
            emission.enabled = false;

            var shape = _burstParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.3f;

            var sizeOverLife = _burstParticles.sizeOverLifetime;
            sizeOverLife.enabled = true;
            sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));

            var colorOverLife = _burstParticles.colorOverLifetime;
            colorOverLife.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new(Color.white, 0f), new(Color.white, 1f) },
                new GradientAlphaKey[] { new(1f, 0f), new(0f, 1f) }
            );
            colorOverLife.color = grad;

            obj.GetComponent<ParticleSystemRenderer>().material = CreateParticleMaterial(DefaultColor);
        }

        private void CreateAuraSystem()
        {
            GameObject obj = new GameObject("Ability_Aura");
            obj.transform.SetParent(transform);
            obj.transform.localPosition = Vector3.zero;
            _auraParticles = obj.AddComponent<ParticleSystem>();

            var main = _auraParticles.main;
            main.startLifetime = 1.5f;
            main.startSpeed = 0.5f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
            main.startColor = new Color(1f, 1f, 1f, 0.3f);
            main.maxParticles = 50;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;
            main.gravityModifier = -0.3f;
            main.loop = true;

            var emission = _auraParticles.emission;
            emission.rateOverTime = 10f;

            var shape = _auraParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.5f;

            var sizeOverLife = _auraParticles.sizeOverLifetime;
            sizeOverLife.enabled = true;
            sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.5f, 1f, 0f));

            obj.GetComponent<ParticleSystemRenderer>().material = CreateParticleMaterial(Color.white);

            _auraParticles.Stop();
        }

        private Material CreateParticleMaterial(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null) shader = Shader.Find("Standard");

            Material mat = new Material(shader);
            mat.color = color;
            return mat;
        }
    }
}
