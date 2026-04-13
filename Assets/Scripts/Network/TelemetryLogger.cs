using System;
using System.Collections.Generic;
using System.IO;
using FishNet.Object;
using ProjectZ.Core;
using UnityEngine;

namespace ProjectZ.Network
{
    [Serializable]
    public struct HeatmapEvent
    {
        public Vector3 killerPos;
        public Vector3 victimPos;
        public float time;
    }

    [Serializable]
    public class MatchTelemetryData
    {
        public string matchId;
        public string date;
        public List<HeatmapEvent> killEvents = new List<HeatmapEvent>();
    }

    /// <summary>
    /// Listens to death events on the server, caches positions, 
    /// and flushes to a JSON file at match end for Editor Heatmap Visualization.
    /// </summary>
    public class TelemetryLogger : NetworkBehaviour
    {
        private MatchTelemetryData _currentData;
        private bool _isRecording = false;

        public override void OnStartServer()
        {
            base.OnStartServer();
            GameEvents.OnPlayerDeath += HandlePlayerDeath;
            GameEvents.OnMatchEnd += HandleMatchEnd;
            StartRecording();
        }

        public override void OnStopServer()
        {
            base.OnStopServer();
            GameEvents.OnPlayerDeath -= HandlePlayerDeath;
            GameEvents.OnMatchEnd -= HandleMatchEnd;
        }

        private void StartRecording()
        {
            _currentData = new MatchTelemetryData
            {
                matchId = Guid.NewGuid().ToString(),
                date = DateTime.UtcNow.ToString("o")
            };
            _isRecording = true;
            Debug.Log("[TelemetryLogger] Started recording match telemetry.");
        }

        private void HandlePlayerDeath(int victimId, int killerId)
        {
            if (!_isRecording || !IsServerInitialized) return;

            Vector3 vPos = Vector3.zero;
            Vector3 kPos = Vector3.zero;

            if (ServerManager.Clients.TryGetValue(victimId, out var victimConn) && victimConn.FirstObject != null)
            {
                vPos = victimConn.FirstObject.transform.position;
            }

            if (ServerManager.Clients.TryGetValue(killerId, out var killerConn) && killerConn.FirstObject != null)
            {
                kPos = killerConn.FirstObject.transform.position;
            }

            _currentData.killEvents.Add(new HeatmapEvent
            {
                victimPos = vPos,
                killerPos = kPos,
                time = Time.time
            });
        }

        private void HandleMatchEnd(Team winner)
        {
            if (!_isRecording) return;
            
            _isRecording = false;
            SaveTelemetryData();
        }

        private void SaveTelemetryData()
        {
            try
            {
                string json = JsonUtility.ToJson(_currentData, true);
                string logsDir = Path.Combine(Application.dataPath, "../Logs");
                
                if (!Directory.Exists(logsDir))
                {
                    Directory.CreateDirectory(logsDir);
                }

                string filePath = Path.Combine(logsDir, "Match_Heatmap.json");
                File.WriteAllText(filePath, json);
                
                Debug.Log($"[TelemetryLogger] Saved telemetry data with {_currentData.killEvents.Count} kills to: {filePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[TelemetryLogger] Failed to save telemetry: {e.Message}");
            }
        }
    }
}
