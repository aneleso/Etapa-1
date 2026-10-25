using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ThePrimeGame.Editor
{
    /// <summary>Provides explicit editor commands to create the playable scene and build the Windows game.</summary>
    public static class ProjectSetup
    {
        public const string ScenePath = "Assets/Scenes/Level1.unity";
        public const string ArtPath = "Assets/Settings/PrimeGameAssets.asset";

        /// <summary>Rebuilds Level1 with verified resource GUIDs and a consistent product configuration.</summary>
        [MenuItem("The PrimeGame/Create or update playable level")]
        public static void CreateGameScene()
        {
            var art = AssetDatabase.LoadAssetAtPath<PrimeGameAssets>(ArtPath);
            if (art == null)
            {
                art = ScriptableObject.CreateInstance<PrimeGameAssets>();
                AssetDatabase.CreateAsset(art, ArtPath);
            }
            art.blue = Sprite("6399ed4915e18bc4c83c1e218ab2a981");
            art.mint = Sprite("36da0b33242cbf7428d2a3f58113b5d8");
            art.cream = Sprite("180e3616018ed294ea79759fa2240f64");
            art.coral = Sprite("2197c9164b9374a4c8322268f700329d");
            art.silhouetteWide = Sprite("5673e0e28e6a08443b7f5d6bb6ba8738");
            art.silhouetteSmall = Sprite("bd2adeb53dda2674e9b095195340c95e");
            art.logo = Sprite("419975d502f0c2d4c8c8aa075d9edff2");
            art.soundOn = Sprite("ea957e5d28b52fc41b72b0521e642e17");
            art.soundOff = Sprite("01c7bd109fdfad54696062dde61e9360");
            art.restart = Sprite("d7a3e67da8158f74d8172047e1fdb3f8");
            art.star = Sprite("4cb9b036bc4229741ae6265e694b5485");
            art.pickup = Audio("7f59547e240bbf74383220012eb07036");
            art.snap = Audio("6888135da3a76aa49b09f1f02c59daac");
            art.success = Audio("e219c0cdeaaf4444bab0006106938176");
            art.ambient = Audio("9302407b1932abf488e2121e6f24f838");
            EditorUtility.SetDirty(art);
            AssetDatabase.SaveAssets();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0, 0, -10);
            var camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(250, 248, 243, 255);
            var game = new GameObject("The PrimeGame", typeof(GameManager)).GetComponent<GameManager>();
            game.SetAssets(art);
            EditorUtility.SetDirty(game);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };
            PlayerSettings.productName = "The PrimeGame";
            PlayerSettings.companyName = "The PrimeGame Team";
            PlayerSettings.bundleVersion = "2.0";
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 800;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.runInBackground = false;
            AssetDatabase.SaveAssets();
            Debug.Log("PRIMEGAME_SETUP_OK: Level1 created with all original art and audio.");
        }

        /// <summary>Builds the active level with Mono and reports failure instead of presenting an incomplete build as ready.</summary>
        [MenuItem("The PrimeGame/Build for Windows")]
        public static void BuildWindows()
        {
            if (!File.Exists(ScenePath)) CreateGameScene();
            string output = Argument("-primeGameBuildPath") ?? Path.GetFullPath("Builds/Windows/ThePrimeGame.exe");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                target = BuildTarget.StandaloneWindows64,
                locationPathName = output,
                options = BuildOptions.None
            });
            // Export Unity's own result so the delivery does not rely on a manually written success claim.
            string resultsPath = Path.GetFullPath("TestResults");
            Directory.CreateDirectory(resultsPath);
            File.WriteAllText(Path.Combine(resultsPath, "BuildSummary.json"), JsonUtility.ToJson(new BuildEvidence
            {
                unityVersion = Application.unityVersion,
                productVersion = PlayerSettings.bundleVersion,
                result = report.summary.result.ToString(),
                platform = report.summary.platform.ToString(),
                errors = report.summary.totalErrors,
                warnings = report.summary.totalWarnings,
                bytes = report.summary.totalSize.ToString(),
                completedUtc = DateTime.UtcNow.ToString("O")
            }, true));
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Windows build failed: " + report.summary.result);
            Debug.Log("PRIMEGAME_BUILD_OK: " + output);
        }

        [Serializable]
        private sealed class BuildEvidence
        {
            public string unityVersion, productVersion, result, platform, bytes, completedUtc;
            public int errors, warnings;
        }

        /// <summary>Finds supplied sprites by stable GUID so localized filenames do not leak into English source code.</summary>
        private static Sprite Sprite(string assetGuid)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(assetGuid));
            if (sprite == null) throw new FileNotFoundException("Missing original sprite: " + assetGuid);
            return sprite;
        }

        /// <summary>Loads the existing audio clip by GUID and fails clearly if the original resource is missing.</summary>
        private static AudioClip Audio(string assetGuid)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(assetGuid));
            if (clip == null) throw new FileNotFoundException("Missing original audio: " + assetGuid);
            return clip;
        }

        /// <summary>Allows a command-line build to select an output directory without changing project source.</summary>
        private static string Argument(string key)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++) if (args[i] == key) return args[i + 1];
            return null;
        }
    }
}
