// One-off project configuration, kept as code so the settings are reviewable and repeatable.
// Run from the menu (Doomsday > Apply Project Settings) or headless:
//   Unity -batchmode -quit -projectPath . -executeMethod DoomsdayTunnels.EditorTools.ProjectBootstrap.Apply
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;

namespace DoomsdayTunnels.EditorTools
{
    public static class ProjectBootstrap
    {
        public const string ProductName = "Doomsday Tunnels 3D";
        public const string CompanyName = "Andrew Chase";
        public const string BundleId = "io.github.acechase.doomsdaytunnels3d";
        public const string Version = "0.1.0";

        [MenuItem("Doomsday/Apply Project Settings")]
        public static void Apply()
        {
            PlayerSettings.productName = ProductName;
            PlayerSettings.companyName = CompanyName;
            PlayerSettings.bundleVersion = Version;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, BundleId);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, BundleId);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, BundleId);

            // The game always plays sideways. Auto-rotate between the two landscape orientations only.
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.statusBarHidden = true;
            PlayerSettings.accelerometerFrequency = 60;
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.runInBackground = true;      // desktop only: keep running without focus (useful for the screenshot run); phones ignore it

            // iOS: iPhone and iPad, iOS 15 or newer, Metal, IL2CPP (the only option), automatic signing.
            PlayerSettings.iOS.targetOSVersionString = "15.0";
            PlayerSettings.iOS.targetDevice = iOSTargetDevice.iPhoneAndiPad;
            PlayerSettings.iOS.requiresFullScreen = true;
            PlayerSettings.iOS.hideHomeButton = true;
            PlayerSettings.iOS.appleEnableAutomaticSigning = true;
            // The Apple team is per developer, so it lives in an untracked file next to the project (see README).
            var teamFile = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), ".apple-team-id");
            if (System.IO.File.Exists(teamFile)) PlayerSettings.iOS.appleDeveloperTeamID = System.IO.File.ReadAllText(teamFile).Trim();
            PlayerSettings.iOS.requiresPersistentWiFi = false;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetArchitecture(NamedBuildTarget.iOS, 1); // ARM64
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.iOS, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.iOS, new[] { GraphicsDeviceType.Metal });

            // Android: 64-bit only, API 25 and up, Vulkan with a GLES3 fallback, IL2CPP, ASTC textures.
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.startInFullscreen = true;
            PlayerSettings.Android.renderOutsideSafeArea = true;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan, GraphicsDeviceType.OpenGLES3 });
            EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ASTC;

            // Scripting: Input System only, .NET Standard 2.1, strip aggressively for small native builds.
            SetActiveInputHandler(1);
            PlayerSettings.SetApiCompatibilityLevel(NamedBuildTarget.iOS, ApiCompatibilityLevel.NET_Standard);
            PlayerSettings.SetApiCompatibilityLevel(NamedBuildTarget.Android, ApiCompatibilityLevel.NET_Standard);
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.iOS, ManagedStrippingLevel.Medium);
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Android, ManagedStrippingLevel.Medium);

            TidyTemplateAssets();
            AssetDatabase.SaveAssets();
            Debug.Log("[Doomsday] Project settings applied.");
        }

        // The URP template drops its scene, render settings and input actions at the top of Assets. Move them under
        // _Project so everything we own is in one place. AssetDatabase.MoveAsset keeps the GUIDs, so every reference
        // (quality levels, the build list, the Input System settings) stays intact. Safe to run again.
        static void TidyTemplateAssets()
        {
            Move("Assets/Scenes/SampleScene.unity", "Assets/_Project/Scenes/Main.unity");
            foreach (var guid in AssetDatabase.FindAssets("", new[] { "Assets/Settings" }))
            {
                var from = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetDatabase.IsValidFolder(from)) continue;
                Move(from, "Assets/_Project/Settings/" + System.IO.Path.GetFileName(from));
            }
            Move("Assets/InputSystem_Actions.inputactions", "Assets/_Project/Scripts/Input/InputSystem_Actions.inputactions");
            foreach (var dir in new[] { "Assets/Scenes", "Assets/Settings" })
                if (AssetDatabase.IsValidFolder(dir) && AssetDatabase.FindAssets("", new[] { dir }).Length == 0) AssetDatabase.DeleteAsset(dir);
            var main = "Assets/_Project/Scenes/Main.unity";
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(main) != null)
                EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(main, true) };
        }

        static void Move(string from, string to)
        {
            if (AssetDatabase.LoadMainAssetAtPath(from) == null) return;
            var err = AssetDatabase.MoveAsset(from, to);
            if (!string.IsNullOrEmpty(err)) Debug.LogWarning("[Doomsday] Could not move " + from + ": " + err);
        }

        // 0 = legacy Input Manager, 1 = Input System package, 2 = both. Not exposed on PlayerSettings, so set it on the asset.
        static void SetActiveInputHandler(int value)
        {
            var settings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (settings == null || settings.Length == 0) return;
            var so = new SerializedObject(settings[0]);
            var prop = so.FindProperty("activeInputHandler");
            if (prop != null && prop.intValue != value) { prop.intValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
        }
    }
}
