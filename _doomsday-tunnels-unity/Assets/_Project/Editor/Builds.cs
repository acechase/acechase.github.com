// Player builds, from the menu or headless. Output goes to Builds/ (ignored by git).
//   iOS:     Unity -batchmode -quit -projectPath . -buildTarget iOS -executeMethod DoomsdayTunnels.EditorTools.Builds.IOS
//            then open Builds/iOS/Unity-iPhone.xcodeproj in Xcode, pick your team, run on a phone or archive for TestFlight.
//   Android: Unity -batchmode -quit -projectPath . -buildTarget Android -executeMethod DoomsdayTunnels.EditorTools.Builds.AndroidApk
//            then: adb install -r Builds/DoomsdayTunnels3D.apk
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace DoomsdayTunnels.EditorTools
{
    public static class Builds
    {
        static string[] Scenes => EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();

        [MenuItem("Doomsday/Build/iOS (Xcode project)")]
        public static void IOS() => Run(BuildTarget.iOS, "Builds/iOS");

        [MenuItem("Doomsday/Build/Android APK (sideload)")]
        public static void AndroidApk() { EditorUserBuildSettings.buildAppBundle = false; Run(BuildTarget.Android, "Builds/DoomsdayTunnels3D.apk"); }

        [MenuItem("Doomsday/Build/Android AAB (Play Store)")]
        public static void AndroidAab() { EditorUserBuildSettings.buildAppBundle = true; Run(BuildTarget.Android, "Builds/DoomsdayTunnels3D.aab"); }

        // A desktop build for checking the game by eye without a phone: run it with "-shot <folder>" to get screenshots.
        [MenuItem("Doomsday/Build/macOS (debug)")]
        public static void Mac() => Run(BuildTarget.StandaloneOSX, "Builds/DoomsdayTunnels3D.app");

        static void Run(BuildTarget target, string path)
        {
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = Scenes, locationPathName = path, target = target,
                targetGroup = BuildPipeline.GetBuildTargetGroup(target), options = BuildOptions.None
            });
            var ok = report.summary.result == BuildResult.Succeeded;
            Debug.Log("[Doomsday] Build " + target + " " + report.summary.result + " (" + report.summary.totalSize / (1024 * 1024) + " MB) -> " + path);
            if (!ok && Application.isBatchMode) EditorApplication.Exit(1);
        }
    }
}
