using System.Collections.Generic;
using FishNet.Object;
using ProjectZ.Core;
using ProjectZ.GameMode;
using ProjectZ.Sphere;
using UnityEngine;

namespace ProjectZ.Map
{
    /// <summary>
    /// Reads Map JSON data and dynamically instantiates sites, buy zones,
    /// and feeds spawn points to the TeamManager.
    /// Runs on Server only.
    /// </summary>
    [RequireComponent(typeof(TeamManager))]
    public class MapLoader : NetworkBehaviour
    {
        [Header("Data")]
        [SerializeField] private TextAsset _mapJsonFile;

        [Header("Prefabs to Spawn")]
        [SerializeField] private GameObject _sphereSitePrefab;
        [SerializeField] private GameObject _buyZonePrefab;
        [SerializeField] private GameObject _barrierPrefab;

        // ─── Parsed Data ──────────────────────────────────────────────────
        private MapDataConfig _currentMap;

        public override void OnStartServer()
        {
            base.OnStartServer();
            
            if (_mapJsonFile != null)
            {
                LoadMap(_mapJsonFile.text);
            }
            else
            {
                Debug.LogError("[MapLoader] No JSON file assigned!");
            }
        }

        private void LoadMap(string jsonString)
        {
            _currentMap = JsonUtility.FromJson<MapDataConfig>(jsonString);
            if (_currentMap == null) return;

            Debug.Log($"[MapLoader] Loaded Map: {_currentMap.map_name} ({_currentMap.map_id})");

            SpawnSites();
            SpawnBuyZonesAndBarriers();
            FeedSpawnPoints();
        }

        private void SpawnSites()
        {
            if (_currentMap.sites == null || _sphereSitePrefab == null) return;

            foreach (var site in _currentMap.sites)
            {
                // Convert cm vector to Unity metres
                Vector3 pos = new Vector3(site.center[0] * 0.01f, site.center[1] * 0.01f, site.center[2] * 0.01f);
                float radiusMetres = site.radius * 0.01f;

                GameObject obj = Instantiate(_sphereSitePrefab, pos, Quaternion.identity);
                ServerManager.Spawn(obj);

                // Configure site ID and trigger radius
                var sphereSite = obj.GetComponent<SphereSite>();
                if (sphereSite != null)
                {
                    sphereSite.SiteID = site.id;
                }

                // If uses SphereCollider, adjust radius
                var col = obj.GetComponent<SphereCollider>();
                if (col != null)
                {
                    col.radius = radiusMetres;
                }

                Debug.Log($"[MapLoader] Spawned Site {site.id} at {pos}");
            }
        }

        private void SpawnBuyZonesAndBarriers()
        {
            if (_currentMap.buy_zones == null) return;

            foreach (var zone in _currentMap.buy_zones)
            {
                Vector3 center = zone.GetCenter();
                Vector3 size = zone.GetSize();

                // Build BuyZone trigger
                if (_buyZonePrefab != null)
                {
                    GameObject bzObj = Instantiate(_buyZonePrefab, center, Quaternion.identity);
                    BoxCollider box = bzObj.GetComponent<BoxCollider>();
                    if (box == null)
                        box = bzObj.AddComponent<BoxCollider>();

                    box.size = size;
                    box.isTrigger = true;

                    BuyZone buyZone = bzObj.GetComponent<BuyZone>();
                    if (buyZone == null)
                        buyZone = bzObj.AddComponent<BuyZone>();

                    Team zoneTeam = zone.team != null && zone.team.ToLower() == "attacker"
                        ? Team.Attacker
                        : Team.Defender;
                    buyZone.Configure(zoneTeam);

                    ServerManager.Spawn(bzObj);
                }

                // Build Barrier walls surrounding the buy zone
                if (_barrierPrefab != null)
                {
                    GameObject barrierObj = Instantiate(_barrierPrefab, center, Quaternion.identity);
                    BoxCollider box = barrierObj.GetComponent<BoxCollider>();
                    if (box == null)
                        box = barrierObj.AddComponent<BoxCollider>();

                    box.size = size;
                    box.isTrigger = false;

                    if (barrierObj.GetComponent<BarrierSystem>() == null)
                        barrierObj.AddComponent<BarrierSystem>();

                    ServerManager.Spawn(barrierObj);
                }
            }
        }

        private void FeedSpawnPoints()
        {
            if (_currentMap.spawn_points == null) return;

            TeamManager teamMgr = GetComponent<TeamManager>();

            var atkSpawns = new List<Transform>();
            foreach (var sp in _currentMap.spawn_points.attackers)
            {
                GameObject dummy = new GameObject("AtkSpawn");
                dummy.transform.position = sp.GetPositionMetres();
                dummy.transform.rotation = sp.GetRotation();
                atkSpawns.Add(dummy.transform);
            }

            var defSpawns = new List<Transform>();
            foreach (var sp in _currentMap.spawn_points.defenders)
            {
                GameObject dummy = new GameObject("DefSpawn");
                dummy.transform.position = sp.GetPositionMetres();
                dummy.transform.rotation = sp.GetRotation();
                defSpawns.Add(dummy.transform);
            }

            // Replace TeamManager's static arrays with our dynamic JSON ones
            teamMgr.SetDynamicSpawns(atkSpawns.ToArray(), defSpawns.ToArray());
            Debug.Log($"[MapLoader] Fed {atkSpawns.Count} ATK and {defSpawns.Count} DEF spawns to TeamManager.");
        }
    }
}
