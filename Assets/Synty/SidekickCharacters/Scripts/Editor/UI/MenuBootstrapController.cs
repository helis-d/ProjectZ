// Copyright (c) 2024 Synty Studios Limited. All rights reserved.
//
// Use of this software is subject to the terms and conditions of the Synty Studios End User Licence Agreement (EULA)
// available at: https://syntystore.com/pages/end-user-licence-agreement
//
// For additional details, see the LICENSE.MD file bundled with this software.

#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace Synty.SidekickCharacters.UI
{
    [InitializeOnLoad]
    public static class MenuBootstrapController
    {
        private const string _AUTO_OPEN_STATE = "syntySkAutoOpenState";
        private const string _PREFS_CHECK_NAME = "syntySKCheckDependencies";

        private static ModularCharacterWindow _sidekickCharacterWindow;
        private static bool _openWindowOnStart = true;

        static MenuBootstrapController()
        {
            EditorApplication.update += OpenWindowOnStartup;
        }

        /// <summary>
        ///     Opens the Character Creator window when Unity starts or the plugin is added to a project.
        /// </summary>
        private static void OpenWindowOnStartup()
        {
            _openWindowOnStart = EditorPrefs.GetBool(_AUTO_OPEN_STATE, true);
            EditorApplication.update -= OpenWindowOnStartup;
            if (_openWindowOnStart)
            {
                if (!SessionState.GetBool("FirstInitDone", false))
                {
                    ShowSidekickCharacterWindow();
          
                    SessionState.SetBool("FirstInitDone", true);
                }
                
            }
        }

        /// <summary>
        ///     Creates the Sidekick Character Creator window and adds it to the toolbar.
        /// </summary>
        [MenuItem("Synty/Sidekick Character Tool")]
        public static void ShowSidekickCharacterWindow()
        {
            FindSidekickCharacterWindow();
            _sidekickCharacterWindow.Show();
        }

        /// <summary>
        ///     Find the existing Sidekick Character Creator window, or create one if it doesn't exist
        /// </summary>
        private static void FindSidekickCharacterWindow()
        {
            if (_sidekickCharacterWindow != null)
            {
                return;
            }

            _sidekickCharacterWindow = EditorWindow.GetWindow<ModularCharacterWindow>("Sidekick Character Tool");
            _sidekickCharacterWindow.minSize = new Vector2(600, 600);
        }
    }
}
#endif
