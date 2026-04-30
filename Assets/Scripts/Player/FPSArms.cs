using UnityEngine;

namespace ProjectZ.Player
{
    /// <summary>
    /// Prosedürel FPS elleri — Silahı tutan iki el ve kolları oluşturur.
    /// FPSWeaponHolder tarafından spawn edilen silah objesine otomatik eklenir.
    /// Valorant tarzı: Silahı kavramış iki el görünür.
    /// </summary>
    public class FPSArms : MonoBehaviour
    {
        [Header("Arm Settings")]
        [SerializeField] private Color _skinColor = new Color(0.85f, 0.7f, 0.55f);
        [SerializeField] private Color _sleeveColor = new Color(0.15f, 0.2f, 0.25f);
        [Tooltip("Oyun çalışırken pozisyonları ayarlayabilmek için tikli bırak")]
        [SerializeField] private bool _autoUpdateInPlayMode = true;
        [SerializeField] private Material _skinMaterialTemplate;
        [SerializeField] private Material _sleeveMaterialTemplate;

        [Header("Right Hand — Tetik Eli")]
        [SerializeField] private Vector3 _rightHandPos = new Vector3(-0.06f, -0.65f, -0.4f);
        [SerializeField] private Vector3 _rightHandRot = new Vector3(0f, 0f, 0f);

        [Header("Left Hand — Destek Eli")]
        [SerializeField] private Vector3 _leftHandPos = new Vector3(-0.35f, -0.2f, 0.3f);
        [SerializeField] private Vector3 _leftHandRot = new Vector3(0f, 45f, 45f);

        private Material _skinMat;
        private Material _sleeveMat;
        private Transform _rightArmRoot;
        private Transform _leftArmRoot;
        private bool _initialized;

        public void ConfigureRuntimeMaterials(Material skinMaterial, Material sleeveMaterial)
        {
            if (skinMaterial != null)
                _skinMaterialTemplate = skinMaterial;

            if (sleeveMaterial != null)
                _sleeveMaterialTemplate = sleeveMaterial;
        }

        private void Start()
        {
            InitializeIfNeeded();
        }

        public void InitializeIfNeeded()
        {
            if (_initialized)
                return;

            _initialized = true;
            CreateMaterials();
            CreateRightArm();
            CreateLeftArm();
        }

        private void CreateMaterials()
        {
            _skinMat = CreateRuntimeMaterial(_skinMaterialTemplate, _skinColor, "skin");
            _sleeveMat = CreateRuntimeMaterial(_sleeveMaterialTemplate, _sleeveColor, "sleeve");
        }

        // ─── SAĞ EL (Tetik eli) ─────────────────────────────────────────
        private void CreateRightArm()
        {
            GameObject rightArm = new GameObject("RightArm");
            rightArm.transform.SetParent(transform);
            rightArm.transform.localPosition = _rightHandPos;
            rightArm.transform.localRotation = Quaternion.Euler(_rightHandRot);
            _rightArmRoot = rightArm.transform;

            // El (avuç)
            CreatePart(rightArm.transform, "Hand", 
                new Vector3(0f, 0f, 0f), 
                new Vector3(80f, 0f, 0f),
                new Vector3(0.04f, 0.06f, 0.08f), _skinMat);

            // Parmaklar (silahı kavrayan)
            CreatePart(rightArm.transform, "Fingers",
                new Vector3(0f, -0.02f, 0.04f),
                new Vector3(40f, 0f, 0f),
                new Vector3(0.035f, 0.04f, 0.05f), _skinMat);

            // Tetik parmağı
            CreatePart(rightArm.transform, "TriggerFinger",
                new Vector3(0.015f, -0.01f, 0.05f),
                new Vector3(60f, 10f, 0f),
                new Vector3(0.012f, 0.012f, 0.04f), _skinMat);

            // Bilek
            CreatePart(rightArm.transform, "Wrist",
                new Vector3(0f, 0.01f, -0.06f),
                new Vector3(0f, 0f, 0f),
                new Vector3(0.045f, 0.04f, 0.06f), _skinMat);

            // Kol (sleeve/zırh)
            CreatePart(rightArm.transform, "Forearm",
                new Vector3(0.02f, 0.02f, -0.18f),
                new Vector3(-10f, 15f, 0f),
                new Vector3(0.055f, 0.05f, 0.2f), _sleeveMat);
        }

        // ─── SOL EL (Destek eli) ────────────────────────────────────────
        private void CreateLeftArm()
        {
            GameObject leftArm = new GameObject("LeftArm");
            leftArm.transform.SetParent(transform);
            leftArm.transform.localPosition = _leftHandPos;
            leftArm.transform.localRotation = Quaternion.Euler(_leftHandRot);
            _leftArmRoot = leftArm.transform;

            // El (avuç)
            CreatePart(leftArm.transform, "Hand",
                new Vector3(0f, 0f, 0f),
                new Vector3(70f, 0f, 0f),
                new Vector3(0.04f, 0.06f, 0.08f), _skinMat);

            // Kavrama parmakları (silahın altını saran)
            CreatePart(leftArm.transform, "Fingers",
                new Vector3(0f, -0.025f, 0.03f),
                new Vector3(50f, 0f, 0f),
                new Vector3(0.04f, 0.04f, 0.06f), _skinMat);

            // Başparmak (silahın yan tarafında)
            CreatePart(leftArm.transform, "Thumb",
                new Vector3(0.03f, 0f, 0.02f),
                new Vector3(30f, 30f, 0f),
                new Vector3(0.015f, 0.015f, 0.04f), _skinMat);

            // Bilek
            CreatePart(leftArm.transform, "Wrist",
                new Vector3(0f, 0.01f, -0.06f),
                new Vector3(0f, 0f, 0f),
                new Vector3(0.045f, 0.04f, 0.06f), _skinMat);

            // Sol kol (sleeve/zırh)
            CreatePart(leftArm.transform, "Forearm",
                new Vector3(-0.04f, 0.03f, -0.2f),
                new Vector3(-5f, -20f, 5f),
                new Vector3(0.055f, 0.05f, 0.22f), _sleeveMat);
        }

        private void Update()
        {
            if (_autoUpdateInPlayMode)
            {
                if (_rightArmRoot != null)
                {
                    _rightArmRoot.localPosition = _rightHandPos;
                    _rightArmRoot.localRotation = Quaternion.Euler(_rightHandRot);
                }
                if (_leftArmRoot != null)
                {
                    _leftArmRoot.localPosition = _leftHandPos;
                    _leftArmRoot.localRotation = Quaternion.Euler(_leftHandRot);
                }
            }
        }

        // ─── Yardımcı ───────────────────────────────────────────────────
        private void CreatePart(Transform parent, string name,
            Vector3 localPos, Vector3 localRot, Vector3 scale, Material mat)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = name;
            part.transform.SetParent(parent);
            part.transform.localPosition = localPos;
            part.transform.localRotation = Quaternion.Euler(localRot);
            part.transform.localScale = scale;

            // Collider'ı kaldır (fizik istemiyoruz)
            Collider col = part.GetComponent<Collider>();
            if (col != null) Destroy(col);

            // Malzeme ata
            Renderer rend = part.GetComponent<Renderer>();
            if (rend != null && mat != null)
                rend.material = mat;
        }

        private static Material CreateRuntimeMaterial(Material template, Color fallbackColor, string label)
        {
            if (template != null)
                return new Material(template);

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");

            if (shader == null)
            {
                Debug.LogWarning($"[FPSArms] No runtime shader available for {label} material generation.");
                return null;
            }

            Material material = new Material(shader);
            material.color = fallbackColor;
            return material;
        }

        private void OnDestroy()
        {
            if (_skinMat != null)
                Destroy(_skinMat);

            if (_sleeveMat != null)
                Destroy(_sleeveMat);
        }
    }
}
