using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ProjectZ.Core;
using ProjectZ.GameMode;
using ProjectZ.Player;
using ProjectZ.Weapon;

namespace ProjectZ.UI
{
    /// <summary>
    /// Main HUD Manager handling health, armor, ammo, mastery, ultimate, and round flow displays.
    /// Implements GDD Section 3 and Section 10 visibility rules.
    ///
    /// Fix log:
    ///  - Removed FindFirstObjectByType calls from Update() — managers now cached in BindLocalPlayer.
    ///  - Bomb timer panel driven by GameEvents (OnSpherePlanted / OnSphereTimerTick / Defused / Detonated).
    ///  - Added OnEnable/OnDisable subscription pattern for all sphere events.
    /// </summary>
    public class HUDManager : MonoBehaviour
    {
        public static HUDManager Instance { get; private set; }

        // ── Health & Armor ────────────────────────────────────────────────────
        [Header("Health & Armor")]
        [SerializeField] private Slider          _healthBar;
        [SerializeField] private Slider          _armorBar;
        [SerializeField] private TextMeshProUGUI _healthText;
        [SerializeField] private TextMeshProUGUI _armorText;

        // ── Weapon & Mastery ──────────────────────────────────────────────────
        [Header("Weapon & Mastery")]
        [SerializeField] private TextMeshProUGUI _ammoText;
        [SerializeField] private TextMeshProUGUI _masteryLevelText;
        [SerializeField] private Slider          _masteryXpBar;
        [SerializeField] private Image           _masteryIcon;

        // ── Round Flow ────────────────────────────────────────────────────────
        [Header("Round Flow")]
        [SerializeField] private TextMeshProUGUI _modeText;
        [SerializeField] private TextMeshProUGUI _phaseText;
        [SerializeField] private TextMeshProUGUI _roundText;
        [SerializeField] private TextMeshProUGUI _scoreText;
        [SerializeField] private TextMeshProUGUI _heroIdentityText;
        [SerializeField] private TextMeshProUGUI _ultimateStatusText;
        [SerializeField] private GameObject      _pistolRoundBadge;
        [SerializeField] private GameObject      _overtimeBadge;

        // ── XP Popup ──────────────────────────────────────────────────────────
        [Header("XP Popup")]
        [SerializeField] private TextMeshProUGUI _xpPopupText;
        [SerializeField] private CanvasGroup     _xpPopupGroup;

        // ── Buff Icons ────────────────────────────────────────────────────────
        [Header("Buff Icons")]
        [SerializeField] private Image[] _buffIcons;
        [SerializeField] private Sprite  _adsBuffSprite;
        [SerializeField] private Sprite  _reloadBuffSprite;
        [SerializeField] private Sprite  _moveBuffSprite;
        [SerializeField] private Sprite  _fireRateBuffSprite;
        [SerializeField] private Sprite  _drawBuffSprite;

        // ── Sphere Bomb Timer (GDD §7) ────────────────────────────────────────
        [Header("Sphere Bomb Timer")]
        [SerializeField] private GameObject      _sphereTimerPanel;
        [SerializeField] private TextMeshProUGUI _sphereTimerText;
        [SerializeField] private Image           _sphereTimerFill;
        [SerializeField] private Color _sphereTimerSafeColor   = new Color(0.2f, 0.9f, 0.2f);
        [SerializeField] private Color _sphereTimerUrgentColor = new Color(0.95f, 0.2f, 0.1f);

        // ── Ultimate ──────────────────────────────────────────────────────────
        [Header("Ultimate")]
        [SerializeField] private Image _ultimateProgressImage;
        [SerializeField] private Color _ultimateReadyColor    = Color.yellow;
        [SerializeField] private Color _ultimateChargingColor = Color.gray;
        [SerializeField] private Color _ultimateDisabledColor = new Color(0.45f, 0.45f, 0.45f, 1f);

        // ── Private State ─────────────────────────────────────────────────────
        private float _xpPopupTimer;
        private const float SphereDetonateMaxSeconds = 45f; // GDD §7

        private PlayerHealth          _localHealth;
        private WeaponManager         _localWeaponManager;
        private WeaponMasteryManager  _localMastery;
        private PlayerHeroController  _localHero;
        private RoundManager          _roundManager;
        private RankedGameMode        _rankedMode;
        private FastFightMode         _fastFightMode;

        // ─────────────────────────────────────────────────────────────────────
        // Unity lifecycle
        // ─────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);

            AutoWireRuntimeHud();
        }

        private void OnEnable()
        {
            GameEvents.OnSphereTimerTick += HandleSphereTimerTick;
            GameEvents.OnSpherePlanted   += HandleSpherePlanted;
            GameEvents.OnSphereDefused   += HandleSphereCleared;
            GameEvents.OnSphereDetonated += HandleSphereCleared;
        }

        private void OnDisable()
        {
            GameEvents.OnSphereTimerTick -= HandleSphereTimerTick;
            GameEvents.OnSpherePlanted   -= HandleSpherePlanted;
            GameEvents.OnSphereDefused   -= HandleSphereCleared;
            GameEvents.OnSphereDetonated -= HandleSphereCleared;
        }

        private void Update()
        {
            if (_localHealth == null)
                return;

            UpdateHealth();
            UpdateWeapon();
            UpdateUltimate();
            UpdateMatchFlow();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Public API
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Call once when the local player spawns so the HUD can bind to their components.
        /// Also resolves scene-level managers once to avoid per-frame FindFirstObjectByType.
        /// </summary>
        public void BindLocalPlayer(GameObject playerRoot)
        {
            if (playerRoot == null)
                return;

            AutoWireRuntimeHud();

            _localHealth        = playerRoot.GetComponent<PlayerHealth>();
            _localMastery       = playerRoot.GetComponent<WeaponMasteryManager>();
            _localHero          = playerRoot.GetComponent<PlayerHeroController>();
            _localWeaponManager = playerRoot.GetComponent<WeaponManager>()
                                  ?? playerRoot.GetComponentInChildren<WeaponManager>();

            // Cache scene-level singletons once per player bind, not every frame.
            if (_roundManager  == null) _roundManager  = FindFirstObjectByType<RoundManager>();
            if (_rankedMode    == null) _rankedMode    = FindFirstObjectByType<RankedGameMode>();
            if (_fastFightMode == null) _fastFightMode = FindFirstObjectByType<FastFightMode>();
        }

        public void ShowXPGain(int amount)
        {
            if (_xpPopupText  != null) _xpPopupText.text   = $"+{amount} XP";
            if (_xpPopupGroup != null) _xpPopupGroup.alpha = 1f;
            _xpPopupTimer = 1.5f;
        }

        public void ShowSystemFailure(float duration)
        {
            StartCoroutine(SystemFailureRoutine(duration));
        }

        // ─────────────────────────────────────────────────────────────────────
        // Per-frame helpers
        // ─────────────────────────────────────────────────────────────────────

        private void UpdateHealth()
        {
            float hp    = _localHealth.CurrentHealth.Value;
            float armor = _localHealth.CurrentArmor.Value;

            if (_healthBar  != null) _healthBar.value  = hp / 100f;
            if (_armorBar   != null) _armorBar.value   = armor / 50f;
            if (_healthText != null) _healthText.text  = hp.ToString("0");
            if (_armorText  != null) _armorText.text   = armor.ToString("0");
        }

        private void UpdateWeapon()
        {
            if (_localWeaponManager == null)
                return;

            BaseWeapon activeWeapon  = _localWeaponManager.GetActiveWeapon();
            bool       isPistolRound = _localHero != null && _localHero.IsPistolRound;

            if (activeWeapon == null || activeWeapon.data == null)
            {
                if (_ammoText != null) _ammoText.text = "- / -";
                ClearBuffIcons();
                return;
            }

            if (_ammoText != null)
            {
                _ammoText.text  = $"{activeWeapon.CurrentAmmo} / {activeWeapon.data.maxReserveAmmo}";
                _ammoText.color = activeWeapon.CurrentAmmo <= (activeWeapon.data.magazineSize * 0.2f)
                    ? Color.red : Color.white;
            }

            if (_localMastery == null)
                return;

            if (isPistolRound)
            {
                if (_masteryLevelText != null)
                {
                    _masteryLevelText.text  = "MASTERY OFF";
                    _masteryLevelText.color = Color.gray;
                }
                if (_masteryXpBar != null) _masteryXpBar.value = 0f;
                ClearBuffIcons();
                return;
            }

            string weaponId         = activeWeapon.data.weaponId;
            int    level            = _localMastery.GetLevel(weaponId);
            int    totalXp          = _localMastery.GetXP(weaponId);
            int    currentLevelBase = (level - 1) * 1000;
            int    xpProgress       = totalXp - currentLevelBase;

            if (_masteryLevelText != null)
            {
                _masteryLevelText.text  = $"LVL {MasteryLevelColors.GetRomanNumeral(level)}";
                _masteryLevelText.color = MasteryLevelColors.GetColor(level);
            }

            if (_masteryXpBar != null)
                _masteryXpBar.value = Mathf.Clamp01(xpProgress / 1000f);

            UpdateBuffIcons(level);

            if (_xpPopupTimer > 0f)
            {
                _xpPopupTimer -= Time.deltaTime;
                if (_xpPopupGroup != null)
                    _xpPopupGroup.alpha = Mathf.Clamp01(_xpPopupTimer / 0.5f);
            }
        }

        private void UpdateUltimate()
        {
            if (_localHero == null)
                return;

            bool  isPistolRound = _localHero.IsPistolRound;
            float charge        = _localHero.UltimateCharge.Value / 100f;

            if (_ultimateProgressImage != null)
            {
                _ultimateProgressImage.fillAmount = isPistolRound ? 0f : charge;

                if (isPistolRound)
                {
                    _ultimateProgressImage.color = _ultimateDisabledColor;
                }
                else if (charge >= 1f)
                {
                    float pulse = (Mathf.Sin(Time.time * 3f) + 1f) * 0.5f;
                    _ultimateProgressImage.color = Color.Lerp(_ultimateReadyColor, Color.white, pulse * 0.3f);
                }
                else
                {
                    _ultimateProgressImage.color = _ultimateChargingColor;
                }
            }

            if (_ultimateStatusText != null)
            {
                if (isPistolRound)
                    _ultimateStatusText.text = "ULT DISABLED";
                else if (charge >= 1f)
                    _ultimateStatusText.text = "ULT READY";
                else
                    _ultimateStatusText.text = $"ULT {Mathf.RoundToInt(charge * 100f)}%";
            }
        }

        private void UpdateMatchFlow()
        {
            // Managers are cached in BindLocalPlayer — zero FindFirstObjectByType calls here.
            if (_roundManager != null)
            {
                if (_phaseText != null)
                    _phaseText.text = GetPhaseLabel(_roundManager.CurrentState.Value);
                if (_roundText != null)
                    _roundText.text = $"ROUND {_roundManager.RoundNumber.Value}";
            }

            if (_modeText  != null) _modeText.text  = GetModeLabel();
            if (_scoreText != null) _scoreText.text = GetScoreLabel();

            if (_heroIdentityText != null && _localHero != null && _localHero.Hero != null)
            {
                string heroTitle = string.IsNullOrWhiteSpace(_localHero.Hero.heroTitle)
                    ? _localHero.Hero.heroName
                    : $"{_localHero.Hero.heroName} | {_localHero.Hero.heroTitle}";
                _heroIdentityText.text = heroTitle;
            }

            bool isPistolRound = _localHero != null && _localHero.IsPistolRound;
            if (_pistolRoundBadge != null) _pistolRoundBadge.SetActive(isPistolRound);
            if (_overtimeBadge   != null)
                _overtimeBadge.SetActive(_rankedMode != null && _rankedMode.IsOvertimeActive);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Sphere bomb timer — event-driven, no polling
        // ─────────────────────────────────────────────────────────────────────

        private void HandleSpherePlanted(string siteId)
        {
            if (_sphereTimerPanel != null) _sphereTimerPanel.SetActive(true);
        }

        private void HandleSphereCleared()
        {
            if (_sphereTimerPanel != null) _sphereTimerPanel.SetActive(false);
        }

        private void HandleSphereTimerTick(float remaining)
        {
            if (_sphereTimerText != null)
                _sphereTimerText.text = Mathf.CeilToInt(remaining).ToString();

            if (_sphereTimerFill != null)
            {
                float t = Mathf.Clamp01(remaining / SphereDetonateMaxSeconds);
                _sphereTimerFill.fillAmount = t;
                _sphereTimerFill.color = Color.Lerp(_sphereTimerUrgentColor, _sphereTimerSafeColor, t);
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Buff icons
        // ─────────────────────────────────────────────────────────────────────

        private void UpdateBuffIcons(int level)
        {
            if (_buffIcons == null)
                return;

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
                bool hasSprite = i < activeBuffs.Length && activeBuffs[i] != null;
                if (hasSprite) _buffIcons[i].sprite = activeBuffs[i];
                _buffIcons[i].enabled = hasSprite;
            }
        }

        private void ClearBuffIcons()
        {
            if (_buffIcons == null) return;
            foreach (Image icon in _buffIcons)
                if (icon != null) icon.enabled = false;
        }

        // ─────────────────────────────────────────────────────────────────────
        // System Failure (Volt ultimate — GDD §8)
        // ─────────────────────────────────────────────────────────────────────

        private IEnumerator SystemFailureRoutine(float duration)
        {
            if (_healthText  != null) _healthText.text  = "ERR";
            if (_ammoText    != null) _ammoText.text    = "ERR";
            if (_armorText   != null) _armorText.text   = "ERR";

            if (_healthBar          != null) _healthBar.gameObject.SetActive(false);
            if (_armorBar           != null) _armorBar.gameObject.SetActive(false);
            if (_masteryXpBar       != null) _masteryXpBar.gameObject.SetActive(false);
            if (_ultimateProgressImage != null) _ultimateProgressImage.gameObject.SetActive(false);
            if (_phaseText          != null) _phaseText.gameObject.SetActive(false);
            if (_roundText          != null) _roundText.gameObject.SetActive(false);
            if (_scoreText          != null) _scoreText.gameObject.SetActive(false);
            if (_heroIdentityText   != null) _heroIdentityText.gameObject.SetActive(false);
            if (_ultimateStatusText != null) _ultimateStatusText.gameObject.SetActive(false);
            if (_pistolRoundBadge   != null) _pistolRoundBadge.SetActive(false);
            if (_overtimeBadge      != null) _overtimeBadge.SetActive(false);

            Debug.Log("[HUD] System Failure active. HUD disabled.");
            yield return new WaitForSeconds(duration);

            if (_healthBar          != null) _healthBar.gameObject.SetActive(true);
            if (_armorBar           != null) _armorBar.gameObject.SetActive(true);
            if (_masteryXpBar       != null) _masteryXpBar.gameObject.SetActive(true);
            if (_ultimateProgressImage != null) _ultimateProgressImage.gameObject.SetActive(true);
            if (_phaseText          != null) _phaseText.gameObject.SetActive(true);
            if (_roundText          != null) _roundText.gameObject.SetActive(true);
            if (_scoreText          != null) _scoreText.gameObject.SetActive(true);
            if (_heroIdentityText   != null) _heroIdentityText.gameObject.SetActive(true);
            if (_ultimateStatusText != null) _ultimateStatusText.gameObject.SetActive(true);

            Debug.Log("[HUD] System Failure ended. HUD restored.");
        }

        // ─────────────────────────────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────────────────────────────

        private void AutoWireRuntimeHud()
        {
            if (_healthText         == null) _healthText         = FindChild<TextMeshProUGUI>("HealthText");
            if (_armorText          == null) _armorText          = FindChild<TextMeshProUGUI>("ArmorText");
            if (_ammoText           == null) _ammoText           = FindChild<TextMeshProUGUI>("AmmoText");
            if (_modeText           == null) _modeText           = FindChild<TextMeshProUGUI>("ModeText");
            if (_phaseText          == null) _phaseText          = FindChild<TextMeshProUGUI>("PhaseText");
            if (_roundText          == null) _roundText          = FindChild<TextMeshProUGUI>("RoundText");
            if (_scoreText          == null) _scoreText          = FindChild<TextMeshProUGUI>("ScoreText");
            if (_heroIdentityText   == null) _heroIdentityText   = FindChild<TextMeshProUGUI>("HeroIdentityText");
            if (_ultimateStatusText == null) _ultimateStatusText = FindChild<TextMeshProUGUI>("UltimateStatusText");
            if (_healthBar          == null) _healthBar          = FindChild<Slider>("HealthBar");
            if (_armorBar           == null) _armorBar           = FindChild<Slider>("ArmorBar");
            if (_ultimateProgressImage == null) _ultimateProgressImage = FindChild<Image>("UltimateProgress");
            if (_sphereTimerText    == null) _sphereTimerText    = FindChild<TextMeshProUGUI>("SphereTimerText");
            if (_sphereTimerFill    == null) _sphereTimerFill    = FindChild<Image>("SphereTimerFill");

            if (_pistolRoundBadge == null)
            {
                Transform t = transform.Find("PistolRoundBadge");
                _pistolRoundBadge = t != null ? t.gameObject : null;
            }

            if (_overtimeBadge == null)
            {
                Transform t = transform.Find("OvertimeBadge");
                _overtimeBadge = t != null ? t.gameObject : null;
            }

            if (_sphereTimerPanel == null)
            {
                Transform t = transform.Find("SphereTimerPanel");
                _sphereTimerPanel = t != null ? t.gameObject : null;
            }

            // Bomb panel starts hidden.
            if (_sphereTimerPanel != null) _sphereTimerPanel.SetActive(false);
        }

        private T FindChild<T>(string childName) where T : Component
        {
            Transform child = transform.Find(childName);
            return child != null ? child.GetComponent<T>() : null;
        }

        private string GetModeLabel()
        {
            if (_rankedMode    != null) return "RANKED";
            if (_fastFightMode != null) return "FAST FIGHT";
            return "PROJECT Z";
        }

        private string GetScoreLabel()
        {
            if (_rankedMode    != null) return $"ATK {_rankedMode.AttackerRoundWins} - {_rankedMode.DefenderRoundWins} DEF";
            if (_fastFightMode != null) return $"ATK {_fastFightMode.AttackerRoundWins} - {_fastFightMode.DefenderRoundWins} DEF";
            return string.Empty;
        }

        private static string GetPhaseLabel(RoundManager.RoundState state)
        {
            return state switch
            {
                RoundManager.RoundState.BuyPhase    => "BUY PHASE",
                RoundManager.RoundState.ActionPhase => "ACTION PHASE",
                RoundManager.RoundState.EndPhase    => "ROUND END",
                _                                   => "WAITING"
            };
        }
    }
}
