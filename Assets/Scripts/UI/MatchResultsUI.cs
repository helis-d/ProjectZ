using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ProjectZ.Core;

namespace ProjectZ.UI
{
    /// <summary>
    /// Maç sonu sonuç ekranı. GameEvents.OnMatchEnd dinler.
    /// ELO, öldürme/ölme sayıları, MVP bilgisi gösterir.
    /// 
    /// KURULUM:
    /// 1. Canvas > MatchResults paneli oluştur (başlangıçta kapalı)
    /// 2. Bu scripti panele ekle ve UI referanslarını bağla
    /// </summary>
    public class MatchResultsUI : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject _resultsPanel;

        [Header("Match Outcome")]
        [SerializeField] private TextMeshProUGUI _outcomeText;
        [SerializeField] private TextMeshProUGUI _scoreText;

        [Header("Player Stats")]
        [SerializeField] private TextMeshProUGUI _killsText;
        [SerializeField] private TextMeshProUGUI _deathsText;
        [SerializeField] private TextMeshProUGUI _assistsText;
        [SerializeField] private TextMeshProUGUI _kdaText;

        [Header("ELO Change")]
        [SerializeField] private TextMeshProUGUI _eloChangeText;
        [SerializeField] private TextMeshProUGUI _xpGainText;

        [Header("MVP")]
        [SerializeField] private TextMeshProUGUI _mvpNameText;
        [SerializeField] private TextMeshProUGUI _mvpStatsText;

        [Header("Buttons")]
        [SerializeField] private Button _playAgainButton;
        [SerializeField] private Button _returnToLobbyButton;

        // Lokal istatistikler (basit tracking)
        private int _localKills;
        private int _localDeaths;
        private int _localAssists;
        private int _localOwnerId = -1;

        private void Awake()
        {
            if (_resultsPanel != null)
                _resultsPanel.SetActive(false);

            if (_playAgainButton != null)
                _playAgainButton.onClick.AddListener(OnPlayAgain);
            if (_returnToLobbyButton != null)
                _returnToLobbyButton.onClick.AddListener(OnReturnToLobby);
        }

        private void OnEnable()
        {
            GameEvents.OnMatchEnd += OnMatchEnd;
            GameEvents.OnPlayerDeath += OnPlayerDeath;
            GameEvents.OnPlayerAssist += OnPlayerAssist;
        }

        private void OnDisable()
        {
            GameEvents.OnMatchEnd -= OnMatchEnd;
            GameEvents.OnPlayerDeath -= OnPlayerDeath;
            GameEvents.OnPlayerAssist -= OnPlayerAssist;
        }

        /// <summary>
        /// Yerel oyuncunun connection ID'sini ayarlayın (spawn olduğunda).
        /// </summary>
        public void SetLocalPlayer(int ownerId)
        {
            _localOwnerId = ownerId;
            _localKills = 0;
            _localDeaths = 0;
            _localAssists = 0;
        }

        private void OnPlayerDeath(int victimId, int killerId)
        {
            if (killerId == _localOwnerId) _localKills++;
            if (victimId == _localOwnerId) _localDeaths++;
        }

        private void OnPlayerAssist(int assisterId, int victimId)
        {
            if (assisterId == _localOwnerId) _localAssists++;
        }

        private void OnMatchEnd(Team winningTeam)
        {
            ShowResults(winningTeam);
        }

        /// <summary>
        /// Sonuç ekranını göster.
        /// </summary>
        public void ShowResults(Team winningTeam)
        {
            if (_resultsPanel != null)
                _resultsPanel.SetActive(true);

            // Sonuç metni
            bool isWinner = true; // Gerçek uygulamada oyuncunun takımı kontrol edilmeli
            if (_outcomeText != null)
            {
                _outcomeText.text = isWinner ? "ZAFER!" : "YENİLGİ";
                _outcomeText.color = isWinner ? new Color(0.2f, 0.9f, 0.3f) : new Color(0.9f, 0.2f, 0.2f);
            }

            // İstatistikler
            if (_killsText != null) _killsText.text = _localKills.ToString();
            if (_deathsText != null) _deathsText.text = _localDeaths.ToString();
            if (_assistsText != null) _assistsText.text = _localAssists.ToString();

            float kda = _localDeaths > 0 ? (float)(_localKills + _localAssists) / _localDeaths : _localKills + _localAssists;
            if (_kdaText != null) _kdaText.text = $"KDA: {kda:F1}";

            // ELO değişimi (basit hesaplama)
            int eloChange = isWinner ? 25 : -15;
            if (_eloChangeText != null)
            {
                _eloChangeText.text = eloChange > 0 ? $"+{eloChange} ELO" : $"{eloChange} ELO";
                _eloChangeText.color = eloChange > 0 ? new Color(0.3f, 1f, 0.4f) : new Color(1f, 0.3f, 0.3f);
            }

            // XP kazanımı
            int xpGain = _localKills * 200 + _localAssists * 100 + (isWinner ? 500 : 200);
            if (_xpGainText != null) _xpGainText.text = $"+{xpGain} XP";

            // MVP (burada basitçe en çok kill yapan kişi — production'da tüm oyunculardan kontrol edilmeli)
            if (_mvpNameText != null) _mvpNameText.text = "MVP";
            if (_mvpStatsText != null) _mvpStatsText.text = $"{_localKills}K / {_localDeaths}D / {_localAssists}A";

            // Cursor'ı serbest bırak
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            Debug.Log($"[MatchResults] Shown. K:{_localKills} D:{_localDeaths} A:{_localAssists} ELO:{eloChange}");
        }

        private void OnPlayAgain()
        {
            if (_resultsPanel != null) _resultsPanel.SetActive(false);
            Debug.Log("[MatchResults] Play Again clicked — matchmaking'e yeniden gir.");
            // NakamaManager.Instance.FindMatchAsync(2, 10, "*");
        }

        private void OnReturnToLobby()
        {
            if (_resultsPanel != null) _resultsPanel.SetActive(false);
            Debug.Log("[MatchResults] Return to Lobby clicked.");
            // UnityEngine.SceneManagement.SceneManager.LoadScene("Lobby");
        }
    }
}
