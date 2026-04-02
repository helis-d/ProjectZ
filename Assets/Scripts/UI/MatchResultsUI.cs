using FishNet.Managing;
using ProjectZ.Core;
using ProjectZ.GameMode;
using ProjectZ.Network;
using ProjectZ.Player;
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
        private bool _rankPersistenceQueued;

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
            _rankPersistenceQueued = false;
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

            ApplyRankPresentation(isWinner);

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

        private void ApplyRankPresentation(bool isWinner)
        {
            RankedGameMode rankedMode = FindFirstObjectByType<RankedGameMode>();
            if (rankedMode == null)
            {
                int fallbackDelta = isWinner ? 25 : -15;
                if (_eloChangeText != null)
                {
                    _eloChangeText.text = fallbackDelta > 0 ? $"+{fallbackDelta} ELO" : $"{fallbackDelta} ELO";
                    _eloChangeText.color = fallbackDelta > 0
                        ? new Color(0.3f, 1f, 0.4f)
                        : new Color(1f, 0.3f, 0.3f);
                }

                return;
            }

            RankedMatchPerformance performance = BuildRankedPerformance(isWinner);
            RankedProgressionResult preview = NakamaManager.Instance != null
                ? NakamaManager.Instance.PreviewRankedProgression(performance)
                : CompetitiveRankSystem.BuildProgressionResult(
                    CompetitiveRankSystem.StartingRating,
                    CompetitiveRankSystem.ApplyRatingDelta(
                        CompetitiveRankSystem.StartingRating,
                        CompetitiveRankSystem.CalculateRatingDelta(performance)));

            if (_eloChangeText != null)
            {
                string deltaLabel = preview.Delta > 0 ? $"+{preview.Delta}" : preview.Delta.ToString();
                string rankLabel = preview.PreviousRank.DisplayName == preview.NewRank.DisplayName
                    ? preview.NewRank.DisplayName
                    : $"{preview.PreviousRank.DisplayName} -> {preview.NewRank.DisplayName}";

                _eloChangeText.text = $"{deltaLabel} ELO | {rankLabel}";
                _eloChangeText.color = preview.Delta >= 0
                    ? new Color(0.3f, 1f, 0.4f)
                    : new Color(1f, 0.3f, 0.3f);
            }

            if (!_rankPersistenceQueued && NakamaManager.Instance != null)
            {
                _rankPersistenceQueued = true;
                PersistRankedProgressionAsync(performance);
            }
        }

        private RankedMatchPerformance BuildRankedPerformance(bool isWinner)
        {
            int currentRating = NakamaManager.Instance?.CachedProfile != null
                ? NakamaManager.Instance.CachedProfile.elo
                : CompetitiveRankSystem.StartingRating;

            int rankedMatchesPlayed = NakamaManager.Instance?.CachedProfile != null
                ? NakamaManager.Instance.CachedProfile.rankedMatchesPlayed
                : 0;

            GetMatchRoundBreakdown(isWinner, out int roundsWon, out int roundsLost);

            return new RankedMatchPerformance(
                currentRating,
                currentRating,
                isWinner,
                _localKills,
                _localDeaths,
                _localAssists,
                roundsWon,
                roundsLost,
                IsLocalMvp(),
                rankedMatchesPlayed);
        }

        private void GetMatchRoundBreakdown(bool isWinner, out int roundsWon, out int roundsLost)
        {
            roundsWon = 13;
            roundsLost = 11;

            RankedGameMode rankedMode = FindFirstObjectByType<RankedGameMode>();
            if (rankedMode != null)
            {
                int topScore = Mathf.Max(rankedMode.AttackerRoundWins, rankedMode.DefenderRoundWins);
                int lowScore = Mathf.Min(rankedMode.AttackerRoundWins, rankedMode.DefenderRoundWins);
                roundsWon = isWinner ? topScore : lowScore;
                roundsLost = isWinner ? lowScore : topScore;
                return;
            }

            FastFightMode fastFightMode = FindFirstObjectByType<FastFightMode>();
            if (fastFightMode != null)
            {
                int topScore = Mathf.Max(fastFightMode.AttackerRoundWins, fastFightMode.DefenderRoundWins);
                int lowScore = Mathf.Min(fastFightMode.AttackerRoundWins, fastFightMode.DefenderRoundWins);
                roundsWon = isWinner ? topScore : lowScore;
                roundsLost = isWinner ? lowScore : topScore;
            }
        }

        private bool IsLocalMvp()
        {
            PlayerStats[] stats = FindObjectsByType<PlayerStats>(FindObjectsSortMode.None);
            if (stats == null || stats.Length == 0)
                return false;

            int localScore = int.MinValue;
            int bestScore = int.MinValue;

            foreach (PlayerStats stat in stats)
            {
                if (stat == null)
                    continue;

                int score = (stat.Kills.Value * 3) + stat.Assists.Value - stat.Deaths.Value;
                bestScore = Mathf.Max(bestScore, score);

                if (stat.OwnerId == _localOwnerId)
                    localScore = score;
            }

            return localScore != int.MinValue && localScore >= bestScore;
        }

        private async void PersistRankedProgressionAsync(RankedMatchPerformance performance)
        {
            await NakamaManager.Instance.ApplyRankedMatchResultAsync(performance);
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
