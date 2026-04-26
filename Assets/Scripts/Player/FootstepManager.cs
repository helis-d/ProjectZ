using UnityEngine;
using ProjectZ.Player;
using ProjectZ.Core;

namespace ProjectZ.Audio
{
    /// <summary>
    /// Handles footstep emission based on PlayerMovement and Input.
    /// Implements GDD Section 11 & User request:
    /// - Normal (Sprint): Sound emitted with radius.
    /// - Shift (Walk) or Crouch: Completely silent.
    /// </summary>
    [RequireComponent(typeof(PlayerMovement), typeof(PlayerInputHandler))]
    public class FootstepManager : MonoBehaviour
    {
        [Header("Footstep Settings")]
        [Tooltip("Distance traveled before registering the next footstep.")]
        [SerializeField] private float _stepDistance = 2.0f; 
        
        [Tooltip("Small-Medium radius for footstep audibility (User specified).")]
        [SerializeField] private float _runSoundRadius = 15f; 

        private PlayerMovement _movement;
        private PlayerInputHandler _input;
        private CharacterController _cc;

        [Header("Audio Clips — Run")]
        [SerializeField] private AudioClip[] _defaultRunClips;
        [SerializeField] private AudioClip[] _metalRunClips;
        [SerializeField] private AudioClip[] _woodRunClips;
        [SerializeField] private AudioClip[] _concreteRunClips;
        [SerializeField] private AudioClip[] _grassRunClips;

        [Header("Audio Clips — Walk")]
        [SerializeField] private AudioClip[] _defaultWalkClips;

        [Header("Audio Clips — Land")]
        [SerializeField] private AudioClip[] _landClips;

        private AudioSource _audioSource;
        private float _distanceAccumulator;
        private Vector3 _lastPosition;
        private bool _wasGrounded;

        private void Awake()
        {
            _movement = GetComponent<PlayerMovement>();
            _input = GetComponent<PlayerInputHandler>();
            _cc = GetComponent<CharacterController>();

            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
                _audioSource = gameObject.AddComponent<AudioSource>();

            _audioSource.spatialBlend = 1.0f; // Full 3D spatialization
            _audioSource.maxDistance = _runSoundRadius;
            _audioSource.rolloffMode = AudioRolloffMode.Linear;
            _audioSource.playOnAwake = false;

            _lastPosition = transform.position;
        }

        private void Update()
        {
            if (_cc == null) return;

            // Landing detection
            bool isGrounded = _cc.isGrounded;
            if (isGrounded && !_wasGrounded)
                PlayLandSound();
            _wasGrounded = isGrounded;

            if (!isGrounded)
            {
                _lastPosition = transform.position;
                return;
            }

            // GDD: Crouching = No sound, Shift/Walk = silent
            bool isSilent = _input.IsCrouching || _input.IsSprinting;

            float distanceMoved = Vector3.Distance(transform.position, _lastPosition);
            _lastPosition = transform.position;

            if (distanceMoved > 0.001f && !isSilent)
            {
                _distanceAccumulator += distanceMoved;

                if (_distanceAccumulator >= _stepDistance)
                {
                    _distanceAccumulator = 0f;
                    EmitFootstep(isRunning: true);
                }
            }
            else if (distanceMoved > 0.001f && _input.IsSprinting && !_input.IsCrouching)
            {
                // Walking (shift held): use walk clips if available, audible at smaller radius
                _distanceAccumulator += distanceMoved;
                if (_distanceAccumulator >= _stepDistance * 1.5f)
                {
                    _distanceAccumulator = 0f;
                    EmitFootstep(isRunning: false);
                }
            }
            else
            {
                _distanceAccumulator = 0f;
            }
        }

        private void EmitFootstep(bool isRunning)
        {
            AudioClip[] clips = isRunning ? _defaultRunClips : _defaultWalkClips;

            if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 2f))
            {
                var surface = hit.collider.GetComponent<ProjectZ.Combat.SurfaceMaterial>();
                if (surface != null && isRunning)
                {
                    clips = surface.Type switch
                    {
                        ProjectZ.Combat.SurfaceType.Metal => _metalRunClips ?? _defaultRunClips,
                        ProjectZ.Combat.SurfaceType.Wood  => _woodRunClips ?? _defaultRunClips,
                        ProjectZ.Combat.SurfaceType.Stone => _concreteRunClips ?? _defaultRunClips,
                        _ => _defaultRunClips
                    };
                }
            }

            clips ??= _defaultRunClips;
            if (clips != null && clips.Length > 0 && _audioSource != null)
            {
                AudioClip clip = clips[SecureRandom.Range(0, clips.Length)];
                float volume = ProjectZ.Settings.SettingsManager.Instance?.Current.audio.footstepVolume ?? 1.0f;

                // Use priority manager if available
                if (AudioPriorityManager.Instance != null)
                    AudioPriorityManager.Instance.PlaySound(_audioSource, clip, AudioPriorityManager.AudioChannel.Footsteps, volume);
                else
                    _audioSource.PlayOneShot(clip, volume);
            }
        }

        private void PlayLandSound()
        {
            if (_landClips == null || _landClips.Length == 0 || _audioSource == null) return;
            AudioClip clip = _landClips[SecureRandom.Range(0, _landClips.Length)];
            float volume = ProjectZ.Settings.SettingsManager.Instance?.Current.audio.footstepVolume ?? 1.0f;
            _audioSource.PlayOneShot(clip, volume);
        }

        private void OnDrawGizmos()
        {
            // Vizualize the "Küçük-Orta boyutta yuvarlak alan" requested by the user.
            // Only drawn in the Scene view when the character is actually running.
            if (!Application.isPlaying || _input == null || _cc == null) return;

            bool isSilent = _input.IsCrouching || _input.IsSprinting;
            bool isMoving = _cc.velocity.magnitude > 0.1f && _cc.isGrounded;

            if (!isSilent && isMoving)
            {
                Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.15f); // Transparent red circle
                Gizmos.DrawSphere(transform.position, _runSoundRadius);
                
                // Draw outline for better visibility
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position, _runSoundRadius);
            }
        }
    }
}
