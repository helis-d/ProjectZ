using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object;
using ProjectZ.Core;
using UnityEngine;

namespace ProjectZ.GameMode
{
    /// <summary>
    /// Manages team assignment, spawn point selection, and team membership lookup.
    /// Runs on the server.
    /// </summary>
    public class TeamManager : NetworkBehaviour
    {
        public static TeamManager Instance { get; private set; }

        [Header("Spawn Points")]
        [SerializeField] private Transform[] _attackerSpawns;
        [SerializeField] private Transform[] _defenderSpawns;

        private readonly Dictionary<int, Team> _playerTeams = new();
        private List<int> _attackers = new();
        private List<int> _defenders = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>Auto-balance a new player into the smaller team.</summary>
        public Team AssignTeam(NetworkConnection conn)
        {
            if (!IsServerInitialized) return Team.None;

            Team assigned = _attackers.Count <= _defenders.Count ? Team.Attacker : Team.Defender;

            _playerTeams[conn.ClientId] = assigned;
            if (assigned == Team.Attacker)
                _attackers.Add(conn.ClientId);
            else
                _defenders.Add(conn.ClientId);

            Debug.Log($"[TeamManager] Player {conn.ClientId} -> {assigned}");
            return assigned;
        }

        /// <summary>Remove a player from team tracking on disconnect.</summary>
        public void RemovePlayer(int clientId)
        {
            _playerTeams.Remove(clientId);
            _attackers.Remove(clientId);
            _defenders.Remove(clientId);
        }

        /// <summary>Swap teams (called after half-time).</summary>
        public void SwapTeams()
        {
            foreach (int id in _attackers)
                _playerTeams[id] = Team.Defender;

            foreach (int id in _defenders)
                _playerTeams[id] = Team.Attacker;

            List<int> tmp = _attackers;
            _attackers = _defenders;
            _defenders = tmp;

            Debug.Log("[TeamManager] Teams swapped.");
        }

        public Team GetTeam(int clientId)
        {
            return _playerTeams.TryGetValue(clientId, out Team team) ? team : Team.None;
        }

        public Transform GetSpawnPoint(Team team)
        {
            Transform[] pool = team == Team.Attacker ? _attackerSpawns : _defenderSpawns;
            if (pool == null || pool.Length == 0)
                return null;

            int randomIndex = Random.Range(0, pool.Length);
            return pool[randomIndex];
        }

        public IReadOnlyList<int> Attackers => _attackers;
        public IReadOnlyList<int> Defenders => _defenders;

        /// <summary>Inject dynamic JSON-based spawn points.</summary>
        public void SetDynamicSpawns(Transform[] atkSpawns, Transform[] defSpawns)
        {
            if (atkSpawns != null && atkSpawns.Length > 0)
                _attackerSpawns = atkSpawns;

            if (defSpawns != null && defSpawns.Length > 0)
                _defenderSpawns = defSpawns;
        }
    }
}
