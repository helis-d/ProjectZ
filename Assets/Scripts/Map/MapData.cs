using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectZ.Map
{
    /// <summary>
    /// GDD Section 9: Data-Driven Map JSON Serialization Structures
    /// Coordinate unit in JSON is Centimetres.
    /// Needs conversion to Unity Metres (x * 0.01).
    /// </summary>
    [Serializable]
    public class MapDataConfig
    {
        public string map_id;
        public string map_name;
        public BoundsData bounds;
        public List<SiteData> sites;
        public SpawnPointsData spawn_points;
        public List<BuyZoneData> buy_zones;
    }

    [Serializable]
    public class BoundsData
    {
        public float[] min; // [x, y, z]
        public float[] max;
    }

    [Serializable]
    public class SiteData
    {
        public string id;       // e.g. "A", "B", "C"
        public float[] center;  // [x, y, z]
        public float radius;    // cm
    }

    [Serializable]
    public class SpawnPointsData
    {
        public List<TransformData> attackers;
        public List<TransformData> defenders;
    }

    [Serializable]
    public class TransformData
    {
        public float[] pos; // [x, y, z]
        public float[] rot; // euler angles [x, y, z]
        
        /// <summary>Helper to convert JSON cm array to Unity Vector3 (metres).</summary>
        public Vector3 GetPositionMetres()
        {
            if (pos == null || pos.Length < 3) return Vector3.zero;
            return new Vector3(pos[0] * 0.01f, pos[1] * 0.01f, pos[2] * 0.01f);
        }

        /// <summary>Helper to convert JSON rot array to Unity Quaternion.</summary>
        public Quaternion GetRotation()
        {
            if (rot == null || rot.Length < 3) return Quaternion.identity;
            return Quaternion.Euler(rot[0], rot[1], rot[2]);
        }
    }

    [Serializable]
    public class BuyZoneData
    {
        public string team; // "attacker" or "defender"
        public float[] bounds_min;
        public float[] bounds_max;

        public Vector3 GetMinMetres() => new Vector3(bounds_min[0] * 0.01f, bounds_min[1] * 0.01f, bounds_min[2] * 0.01f);
        public Vector3 GetMaxMetres() => new Vector3(bounds_max[0] * 0.01f, bounds_max[1] * 0.01f, bounds_max[2] * 0.01f);
        
        /// <summary>Get Unity center position of the bounds.</summary>
        public Vector3 GetCenter() => (GetMinMetres() + GetMaxMetres()) * 0.5f;

        /// <summary>Get Unity size of the bounds.</summary>
        public Vector3 GetSize() => GetMaxMetres() - GetMinMetres();
    }
}
