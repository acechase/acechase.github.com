// Builds the one scene the game runs in, from code, so the scene file is reproducible and nothing in it is a mystery.
// Menu: Doomsday > Create Main Scene. Headless: -executeMethod DoomsdayTunnels.EditorTools.SceneSetup.Create
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DoomsdayTunnels.EditorTools
{
    public static class SceneSetup
    {
        const string ScenePath = "Assets/_Project/Scenes/Main.unity";

        [MenuItem("Doomsday/Create Main Scene")]
        public static void Create()
        {
            var unlit = Material("Assets/_Project/Materials/Unlit.mat", "Doomsday/Unlit");
            var additive = Material("Assets/_Project/Materials/UnlitAdd.mat", "Doomsday/UnlitAdd");
            var profile = Profile("Assets/_Project/Settings/MainVolume.asset");
            foreach (var guid in AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset", new[] { "Assets/_Project/Settings" }))
            {
                var rp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(AssetDatabase.GUIDToAssetPath(guid));
                rp.supportsHDR = true;                                        // bloom needs colours brighter than white
                EditorUtility.SetDirty(rp);
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.black;
            cam.fieldOfView = 61f; cam.nearClipPlane = 0.1f; cam.farClipPlane = 140f; cam.allowHDR = true;
            camGo.AddComponent<AudioListener>();
            var data = cam.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true; data.antialiasing = AntialiasingMode.None; data.renderShadows = false;

            var volGo = new GameObject("Global Volume");
            var vol = volGo.AddComponent<Volume>(); vol.isGlobal = true; vol.sharedProfile = profile;

            var game = new GameObject("Game");
            var director = game.AddComponent<GameDirector>();
            director.Unlit = unlit; director.Additive = additive;

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("[Doomsday] Main scene created.");
        }

        static Material Material(string path, string shaderName)
        {
            var shader = Shader.Find(shaderName);
            if (shader == null) throw new System.Exception("Shader not found: " + shaderName);
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
            else m.shader = shader;
            return m;
        }

        // Bloom tuned for phones: few iterations, no high-quality filtering. Neutral tonemapping keeps the neon hues honest.
        static VolumeProfile Profile(string path)
        {
            var p = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (p != null) return p;
            p = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(p, path);
            var bloom = p.Add<Bloom>(true);
            bloom.intensity.Override(0.9f); bloom.threshold.Override(1.0f); bloom.scatter.Override(0.65f);
            bloom.maxIterations.Override(4); bloom.highQualityFiltering.Override(false);
            AssetDatabase.AddObjectToAsset(bloom, p);
            var tone = p.Add<Tonemapping>(true);
            tone.mode.Override(TonemappingMode.Neutral);
            AssetDatabase.AddObjectToAsset(tone, p);
            EditorUtility.SetDirty(p);
            return p;
        }
    }
}
