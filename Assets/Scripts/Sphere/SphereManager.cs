using FishNet.Object;
using FishNet.Object.Synchronizing;
using ProjectZ.Combat;
using ProjectZ.Core;
using ProjectZ.GameMode;
using ProjectZ.Player;
using UnityEngine;

namespace ProjectZ.Sphere
{
    /// <summary>
    /// Server-authoritative Sphere logic manager.
    /// Implements plant/defuse/detonate state machine from GDD Section 7.
    /// </summary>
    public class SphereManager : NetworkBehaviour
    {
        public static SphereManager Instance { get; private set; }

        [Header("GDD Timings (Seconds)")]
        [SerializeField] private float _plantTime = 4.0f;
        [SerializeField] private float _defuseTime = 7.0f;
        [SerializeField] private float _defuseTimeKit = 3.5f;
        [SerializeField] private float _detonateTime = 45.0f;
        [SerializeField] private float _killRadius = 35.0f;
        [SerializeField] private float _interactionDistance = 3.0f;

        public readonly SyncVar<SphereState> CurrentState   = new(SphereState.Idle);
        public readonly SyncVar<string>       PlantedSiteId  = new(string.Empty);
        public readonly SyncVar<float>        Timer          = new(0f);

        private bool _isPlanted;
        private int _activePlanterId = -1;
        private int _activeDefuserId = -1;
        private float _defuseStartTime;
        private bool _defuseUsedKit;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Update()
        {
            if (!IsServerInitialized)
                return;

            if (CurrentState.Value == SphereState.Active && _isPlanted)
            {
                Timer.Value -= Time.deltaTime;
                if (Timer.Value <= 0f)
                    Detonate();
            }
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            GameEvents.OnRoundStart += ResetSphere;
        }

        public override void OnStopServer()
        {
            base.OnStopServer();
            GameEvents.OnRoundStart -= ResetSphere;
        }

        [Server]
        public bool TryStartPlant(int connId, string siteId)
        {
            if (CurrentState.Value != SphereState.Idle)
                return false;

            RoundManager roundManager = RoundManager.Instance;
            if (roundManager != null && roundManager.CurrentState.Value != RoundManager.RoundState.ActionPhase)
                return false;

            TeamManager tm = TeamManager.Instance;
            if (tm == null || tm.GetTeam(connId) != Team.Attacker)
                return false;

            if (!IsValidSite(siteId))
                return false;

            if (!IsPlayerNearSite(connId, siteId))
                return false;

            _activePlanterId = connId;
            CurrentState.Value = SphereState.Planting;
            return true;
        }

        [Server]
        public void CancelPlant(int connId)
        {
            if (CurrentState.Value == SphereState.Planting && _activePlanterId == connId)
            {
                CurrentState.Value = SphereState.Idle;
                _activePlanterId = -1;
            }
        }

        [Server]
        public void ConfirmPlant(int connId, string siteId, Vector3 position)
        {
            if (CurrentState.Value != SphereState.Planting)
                return;

            if (_activePlanterId < 0 || _activePlanterId != connId)
                return;

            if (!IsValidSite(siteId) || !IsPlayerNearSite(_activePlanterId, siteId))
            {
                CancelPlant(connId);
                return;
            }

            CurrentState.Value = SphereState.Active;
            PlantedSiteId.Value = siteId;
            Timer.Value = _detonateTime;
            _isPlanted = true;
            _activePlanterId = -1;

            SphereSite site = FindSite(siteId);
            transform.position = site != null ? site.transform.position : position;

            GameEvents.InvokeSpherePlanted(siteId);
            Debug.Log($"[Sphere] Planted at {siteId}. Detonating in {_detonateTime:F0}s.");
        }

        [Server]
        public bool TryStartDefuse(int connId)
        {
            if (CurrentState.Value != SphereState.Active)
                return false;

            TeamManager tm = TeamManager.Instance;
            if (tm == null || tm.GetTeam(connId) != Team.Defender)
                return false;

            if (!IsPlayerNearSphere(connId))
                return false;

            _defuseUsedKit = TryGetDefuseKit(connId, out bool kit) && kit;
            _defuseStartTime = Time.time;
            _activeDefuserId = connId;
            CurrentState.Value = SphereState.Defusing;
            return true;
        }

        [Server]
        public void CancelDefuse(int connId)
        {
            if (CurrentState.Value == SphereState.Defusing && _activeDefuserId == connId)
            {
                CurrentState.Value = SphereState.Active;
                _activeDefuserId = -1;
                _defuseStartTime = 0f;
            }
        }

        [Server]
        public void ConfirmDefuse(int connId)
        {
            if (CurrentState.Value != SphereState.Defusing)
                return;

            if (_activeDefuserId < 0 || _activeDefuserId != connId || !IsPlayerNearSphere(_activeDefuserId))
            {
                CancelDefuse(connId);
                return;
            }

            float required = GetDefuseTime(_defuseUsedKit);
            if (Time.time < _defuseStartTime + required - 0.08f)
            {
                CancelDefuse(connId);
                return;
            }

            if (_defuseUsedKit && TryGetPlayerObject(_activeDefuserId, out GameObject playerObj))
            {
                PlayerEquipment equipment = playerObj.GetComponent<PlayerEquipment>();
                equipment?.ConsumeDefuseKitAfterSuccessfulDefuse();
            }

            CurrentState.Value = SphereState.Defused;
            _isPlanted = false;
            _activeDefuserId = -1;
            _defuseStartTime = 0f;

            GameEvents.InvokeSphereDefused();
            Debug.Log("[Sphere] Defused.");
        }

        [Server]
        private void Detonate()
        {
            CurrentState.Value = SphereState.Exploded;
            _isPlanted = false;
            _activeDefuserId = -1;
            _defuseStartTime = 0f;

            Collider[] colliders = Physics.OverlapSphere(transform.position, _killRadius);
            foreach (Collider col in colliders)
            {
                ProjectZ.Player.PlayerHealth health = col.GetComponentInParent<ProjectZ.Player.PlayerHealth>();
                if (health != null)
                {
                    DamageProcessor damageProcessor = health.GetComponent<DamageProcessor>();
                    if (damageProcessor != null)
                        damageProcessor.ProcessEnvironmentalDamage(9999f, health, "sphere_detonation");
                }
            }

            GameEvents.InvokeSphereDetonated();
            Debug.Log("[Sphere] Detonated.");
        }

        [Server]
        private void ResetSphere(int _)
        {
            CurrentState.Value = SphereState.Idle;
            PlantedSiteId.Value = string.Empty;
            Timer.Value = 0f;
            _isPlanted = false;
            _activePlanterId = -1;
            _activeDefuserId = -1;
            _defuseStartTime = 0f;
        }

        private bool IsValidSite(string siteId)
        {
            return FindSite(siteId) != null;
        }

        private SphereSite FindSite(string siteId)
        {
            SphereSite[] sites = FindObjectsByType<SphereSite>(FindObjectsSortMode.None);
            foreach (SphereSite site in sites)
            {
                if (site != null && site.SiteID == siteId)
                    return site;
            }
            return null;
        }

        private bool IsPlayerNearSite(int connId, string siteId)
        {
            if (!TryGetPlayerObject(connId, out GameObject player))
                return false;

            SphereSite site = FindSite(siteId);
            if (site == null)
                return false;

            return Vector3.Distance(player.transform.position, site.transform.position) <= _interactionDistance + 2f;
        }

        private bool IsPlayerNearSphere(int connId)
        {
            if (!TryGetPlayerObject(connId, out GameObject player))
                return false;

            return Vector3.Distance(player.transform.position, transform.position) <= _interactionDistance;
        }

        private bool TryGetPlayerObject(int connId, out GameObject player)
        {
            player = null;
            if (!ServerManager.Clients.TryGetValue(connId, out var conn) || conn.FirstObject == null)
                return false;

            player = conn.FirstObject.gameObject;
            return true;
        }

        private bool TryGetDefuseKit(int connId, out bool hasKit)
        {
            hasKit = false;
            if (!ServerManager.Clients.TryGetValue(connId, out var conn) || conn.FirstObject == null)
                return false;

            PlayerEquipment equipment = conn.FirstObject.GetComponent<PlayerEquipment>();
            if (equipment == null)
                return false;

            hasKit = equipment.HasDefuseKit.Value;
            return true;
        }

        public float PlantTime => _plantTime;
        public float GetDefuseTime(bool hasKit) => hasKit ? _defuseTimeKit : _defuseTime;
    }
}
