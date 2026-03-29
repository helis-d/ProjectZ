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
    public class SphereInteraction : NetworkBehaviour
    {
        // ─── State ────────────────────────────────────────────────────────
        private PlayerInputHandler _input;
        private TeamManager        _teamManager;
        private SphereManager      _sphereManager;

        private SphereSite _currentSite;
        private bool       _isInteracting;
        private float      _interactionTimer;
        private bool       _hasDefuseKit = false; // Stub for economy/equipment

        // Local progress (0.0 to 1.0) for UI
        public float Progress => _interactionTimer / GetRequiredTime();

        private void Awake()
        {
            _input = GetComponent<PlayerInputHandler>();
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

            bool tryingToInteract = _input.FireHeld; // Placeholder for interact key, or we can use another bind.
                                                     // GDD usually maps Plant/Defuse to '4' or 'F' holding.
                                                     // We assume FireHeld or ReloadPressed (Interact) is used. Let's use IsSprinting temporarily for this stub if no explicit Interact key exists.
                                                     // For precision, let's assume `_input.FireHeld` is used for now when holding the bomb.

            // Just mapped to an arbitrary button state that a player would hold
            // In a real project: `_input.InteractHeld`
            bool interactHeld = Input.GetKey(KeyCode.F);

            Team myTeam = _teamManager.GetTeam(OwnerId);

            if (interactHeld)
            {
                if (!_isInteracting)
                {
                    TryStartInteraction(myTeam);
                }
                else
                {
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
            if (myTeam == Team.Attacker && _currentSite != null)
            {
                // Attackers can plant if in a site and Idle
                CmdStartPlant(_currentSite.SiteID);
                _isInteracting = true;
                _interactionTimer = 0f;
            }
            else if (myTeam == Team.Defender && _sphereManager.CurrentState.Value == Sphere.SphereState.Active)
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
                return _sphereManager.GetDefuseTime(_hasDefuseKit);

            return 999f;
        }

        // ─── Server RPCs ──────────────────────────────────────────────────

        [ServerRpc]
        private void CmdStartPlant(string siteId) => _sphereManager.TryStartPlant(OwnerId, siteId);

        [ServerRpc]
        private void CmdCancelPlant() => _sphereManager.CancelPlant();

        [ServerRpc]
        private void CmdConfirmPlant(string siteId, Vector3 pos) => _sphereManager.ConfirmPlant(siteId, pos);

        [ServerRpc]
        private void CmdStartDefuse() => _sphereManager.TryStartDefuse(OwnerId);

        [ServerRpc]
        private void CmdCancelDefuse() => _sphereManager.CancelDefuse();

        [ServerRpc]
        private void CmdConfirmDefuse() => _sphereManager.ConfirmDefuse();
    }
}
