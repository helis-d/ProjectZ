using UnityEngine;

namespace ProjectZ.Combat
{
    /// <summary>
    /// Surface type enum matching GDD Section 5 material resistance table.
    /// </summary>
    public enum SurfaceType
    {
        Wood,
        Stone,
        Metal,
        Glass,
        Flesh
    }

    /// <summary>
    /// Attach to any world object (walls, crates, etc.) to define its
    /// penetration properties for the wallbang system.
    ///
    /// GDD Section 5 Material Resistance Table:
    ///   Wood   — penetrable, resistance 0.8/cm, max 60 cm
    ///   Stone  — penetrable, resistance 2.5/cm, max 30 cm
    ///   Metal  — NOT penetrable
    ///   Glass  — penetrable, minimal resistance
    ///   Flesh  — penetrable, resistance 0.1/cm
    /// </summary>
    public class SurfaceMaterial : MonoBehaviour
    {
        [SerializeField] private SurfaceType _surfaceType = SurfaceType.Stone;

        /// <summary>Resistance per centimetre of thickness.</summary>
        public float ResistancePerCm => _surfaceType switch
        {
            SurfaceType.Wood  => 0.8f,
            SurfaceType.Stone => 2.5f,
            SurfaceType.Metal => float.PositiveInfinity,
            SurfaceType.Glass => 0.05f,
            SurfaceType.Flesh => 0.1f,
            _ => 1.0f
        };

        /// <summary>Maximum thickness the surface can have (cm). 0 = not penetrable.</summary>
        public float MaxThickness => _surfaceType switch
        {
            SurfaceType.Wood  => 60f,
            SurfaceType.Stone => 30f,
            SurfaceType.Metal => 0f,
            SurfaceType.Glass => 5f,
            SurfaceType.Flesh => 100f,
            _ => 0f
        };

        /// <summary>Can bullets pass through this material at all?</summary>
        public bool IsPenetrable => _surfaceType != SurfaceType.Metal;

        public SurfaceType Type => _surfaceType;
    }
}
