using UnityEngine;
using ProjectZ.Player;

namespace ProjectZ.UI
{
    /// <summary>
    /// Dynamic Crosshair implementation (GDD Section 10).
    /// Gap expands based on player movement velocity and weapon firing recoil.
    /// CurrentGap = BaseGap + (PlayerVelocity * MoveError) + (Recoil * FireError)
    /// </summary>
    public class CrosshairUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private RectTransform _topLine;
        [SerializeField] private RectTransform _bottomLine;
        [SerializeField] private RectTransform _leftLine;
        [SerializeField] private RectTransform _rightLine;

        [Header("Settings")]
        [SerializeField] private float _baseGap = 10f;
        [SerializeField] private float _moveErrorMultiplier = 2f;
        [SerializeField] private float _fireErrorMultiplier = 15f;
        [SerializeField] private float _recoverySpeed = 10f;

        // ─── Local State ──────────────────────────────────────────────────
        private CharacterController _localCC;
        private float _currentRecoil = 0f;

        private void Awake()
        {
            TryResolveLines();
        }

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

        private void Update()
        {
            TryResolveLines();

            // Recover recoil over time
            if (_currentRecoil > 0f)
            {
                _currentRecoil = Mathf.Lerp(_currentRecoil, 0f, Time.deltaTime * _recoverySpeed);
            }

            float velocityMag = 0f;
            if (_localCC != null)
            {
                // Only care about horizontal velocity for crosshair bloom typically
                Vector3 vel = _localCC.velocity;
                vel.y = 0;
                velocityMag = vel.magnitude;
            }

            // GDD Formula: CurrentGap = BaseGap + (PlayerVelocity * MoveError) + (Recoil * FireError)
            float currentGap = _baseGap 
                             + (velocityMag * _moveErrorMultiplier) 
                             + (_currentRecoil * _fireErrorMultiplier);

            ApplyGap(currentGap);
        }

        private void ApplyGap(float gap)
        {
            if (_topLine != null)    _topLine.anchoredPosition    = new Vector2(0, gap);
            if (_bottomLine != null) _bottomLine.anchoredPosition = new Vector2(0, -gap);
            if (_leftLine != null)   _leftLine.anchoredPosition   = new Vector2(-gap, 0);
            if (_rightLine != null)  _rightLine.anchoredPosition  = new Vector2(gap, 0);
        }

        private void TryResolveLines()
        {
            if (_topLine == null) _topLine = transform.Find("TopLine") as RectTransform;
            if (_bottomLine == null) _bottomLine = transform.Find("BottomLine") as RectTransform;
            if (_leftLine == null) _leftLine = transform.Find("LeftLine") as RectTransform;
            if (_rightLine == null) _rightLine = transform.Find("RightLine") as RectTransform;
        }
    }
}
