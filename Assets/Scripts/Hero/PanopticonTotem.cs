using FishNet.Object;
using ProjectZ.Core;
using ProjectZ.GameMode;
using ProjectZ.Player;
using System.Collections;
using UnityEngine;

namespace ProjectZ.Hero.Sector
{
    /// <summary>
    /// Sector's Ultimate: Panopticon (GDD Section 8)
    /// Spawns a physical Radar Totem at the player's position.
    /// </summary>
    public class PanopticonTotem : UltimateAbility
    {
        [Header("Settings")]
        [SerializeField] private GameObject _totemPrefab;
        [SerializeField] private float _spawnForwardOffset = 1.0f;
        [SerializeField] private float _scanRadius = 30.0f;
        [SerializeField] private float _scanInterval = 1.0f;
        [SerializeField] private float _lifeTime = 10.0f;
        [SerializeField] private LayerMask _playerLayer;

        [Server]
        public override void Activate()
        {
            if (OwnerController == null) return;

            // Calculate spawn position (slightly in front of the player, on the ground)
            Vector3 spawnPos = CasterTransform.position + CasterTransform.forward * _spawnForwardOffset;
            
            // Adjust to ground level if necessary via Raycast, but assuming transform.position is roughly feet level for now.

            if (_totemPrefab != null)
            {
                GameObject totemObj = Instantiate(_totemPrefab, spawnPos, Quaternion.identity);
                ServerManager.Spawn(totemObj, OwnerController.Owner);

                TotemBehaviour behaviour = totemObj.GetComponent<TotemBehaviour>();
                if (behaviour != null)
                {
                    behaviour.Initialize(OwnerController.OwnerId);
                }
            }
            else
            {
                GameObject fallbackTotem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                fallbackTotem.name = "PanopticonTotem";
                fallbackTotem.transform.position = spawnPos;
                fallbackTotem.transform.localScale = new Vector3(0.5f, 0.75f, 0.5f);

                Collider collider = fallbackTotem.GetComponent<Collider>();
                if (collider != null)
                    collider.isTrigger = false;

                PanopticonScanner scanner = fallbackTotem.AddComponent<PanopticonScanner>();
                Team ownerTeam = TeamManager.Instance != null ? TeamManager.Instance.GetTeam(OwnerController.OwnerId) : Team.None;
                scanner.Initialize(OwnerController.OwnerId, ownerTeam, _scanRadius, _scanInterval, _lifeTime, ResolveLayerMask(_playerLayer));
            }
        }
    }

    internal sealed class PanopticonScanner : MonoBehaviour
    {
        private int _ownerId = -1;
        private Team _ownerTeam = Team.None;
        private float _scanRadius = 30f;
        private float _scanInterval = 1f;
        private float _lifeTime = 10f;
        private int _playerMask = Physics.AllLayers;

        public void Initialize(int ownerId, Team ownerTeam, float scanRadius, float scanInterval, float lifeTime, int playerMask)
        {
            _ownerId = ownerId;
            _ownerTeam = ownerTeam;
            _scanRadius = scanRadius;
            _scanInterval = scanInterval;
            _lifeTime = lifeTime;
            _playerMask = playerMask;

            StartCoroutine(ScanRoutine());
            StartCoroutine(SelfDestructRoutine());
        }

        private IEnumerator ScanRoutine()
        {
            while (true)
            {
                ScanPulse();
                yield return new WaitForSeconds(_scanInterval);
            }
        }

        private IEnumerator SelfDestructRoutine()
        {
            yield return new WaitForSeconds(_lifeTime);
            Destroy(gameObject);
        }

        private void ScanPulse()
        {
            TeamManager teamManager = TeamManager.Instance;
            if (teamManager == null)
                return;

            if (!FishNet.Managing.NetworkManager.Instances[0].ServerManager.Clients.TryGetValue(_ownerId, out var ownerConn))
                return;

            Collider[] hits = Physics.OverlapSphere(transform.position, _scanRadius, _playerMask);
            foreach (Collider hit in hits)
            {
                FishNet.Object.NetworkObject targetNetworkObject = hit.GetComponentInParent<FishNet.Object.NetworkObject>();
                if (targetNetworkObject == null || !targetNetworkObject.Owner.IsValid)
                    continue;

                Team targetTeam = teamManager.GetTeam(targetNetworkObject.OwnerId);
                if (targetTeam == _ownerTeam || targetTeam == Team.None)
                    continue;

                OutlineController outline = targetNetworkObject.GetComponent<OutlineController>();
                if (outline != null)
                    outline.TargetShowOutline(ownerConn, _scanInterval);
            }
        }
    }
}
