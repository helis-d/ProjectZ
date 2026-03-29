using UnityEngine;
using FishNet.Object;

namespace ProjectZ.Player
{
    /// <summary>
    /// FPS Weapon View — Silahı kameranın önüne koyup prosedürel animasyonlar ekler.
    /// Tutma sway, recoil (geri tepme), idle bob ve reload animasyonu içerir.
    /// Player prefab'ına ekle.
    /// </summary>
    public class FPSWeaponHolder : NetworkBehaviour
    {
        [Header("Weapon Model")]
        [Tooltip("Silah modeli prefab'ı (SK_Gun gibi). Boş bırakırsan kod çalışmaz.")]
        [SerializeField] private GameObject _weaponPrefab;

        [Header("Position — Silahın Ekrandaki Konumu")]
        [SerializeField] private Vector3 _weaponOffset = new Vector3(0.18f, -0.25f, 0.4f);
        [SerializeField] private Vector3 _weaponRotation = new Vector3(0f, -5f, 0f);
        [SerializeField] private float _weaponScale = 0.4f;

        [Header("Idle Sway — Duruyorken Sallanma")]
        [SerializeField] private float _swayAmount = 0.02f;
        [SerializeField] private float _swaySpeed = 1.5f;
        [SerializeField] private float _swayMaxAmount = 0.05f;

        [Header("Movement Bob — Yürürken")]
        [SerializeField] private float _moveBobSpeed = 10f;
        [SerializeField] private float _moveBobAmount = 0.01f;

        [Header("Recoil — Geri Tepme")]
        [SerializeField] private float _recoilKickBack = 0.08f;
        [SerializeField] private float _recoilKickUp = 0.04f;
        [SerializeField] private float _recoilSnapSpeed = 20f;
        [SerializeField] private float _recoilReturnSpeed = 8f;

        [Header("ADS — Nişan Alma")]
        [SerializeField] private Vector3 _adsOffset = new Vector3(0f, -0.15f, 0.1f);
        [SerializeField] private float _adsFOV = 45f;
        [SerializeField] private float _adsSpeed = 10f;

        [Header("Reload — Şarjör Değiştirme")]
        [SerializeField] private float _reloadDropAmount = 0.3f;
        [SerializeField] private float _reloadTiltAngle = 30f;
        [SerializeField] private float _reloadSpeed = 4f;

        // ─── Internals ───
        private Transform _weaponTransform;
        private PlayerInputHandler _input;
        private Camera _mainCam;

        // Sway
        private Vector3 _currentSway;

        // Bob
        private float _bobTimer;

        // Recoil
        private Vector3 _recoilOffset;
        private Vector3 _recoilTarget;

        // Reload
        private bool _isReloading;
        private float _reloadProgress;

        // ADS
        private bool _isAiming;
        private float _adsLerp; // 0 = hip, 1 = ADS
        public bool IsAiming => _isAiming;

        // Base pose
        private Vector3 _basePosition;
        private Quaternion _baseRotation;

        // Sprint FOV iletişimi
        private VFX.SprintFOVEffect _fovEffect;

        public override void OnStartClient()
        {
            base.OnStartClient();

            if (!IsOwner)
            {
                enabled = false;
                return;
            }

            _input = GetComponent<PlayerInputHandler>();
            _mainCam = Camera.main;
            _fovEffect = GetComponent<VFX.SprintFOVEffect>();

            SpawnWeaponModel();
        }

        private void SpawnWeaponModel()
        {
            if (_mainCam == null) return;

            // Silah prefab'ı varsa onu klonla, yoksa basit bir küp oluştur (test için)
            GameObject weaponObj;
            if (_weaponPrefab != null)
            {
                weaponObj = Instantiate(_weaponPrefab, _mainCam.transform);
            }
            else
            {
                // Test amaçlı basit bir silah şekli
                weaponObj = CreatePlaceholderGun();
                weaponObj.transform.SetParent(_mainCam.transform);
            }

            _weaponTransform = weaponObj.transform;
            _weaponTransform.localPosition = _weaponOffset;
            _weaponTransform.localRotation = Quaternion.Euler(_weaponRotation);
            _weaponTransform.localScale = Vector3.one * _weaponScale;

            _basePosition = _weaponOffset;
            _baseRotation = Quaternion.Euler(_weaponRotation);

            // Silaha otomatik olarak FPS ellerini ekle
            if (weaponObj.GetComponent<FPSArms>() == null)
            {
                weaponObj.AddComponent<FPSArms>();
            }
        }

        private GameObject CreatePlaceholderGun()
        {
            // Ana gövde (uzun dikdörtgen)
            GameObject gun = new GameObject("FPS_Weapon_Placeholder");

            // Gövde
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.transform.SetParent(gun.transform);
            body.transform.localPosition = new Vector3(0f, 0f, 0.15f);
            body.transform.localScale = new Vector3(0.05f, 0.06f, 0.35f);
            Destroy(body.GetComponent<Collider>());

            // Namlu
            GameObject barrel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            barrel.transform.SetParent(gun.transform);
            barrel.transform.localPosition = new Vector3(0f, 0.01f, 0.4f);
            barrel.transform.localScale = new Vector3(0.03f, 0.03f, 0.2f);
            Destroy(barrel.GetComponent<Collider>());

            // Kabza
            GameObject grip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            grip.transform.SetParent(gun.transform);
            grip.transform.localPosition = new Vector3(0f, -0.06f, 0.05f);
            grip.transform.localScale = new Vector3(0.04f, 0.12f, 0.05f);
            Destroy(grip.GetComponent<Collider>());

            // Koyu gri malzeme
            Material gunMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            if (gunMat != null)
            {
                gunMat.color = new Color(0.15f, 0.15f, 0.17f);
                foreach (var r in gun.GetComponentsInChildren<Renderer>())
                    r.material = gunMat;
            }

            return gun;
        }

        private void LateUpdate()
        {
            if (!IsOwner || _weaponTransform == null || _input == null) return;

            Vector3 finalPos = _basePosition;
            Quaternion finalRot = _baseRotation;

            // 1) Idle Sway — fareyi hareket ettirince silah hafifçe takip eder
            ApplyMouseSway(ref finalPos);

            // 2) Movement Bob — yürürken silah sallanır
            ApplyMovementBob(ref finalPos);

            // 3) Recoil — ateş edince geri tepme
            UpdateRecoil();
            finalPos += _recoilOffset;

            // 4) Reload — şarjör değiştirme animasyonu
            if (_isReloading)
            {
                ApplyReloadAnimation(ref finalPos, ref finalRot);
            }

            // 5) ADS — nişan alma pozisyonu
            _adsLerp = Mathf.Lerp(_adsLerp, _isAiming ? 1f : 0f, Time.deltaTime * _adsSpeed);
            if (_adsLerp > 0.01f)
            {
                finalPos = Vector3.Lerp(finalPos, _adsOffset, _adsLerp);
            }

            // Sonuçları uygula
            _weaponTransform.localPosition = Vector3.Lerp(_weaponTransform.localPosition, finalPos, Time.deltaTime * 15f);
            _weaponTransform.localRotation = Quaternion.Slerp(_weaponTransform.localRotation, finalRot, Time.deltaTime * 15f);
        }

        private void ApplyMouseSway(ref Vector3 pos)
        {
            Vector2 look = _input.LookInput;

            float swayX = Mathf.Clamp(-look.x * _swayAmount * 0.1f, -_swayMaxAmount, _swayMaxAmount);
            float swayY = Mathf.Clamp(-look.y * _swayAmount * 0.1f, -_swayMaxAmount, _swayMaxAmount);

            _currentSway = Vector3.Lerp(_currentSway, new Vector3(swayX, swayY, 0f), Time.deltaTime * _swaySpeed * 10f);
            pos += _currentSway;
        }

        private void ApplyMovementBob(ref Vector3 pos)
        {
            float speed = _input.MoveInput.magnitude;

            if (speed > 0.1f)
            {
                _bobTimer += Time.deltaTime * _moveBobSpeed;
                float bobX = Mathf.Cos(_bobTimer) * _moveBobAmount * 0.5f;
                float bobY = Mathf.Sin(_bobTimer * 2f) * _moveBobAmount;
                pos += new Vector3(bobX, bobY, 0f);
            }
            else
            {
                _bobTimer = 0f;
            }
        }

        private void UpdateRecoil()
        {
            // Geri tepmeyi hedefe doğru it
            _recoilOffset = Vector3.Lerp(_recoilOffset, _recoilTarget, Time.deltaTime * _recoilSnapSpeed);

            // Sonra yavaşça başlangıca geri dön
            _recoilTarget = Vector3.Lerp(_recoilTarget, Vector3.zero, Time.deltaTime * _recoilReturnSpeed);
        }

        // ─── Public API ───

        /// <summary>
        /// Ateş ettiğinde bu metodu çağır — silah geri teper.
        /// </summary>
        public void TriggerRecoil()
        {
            _recoilTarget += new Vector3(0f, _recoilKickUp, -_recoilKickBack);
        }

        /// <summary>
        /// Reload başlat — silah aşağı iner ve geri gelir.
        /// </summary>
        public void TriggerReload(float duration)
        {
            if (!_isReloading)
            {
                _isReloading = true;
                _reloadProgress = 0f;
                Invoke(nameof(FinishReload), duration);
            }
        }

        private void FinishReload()
        {
            _isReloading = false;
            _reloadProgress = 0f;
        }

        private void ApplyReloadAnimation(ref Vector3 pos, ref Quaternion rot)
        {
            _reloadProgress += Time.deltaTime * _reloadSpeed;
            // 0→1→0 eğrisi (aşağı inip geri çık)
            float curve = Mathf.Sin(Mathf.Clamp01(_reloadProgress) * Mathf.PI);
            pos.y -= curve * _reloadDropAmount;
            rot = Quaternion.Euler(
                _weaponRotation.x + curve * _reloadTiltAngle,
                _weaponRotation.y,
                _weaponRotation.z
            );
        }

        // ─── Input Based Triggers ───
        private void Update()
        {
            if (!IsOwner || _input == null) return;

            // Sağ tık ADS (nişan alma)
            bool rightClickHeld = UnityEngine.InputSystem.Mouse.current != null &&
                                  UnityEngine.InputSystem.Mouse.current.rightButton.isPressed;
            _isAiming = rightClickHeld && !_isReloading;

            // FOV yönetimi
            if (_fovEffect != null)
            {
                if (_isAiming)
                    _fovEffect.SetADSFOV(_adsFOV);
                else if (!_input.IsSprinting)
                    _fovEffect.ResetFOV();
            }

            // Sol tık ateş — recoil tetikle
            if (_input.FirePressed || _input.FireHeld)
            {
                TriggerRecoil();
            }

            // R tuşu reload
            if (_input.ReloadPressed && !_isReloading)
            {
                TriggerReload(2f); // 2 saniye reload
            }
        }
    }
}
