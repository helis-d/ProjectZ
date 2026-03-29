using System.Collections.Generic;
using UnityEngine;

namespace ProjectZ.Combat
{
    /// <summary>
    /// Result of a hitbox ray test — which zone was hit and at what multiplier.
    /// </summary>
    public struct HitResult
    {
        public bool    DidHit;
        public HitboxZone Zone;
        public float   DamageMultiplier;
        public Vector3 HitPoint;
    }

    /// <summary>
    /// Manages all HitboxCapsule children on a player and implements the
    /// GDD Section 6 hit priority system:
    ///   "A bullet can penetrate multiple capsules (e.g., hand then head).
    ///    Priority Rule: Zone with higher damage multiplier is selected."
    /// </summary>
    public class HitboxManager : MonoBehaviour
    {
        private HitboxCapsule[] _capsules;

        private void Awake()
        {
            _capsules = GetComponentsInChildren<HitboxCapsule>();
        }

        /// <summary>
        /// Test a ray against all capsules. Returns the hit result with
        /// the HIGHEST damage multiplier zone (hit priority rule).
        /// </summary>
        public HitResult ProcessHit(Vector3 rayOrigin, Vector3 rayDir)
        {
            HitResult best = new HitResult { DidHit = false, DamageMultiplier = 0f };

            foreach (var capsule in _capsules)
            {
                if (capsule.CheckRayHit(rayOrigin, rayDir, out float sqrDist))
                {
                    float mult = capsule.DamageMultiplier;

                    // Hit priority: select the zone with the highest multiplier
                    if (mult > best.DamageMultiplier)
                    {
                        best.DidHit           = true;
                        best.Zone             = capsule.Zone;
                        best.DamageMultiplier = mult;

                        // Approximate hit point (closest point on ray to capsule)
                        float dist = Mathf.Sqrt(sqrDist);
                        best.HitPoint = rayOrigin + rayDir *
                            Vector3.Distance(rayOrigin, capsule.AxisTop);
                    }
                }
            }

            return best;
        }

        /// <summary>
        /// Returns all capsules that the ray intersects, sorted by distance.
        /// Useful for penetration/multi-hit scenarios.
        /// </summary>
        public List<(HitboxCapsule capsule, float sqrDist)> GetAllHits(Vector3 rayOrigin, Vector3 rayDir)
        {
            var hits = new List<(HitboxCapsule capsule, float sqrDist)>();
            foreach (var capsule in _capsules)
            {
                if (capsule.CheckRayHit(rayOrigin, rayDir, out float sqrDist))
                    hits.Add((capsule, sqrDist));
            }
            hits.Sort((a, b) => a.sqrDist.CompareTo(b.sqrDist));
            return hits;
        }
    }
}
