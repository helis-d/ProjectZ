using UnityEngine;

namespace ProjectZ.Weapon
{
    /// <summary>
    /// GDD Section 3: First-person visual effects based on weapon mastery level.
    /// Level 1: Matte, standard weapon
    /// Level 2: Slight heat effect on barrel
    /// Level 3: Light leaks from weapon vents
    /// Level 4: Static electricity ripple around weapon
    /// Level 5: Core glows brightly, steam effect on reload
    /// </summary>
    public class WeaponVisualEffects : MonoBehaviour
    {
        [Header("Level VFX References")]
        [SerializeField] private ParticleSystem _heatEffect;        // Lv2
        [SerializeField] private ParticleSystem _lightLeakEffect;   // Lv3
        [SerializeField] private ParticleSystem _staticEffect;      // Lv4
        [SerializeField] private ParticleSystem _glowEffect;        // Lv5
        [SerializeField] private ParticleSystem _steamEffect;       // Lv5 on reload

        [Header("Material Slots")]
        [SerializeField] private Renderer _weaponRenderer;
        [SerializeField] private int _emissionMaterialIndex = 0;

        private int _currentLevel = 1;
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
        private MaterialPropertyBlock _propertyBlock; // [FIX] BUG-10: reusable block, zero GC

        private void Awake()
        {
            _propertyBlock = new MaterialPropertyBlock(); // [FIX] BUG-10: allocate once
        }

        /// <summary>Call when weapon mastery level changes.</summary>
        public void SetMasteryLevel(int level)
        {
            _currentLevel = Mathf.Clamp(level, 1, 5);
            UpdateEffects();
        }

        /// <summary>Call during reload for Lv5 steam effect.</summary>
        public void OnReloadStarted()
        {
            if (_currentLevel >= 5 && _steamEffect != null)
                _steamEffect.Play();
        }

        private void UpdateEffects()
        {
            // Disable all first
            SetParticle(_heatEffect, false);
            SetParticle(_lightLeakEffect, false);
            SetParticle(_staticEffect, false);
            SetParticle(_glowEffect, false);

            // Apply level-specific effects (cumulative)
            switch (_currentLevel)
            {
                case 5:
                    SetParticle(_glowEffect, true);
                    SetEmissionIntensity(3.0f);
                    goto case 4;
                case 4:
                    SetParticle(_staticEffect, true);
                    goto case 3;
                case 3:
                    SetParticle(_lightLeakEffect, true);
                    goto case 2;
                case 2:
                    SetParticle(_heatEffect, true);
                    SetEmissionIntensity(_currentLevel >= 3 ? 1.5f : 0.5f);
                    break;
                default: // Level 1
                    SetEmissionIntensity(0f);
                    break;
            }
        }

        private void SetParticle(ParticleSystem ps, bool active)
        {
            if (ps == null) return;
            if (active && !ps.isPlaying) ps.Play();
            else if (!active && ps.isPlaying) ps.Stop();
        }

        private void SetEmissionIntensity(float intensity)
        {
            if (_weaponRenderer == null) return;
            // [FIX] BUG-10: MaterialPropertyBlock — no Material[] allocation
            _weaponRenderer.GetPropertyBlock(_propertyBlock, _emissionMaterialIndex);
            Color levelColor = MasteryLevelColors.GetColor(_currentLevel);
            _propertyBlock.SetColor(EmissionColor, levelColor * intensity);
            _weaponRenderer.SetPropertyBlock(_propertyBlock, _emissionMaterialIndex);
        }
    }
}
