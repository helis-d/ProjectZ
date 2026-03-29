using System.Collections;
using FishNet.Object;
using UnityEngine;

namespace ProjectZ.Player
{
    /// <summary>
    /// Attached to the Player. Handles visual "Glow" or "Outline" 
    /// effects triggered by abilities like Sector's Panopticon.
    /// </summary>
    public class OutlineController : NetworkBehaviour
    {
        [Header("References")]
        [SerializeField] private SkinnedMeshRenderer[] _renderers; // The player body meshes
        
        [Header("Materials")]
        [SerializeField] private Material _outlineMaterial; // A red-tinted, always-on-top shader material

        // Store original materials to restore them after the effect ends
        private Material[][] _originalMaterials;
        private Coroutine _outlineRoutine;

        private void Awake()
        {
            if (_renderers == null || _renderers.Length == 0) return;

            // Cache original materials
            _originalMaterials = new Material[_renderers.Length][];
            for (int i = 0; i < _renderers.Length; i++)
            {
                _originalMaterials[i] = _renderers[i].materials;
            }
        }

        /// <summary>
        /// Called ONLY on clients who are allowed to see the outline
        /// (e.g. the Sector who placed the Totem, or their whole team).
        /// </summary>
        [TargetRpc]
        public void TargetShowOutline(FishNet.Connection.NetworkConnection conn, float duration)
        {
            if (_renderers == null || _renderers.Length == 0 || _outlineMaterial == null) return;

            if (_outlineRoutine != null) StopCoroutine(_outlineRoutine);
            _outlineRoutine = StartCoroutine(ShowOutlineRoutine(duration));
        }

        private IEnumerator ShowOutlineRoutine(float duration)
        {
            // Apply outline material alongside or replacing the original
            // In a real proj, this is usually an overlay pass or appending to the materials array.
            // For prototype: we swap out the first material to the outline material.
            
            for (int i = 0; i < _renderers.Length; i++)
            {
                Material[] newMats = _renderers[i].materials;
                // Add the outline material to the end of the array, or replace depending on shader setup.
                // Assuming replacement for simplicity of the visual prototype.
                newMats[0] = _outlineMaterial; 
                _renderers[i].materials = newMats;
            }

            yield return new WaitForSeconds(duration);

            // Restore
            for (int i = 0; i < _renderers.Length; i++)
            {
                _renderers[i].materials = _originalMaterials[i];
            }
            
            _outlineRoutine = null;
        }
    }
}
