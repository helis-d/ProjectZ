using UnityEngine;

namespace ProjectZ.VFX
{
    /// <summary>
    /// Prosedürel mermi izi sistemi. LineRenderer ile namludan isabet noktasına
    /// hızlı bir ışık çizgisi çizer ve kısa sürede solar.
    /// 
    /// KURULUM: Player prefab'ına ekle. Otomatik çalışır.
    /// </summary>
    public class BulletTrailRenderer : MonoBehaviour
    {
        [Header("Trail Settings")]
        [SerializeField] private Color _trailStartColor = new Color(1f, 0.95f, 0.7f, 0.8f);
        [SerializeField] private Color _trailEndColor = new Color(1f, 0.8f, 0.4f, 0f);
        [SerializeField] private float _trailWidth = 0.015f;
        [SerializeField] private float _trailDuration = 0.08f;
        [SerializeField] private int _poolSize = 10;

        private LineRenderer[] _pool;
        private float[] _timers;
        private int _poolIndex;
        private Material _trailMaterial;

        private void Start()
        {
            CreateMaterial();
            InitPool();
        }

        /// <summary>
        /// Dışarıdan çağrılır — namlu ucundan isabet noktasına mermi izi çizer.
        /// </summary>
        public void ShowTrail(Vector3 from, Vector3 to)
        {
            LineRenderer lr = _pool[_poolIndex];
            lr.enabled = true;
            lr.SetPosition(0, from);
            lr.SetPosition(1, to);
            _timers[_poolIndex] = _trailDuration;

            _poolIndex = (_poolIndex + 1) % _poolSize;
        }

        private void Update()
        {
            for (int i = 0; i < _poolSize; i++)
            {
                if (_timers[i] <= 0f) continue;

                _timers[i] -= Time.deltaTime;
                float t = Mathf.Clamp01(_timers[i] / _trailDuration);

                // Solma efekti
                Color startC = _trailStartColor;
                startC.a *= t;
                Color endC = _trailEndColor;
                endC.a *= t;

                _pool[i].startColor = startC;
                _pool[i].endColor = endC;
                _pool[i].startWidth = _trailWidth * t;
                _pool[i].endWidth = _trailWidth * t * 0.3f;

                if (_timers[i] <= 0f)
                    _pool[i].enabled = false;
            }
        }

        private void CreateMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null) shader = Shader.Find("Standard");

            _trailMaterial = new Material(shader);
            _trailMaterial.color = Color.white;
            _trailMaterial.SetFloat("_Surface", 1f); // Transparent
        }

        private void InitPool()
        {
            _pool = new LineRenderer[_poolSize];
            _timers = new float[_poolSize];

            for (int i = 0; i < _poolSize; i++)
            {
                GameObject obj = new GameObject($"BulletTrail_{i}");
                obj.transform.SetParent(transform);

                LineRenderer lr = obj.AddComponent<LineRenderer>();
                lr.positionCount = 2;
                lr.material = _trailMaterial;
                lr.startWidth = _trailWidth;
                lr.endWidth = _trailWidth * 0.3f;
                lr.startColor = _trailStartColor;
                lr.endColor = _trailEndColor;
                lr.useWorldSpace = true;
                lr.numCornerVertices = 0;
                lr.numCapVertices = 0;
                lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                lr.receiveShadows = false;
                lr.enabled = false;

                _pool[i] = lr;
                _timers[i] = 0f;
            }
        }
    }
}
