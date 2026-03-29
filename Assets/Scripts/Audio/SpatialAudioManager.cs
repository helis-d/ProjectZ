using UnityEngine;

namespace ProjectZ.Audio
{
    /// <summary>
    /// GDD Section 11: 3D Spatial Audio Engine.
    /// Handles distance attenuation, 3D pan, front/back differentiation,
    /// and dynamic occlusion via raycasting.
    /// 
    /// Gain = ReferenceDistance / (ReferenceDistance + (Distance × RolloffFactor))
    /// Pan = SoundVector · RightVector
    /// CutoffFrequency = 22000Hz × (1.0 – Occlusion)
    /// </summary>
    public class SpatialAudioManager : MonoBehaviour
    {
        public static SpatialAudioManager Instance { get; private set; }

        [Header("Attenuation")]
        [SerializeField] private float _referenceDistance = 1f;
        [SerializeField] private float _rolloffFactor = 1f;
        [SerializeField] private float _minGain = 0.01f;

        [Header("Occlusion")]
        [SerializeField] private LayerMask _occlusionMask;

        private AudioListener _listener;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            _listener = FindFirstObjectByType<AudioListener>();
        }

        /// <summary>
        /// Calculate the gain for a sound at the given world position,
        /// relative to the listener position.
        /// </summary>
        public float CalculateGain(Vector3 soundPos)
        {
            if (_listener == null) return 1f;
            float dist = Vector3.Distance(_listener.transform.position, soundPos);
            float gain = _referenceDistance / (_referenceDistance + (dist * _rolloffFactor));
            return gain < _minGain ? 0f : gain;
        }

        /// <summary>
        /// Calculate stereo pan (-1 left, 0 center, 1 right) for a sound.
        /// </summary>
        public float CalculatePan(Vector3 soundPos)
        {
            if (_listener == null) return 0f;
            Vector3 toSound = (soundPos - _listener.transform.position).normalized;
            return Vector3.Dot(toSound, _listener.transform.right);
        }

        /// <summary>
        /// Check if a sound is behind the listener (for low-pass filtering).
        /// </summary>
        public bool IsBehindListener(Vector3 soundPos)
        {
            if (_listener == null) return false;
            Vector3 toSound = (soundPos - _listener.transform.position).normalized;
            return Vector3.Dot(toSound, _listener.transform.forward) < 0f;
        }

        /// <summary>
        /// Calculate occlusion value (0 = no obstruction, 0-1 based on materials).
        /// Used to determine low-pass filter cutoff:
        ///   CutoffFrequency = 22000Hz × (1.0 – Occlusion)
        /// </summary>
        public float CalculateOcclusion(Vector3 soundPos)
        {
            if (_listener == null) return 0f;

            Vector3 listenerPos = _listener.transform.position;
            Vector3 dir = soundPos - listenerPos;
            float dist = dir.magnitude;

            if (!Physics.Raycast(listenerPos, dir.normalized, out RaycastHit hit, dist, _occlusionMask))
                return 0f; // No obstruction

            // Determine occlusion from material
            var surface = hit.collider.GetComponent<Combat.SurfaceMaterial>();
            if (surface == null) return 0.5f; // Default occlusion

            return surface.Type switch
            {
                Combat.SurfaceType.Wood  => 0.4f,
                Combat.SurfaceType.Stone => 0.7f,
                Combat.SurfaceType.Metal => 0.9f,
                Combat.SurfaceType.Glass => 0.2f,
                _ => 0.5f
            };
        }

        /// <summary>
        /// Get the low-pass filter cutoff frequency based on occlusion.
        /// </summary>
        public float GetCutoffFrequency(float occlusion)
        {
            return 22000f * (1f - occlusion);
        }

        /// <summary>
        /// Configure an AudioSource for 3D spatial positioning.
        /// </summary>
        public void ConfigureSource(AudioSource source, Vector3 soundPos)
        {
            if (source == null) return;

            float gain = CalculateGain(soundPos);
            float pan = CalculatePan(soundPos);
            float occlusion = CalculateOcclusion(soundPos);
            float cutoff = GetCutoffFrequency(occlusion);

            source.volume = gain;
            source.panStereo = pan;

            // Apply low-pass filter if behind listener or occluded
            var lpf = source.GetComponent<AudioLowPassFilter>();
            if (lpf == null && (occlusion > 0f || IsBehindListener(soundPos)))
                lpf = source.gameObject.AddComponent<AudioLowPassFilter>();

            if (lpf != null)
            {
                bool isBehind = IsBehindListener(soundPos);
                float behindCutoff = isBehind ? 8000f : 22000f;
                lpf.cutoffFrequency = Mathf.Min(cutoff, behindCutoff);
            }
        }
    }
}
