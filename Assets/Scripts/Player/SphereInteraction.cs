using FishNet.Object;
using ProjectZ.Core;
using ProjectZ.GameMode;
using ProjectZ.Sphere;
using UnityEngine;

namespace ProjectZ.Player
{
    /// <summary>
    /// Player component handling input and local progress for planting/defusing the Sphere.
    /// Evaluates if the player is in a valid site and on the correct team.
    /// </summary>
    [RequireComponent(typeof(PlayerInputHandler))]
    [RequireComponent(typeof(PlayerEquipment))]
    public class SphereInteraction : NetworkBehaviour
    {
        // ─── State ────────────────────────────────────────────────────────
        private PlayerInputHandler _input;
        private PlayerEquipment      _equipment;
        private TeamManager        _teamManager;
        private SphereManager      _sphereManager;

        private SphereSite _currentSite;
        private bool       _isInteracting;
        private float      _interactionTimer;

        // Local progress (0.0 to 1.0) for UI
        public float Progress => Mathf.Clamp01(_interactionTimer / Mathf.Max(0.01f, GetRequiredTime()));

        private void Awake()
        {
            _input = GetComponent<PlayerInputHandler>();
            _equipment = GetComponent<PlayerEquipment>();
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            if (!IsOwner) enabled = false;
        }

        private void Start()
        {
            _teamManager   = FindFirstObjectByType<TeamManager>();
            _sphereManager = SphereManager.Instance;
        }

        private void Update()
        {
            if (!IsOwner || _sphereManager == null || _teamManager == null) return;

            bool interactHeld = _input != null && _input.InteractHeld;

            Team myTeam = _teamManager.GetTeam(OwnerId);

            if (interactHeld)
            {
                if (!_isInteracting)
                {
                    TryStartInteraction(myTeam);
                }
                else
                {
                    if (!CanContinueInteraction(myTeam))
                    {
                        CancelInteraction(myTeam);
                        return;
                    }

                    _interactionTimer += Time.deltaTime;
                    if (_interactionTimer >= GetRequiredTime())
                    {
                        CompleteInteraction(myTeam);
                    }
                }
            }
            else
            {
                if (_isInteracting)
                {
                    CancelInteraction(myTeam);
                }
            }
        }

        // ─── Triggers from SphereSite ─────────────────────────────────────
        public void EnterSite(SphereSite site) => _currentSite = site;
        public void ExitSite(SphereSite site)
        {
            if (_currentSite == site)
                _currentSite = null;
        }

        // ─── Internal Logic ───────────────────────────────────────────────

        private void TryStartInteraction(Team myTeam)
        {
            if (myTeam == Team.Attacker && _currentSite != null && CanContinueInteraction(myTeam))
            {
                // Attackers can plant if in a site and Idle
                CmdStartPlant(_currentSite.SiteID);
                _isInteracting = true;
                _interactionTimer = 0f;
            }
            else if (myTeam == Team.Defender && CanContinueInteraction(myTeam))
            {
                // Defenders can defuse if Active
                CmdStartDefuse();
                _isInteracting = true;
                _interactionTimer = 0f;
            }
        }

        private void CancelInteraction(Team myTeam)
        {
            _isInteracting = false;
            _interactionTimer = 0f;

            if (myTeam == Team.Attacker)
                CmdCancelPlant();
            else if (myTeam == Team.Defender)
                CmdCancelDefuse();
        }

        private void CompleteInteraction(Team myTeam)
        {
            _isInteracting = false;
            _interactionTimer = 0f;

            if (myTeam == Team.Attacker && _currentSite != null)
                CmdConfirmPlant(_currentSite.SiteID, transform.position);
            else if (myTeam == Team.Defender)
                CmdConfirmDefuse();
        }

        private float GetRequiredTime()
        {
            Team myTeam = _teamManager.GetTeam(OwnerId);
            if (myTeam == Team.Attacker)
                return _sphereManager.PlantTime;
            if (myTeam == Team.Defender)
            {
                bool hasKit = _equipment != null && _equipment.HasDefuseKit.Value;
                return _sphereManager.GetDefuseTime(hasKit);
            }

            return 999f;
        }

        private bool CanContinueInteraction(Team myTeam)
        {
            if (_sphereManager == null)
                return false;

            if (myTeam == Team.Attacker)
            {
                RoundManager roundManager = RoundManager.Instance;
                return _currentSite != null
                    && _sphereManager.CurrentState.Value == SphereState.Idle
                    && (roundManager == null || roundManager.CurrentState.Value == RoundManager.RoundState.ActionPhase);
            }

            if (myTeam == Team.Defender)
            {
                if (_sphereManager.CurrentState.Value != SphereState.Active && _sphereManager.CurrentState.Value != SphereState.Defusing)
                    return false;

                return Vector3.Distance(transform.position, _sphereManager.transform.position) <= 3.25f;
            }

            return false;
        }

        // ─── Server RPCs ──────────────────────────────────────────────────

        [ServerRpc]
        private void CmdStartPlant(string siteId)
        {
            if (_sphereManager != null)
                _sphereManager.TryStartPlant(OwnerId, siteId);
        }

        [ServerRpc]
        private void CmdCancelPlant()
        {
            if (_sphereManager != null)
                _sphereManager.CancelPlant(OwnerId);
        }

        [ServerRpc]
        private void CmdConfirmPlant(string siteId, Vector3 pos)
        {
            if (_sphereManager != null)
                _sphereManager.ConfirmPlant(OwnerId, siteId, pos);
        }

        [ServerRpc]
        private void CmdStartDefuse()
        {
            if (_sphereManager != null)
                _sphereManager.TryStartDefuse(OwnerId);
        }

        [ServerRpc]
        private void CmdCancelDefuse()
        {
            if (_sphereManager != null)
                _sphereManager.CancelDefuse(OwnerId);
        }

        [ServerRpc]
        private void CmdConfirmDefuse()
        {
            if (_sphereManager != null)
                _sphereManager.ConfirmDefuse(OwnerId);
        }
    }
}
