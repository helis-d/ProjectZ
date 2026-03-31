using FishNet.Managing;
using ProjectZ.Core;
using ProjectZ.GameMode;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectZ.UI
{
    /// <summary>
    /// Match end results screen. Listens to GameEvents.OnMatchEnd and presents
    /// a local summary using the canonical mode score where possible.
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

        [Header("Progression")]
        [SerializeField] private TextMeshProUGUI _eloChangeText;
        [SerializeField] private TextMeshProUGUI _xpGainText;

        [Header("MVP")]
        [SerializeField] private TextMeshProUGUI _mvpNameText;
        [SerializeField] private TextMeshProUGUI _mvpStatsText;

        [Header("Buttons")]
        [SerializeField] private Button _playAgainButton;
        [SerializeField] private Button _returnToLobbyButton;

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

        public void SetLocalPlayer(int ownerId)
        {
            _localOwnerId = ownerId;
            _localKills = 0;
            _localDeaths = 0;
            _localAssists = 0;
        }

        private void OnPlayerDeath(int victimId, int killerId)
        {
            EnsureLocalOwnerId();

            if (killerId == _localOwnerId)
                _localKills++;
            if (victimId == _localOwnerId)
                _localDeaths++;
        }

        private void OnPlayerAssist(int assisterId, int victimId)
        {
            EnsureLocalOwnerId();

            if (assisterId == _localOwnerId)
                _localAssists++;
        }

        private void OnMatchEnd(Team winningTeam)
        {
            ShowResults(winningTeam);
        }

        public void ShowResults(Team winningTeam)
        {
            EnsureLocalOwnerId();

            if (_resultsPanel != null)
                _resultsPanel.SetActive(true);

            bool isWinner = TryGetLocalTeam(out Team myTeam) && myTeam == winningTeam;
            bool overtimeWin = false;

            RankedGameMode rankedMode = FindFirstObjectByType<RankedGameMode>();
            if (rankedMode != null)
                overtimeWin = rankedMode.IsOvertimeActive;

            if (_outcomeText != null)
            {
                if (isWinner && overtimeWin)
                    _outcomeText.text = "OVERTIME VICTORY";
                else if (isWinner)
                    _outcomeText.text = "VICTORY";
                else
                    _outcomeText.text = "DEFEAT";

                _outcomeText.color = isWinner
                    ? new Color(0.2f, 0.9f, 0.3f)
                    : new Color(0.9f, 0.2f, 0.2f);
            }

            if (_scoreText != null)
                _scoreText.text = BuildScoreText();

            if (_killsText != null) _killsText.text = _localKills.ToString();
            if (_deathsText != null) _deathsText.text = _localDeaths.ToString();
            if (_assistsText != null) _assistsText.text = _localAssists.ToString();

            float kda = _localDeaths > 0
                ? (float)(_localKills + _localAssists) / _localDeaths
                : _localKills + _localAssists;
            if (_kdaText != null)
                _kdaText.text = $"KDA: {kda:F1}";

            int eloChange = isWinner ? 25 : -15;
            if (_eloChangeText != null)
            {
                _eloChangeText.text = eloChange > 0 ? $"+{eloChange} ELO" : $"{eloChange} ELO";
                _eloChangeText.color = eloChange > 0
                    ? new Color(0.3f, 1f, 0.4f)
                    : new Color(1f, 0.3f, 0.3f);
            }

            int xpGain = _localKills * 200 + _localAssists * 100 + (isWinner ? 500 : 200);
            if (_xpGainText != null)
                _xpGainText.text = $"+{xpGain} XP";

            if (_mvpNameText != null)
                _mvpNameText.text = "MVP";
            if (_mvpStatsText != null)
                _mvpStatsText.text = $"{_localKills}K / {_localDeaths}D / {_localAssists}A";

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            Debug.Log($"[MatchResults] Displayed. Winner={winningTeam} LocalWinner={isWinner}");
        }

        private void OnPlayAgain()
        {
            if (_resultsPanel != null)
                _resultsPanel.SetActive(false);

            Debug.Log("[MatchResults] Play Again clicked.");
        }

        private void OnReturnToLobby()
        {
            if (_resultsPanel != null)
                _resultsPanel.SetActive(false);

            Debug.Log("[MatchResults] Return to Lobby clicked.");
        }

        private void EnsureLocalOwnerId()
        {
            if (_localOwnerId >= 0)
                return;

            if (NetworkManager.Instances.Count == 0)
                return;

            if (NetworkManager.Instances[0].ClientManager.Connection != null)
                _localOwnerId = NetworkManager.Instances[0].ClientManager.Connection.ClientId;
        }

        private bool TryGetLocalTeam(out Team team)
        {
            team = Team.None;
            TeamManager teamManager = TeamManager.Instance ?? FindFirstObjectByType<TeamManager>();
            if (teamManager == null || _localOwnerId < 0)
                return false;

            team = teamManager.GetTeam(_localOwnerId);
            return team != Team.None;
        }

        private static string BuildScoreText()
        {
            RankedGameMode rankedMode = UnityEngine.Object.FindFirstObjectByType<RankedGameMode>();
            if (rankedMode != null)
                return $"ATK {rankedMode.AttackerRoundWins} - {rankedMode.DefenderRoundWins} DEF";

            FastFightMode fastFightMode = UnityEngine.Object.FindFirstObjectByType<FastFightMode>();
            if (fastFightMode != null)
                return $"ATK {fastFightMode.AttackerRoundWins} - {fastFightMode.DefenderRoundWins} DEF";

            return "MATCH COMPLETE";
        }
    }
}
