using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ProjectZ.GameMode;
using ProjectZ.Network;

namespace ProjectZ.UI
{
    /// <summary>
    /// Ana Menü / Lobi kontrolcüsü. Nakama bağlantısını yönetir.
    /// Butonlar: Bağlan → Maç Ara → İptal
    /// 
    /// KURULUM:
    /// 1. Yeni bir sahne oluştur (Lobby)
    /// 2. Canvas > Panel > Bu scriptlerin bağlı olduğu UI
    /// 3. NakamaManager prefab'ını bu sahneye koy
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI _statusText;
        [SerializeField] private TextMeshProUGUI _playerNameText;
        [SerializeField] private Button _connectButton;
        [SerializeField] private Button _findMatchButton;
        [SerializeField] private Button _cancelMatchButton;

        [Header("Panels")]
        [SerializeField] private GameObject _connectPanel;
        [SerializeField] private GameObject _lobbyPanel;
        [SerializeField] private GameObject _searchingPanel;

        private Nakama.IMatchmakerTicket _currentTicket;

        private void Start()
        {
            // Buton eventleri
            if (_connectButton != null)
                _connectButton.onClick.AddListener(OnConnectClicked);
            if (_findMatchButton != null)
                _findMatchButton.onClick.AddListener(OnFindMatchClicked);
            if (_cancelMatchButton != null)
                _cancelMatchButton.onClick.AddListener(OnCancelMatchClicked);

            // Başlangıç durumu
            ShowPanel("connect");
            SetStatus("Nakama sunucusuna bağlanmak için butona basın.");
        }

        private void OnEnable()
        {
            if (NakamaManager.Instance != null)
            {
                NakamaManager.Instance.OnAuthenticationSuccess += OnAuthenticated;
                NakamaManager.Instance.OnAuthenticationFailed += OnAuthFailed;
                NakamaManager.Instance.OnMatchFound += OnMatchFound;
            }
        }

        private void OnDisable()
        {
            if (NakamaManager.Instance != null)
            {
                NakamaManager.Instance.OnAuthenticationSuccess -= OnAuthenticated;
                NakamaManager.Instance.OnAuthenticationFailed -= OnAuthFailed;
                NakamaManager.Instance.OnMatchFound -= OnMatchFound;
            }
        }

        // ─── Buton İşleyicileri ───────────────────────────────────────────
        private async void OnConnectClicked()
        {
            SetStatus("Bağlanılıyor...");
            if (_connectButton != null) _connectButton.interactable = false;

            bool success = await NakamaManager.Instance.AuthenticateWithDeviceAsync();

            if (!success)
            {
                SetStatus("Bağlantı başarısız. Tekrar deneyin.");
                if (_connectButton != null) _connectButton.interactable = true;
            }
        }

        private async void OnFindMatchClicked()
        {
            if (NakamaManager.Instance == null || !NakamaManager.Instance.IsAuthenticated)
            {
                SetStatus("Önce bağlanmalısınız!");
                return;
            }

            SetStatus("Maç aranıyor...");
            ShowPanel("searching");

            _currentTicket = await NakamaManager.Instance.FindMatchAsync(2, 10, "*");

            if (_currentTicket == null)
            {
                SetStatus("Maç arama başarısız.");
                ShowPanel("lobby");
            }
        }

        private async void OnCancelMatchClicked()
        {
            if (_currentTicket != null && NakamaManager.Instance != null)
            {
                await NakamaManager.Instance.CancelMatchAsync(_currentTicket);
                _currentTicket = null;
            }

            SetStatus("Maç arama iptal edildi.");
            ShowPanel("lobby");
        }

        // ─── Event İşleyicileri ───────────────────────────────────────────
        private void OnAuthenticated()
        {
            SetStatus($"Hoş geldin, {NakamaManager.Instance.Username}!");
            if (_playerNameText != null)
                _playerNameText.text = NakamaManager.Instance.Username;

            ShowPanel("lobby");

            // Profili yükle
            LoadProfileAsync();
        }

        private async void LoadProfileAsync()
        {
            var profile = await NakamaManager.Instance.LoadPlayerProfileAsync();
            if (profile != null)
            {
                CompetitiveRankInfo rank = CompetitiveRankSystem.GetRankInfo(profile.elo);
                SetStatus($"Profil y\u00fcklendi: {profile.displayName} | Rank: {rank.DisplayName} | ELO: {profile.elo} | Para: {profile.currency}");
            }
        }

        private void OnAuthFailed(string error)
        {
            SetStatus($"Bağlantı hatası: {error}");
            if (_connectButton != null) _connectButton.interactable = true;
            ShowPanel("connect");
        }

        private void OnMatchFound(Nakama.IMatchmakerMatched matched)
        {
            SetStatus($"MAÇ BULUNDU! Token: {matched.Token.Substring(0, 8)}...");
            _currentTicket = null;

            // Burada oyun sahnesine geçiş yapılır
            // UnityEngine.SceneManagement.SceneManager.LoadScene("GameScene");
            Debug.Log($"[MainMenu] Matched ticket token: {matched.Token}");
        }

        // ─── Yardımcılar ─────────────────────────────────────────────────
        private void SetStatus(string message)
        {
            if (_statusText != null) _statusText.text = message;
            Debug.Log($"[MainMenu] {message}");
        }

        private void ShowPanel(string panel)
        {
            if (_connectPanel != null)   _connectPanel.SetActive(panel == "connect");
            if (_lobbyPanel != null)     _lobbyPanel.SetActive(panel == "lobby");
            if (_searchingPanel != null)  _searchingPanel.SetActive(panel == "searching");
        }
    }
}
