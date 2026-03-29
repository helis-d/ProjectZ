using System.Collections.Generic;
using UnityEngine;

namespace ProjectZ.Combat
{
    /// <summary>
    /// Result of a single hitscan trace including wallbang information.
    /// </summary>
    public struct HitscanResult
    {
        public bool DidHitPlayer;
        public HitResult HitboxResult;
        public float DamageMultiplier;
        public int WallsPenetrated;
        public Vector3 FinalHitPoint;
        public GameObject TargetObject; // Root object that owns the hitboxes.
        public List<Vector3> PenetrationPoints;
    }

    /// <summary>
    /// Hitscan raycast system with wallbang support.
    /// </summary>
    public class HitscanShooter : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float _maxRange = 200f;
        [SerializeField] private int _maxWallPenetrations = 3;
        [SerializeField] private LayerMask _hitMask = ~0;
        [SerializeField] private LayerMask _playerMask;

        [Header("Debug")]
        [SerializeField] private bool _drawDebugRays = true;
        [SerializeField] private float _debugRayDuration = 2f;

        private const float RAY_MARCH_STEP = 0.02f;
        private const float RAY_MARCH_MAX = 1.0f;

        /// <summary>
        /// Fire a hitscan ray from the given origin in the given direction.
        /// Returns a full trace result including wallbang data.
        /// </summary>
        public HitscanResult FireRay(Vector3 origin, Vector3 direction, float penetrationPower)
        {
            direction = direction.normalized;

            HitscanResult result = new HitscanResult
            {
                DidHitPlayer = false,
                DamageMultiplier = 1f,
                WallsPenetrated = 0,
                FinalHitPoint = origin,
                TargetObject = null,
                PenetrationPoints = new List<Vector3>()
            };

            Vector3 currentOrigin = origin;
            float remainingPower = penetrationPower;
            float totalDistance = 0f;

            for (int i = 0; i <= _maxWallPenetrations; i++)
            {
                float maxDist = _maxRange - totalDistance;
                if (maxDist <= 0f)
                    break;

                if (!Physics.Raycast(currentOrigin, direction, out RaycastHit hit, maxDist, _hitMask))
                {
                    if (_drawDebugRays)
                        Debug.DrawRay(currentOrigin, direction * maxDist, Color.gray, _debugRayDuration);
                    break;
                }

                totalDistance += hit.distance;

                if (_drawDebugRays)
                    Debug.DrawLine(currentOrigin, hit.point, Color.yellow, _debugRayDuration);

                // Check player hit.
                HitboxManager hitboxMgr = hit.collider.GetComponentInParent<HitboxManager>();
                if (hitboxMgr != null)
                {
                    HitResult hitboxResult = hitboxMgr.ProcessHit(origin, direction);
                    if (hitboxResult.DidHit)
                    {
                        result.DidHitPlayer = true;
                        result.HitboxResult = hitboxResult;
                        result.FinalHitPoint = hit.point;
                        result.TargetObject = hitboxMgr.gameObject;

                        if (_drawDebugRays)
                            Debug.DrawLine(currentOrigin, hit.point, Color.red, _debugRayDuration);

                        return result;
                    }
                }

                // Check world material.
                SurfaceMaterial surface = hit.collider.GetComponent<SurfaceMaterial>();
                if (surface == null)
                {
                    result.FinalHitPoint = hit.point;
                    break;
                }

                if (!surface.IsPenetrable)
                {
                    result.FinalHitPoint = hit.point;
                    if (_drawDebugRays)
                        Debug.DrawLine(currentOrigin, hit.point, Color.blue, _debugRayDuration);
                    break;
                }

                float thickness = FindExitThickness(hit.point, direction, hit.collider);
                float thicknessCm = thickness * 100f;

                if (thicknessCm > surface.MaxThickness)
                {
                    result.FinalHitPoint = hit.point;
                    break;
                }

                float damageReduction = (thicknessCm * surface.ResistancePerCm) / remainingPower;
                damageReduction = Mathf.Clamp01(damageReduction);

                result.DamageMultiplier *= (1f - damageReduction);
                result.WallsPenetrated++;
                result.PenetrationPoints.Add(hit.point);

                remainingPower -= thicknessCm * surface.ResistancePerCm;
                if (remainingPower <= 0f)
                {
                    result.FinalHitPoint = hit.point;
                    break;
                }

                Vector3 exitPoint = hit.point + direction * thickness;
                currentOrigin = exitPoint + direction * 0.01f;

                if (_drawDebugRays)
                    Debug.DrawLine(hit.point, exitPoint, Color.green, _debugRayDuration);
            }

            return result;
        }

        /// <summary>
        /// Ray march through the collider to find exit point thickness.
        /// Steps forward from the entry point until outside the collider.
        /// </summary>
        private float FindExitThickness(Vector3 entryPoint, Vector3 direction, Collider wallCollider)
        {
            float distance = RAY_MARCH_STEP;

            while (distance < RAY_MARCH_MAX)
            {
                Vector3 testPoint = entryPoint + direction * distance;
                if (!wallCollider.bounds.Contains(testPoint))
                    return distance;

                distance += RAY_MARCH_STEP;
            }

            return RAY_MARCH_MAX;
        }
    }
}
