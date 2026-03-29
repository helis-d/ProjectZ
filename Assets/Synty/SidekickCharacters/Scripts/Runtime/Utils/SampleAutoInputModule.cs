// Copyright (c) 2025 Synty Studios Limited. All rights reserved.
//
// Use of this software is subject to the terms and conditions of the End User Licence Agreement (EULA)
// of the store at which you purchased this asset.
//
// Synty assets are available at:
// https://www.syntystore.com
// https://assetstore.unity.com/publishers/5217
// https://www.fab.com/sellers/Synty%20Studios
//
// Sample scripts are included only as examples and are not intended as production-ready.

using UnityEngine;
using UnityEngine.EventSystems;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
// compile errors?
using UnityEngine.InputSystem.UI;
// compile errors?
// if you are getting a compile error here you likely need to import the Input System package (com.unity.inputsystem) in the package manager or change the input setting in player settings back to 'Input Manager (Old)'
#endif


namespace Synty.SidekickCharacters.Utils
{
    /// <summary>
    ///     Sample script that helps automatically select the correct input event module depending on your project's settings.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(EventSystem))]
    public class SampleAutoInputModule : MonoBehaviour
    {
        void OnEnable()
        {
            UpdateInputModule();
        }

        void UpdateInputModule()
        {
#if ENABLE_INPUT_SYSTEM
            // New Input System only
            if (GetComponent<InputSystemUIInputModule>() == null)
            {
                // Remove any existing modules
                foreach (var module in GetComponents<BaseInputModule>())
                {
                    DestroyImmediate(module);
                }
                gameObject.AddComponent<InputSystemUIInputModule>();
                if(!Application.isPlaying) Debug.Log("Added InputSystemUIInputModule (new input system)");
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            // Old Input Manager only
            if (GetComponent<StandaloneInputModule>() == null)
            {
                // Remove any existing modules
                foreach (var module in GetComponents<BaseInputModule>())
                {
                    DestroyImmediate(module);
                }
                gameObject.AddComponent<StandaloneInputModule>();
                if(!Application.isPlaying) Debug.Log("Added StandaloneInputModule (old input manager)");
            }
#else
            Debug.LogWarning("No input system enabled in project settings.");
#endif
        }
    }
}
