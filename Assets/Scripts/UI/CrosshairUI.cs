using UnityEngine;
using ProjectZ.Player;
using ProjectZ.Settings;

namespace ProjectZ.UI
{
    /// <summary>
    /// Dynamic Crosshair implementation (GDD Section 10).
    /// Gap expands based on player movement velocity and weapon firing recoil.
    /// CurrentGap = BaseGap + (PlayerVelocity * MoveError) + (Recoil * FireError)
    ///
    /// Reads color and type from SettingsManager on Start and on every settings save.
    /// </summary>
    public class CrosshairUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private RectTransform _topLine;
        [SerializeField] private RectTransform _bottomLine;
        [SerializeField] private RectTransform _leftLine;
        [SerializeField] private RectTransform _rightLine;

        [Header("Bloom Settings")]
        [SerializeField] private float _baseGap             = 10f;
        [SerializeField] private float _moveErrorMultiplier = 2f;
        [SerializeField] private float _fireErrorMultiplier = 15f;
        [SerializeField] private float _recoverySpeed       = 10f;

        // ── Local State ───────────────────────────────────────────────────────
        private CharacterController _localCC;
        private float _currentRecoil;

        // Resolved from SettingsManager; used for tinting crosshair lines.
        private Color _crosshairColor = Color.green;

        // ── Unity Lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            TryResolveLines();
        }

        private void Start()
        {
            ApplySettingsNow();
        }

        private void OnEnable()
        {
            if (SettingsManager.Instance != null)
                SettingsManager.Instance.OnSettingsApplied += ApplySettingsNow;
        }

        private void OnDisable()
        {
            if (SettingsManager.Instance != null)
                SettingsManager.Instance.OnSettingsApplied -= ApplySettingsNow;
        }

        // ── Public API ────────────────────────────────────────────────────────

        public void BindLocalPlayer(GameObject playerRoot)
        {
            _localCC = playerRoot.GetComponent<CharacterController>();
        }

        /// <summary>
        /// Call this when the local weapon fires to add recoil to the crosshair.
        /// </summary>
        public void AddFireRecoil(float amount = 1f)
        {
            _currentRecoil += amount;
        }

        // ── Per-Frame ─────────────────────────────────────────────────────────

        private void Update()
        {
            TryResolveLines();

            // Recover recoil over time.
            if (_currentRecoil > 0f)
                _currentRecoil = Mathf.Lerp(_currentRecoil, 0f, Time.deltaTime * _recoverySpeed);

            float velocityMag = 0f;
            if (_localCC != null)
            {
                Vector3 vel = _localCC.velocity;
                vel.y      = 0f;
                velocityMag = vel.magnitude;
            }

            // GDD Formula: CurrentGap = BaseGap + (PlayerVelocity × MoveError) + (Recoil × FireError)
            float currentGap = _baseGap
                             + (velocityMag    * _moveErrorMultiplier)
                             + (_currentRecoil * _fireErrorMultiplier);

            ApplyGap(currentGap);
        }

        // ── Settings Integration ──────────────────────────────────────────────

        /// <summary>
        /// Reads crosshair color and type from SettingsManager and applies them.
        /// Called on Start and on every OnSettingsApplied event.
        /// </summary>
        private void ApplySettingsNow()
        {
            if (SettingsManager.Instance == null || SettingsManager.Instance.Current == null)
                return;

            var gameplay = SettingsManager.Instance.Current.gameplay;

            // Parse hex color from settings; fallback to green on failure.
            if (!ColorUtility.TryParseHtmlString(gameplay.crosshairColorHex, out _crosshairColor))
                _crosshairColor = Color.green;

            // crosshairType: 0 = Dot (hide lines), 1 = Cross, 2 = Dynamic
            bool showLines = gameplay.crosshairType != 0;
            if (_topLine    != null) _topLine.gameObject.SetActive(showLines);
            if (_bottomLine != null) _bottomLine.gameObject.SetActive(showLines);
            if (_leftLine   != null) _leftLine.gameObject.SetActive(showLines);
            if (_rightLine  != null) _rightLine.gameObject.SetActive(showLines);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void ApplyGap(float gap)
        {
            if (_topLine    != null) _topLine.anchoredPosition    = new Vector2(0f,   gap);
            if (_bottomLine != null) _bottomLine.anchoredPosition = new Vector2(0f,  -gap);
            if (_leftLine   != null) _leftLine.anchoredPosition   = new Vector2(-gap,  0f);
            if (_rightLine  != null) _rightLine.anchoredPosition  = new Vector2( gap,  0f);
        }

        private void TryResolveLines()
        {
            if (_topLine    == null) _topLine    = transform.Find("TopLine")    as RectTransform;
            if (_bottomLine == null) _bottomLine = transform.Find("BottomLine") as RectTransform;
            if (_leftLine   == null) _leftLine   = transform.Find("LeftLine")   as RectTransform;
            if (_rightLine  == null) _rightLine  = transform.Find("RightLine")  as RectTransform;
        }
    }
}
