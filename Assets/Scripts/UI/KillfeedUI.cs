using System.Collections;
using TMPro;
using UnityEngine;
using ProjectZ.Core;

namespace ProjectZ.UI
{
    /// <summary>
    /// Displays kill events in the top-right corner.
    /// Listens to global GameEvents for detailed kill information.
    /// </summary>
    public class KillfeedUI : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private GameObject _killfeedItemPrefab;
        [SerializeField] private Transform _killfeedContainer;
        [SerializeField] private float _displayDuration = 4.0f;

        private void OnEnable()
        {
            GameEvents.OnKillDetails += HandleKillDetails;

            // Fallback: also listen to basic death for non-weapon kills (environment, etc.)
            GameEvents.OnPlayerDeath += HandlePlayerDeath;
        }

        private void OnDisable()
        {
            GameEvents.OnKillDetails -= HandleKillDetails;
            GameEvents.OnPlayerDeath -= HandlePlayerDeath;
        }

        private void HandleKillDetails(int killerId, int victimId, string weaponId, bool headshot, bool wallbang)
        {
            string killerName = $"Player {killerId}";
            string victimName = $"Player {victimId}";

            // Build detailed kill message
            string weaponTag = string.IsNullOrEmpty(weaponId) ? "" : $"[{weaponId}]";
            string hsTag = headshot ? " \ud83d\udc80" : "";
            string wbTag = wallbang ? " \ud83e\uddf1" : "";

            string message = $"{killerName} {weaponTag}{hsTag}{wbTag} {victimName}";
            SpawnKillfeedItem(message, headshot);
        }

        private void HandlePlayerDeath(int victimId, int killerId)
        {
            // This handles non-weapon kills (environment, fall damage, etc.)
            // OnKillDetails already covers weapon kills, so skip if killerId is valid
            // to avoid duplicate entries. Environment kills use killerId == -1.
            if (killerId >= 0) return;

            string killerName = "Environment";
            string victimName = $"Player {victimId}";
            string message = $"{killerName} ⚔ {victimName}";
            SpawnKillfeedItem(message, false);
        }

        private void SpawnKillfeedItem(string message, bool isHeadshot, bool isOwnAction = false)
        {
            if (_killfeedItemPrefab == null || _killfeedContainer == null) return;

            GameObject item = Instantiate(_killfeedItemPrefab, _killfeedContainer);
            var text = item.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                text.text = message;
                if (isHeadshot)
                    text.color = new Color(1f, 0.3f, 0.3f); // Red tint for headshots
            }

            // GDD: Your Actions have glowing background
            if (isOwnAction)
            {
                var bg = item.GetComponent<UnityEngine.UI.Image>();
                if (bg != null)
                {
                    bg.color = new Color(1f, 0.85f, 0.2f, 0.25f); // Soft gold glow
                }
            }

            StartCoroutine(DestroyItemAfterTime(item));
        }

        private IEnumerator DestroyItemAfterTime(GameObject item)
        {
            yield return new WaitForSeconds(_displayDuration);
            if (item != null) Destroy(item);
        }
    }
}
