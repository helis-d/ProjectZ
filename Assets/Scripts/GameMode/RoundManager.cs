using System.Collections;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using ProjectZ.Core;
using ProjectZ.Sphere;
using UnityEngine;

namespace ProjectZ.GameMode
{
    /// <summary>
    /// Server-authoritative round state machine.
    ///
    /// States (GDD Section 7):
    ///   Idle -> BuyPhase (20s) -> ActionPhase -> EndPhase (5-7s) -> next round
    ///
    /// Only the server drives transitions. Clients observe CurrentState.Value + RoundNumber.Value through SyncVars.
    /// </summary>
    public class RoundManager : NetworkBehaviour
    {
        public static RoundManager Instance { get; private set; }

        [Header("Phase Durations (seconds)")]
        [SerializeField] private float _buyPhaseDuration = 20f;
        [SerializeField] private float _endPhaseDuration = 6f;

        public readonly SyncVar<RoundState> CurrentState = new(RoundState.Idle);
        public readonly SyncVar<int>         RoundNumber  = new(0);

        private BaseGameMode _gameMode;
        private Coroutine _matchCoroutine;
        private bool _forceEndRequested;
        private Team _forcedWinner = Team.None;
        private bool _roundEnded;
        private bool _matchEnded;

        public enum RoundState { Idle, BuyPhase, ActionPhase, EndPhase }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            GameEvents.OnMatchEnd += HandleMatchEnd;
            StartCoroutine(DeferredStartMatch());
        }

        public override void OnStopServer()
        {
            base.OnStopServer();
            GameEvents.OnMatchEnd -= HandleMatchEnd;
        }

        private IEnumerator DeferredStartMatch()
        {
            // Delay one frame so mode components can finalize their server init values.
            yield return null;
            if (_matchCoroutine == null)
                StartMatch();
        }

        /// <summary>Start the match. Call on the server only.</summary>
        public void StartMatch()
        {
            if (!IsServerInitialized)
            {
                Debug.LogWarning("[RoundManager] StartMatch() called on non-server.");
                return;
            }

            _gameMode = GetComponent<BaseGameMode>();
            if (_matchCoroutine != null)
                StopCoroutine(_matchCoroutine);

            _matchEnded = false;
            _matchCoroutine = StartCoroutine(RunMatch());
        }

        /// <summary>
        /// Force the current round to end with the given winning team.
        /// Called by game mode systems when a win condition is met.
        /// </summary>
        public void ForceEndRound(Team winner)
        {
            if (!IsServerInitialized || _roundEnded)
                return;

            _forceEndRequested = true;
            _forcedWinner = winner;
        }

        private IEnumerator RunMatch()
        {
            while (!_matchEnded)
            {
                int nextRoundNumber = RoundNumber.Value + 1;
                bool canStartNextRound = _gameMode != null
                    ? _gameMode.CanStartRound(nextRoundNumber)
                    : nextRoundNumber <= 13;

                if (!canStartNextRound)
                    break;

                RoundNumber.Value = nextRoundNumber;
                yield return StartCoroutine(RunRound());
            }

            Debug.Log("[RoundManager] Match over.");
        }

        private IEnumerator RunRound()
        {
            _forceEndRequested = false;
            _forcedWinner = Team.None;
            _roundEnded = false;

            SetState(RoundState.BuyPhase);
            GameEvents.InvokeBuyPhaseStart(_buyPhaseDuration);
            _gameMode?.OnRoundStart(RoundNumber.Value);
            GameEvents.InvokeRoundStart(RoundNumber.Value);

            float buyRemaining = _buyPhaseDuration;
            while (buyRemaining > 0f && !_forceEndRequested)
            {
                buyRemaining -= Time.deltaTime;
                yield return null;
            }

            if (_forceEndRequested)
            {
                yield return StartCoroutine(EndRoundSequence(_forcedWinner));
                yield break;
            }

            SetState(RoundState.ActionPhase);
            GameEvents.InvokeActionPhaseStart();

            float actionRemaining = _gameMode != null ? _gameMode.RoundTimeLimit : 105f;
            while (actionRemaining > 0f && !_forceEndRequested)
            {
                actionRemaining -= Time.deltaTime;
                yield return null;
            }

            if (_forceEndRequested)
            {
                yield return StartCoroutine(EndRoundSequence(_forcedWinner));
                yield break;
            }

            if (IsSpherePlanted())
            {
                // If sphere is active after timer expires, wait for detonate/defuse outcome events.
                while (!_forceEndRequested && IsSpherePlanted())
                    yield return null;

                if (_forceEndRequested)
                {
                    yield return StartCoroutine(EndRoundSequence(_forcedWinner));
                    yield break;
                }
            }

            yield return StartCoroutine(EndRoundSequence(Team.Defender));
        }

        private IEnumerator EndRoundSequence(Team winner)
        {
            if (_roundEnded)
                yield break;

            _roundEnded = true;
            SetState(RoundState.EndPhase);
            GameEvents.InvokeEndPhaseStart(_endPhaseDuration);
            _gameMode?.OnRoundEnd(winner, RoundNumber.Value);
            GameEvents.InvokeRoundEnd(winner, RoundNumber.Value);

            yield return new WaitForSeconds(_endPhaseDuration);
            SetState(RoundState.Idle);
        }

        private void SetState(RoundState state)
        {
            CurrentState.Value = state;
            OnStateChanged(state);
        }

        [ObserversRpc]
        private void OnStateChanged(RoundState state)
        {
            Debug.Log($"[RoundManager] Round {RoundNumber.Value} -> {state}");
        }

        private bool IsSpherePlanted()
        {
            SphereManager sm = SphereManager.Instance;
            if (sm == null)
                return false;

            return sm.CurrentState.Value == SphereState.Active || sm.CurrentState.Value == SphereState.Defusing;
        }

        private void HandleMatchEnd(Team _)
        {
            _matchEnded = true;
        }
    }
}

