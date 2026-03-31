using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ProjectZ.GameMode;
using ProjectZ.Player;
using ProjectZ.Weapon;

namespace ProjectZ.UI
{
    /// <summary>
    /// Main HUD Manager handling health, armor, ammo, mastery, ultimate, and round flow displays.
    /// Implements the most important GDD Section 3 and Section 10 visibility rules.
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
        [SerializeField] private Image _masteryIcon;

        [Header("Round Flow")]
        [SerializeField] private TextMeshProUGUI _modeText;
        [SerializeField] private TextMeshProUGUI _phaseText;
        [SerializeField] private TextMeshProUGUI _roundText;
        [SerializeField] private TextMeshProUGUI _scoreText;
        [SerializeField] private TextMeshProUGUI _heroIdentityText;
        [SerializeField] private TextMeshProUGUI _ultimateStatusText;
        [SerializeField] private GameObject _pistolRoundBadge;
        [SerializeField] private GameObject _overtimeBadge;

        [Header("XP Popup")]
        [SerializeField] private TextMeshProUGUI _xpPopupText;
        [SerializeField] private CanvasGroup _xpPopupGroup;

        [Header("Buff Icons")]
        [SerializeField] private Image[] _buffIcons;
        [SerializeField] private Sprite _adsBuffSprite;
        [SerializeField] private Sprite _reloadBuffSprite;
        [SerializeField] private Sprite _moveBuffSprite;
        [SerializeField] private Sprite _fireRateBuffSprite;
        [SerializeField] private Sprite _drawBuffSprite;

        [Header("Ultimate")]
        [SerializeField] private Image _ultimateProgressImage;
        [SerializeField] private Color _ultimateReadyColor = Color.yellow;
        [SerializeField] private Color _ultimateChargingColor = Color.gray;
        [SerializeField] private Color _ultimateDisabledColor = new Color(0.45f, 0.45f, 0.45f, 1f);

        private float _xpPopupTimer;

        private PlayerHealth _localHealth;
        private WeaponManager _localWeaponManager;
        private WeaponMasteryManager _localMastery;
        private PlayerHeroController _localHero;
        private RoundManager _roundManager;
        private RankedGameMode _rankedMode;
        private FastFightMode _fastFightMode;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
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

        public void BindLocalPlayer(GameObject playerRoot)
        {
            if (playerRoot == null)
                return;

            _localHealth = playerRoot.GetComponent<PlayerHealth>();
            _localMastery = playerRoot.GetComponent<WeaponMasteryManager>();
            _localHero = playerRoot.GetComponent<PlayerHeroController>();
            _localWeaponManager = playerRoot.GetComponent<WeaponManager>() ?? playerRoot.GetComponentInChildren<WeaponManager>();

            _roundManager = FindFirstObjectByType<RoundManager>();
            _rankedMode = FindFirstObjectByType<RankedGameMode>();
            _fastFightMode = FindFirstObjectByType<FastFightMode>();
        }

        private void UpdateHealth()
        {
            float hp = _localHealth.CurrentHealth.Value;
            float armor = _localHealth.CurrentArmor.Value;

            if (_healthBar != null)
                _healthBar.value = hp / 100f;
            if (_armorBar != null)
                _armorBar.value = armor / 50f;

            if (_healthText != null)
                _healthText.text = hp.ToString("0");
            if (_armorText != null)
                _armorText.text = armor.ToString("0");
        }

        private void UpdateWeapon()
        {
            if (_localWeaponManager == null)
                return;

            BaseWeapon activeWeapon = _localWeaponManager.GetActiveWeapon();
            bool isPistolRound = _localHero != null && _localHero.IsPistolRound;

            if (activeWeapon == null || activeWeapon.data == null)
            {
                if (_ammoText != null)
                    _ammoText.text = "- / -";
                ClearBuffIcons();
                return;
            }

            if (_ammoText != null)
            {
                _ammoText.text = $"{activeWeapon.CurrentAmmo} / {activeWeapon.data.maxReserveAmmo}";
                _ammoText.color = activeWeapon.CurrentAmmo <= (activeWeapon.data.magazineSize * 0.2f)
                    ? Color.red
                    : Color.white;
            }

            if (_localMastery == null)
                return;

            if (isPistolRound)
            {
                if (_masteryLevelText != null)
                {
                    _masteryLevelText.text = "MASTERY OFF";
                    _masteryLevelText.color = Color.gray;
                }

                if (_masteryXpBar != null)
                    _masteryXpBar.value = 0f;

                ClearBuffIcons();
                return;
            }

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

            bool isPistolRound = _localHero.IsPistolRound;
            float charge = _localHero.UltimateCharge.Value / 100f;

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
            if (_roundManager == null)
                _roundManager = FindFirstObjectByType<RoundManager>();
            if (_rankedMode == null)
                _rankedMode = FindFirstObjectByType<RankedGameMode>();
            if (_fastFightMode == null)
                _fastFightMode = FindFirstObjectByType<FastFightMode>();

            if (_roundManager != null)
            {
                if (_phaseText != null)
                    _phaseText.text = GetPhaseLabel(_roundManager.CurrentState.Value);

                if (_roundText != null)
                    _roundText.text = $"ROUND {_roundManager.RoundNumber.Value}";
            }

            if (_modeText != null)
                _modeText.text = GetModeLabel();

            if (_scoreText != null)
                _scoreText.text = GetScoreLabel();

            if (_heroIdentityText != null && _localHero != null && _localHero.Hero != null)
            {
                string heroTitle = string.IsNullOrWhiteSpace(_localHero.Hero.heroTitle)
                    ? _localHero.Hero.heroName
                    : $"{_localHero.Hero.heroName} | {_localHero.Hero.heroTitle}";
                _heroIdentityText.text = heroTitle;
            }

            bool isPistolRound = _localHero != null && _localHero.IsPistolRound;
            if (_pistolRoundBadge != null)
                _pistolRoundBadge.SetActive(isPistolRound);

            if (_overtimeBadge != null)
                _overtimeBadge.SetActive(_rankedMode != null && _rankedMode.IsOvertimeActive);
        }

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

        private void ClearBuffIcons()
        {
            if (_buffIcons == null)
                return;

            for (int i = 0; i < _buffIcons.Length; i++)
            {
                if (_buffIcons[i] != null)
                    _buffIcons[i].enabled = false;
            }
        }

        public void ShowSystemFailure(float duration)
        {
            StartCoroutine(SystemFailureRoutine(duration));
        }

        private System.Collections.IEnumerator SystemFailureRoutine(float duration)
        {
            if (_healthText != null) _healthText.text = "ERR";
            if (_ammoText != null) _ammoText.text = "ERR";
            if (_armorText != null) _armorText.text = "ERR";

            if (_healthBar != null) _healthBar.gameObject.SetActive(false);
            if (_armorBar != null) _armorBar.gameObject.SetActive(false);
            if (_masteryXpBar != null) _masteryXpBar.gameObject.SetActive(false);
            if (_ultimateProgressImage != null) _ultimateProgressImage.gameObject.SetActive(false);
            if (_phaseText != null) _phaseText.gameObject.SetActive(false);
            if (_roundText != null) _roundText.gameObject.SetActive(false);
            if (_scoreText != null) _scoreText.gameObject.SetActive(false);
            if (_heroIdentityText != null) _heroIdentityText.gameObject.SetActive(false);
            if (_ultimateStatusText != null) _ultimateStatusText.gameObject.SetActive(false);
            if (_pistolRoundBadge != null) _pistolRoundBadge.SetActive(false);
            if (_overtimeBadge != null) _overtimeBadge.SetActive(false);

            Debug.Log("[HUD] System Failure active. HUD disabled.");
            yield return new WaitForSeconds(duration);

            if (_healthBar != null) _healthBar.gameObject.SetActive(true);
            if (_armorBar != null) _armorBar.gameObject.SetActive(true);
            if (_masteryXpBar != null) _masteryXpBar.gameObject.SetActive(true);
            if (_ultimateProgressImage != null) _ultimateProgressImage.gameObject.SetActive(true);
            if (_phaseText != null) _phaseText.gameObject.SetActive(true);
            if (_roundText != null) _roundText.gameObject.SetActive(true);
            if (_scoreText != null) _scoreText.gameObject.SetActive(true);
            if (_heroIdentityText != null) _heroIdentityText.gameObject.SetActive(true);
            if (_ultimateStatusText != null) _ultimateStatusText.gameObject.SetActive(true);

            Debug.Log("[HUD] System Failure ended. HUD restored.");
        }

        private string GetModeLabel()
        {
            if (_rankedMode != null)
                return "RANKED";
            if (_fastFightMode != null)
                return "FAST FIGHT";
            return "PROJECT Z";
        }

        private string GetScoreLabel()
        {
            if (_rankedMode != null)
                return $"ATK {_rankedMode.AttackerRoundWins} - {_rankedMode.DefenderRoundWins} DEF";
            if (_fastFightMode != null)
                return $"ATK {_fastFightMode.AttackerRoundWins} - {_fastFightMode.DefenderRoundWins} DEF";
            return string.Empty;
        }

        private static string GetPhaseLabel(RoundManager.RoundState state)
        {
            return state switch
            {
                RoundManager.RoundState.BuyPhase => "BUY PHASE",
                RoundManager.RoundState.ActionPhase => "ACTION PHASE",
                RoundManager.RoundState.EndPhase => "ROUND END",
                _ => "WAITING"
            };
        }
    }
}
