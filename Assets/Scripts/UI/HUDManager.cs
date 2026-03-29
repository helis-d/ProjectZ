using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ProjectZ.Player;
using ProjectZ.Weapon;
using ProjectZ.Economy;

namespace ProjectZ.UI
{
    /// <summary>
    /// Main HUD Manager handling Health, Armor, Ammo, Mastery, and Ultimate displays.
    /// Implements GDD Section 10 UI requirements.
    /// </summary>
    public class HUDManager : MonoBehaviour
    {
        public static HUDManager Instance { get; private set; }

        [Header("Health & Armor")]
        [SerializeField] private Slider _healthBar;
        [SerializeField] private Slider _armorBar;
        [SerializeField] private TextMeshProUGUI _healthText;
        [SerializeField] private TextMeshProUGUI _armorText;

        [Header("Weapon & Mastery")]
        [SerializeField] private TextMeshProUGUI _ammoText;
        [SerializeField] private TextMeshProUGUI _masteryLevelText;
        [SerializeField] private Slider _masteryXpBar;
        [SerializeField] private Image _masteryIcon; // Roman numerals icon

        [Header("XP Popup")]
        [SerializeField] private TextMeshProUGUI _xpPopupText;
        [SerializeField] private CanvasGroup _xpPopupGroup;

        [Header("Buff Icons")]
        [Tooltip("Active buff icon slots (up to 5)")]
        [SerializeField] private Image[] _buffIcons;
        [SerializeField] private Sprite _adsBuffSprite;
        [SerializeField] private Sprite _reloadBuffSprite;
        [SerializeField] private Sprite _moveBuffSprite;
        [SerializeField] private Sprite _fireRateBuffSprite;
        [SerializeField] private Sprite _drawBuffSprite;

        [Header("Ultimate")]
        [SerializeField] private Image _ultimateProgressImage; // Circular fill 0-1
        [SerializeField] private Color _ultimateReadyColor = Color.yellow;
        [SerializeField] private Color _ultimateChargingColor = Color.gray;

        private float _xpPopupTimer;

        // ─── Local Player References ──────────────────────────────────────
        private PlayerHealth _localHealth;
        private WeaponManager _localWeaponManager;
        private WeaponMasteryManager _localMastery;
        private PlayerHeroController _localHero;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Update()
        {
            // Usually, these refs are set once when the local player spawns.
            // Putting a quick null check here for robustness if player dies/respawns.
            if (_localHealth == null) return;

            UpdateHealth();
            UpdateWeapon();
            UpdateUltimate();
        }

        public void BindLocalPlayer(GameObject playerRoot)
        {
            _localHealth = playerRoot.GetComponent<PlayerHealth>();
            _localMastery = playerRoot.GetComponent<WeaponMasteryManager>();
            _localHero = playerRoot.GetComponent<PlayerHeroController>();
            // WeaponManager PlayerRoot üzerindedir
            _localWeaponManager = playerRoot.GetComponent<WeaponManager>();
        }

        private void UpdateHealth()
        {
            float hp = _localHealth.CurrentHealth.Value;
            float armor = _localHealth.CurrentArmor.Value;

            // Assuming max values are 100 and 50 respectively for the slider logic
            if (_healthBar != null) _healthBar.value = hp / 100f;
            if (_armorBar != null) _armorBar.value = armor / 50f;

            if (_healthText != null) _healthText.text = hp.ToString("0");
            if (_armorText != null) _armorText.text = armor.ToString("0");
        }

        private void UpdateWeapon()
        {
            if (_localWeaponManager == null) return;
            var activeWeapon = _localWeaponManager.GetActiveWeapon();

            if (activeWeapon == null || activeWeapon.data == null)
            {
                if (_ammoText != null) _ammoText.text = "- / -";
                return;
            }

            // Ammo
            string ammoString = $"{activeWeapon.CurrentAmmo} / {activeWeapon.data.maxReserveAmmo}";
            if (_ammoText != null)
            {
                _ammoText.text = ammoString;
                _ammoText.color = activeWeapon.CurrentAmmo <= (activeWeapon.data.magazineSize * 0.2f) 
                                    ? Color.red : Color.white; // GDD: Turns red below 20%
            }

            // Mastery
            if (_localMastery != null && activeWeapon != null && activeWeapon.data != null)
            {
                string weaponId = activeWeapon.data.weaponId;
                int level = _localMastery.GetLevel(weaponId);
                int totalXp = _localMastery.GetXP(weaponId);
                
                int currentLevelBaseXp = (level - 1) * 1000;
                int xpProgress = totalXp - currentLevelBaseXp;
                
                if (_masteryLevelText != null)
                {
                    _masteryLevelText.text = $"LVL {MasteryLevelColors.GetRomanNumeral(level)}";
                    _masteryLevelText.color = MasteryLevelColors.GetColor(level);
                }
                if (_masteryXpBar != null) _masteryXpBar.value = Mathf.Clamp01(xpProgress / 1000f);

                UpdateBuffIcons(level);
            }

            // XP Popup fade
            if (_xpPopupTimer > 0f)
            {
                _xpPopupTimer -= Time.deltaTime;
                if (_xpPopupGroup != null)
                    _xpPopupGroup.alpha = Mathf.Clamp01(_xpPopupTimer / 0.5f);
            }
        }

        private void UpdateUltimate()
        {
            if (_localHero == null) return;

            float charge = _localHero.UltimateCharge.Value / 100f;
            if (_ultimateProgressImage != null)
            {
                _ultimateProgressImage.fillAmount = charge;

                if (charge >= 1f)
                {
                    // GDD: 100% → glowing, pulsing colored icon
                    float pulse = (Mathf.Sin(Time.time * 3f) + 1f) * 0.5f;
                    _ultimateProgressImage.color = Color.Lerp(_ultimateReadyColor, Color.white, pulse * 0.3f);
                }
                else
                {
                    _ultimateProgressImage.color = _ultimateChargingColor;
                }
            }
        }

        /// <summary>Show XP gain popup (GDD Section 3: flashes "+100 XP").</summary>
        public void ShowXPGain(int amount)
        {
            if (_xpPopupText != null)
                _xpPopupText.text = $"+{amount} XP";
            if (_xpPopupGroup != null)
                _xpPopupGroup.alpha = 1f;
            _xpPopupTimer = 1.5f;
        }

        private void UpdateBuffIcons(int level)
        {
            if (_buffIcons == null) return;

            // Show buff icons for active level (cumulative)
            Sprite[] activeBuffs = level switch
            {
                5 => new[] { _adsBuffSprite, _reloadBuffSprite, _moveBuffSprite, _fireRateBuffSprite, _drawBuffSprite },
                4 => new[] { _adsBuffSprite, _reloadBuffSprite, _moveBuffSprite, _fireRateBuffSprite },
                3 => new[] { _adsBuffSprite, _reloadBuffSprite, _moveBuffSprite },
                2 => new[] { _adsBuffSprite, _reloadBuffSprite },
                _ => new Sprite[0]
            };

            for (int i = 0; i < _buffIcons.Length; i++)
            {
                if (i < activeBuffs.Length && activeBuffs[i] != null)
                {
                    _buffIcons[i].sprite = activeBuffs[i];
                    _buffIcons[i].enabled = true;
                }
                else
                {
                    _buffIcons[i].enabled = false;
                }
            }
        }
        
        // ─── Ultimate Effects ────────────────────────────────────────────
        
        /// <summary>
        /// Hacks the HUD, turning UI elements red/glitchy or invisible for the duration.
        /// Caused by Volt's Ultimate.
        /// </summary>
        public void ShowSystemFailure(float duration)
        {
            StartCoroutine(SystemFailureRoutine(duration));
        }

        private System.Collections.IEnumerator SystemFailureRoutine(float duration)
        {
            // Simple Prototype Hack: Turn all text / bars invisible/red
            if (_healthText != null) _healthText.text = "ERR";
            if (_ammoText != null) _ammoText.text = "ERR";
            if (_armorText != null) _armorText.text = "ERR";
            
            // Or ideally disable a parent CanvasGroup, but assuming individual components for now
            if (_healthBar != null) _healthBar.gameObject.SetActive(false);
            if (_masteryXpBar != null) _masteryXpBar.gameObject.SetActive(false);

            // Show a "SYSTEM HACKED" overlay if exists (e.g., _hackedOverlayPanel.SetActive(true))

            Debug.Log("[HUD] SYSTEM FAILURE ENCOUNTERED! HUD Disabled.");

            yield return new WaitForSeconds(duration);

            if (_healthBar != null) _healthBar.gameObject.SetActive(true);
            if (_masteryXpBar != null) _masteryXpBar.gameObject.SetActive(true);

            Debug.Log("[HUD] System restored.");
        }
    }
}

