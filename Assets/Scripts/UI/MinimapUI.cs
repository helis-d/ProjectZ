using System.Collections.Generic;
using ProjectZ.Core;
using ProjectZ.Map;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectZ.UI
{
    /// <summary>
    /// GDD Section 10: Vector-drawn minimap.
    /// MapX = (PlayerX – MinX) / (MaxX – MinX) × MinimapSize
    /// Features: vision cone, fog of war, fire ping (0.5s).
    /// </summary>
    public class MinimapUI : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private RectTransform _minimapRect;
        [SerializeField] private RectTransform _playerIcon;
        [SerializeField] private RectTransform _visionCone;
        [SerializeField] private GameObject _dotPrefab;

        [Header("Map Bounds")]
        [SerializeField] private Vector2 _mapMin = new(-3000f, -3000f);
        [SerializeField] private Vector2 _mapMax = new(3000f, 3000f);

        [Header("Settings")]
        [SerializeField] private float _firePingDuration = 0.5f;

        private Transform _localPlayer;
        private readonly Dictionary<int, RectTransform> _allyDots = new();
        private readonly Dictionary<int, float> _enemyPingTimers = new();
        private readonly Dictionary<int, RectTransform> _enemyDots = new();

        private void OnEnable()
        {
            GameEvents.OnKillDetails += HandleFireEvent;
        }

        private void OnDisable()
        {
            GameEvents.OnKillDetails -= HandleFireEvent;
        }

        /// <summary>Initialize with local player reference.</summary>
        public void Initialize(Transform localPlayer, Vector3 mapMin, Vector3 mapMax)
        {
            _localPlayer = localPlayer;
            _mapMin = new Vector2(mapMin.x, mapMin.z);
            _mapMax = new Vector2(mapMax.x, mapMax.z);
        }

        private void Update()
        {
            if (_localPlayer == null || _minimapRect == null) return;

            // Update local player icon
            UpdateIcon(_playerIcon, _localPlayer.position);

            // Update player rotation for vision cone
            if (_visionCone != null)
            {
                float yaw = _localPlayer.eulerAngles.y;
                _visionCone.localRotation = Quaternion.Euler(0f, 0f, -yaw);
            }

            // Decay enemy ping timers
            var expiredPings = new List<int>();
            foreach (var kvp in _enemyPingTimers)
            {
                _enemyPingTimers[kvp.Key] -= Time.deltaTime;
                if (_enemyPingTimers[kvp.Key] <= 0f)
                    expiredPings.Add(kvp.Key);
            }
            foreach (int id in expiredPings)
            {
                _enemyPingTimers.Remove(id);
                if (_enemyDots.TryGetValue(id, out var dot))
                    dot.gameObject.SetActive(false);
            }
        }

        /// <summary>Update an ally's position on the minimap.</summary>
        public void UpdateAlly(int id, Vector3 worldPos)
        {
            if (!_allyDots.TryGetValue(id, out var dot))
            {
                dot = CreateDot(Color.green);
                _allyDots[id] = dot;
            }
            UpdateIcon(dot, worldPos);
        }

        /// <summary>Show enemy on minimap for fire ping duration.</summary>
        public void PingEnemy(int enemyId, Vector3 worldPos)
        {
            if (!_enemyDots.TryGetValue(enemyId, out var dot))
            {
                dot = CreateDot(Color.red);
                _enemyDots[enemyId] = dot;
            }
            dot.gameObject.SetActive(true);
            UpdateIcon(dot, worldPos);
            _enemyPingTimers[enemyId] = _firePingDuration;
        }

        // ─── Internal ──────────────────────────────────────────────────────
        private void UpdateIcon(RectTransform icon, Vector3 worldPos)
        {
            if (icon == null) return;

            float minimapSize = _minimapRect.rect.width;
            float mapX = (worldPos.x - _mapMin.x) / (_mapMax.x - _mapMin.x) * minimapSize;
            float mapY = (worldPos.z - _mapMin.y) / (_mapMax.y - _mapMin.y) * minimapSize;

            // Center offset
            mapX -= minimapSize * 0.5f;
            mapY -= minimapSize * 0.5f;

            icon.anchoredPosition = new Vector2(mapX, mapY);
        }

        private RectTransform CreateDot(Color color)
        {
            if (_dotPrefab == null) return null;
            GameObject dot = Instantiate(_dotPrefab, _minimapRect);
            var img = dot.GetComponent<Image>();
            if (img != null) img.color = color;
            return dot.GetComponent<RectTransform>();
        }

        private void HandleFireEvent(int killerId, int victimId, string weaponId, bool headshot, bool wallbang)
        {
            // Firing enemy visible on minimap for 0.5s — handled by game logic calling PingEnemy
        }

        public void RemovePlayer(int id)
        {
            if (_allyDots.TryGetValue(id, out var a)) { Destroy(a.gameObject); _allyDots.Remove(id); }
            if (_enemyDots.TryGetValue(id, out var e)) { Destroy(e.gameObject); _enemyDots.Remove(id); }
            _enemyPingTimers.Remove(id);
        }
    }
}
