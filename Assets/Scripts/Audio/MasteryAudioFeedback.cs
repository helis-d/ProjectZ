using UnityEngine;

namespace ProjectZ.Audio
{
    /// <summary>
    /// GDD Section 3 Audio Design:
    /// - Level Up Sound: Mechanical "click-ching"
    /// - Max Level Sound: Energy charging sound
    /// - Fire Sound (Optional): More "solid" sound at Level 5 (only player hears)
    /// </summary>
    public class MasteryAudioFeedback : MonoBehaviour
    {
        [Header("Mastery SFX")]
        [SerializeField] private AudioClip _levelUpClip;       // "click-ching"
        [SerializeField] private AudioClip _maxLevelClip;      // Energy charging
        [SerializeField] private AudioClip _level5FireClip;    // More "solid" fire sound

        [Header("Volume")]
        [SerializeField] private float _sfxVolume = 0.8f;

        private AudioSource _audioSource;
        private int _lastKnownLevel = 1;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
                _audioSource = gameObject.AddComponent<AudioSource>();

            _audioSource.spatialBlend = 0f; // 2D (UI feedback) — only local player hears
            _audioSource.playOnAwake = false;
        }

        /// <summary>
        /// Call when weapon mastery level changes. Compares with last known level
        /// to trigger level-up or max-level SFX.
        /// </summary>
        public void OnLevelChanged(int newLevel)
        {
            if (newLevel > _lastKnownLevel)
            {
                if (newLevel >= 5)
                    PlayClip(_maxLevelClip);
                else
                    PlayClip(_levelUpClip);
            }

            _lastKnownLevel = newLevel;
        }

        /// <summary>
        /// Call on weapon fire. At level 5, plays a more impactful sound.
        /// Returns true if a custom fire sound was played.
        /// </summary>
        public bool TryPlayLevel5FireSound(int currentLevel)
        {
            if (currentLevel >= 5 && _level5FireClip != null)
            {
                PlayClip(_level5FireClip);
                return true;
            }
            return false;
        }

        private void PlayClip(AudioClip clip)
        {
            if (clip == null || _audioSource == null) return;

            // Use AudioPriority if available
            if (AudioPriorityManager.Instance != null)
                AudioPriorityManager.Instance.PlaySound(_audioSource, clip, AudioPriorityManager.AudioChannel.Combat, _sfxVolume);
            else
                _audioSource.PlayOneShot(clip, _sfxVolume);
        }
    }
}
