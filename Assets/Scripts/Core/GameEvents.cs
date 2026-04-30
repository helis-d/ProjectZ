using System;
using UnityEngine;

namespace ProjectZ.Core
{
    /// <summary>
    /// Global static EventBus. All systems communicate through here
    /// to avoid tight coupling between components.
    /// </summary>
    public static class GameEvents
    {
        // ─── Round Events ────────────────────────────────────────────────
        /// <summary>Fired on the server when a new round begins. Arg: round number (1-based).</summary>
        public static event Action<int> OnRoundStart;

        /// <summary>Fired on the server when a round ends. Args: winning team, round number.</summary>
        public static event Action<Team, int> OnRoundEnd;

        /// <summary>Fired when the buy phase begins. Arg: duration in seconds.</summary>
        public static event Action<float> OnBuyPhaseStart;

        /// <summary>Fired when the action phase begins.</summary>
        public static event Action OnActionPhaseStart;

        /// <summary>Fired when the end phase begins. Arg: duration in seconds.</summary>
        public static event Action<float> OnEndPhaseStart;

        /// <summary>Fired when the entire match ends. Arg: winning team.</summary>
        public static event Action<Team> OnMatchEnd;

        // ─── Player Events ────────────────────────────────────────────────
        /// <summary>Fired when a player dies. Args: victim connection id, killer connection id.</summary>
        public static event Action<int, int> OnPlayerDeath;

        /// <summary>Fired when a player is damaged. Args: victim connection id, attacker connection id, damage amount.</summary>
        public static event Action<int, int, float> OnPlayerDamaged;

        /// <summary>Fired when a player spawns. Arg: connection id.</summary>
        public static event Action<int> OnPlayerSpawned;

        /// <summary>Fired when a player connects to the server. Arg: connection id.</summary>
        public static event Action<int> OnPlayerConnected;

        /// <summary>Fired when a player disconnects. Arg: connection id.</summary>
        public static event Action<int> OnPlayerDisconnected;

        // ─── Sphere (Bomb) Events ─────────────────────────────────────────
        /// <summary>Fired when the sphere is successfully planted.</summary>
        public static event Action<string> OnSpherePlanted;   // arg: site id ("A", "B", "C")

        /// <summary>Fired when the sphere is defused.</summary>
        public static event Action OnSphereDefused;

        /// <summary>Fired when the sphere detonates.</summary>
        public static event Action OnSphereDetonated;

        // ─── Extended Kill / Assist Events ────────────────────────────────
        /// <summary>Fired after a kill with detailed info for killfeed. Args: killerId, victimId, weaponId, isHeadshot, isWallbang.</summary>
        public static event Action<int, int, string, bool, bool> OnKillDetails;

        /// <summary>Fired when a player earns an assist. Args: assisterId, victimId.</summary>
        public static event Action<int, int> OnPlayerAssist;

        // ─── Flashbang Event ──────────────────────────────────────────────
        /// <summary>
        /// Fired when a flashbang hits a player. Arg: normalizedIntensity (0-1).
        /// 1.0 = fully blinded (white-out), 0.0 = no effect.
        /// </summary>
        public static event Action<float> OnFlashbangHit;

        // ─── Sphere Timer Event ───────────────────────────────────────────
        /// <summary>
        /// Fired every server frame while the sphere is Active.
        /// Arg: remaining seconds on the detonation timer.
        /// </summary>
        public static event Action<float> OnSphereTimerTick;

        // ─── Invokers (called by owning systems only) ────────────────────
        public static void InvokeRoundStart(int round)          => OnRoundStart?.Invoke(round);
        public static void InvokeRoundEnd(Team winner, int round) => OnRoundEnd?.Invoke(winner, round);
        public static void InvokeBuyPhaseStart(float duration)  => OnBuyPhaseStart?.Invoke(duration);
        public static void InvokeActionPhaseStart()             => OnActionPhaseStart?.Invoke();
        public static void InvokeEndPhaseStart(float duration)  => OnEndPhaseStart?.Invoke(duration);
        public static void InvokeMatchEnd(Team winner)          => OnMatchEnd?.Invoke(winner);

        public static void InvokePlayerDeath(int victimId, int killerId) => OnPlayerDeath?.Invoke(victimId, killerId);
        public static void InvokePlayerDamaged(int victim, int attacker, float damage) => OnPlayerDamaged?.Invoke(victim, attacker, damage);
        public static void InvokePlayerSpawned(int connId)      => OnPlayerSpawned?.Invoke(connId);
        public static void InvokePlayerConnected(int connId)    => OnPlayerConnected?.Invoke(connId);
        public static void InvokePlayerDisconnected(int connId) => OnPlayerDisconnected?.Invoke(connId);

        public static void InvokeSpherePlanted(string siteId)   => OnSpherePlanted?.Invoke(siteId);
        public static void InvokeSphereDefused()                => OnSphereDefused?.Invoke();
        public static void InvokeSphereDetonated()              => OnSphereDetonated?.Invoke();

        public static void InvokeKillDetails(int killerId, int victimId, string weaponId, bool headshot, bool wallbang)
            => OnKillDetails?.Invoke(killerId, victimId, weaponId, headshot, wallbang);
        public static void InvokePlayerAssist(int assisterId, int victimId)
            => OnPlayerAssist?.Invoke(assisterId, victimId);
        public static void InvokeFlashbangHit(float intensity)
            => OnFlashbangHit?.Invoke(intensity);
        public static void InvokeSphereTimerTick(float remainingSeconds)
            => OnSphereTimerTick?.Invoke(remainingSeconds);
    }
}
