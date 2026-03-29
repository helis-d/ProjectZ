using UnityEngine;

namespace ProjectZ.Audio
{
    /// <summary>
    /// GDD Section 11: Audio channel priority and ducking system.
    /// 
    /// Priority Order:
    /// 1. Critical: Sphere, headshot, own damage sound
    /// 2. Voice Comms: Team chat
    /// 3. Ultimate Cues: "Fire in the hole!"
    /// 4. Combat: Weapon sounds, explosions
    /// 5. Footsteps
    /// 6. Ambience: Wind, birds, map hum
    /// 
    /// Rule: If Channel 1 or 3 is active, volume of Channels 4-6 reduced by 40%.
    /// </summary>
    public class AudioPriorityManager : MonoBehaviour
    {
        public static AudioPriorityManager Instance { get; private set; }

        public enum AudioChannel
        {
            Critical = 1,
            VoiceComms = 2,
            UltimateCues = 3,
            Combat = 4,
            Footsteps = 5,
            Ambience = 6
        }

        [Header("Ducking")]
        [SerializeField] private float _duckingAmount = 0.6f; // 40% reduction
        [SerializeField] private float _duckingFadeSpeed = 5f;

        private bool _isCriticalActive;
        private bool _isUltimateActive;
        private float _currentDuckMultiplier = 1f;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Update()
        {
            // Smoothly interpolate ducking
            float targetDuck = (_isCriticalActive || _isUltimateActive) ? _duckingAmount : 1f;
            _currentDuckMultiplier = Mathf.Lerp(_currentDuckMultiplier, targetDuck, Time.deltaTime * _duckingFadeSpeed);
        }

        /// <summary>Signal that a critical or ultimate sound is playing.</summary>
        public void SetChannelActive(AudioChannel channel, bool active)
        {
            switch (channel)
            {
                case AudioChannel.Critical:
                    _isCriticalActive = active;
                    break;
                case AudioChannel.UltimateCues:
                    _isUltimateActive = active;
                    break;
            }
        }

        /// <summary>
        /// Returns the volume multiplier for a given channel, considering ducking rules.
        /// Channels 4-6 are ducked when Critical or Ultimate are active.
        /// </summary>
        public float GetVolumeMultiplier(AudioChannel channel)
        {
            if (channel >= AudioChannel.Combat)
                return _currentDuckMultiplier;
            return 1f;
        }

        /// <summary>
        /// Play an AudioClip with channel-aware volume and priority.
        /// Higher priority channels will duck lower ones automatically.
        /// </summary>
        public void PlaySound(AudioSource source, AudioClip clip, AudioChannel channel, float baseVolume = 1f)
        {
            if (source == null || clip == null) return;

            float volumeMult = GetVolumeMultiplier(channel);
            source.priority = (int)channel * 32; // Unity priority 0-256, lower = higher priority
            source.PlayOneShot(clip, baseVolume * volumeMult);
        }
    }
}
