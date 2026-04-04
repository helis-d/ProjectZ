using TMPro;
using ProjectZ.GameMode;
using ProjectZ.Monetization;
using ProjectZ.Network;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ProjectZ.UI
{
    public class MainMenuController : MonoBehaviour
    {
        private const string GameplaySceneName = "SampleScene";

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
        private bool _profileReady;

        private void Start()
        {
            if (_connectButton != null)
                _connectButton.onClick.AddListener(OnConnectClicked);
            if (_findMatchButton != null)
                _findMatchButton.onClick.AddListener(OnFindMatchClicked);
            if (_cancelMatchButton != null)
                _cancelMatchButton.onClick.AddListener(OnCancelMatchClicked);

            ShowPanel("connect");
            SetLobbyActionsEnabled(false);
            SetSearchingState(false);
            SetStatus("Nakama sunucusuna baglanmak icin butona basin.");
        }

        private void OnEnable()
        {
            if (NakamaManager.Instance == null)
                return;

            NakamaManager.Instance.OnAuthenticationSuccess += OnAuthenticated;
            NakamaManager.Instance.OnAuthenticationFailed += OnAuthFailed;
            NakamaManager.Instance.OnMatchFound += OnMatchFound;
        }

        private void OnDisable()
        {
            if (NakamaManager.Instance == null)
                return;

            NakamaManager.Instance.OnAuthenticationSuccess -= OnAuthenticated;
            NakamaManager.Instance.OnAuthenticationFailed -= OnAuthFailed;
            NakamaManager.Instance.OnMatchFound -= OnMatchFound;
        }

        private async void OnConnectClicked()
        {
            if (NakamaManager.Instance == null)
            {
                SetStatus("NakamaManager bulunamadi.");
                return;
            }

            SetStatus("Baglaniliyor...");
            if (_connectButton != null)
                _connectButton.interactable = false;

            bool success = await NakamaManager.Instance.AuthenticateWithDeviceAsync();
            if (!success)
            {
                SetStatus("Baglanti basarisiz. Tekrar deneyin.");
                if (_connectButton != null)
                    _connectButton.interactable = true;
            }
        }

        private async void OnFindMatchClicked()
        {
            if (NakamaManager.Instance == null || !NakamaManager.Instance.IsAuthenticated)
            {
                SetStatus("Once baglanmalisiniz.");
                return;
            }

            if (!_profileReady)
            {
                SetStatus("Profil yuklenmeden ranked kuyruguna girilemez.");
                return;
            }

            if (!NakamaManager.Instance.CanAccessRankedByOwnership())
            {
                int ownedHeroes = NakamaManager.Instance.CachedProfile != null
                    ? MonetizationService.CountOwnedHeroes(NakamaManager.Instance.CachedProfile)
                    : 0;

                SetStatus(
                    $"Ranked icin en az {MonetizationService.RankedRequiredOwnedHeroes} hero gerekli. " +
                    $"Mevcut: {ownedHeroes}/{MonetizationService.RankedRequiredOwnedHeroes}.");
                return;
            }

            SetStatus("Ranked mac araniyor...");
            ShowPanel("searching");
            SetSearchingState(true);

            _currentTicket = await NakamaManager.Instance.FindMatchAsync(2, 10, "*", true);
            if (_currentTicket == null)
            {
                SetSearchingState(false);
                ShowPanel("lobby");
                SetStatus("Mac arama basarisiz.");
            }
        }

        private async void OnCancelMatchClicked()
        {
            if (_currentTicket != null && NakamaManager.Instance != null)
            {
                await NakamaManager.Instance.CancelMatchAsync(_currentTicket);
                _currentTicket = null;
            }

            SetSearchingState(false);
            ShowPanel("lobby");
            SetStatus("Mac arama iptal edildi.");
        }

        private void OnAuthenticated()
        {
            _profileReady = false;
            ShowPanel("lobby");
            SetLobbyActionsEnabled(false);
            SetSearchingState(false);

            SetStatus($"Hos geldin, {NakamaManager.Instance.Username}!");
            if (_playerNameText != null)
                _playerNameText.text = NakamaManager.Instance.Username;

            LoadProfileAsync();
        }

        private async void LoadProfileAsync()
        {
            if (NakamaManager.Instance == null)
                return;

            PlayerProfileData profile = await NakamaManager.Instance.LoadPlayerProfileAsync();
            if (profile == null)
            {
                SetStatus("Profil yuklenemedi. Tekrar deneyin.");
                SetLobbyActionsEnabled(false);
                return;
            }

            CompetitiveRankInfo rank = CompetitiveRankSystem.GetRankInfo(profile.elo);
            int ownedHeroes = MonetizationService.CountOwnedHeroes(profile);
            bool rankedReady = MonetizationService.CanEnterRanked(profile);
            string rankedStatus = rankedReady
                ? "Hazir"
                : $"{ownedHeroes}/{MonetizationService.RankedRequiredOwnedHeroes} hero";

            _profileReady = true;
            SetLobbyActionsEnabled(rankedReady);

            SetStatus(
                $"Profil yuklendi: {profile.displayName} | Rank: {rank.DisplayName} | ELO: {profile.elo} | " +
                $"Komuta Kredisi: {profile.commandCredits} | Z-Core: {profile.zCore} | " +
                $"Hero: {ownedHeroes}/{MonetizationService.TotalHeroCount} | Ranked: {rankedStatus}");
        }

        private void OnAuthFailed(string error)
        {
            _profileReady = false;
            SetLobbyActionsEnabled(false);
            SetSearchingState(false);
            ShowPanel("connect");
            SetStatus($"Baglanti hatasi: {error}");

            if (_connectButton != null)
                _connectButton.interactable = true;
        }

        private void OnMatchFound(Nakama.IMatchmakerMatched matched)
        {
            _currentTicket = null;
            SetSearchingState(false);

            string tokenPreview = string.IsNullOrWhiteSpace(matched?.Token)
                ? "n/a"
                : matched.Token.Substring(0, Mathf.Min(8, matched.Token.Length));

            SetStatus($"Mac bulundu. Token: {tokenPreview}...");

            if (SceneManager.GetActiveScene().name == GameplaySceneName)
                return;

            if (!Application.CanStreamedLevelBeLoaded(GameplaySceneName))
            {
                SetStatus($"Mac bulundu ancak '{GameplaySceneName}' build settings icinde degil.");
                return;
            }

            SceneManager.LoadScene(GameplaySceneName);
        }

        private void SetStatus(string message)
        {
            if (_statusText != null)
                _statusText.text = message;

            Debug.Log($"[MainMenu] {message}");
        }

        private void ShowPanel(string panel)
        {
            if (_connectPanel != null)
                _connectPanel.SetActive(panel == "connect");
            if (_lobbyPanel != null)
                _lobbyPanel.SetActive(panel == "lobby");
            if (_searchingPanel != null)
                _searchingPanel.SetActive(panel == "searching");
        }

        private void SetLobbyActionsEnabled(bool canFindMatch)
        {
            if (_findMatchButton != null)
                _findMatchButton.interactable = _profileReady && canFindMatch;
        }

        private void SetSearchingState(bool isSearching)
        {
            if (_cancelMatchButton != null)
                _cancelMatchButton.interactable = isSearching;
        }
    }
}
