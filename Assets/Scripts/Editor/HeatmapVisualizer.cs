#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using ProjectZ.Network;

namespace ProjectZ.EditorTools
{
    public class HeatmapVisualizer : EditorWindow
    {
        private HeatmapTelemetryData _data;
        private bool _isVisualizing = false;

        [MenuItem("Tools/ProjectZ/Show Heatmap")]
        public static void ShowWindow()
        {
            GetWindow<HeatmapVisualizer>("Heatmap Visualizer");
        }

        private void OnGUI()
        {
            GUILayout.Label("Match Telemetry Heatmap", EditorStyles.boldLabel);

            if (GUILayout.Button("Load Latest Match Data"))
            {
                LoadData();
            }

            if (_data != null)
            {
                int killCount = _data.killEvents != null ? _data.killEvents.Count : 0;
                GUILayout.Label($"Kills Recorded: {killCount}");
                GUILayout.Label($"Match Date: {_data.date}");

                _isVisualizing = GUILayout.Toggle(_isVisualizing, "Enable Scene Visualization");
                
                if (GUI.changed)
                {
                    SceneView.RepaintAll();
                }
            }
        }

        private void LoadData()
        {
            string filePath = Path.Combine(Application.dataPath, "../Logs/Match_Heatmap.json");
            
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                _data = JsonUtility.FromJson<HeatmapTelemetryData>(json);
                if (_data != null && _data.killEvents == null)
                    _data.killEvents = new List<HeatmapEvent>();

                int killCount = _data != null && _data.killEvents != null ? _data.killEvents.Count : 0;
                Debug.Log($"[HeatmapVisualizer] Loaded {killCount} kill events.");
            }
            else
            {
                Debug.LogWarning("[HeatmapVisualizer] No heatmap data found at " + filePath);
                _data = null;
            }
        }

        private void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (!_isVisualizing || _data == null || _data.killEvents == null) return;

            foreach (var evt in _data.killEvents)
            {
                // Draw Victim Marker (Red Dot)
                Handles.color = new Color(1f, 0f, 0f, 0.6f);
                Handles.DrawSolidDisc(evt.victimPos, Vector3.up, 0.5f);

                // Draw Killer Marker (Green Dot)
                Handles.color = new Color(0f, 1f, 0f, 0.6f);
                Handles.DrawSolidDisc(evt.killerPos, Vector3.up, 0.5f);

                // Draw Line connecting Killer to Victim
                Handles.color = new Color(1f, 1f, 0f, 0.3f);
                Handles.DrawDottedLine(evt.killerPos, evt.victimPos, 2f);
            }
        }
    }
}
#endif
