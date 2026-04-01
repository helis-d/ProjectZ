#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace ProjectZ.Editor
{
    /// <summary>
    /// Nakama ve Agones üzerinde çalışacak olan Headless Server (Linux) build'ini
    /// otomatik olarak almak için kullanılan Editor aracıdır.
    /// </summary>
    public class BuildScript
    {
        [MenuItem("ProjectZ/Build Headless Linux Server")]
        public static void BuildLinuxServer()
        {
            // Set build settings to Dedicated Server
            EditorUserBuildSettings.standaloneBuildSubtarget = StandaloneBuildSubtarget.Server;

            var scenes = EditorBuildSettings.scenes;
            var scenePaths = new string[scenes.Length];
            for (int i = 0; i < scenes.Length; i++)
            {
                scenePaths[i] = scenes[i].path;
            }

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = scenePaths,
                locationPathName = "Builds/LinuxServer/ProjectZ_Server.x86_64",
                target = BuildTarget.StandaloneLinux64,
                subtarget = (int)StandaloneBuildSubtarget.Server,
                options = BuildOptions.CompressWithLz4HC
            };

            Debug.Log("[Build] Starting Linux Server Headless Build...");
            
            var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            
            if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                Debug.Log($"[Build] Server Build Succeeded! Path: {report.summary.outputPath} | Size: {report.summary.totalSize / (1024 * 1024)} MB");
            }
            else
            {
                Debug.LogError($"[Build] Server Build Failed: {report.summary.result}");
            }
        }
    }
}
#endif
