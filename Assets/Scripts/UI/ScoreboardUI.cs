using FishNet.Managing;
using ProjectZ.Core;
using ProjectZ.Economy;
using ProjectZ.GameMode;
using ProjectZ.Player;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectZ.UI
{
    /// <summary>
    /// Scoreboard (TAB Menu). Tracks and displays players.
    /// GDD Section 10 Rule: Enemy team's Ultimate and Economy are hidden ("?").
    /// </summary>
    public class ScoreboardUI : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private GameObject _scoreboardPanel;

        [Header("Row Prefab")]
        [Tooltip("Prefab with TextMeshProUGUI children: Ping, CharIcon, PlayerName, K, D, A, Money, Ult")]
        [SerializeField] private GameObject _rowPrefab;
        [SerializeField] private Transform _attackerContainer;
        [SerializeField] private Transform _defenderContainer;

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            if (keyboard.tabKey.wasPressedThisFrame)
            {
                OpenScoreboard();
            }
            else if (keyboard.tabKey.wasReleasedThisFrame)
            {
                CloseScoreboard();
            }
        }

        private void OpenScoreboard()
        {
            if (_scoreboardPanel != null) _scoreboardPanel.SetActive(true);
            RefreshData();
        }

        private void CloseScoreboard()
        {
            if (_scoreboardPanel != null) _scoreboardPanel.SetActive(false);
        }

        private void RefreshData()
        {
            if (NetworkManager.Instances.Count == 0) return;
            var nm = NetworkManager.Instances[0];

            TeamManager teamManager = FindFirstObjectByType<TeamManager>();
            if (teamManager == null) return;

            // Clear existing rows
            ClearContainer(_attackerContainer);
            ClearContainer(_defenderContainer);

            // Determine Local Player's Team to apply GDD visibility rules
            int localId = nm.ClientManager.Connection.ClientId;
            Team myTeam = teamManager.GetTeam(localId);

            foreach (var client in nm.ClientManager.Clients.Values)
            {
                if (client.FirstObject == null) continue;

                int targetId = client.ClientId;
                Team targetTeam = teamManager.GetTeam(targetId);

                bool isEnemy = (myTeam != targetTeam && targetTeam != Team.None && myTeam != Team.None);

                // K/D/A from PlayerStats (visible for all players)
                string killsStr = "0", deathsStr = "0", assistsStr = "0";
                var stats = client.FirstObject.GetComponent<PlayerStats>();
                if (stats != null)
                {
                    killsStr   = stats.Kills.ToString();
                    deathsStr  = stats.Deaths.ToString();
                    assistsStr = stats.Assists.ToString();
                }

                // Economy and Ultimate — hidden for enemies (GDD rule)
                string moneyStr = "???";
                string ultStr   = "???";

                if (!isEnemy)
                {
                    var econ = client.FirstObject.GetComponent<PlayerEconomy>();
                    var hero = client.FirstObject.GetComponent<PlayerHeroController>();

                    if (econ != null) moneyStr = $"${econ.CurrentMoney}";
                    if (hero != null) ultStr = $"{hero.UltimateCharge}%";
                }

                // Ping
                int ping = (int)(nm.TimeManager.RoundTripTime * 1000);

                // Character icon (hero name) 
                string heroName = "?";
                var heroCtrl = client.FirstObject.GetComponent<PlayerHeroController>();
                if (heroCtrl != null && heroCtrl.Hero != null)
                    heroName = heroCtrl.Hero.heroName;

                // Populate row
                Transform container = targetTeam == Team.Attacker ? _attackerContainer : _defenderContainer;
                PopulateRow(container, ping.ToString(), heroName, $"Player {targetId}", killsStr, deathsStr, assistsStr, moneyStr, ultStr);
            }
        }

        private void PopulateRow(Transform container, string ping, string heroName, string playerName, string kills, string deaths, string assists, string money, string ult)
        {
            if (_rowPrefab == null || container == null) return;

            GameObject row = Instantiate(_rowPrefab, container);
            var texts = row.GetComponentsInChildren<TextMeshProUGUI>();

            // Expected order in prefab: [0]=Ping, [1]=Hero, [2]=Name, [3]=K, [4]=D, [5]=A, [6]=Money, [7]=Ult
            if (texts.Length >= 8)
            {
                texts[0].text = ping + "ms";
                texts[1].text = heroName;
                texts[2].text = playerName;
                texts[3].text = kills;
                texts[4].text = deaths;
                texts[5].text = assists;
                texts[6].text = money;
                texts[7].text = ult;
            }
            else if (texts.Length >= 1)
            {
                texts[0].text = $"{ping}ms | {heroName} | {playerName} | K:{kills} D:{deaths} A:{assists} | {money} | {ult}";
            }
        }

        private void ClearContainer(Transform container)
        {
            if (container == null) return;
            for (int i = container.childCount - 1; i >= 0; i--)
            {
                Destroy(container.GetChild(i).gameObject);
            }
        }
    }
}
