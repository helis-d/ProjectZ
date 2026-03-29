using UnityEngine;

namespace ProjectZ.VFX
{
    /// <summary>
    /// Prosedürel isabet efekti sistemi. Mermi bir yüzeye çarptığında:
    ///   1. Kıvılcım/toz partikülleri
    ///   2. Darbe halkası (shockwave ring)
    /// SurfaceMaterial tipine göre renk değişir.
    /// 
    /// KURULUM: Player prefab'ına ekle. Otomatik çalışır.
    /// </summary>
    public class HitImpactSystem : MonoBehaviour
    {
        [Header("Impact Particles")]
        [SerializeField] private int _debrisCount = 8;
        [SerializeField] private float _debrisSpeed = 4f;
        [SerializeField] private float _debrisLifetime = 0.4f;

        [Header("Surface Colors")]
        [SerializeField] private Color _stoneColor = new Color(0.6f, 0.55f, 0.5f);
        [SerializeField] private Color _metalColor = new Color(1f, 0.85f, 0.3f);
        [SerializeField] private Color _woodColor = new Color(0.55f, 0.35f, 0.2f);
        [SerializeField] private Color _glassColor = new Color(0.7f, 0.9f, 1f);
        [SerializeField] private Color _defaultColor = new Color(0.7f, 0.65f, 0.6f);
        [SerializeField] private Color _playerHitColor = new Color(1f, 0.2f, 0.15f);

        private ParticleSystem _debrisParticles;
        private ParticleSystem _ringParticles;
        private Material _debrisMat;
        private Material _ringMat;

        private void Start()
        {
            CreateDebrisSystem();
            CreateRingSystem();
        }

        /// <summary>
        /// Dünya yüzeyine isabet efekti. SurfaceType enum yoksa default renk kullanılır.
        /// </summary>
        public void ShowWorldImpact(Vector3 point, Vector3 normal, Combat.SurfaceType surfaceType = Combat.SurfaceType.Stone)
        {
            Color color = surfaceType switch
            {
                Combat.SurfaceType.Stone => _stoneColor,
                Combat.SurfaceType.Metal => _metalColor,
                Combat.SurfaceType.Wood => _woodColor,
                Combat.SurfaceType.Glass => _glassColor,
                _ => _defaultColor
            };

            EmitImpact(point, normal, color);
        }

        /// <summary>
        /// Oyuncuya isabet efekti (kırmızı kan sıçraması).
        /// </summary>
        public void ShowPlayerImpact(Vector3 point, Vector3 normal)
        {
            EmitImpact(point, normal, _playerHitColor);
        }

        private void EmitImpact(Vector3 point, Vector3 normal, Color color)
        {
            // Debris (moloz/kıvılcım)
            _debrisParticles.transform.position = point;
            _debrisParticles.transform.rotation = Quaternion.LookRotation(normal);

            var debrisMain = _debrisParticles.main;
            debrisMain.startColor = color;
            _debrisParticles.Emit(_debrisCount);

            // Ring (darbe halkası)
            _ringParticles.transform.position = point + normal * 0.01f;
            _ringParticles.transform.rotation = Quaternion.LookRotation(normal);

            var ringMain = _ringParticles.main;
            ringMain.startColor = new Color(color.r, color.g, color.b, 0.5f);
            _ringParticles.Emit(1);
        }

        private void CreateDebrisSystem()
        {
            GameObject obj = new GameObject("Impact_Debris");
            obj.transform.SetParent(transform);
            _debrisParticles = obj.AddComponent<ParticleSystem>();

            var main = _debrisParticles.main;
            main.startLifetime = _debrisLifetime;
            main.startSpeed = _debrisSpeed;
            main.startSize = new ParticleSystem.MinMaxCurve(0.01f, 0.04f);
            main.startColor = _defaultColor;
            main.maxParticles = 100;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;
            main.gravityModifier = 2f;

            var emission = _debrisParticles.emission;
            emission.enabled = false;

            var shape = _debrisParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = 0.05f;

            var sizeOverLife = _debrisParticles.sizeOverLifetime;
            sizeOverLife.enabled = true;
            sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));

            _debrisMat = CreateParticleMaterial(_defaultColor);
            obj.GetComponent<ParticleSystemRenderer>().material = _debrisMat;
        }

        private void CreateRingSystem()
        {
            GameObject obj = new GameObject("Impact_Ring");
            obj.transform.SetParent(transform);
            _ringParticles = obj.AddComponent<ParticleSystem>();

            var main = _ringParticles.main;
            main.startLifetime = 0.2f;
            main.startSpeed = 0f;
            main.startSize = 0.05f;
            main.startColor = new Color(1f, 1f, 1f, 0.5f);
            main.maxParticles = 10;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;

            var emission = _ringParticles.emission;
            emission.enabled = false;

            var sizeOverLife = _ringParticles.sizeOverLifetime;
            sizeOverLife.enabled = true;
            sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.3f, 1f, 1f));

            var colorOverLife = _ringParticles.colorOverLifetime;
            colorOverLife.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new(Color.white, 0f), new(Color.white, 1f) },
                new GradientAlphaKey[] { new(0.6f, 0f), new(0f, 1f) }
            );
            colorOverLife.color = grad;

            _ringMat = CreateParticleMaterial(Color.white);
            obj.GetComponent<ParticleSystemRenderer>().material = _ringMat;
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
