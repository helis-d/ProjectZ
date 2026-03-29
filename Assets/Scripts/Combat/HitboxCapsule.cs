using UnityEngine;

namespace ProjectZ.Combat
{
    /// <summary>
    /// A single hitbox capsule attached as a child of the player rig.
    /// Each capsule maps to a body zone (GDD Section 6) and follows a bone.
    ///
    /// Capsule-ray collision uses the GDD algorithm:
    ///   1. Shortest distance between bullet ray and capsule axis
    ///   2. Hit if distance ≤ radius
    ///   3. Sqrt only at final step (optimization)
    /// </summary>
    public class HitboxCapsule : MonoBehaviour
    {
        [Header("Zone Configuration")]
        [SerializeField] private HitboxZone _zone = HitboxZone.UpperChest;

        [Tooltip("Override radius. If 0, uses default from HitboxZoneData.")]
        [SerializeField] private float _radiusOverride = 0f;

        [Header("Bone References")]
        [Tooltip("Top of the capsule axis (e.g. head top).")]
        [SerializeField] private Transform _boneTop;

        [Tooltip("Bottom of the capsule axis (e.g. neck base). If null, point capsule (sphere) is used.")]
        [SerializeField] private Transform _boneBottom;

        // ─── Properties ───────────────────────────────────────────────────
        public HitboxZone Zone            => _zone;
        public float DamageMultiplier     => HitboxZoneData.GetMultiplier(_zone);
        public float Radius               => _radiusOverride > 0f
                                                ? _radiusOverride
                                                : HitboxZoneData.GetDefaultRadius(_zone);

        /// <summary>Top point of capsule axis in world space.</summary>
        public Vector3 AxisTop    => _boneTop != null ? _boneTop.position : transform.position;

        /// <summary>Bottom point of capsule axis in world space.</summary>
        public Vector3 AxisBottom => _boneBottom != null ? _boneBottom.position : transform.position;

        // ─── Hit Test ─────────────────────────────────────────────────────
        /// <summary>
        /// Test if a ray hits this capsule. Returns true if hit, with
        /// the squared distance for comparison (avoids sqrt per GDD optimization).
        /// </summary>
        /// <param name="rayOrigin">Ray start point.</param>
        /// <param name="rayDir">Normalized ray direction.</param>
        /// <param name="sqrDistance">Squared shortest distance between ray and capsule axis.</param>
        public bool CheckRayHit(Vector3 rayOrigin, Vector3 rayDir, out float sqrDistance)
        {
            sqrDistance = float.MaxValue;

            Vector3 p1 = AxisTop;
            Vector3 p2 = AxisBottom;

            // Capsule axis direction
            Vector3 d1 = rayDir;                    // ray direction
            Vector3 d2 = (p2 - p1);                 // capsule axis
            Vector3 w  = rayOrigin - p1;

            float a = Vector3.Dot(d1, d1);           // always 1 if normalized
            float b = Vector3.Dot(d1, d2);
            float c = Vector3.Dot(d2, d2);
            float d = Vector3.Dot(d1, w);
            float e = Vector3.Dot(d2, w);

            float denom = a * c - b * b;

            float sc, tc;
            if (denom < 1e-6f)
            {
                // Lines are nearly parallel
                sc = 0f;
                tc = (b > c) ? d / b : e / c;
            }
            else
            {
                sc = (b * e - c * d) / denom;
                tc = (a * e - b * d) / denom;
            }

            // Clamp tc to [0,1] (capsule segment)
            tc = Mathf.Clamp01(tc);

            // Clamp sc to >= 0 (ray, not line)
            if (sc < 0f) sc = 0f;

            // Closest points
            Vector3 closestOnRay     = rayOrigin + d1 * sc;
            Vector3 closestOnCapsule = p1 + d2 * tc;

            Vector3 diff = closestOnRay - closestOnCapsule;
            sqrDistance = diff.sqrMagnitude;

            float r = Radius;
            return sqrDistance <= r * r;
        }

        // ─── Debug ────────────────────────────────────────────────────────
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = _zone switch
            {
                HitboxZone.Head or HitboxZone.Neck => Color.red,
                HitboxZone.UpperChest or HitboxZone.Stomach => Color.yellow,
                _ => Color.cyan
            };

            Vector3 top = AxisTop;
            Vector3 bot = AxisBottom;
            float r = Radius;

            Gizmos.DrawWireSphere(top, r);
            Gizmos.DrawWireSphere(bot, r);
            Gizmos.DrawLine(top, bot);
        }
    }
}
