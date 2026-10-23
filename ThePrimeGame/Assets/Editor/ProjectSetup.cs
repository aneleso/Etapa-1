using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ThePrimeGame.Editor
{
    public static class ProjectSetup
    {
        public const string ScenePath = "Assets/Scenes/Level1.unity";
        public const string ArtPath = "Assets/Settings/PrimeGameAssets.asset";

        [MenuItem("The PrimeGame/Crear o actualizar el nivel jugable")]
        public static void CreateGameScene()
        {
            var art = AssetDatabase.LoadAssetAtPath<PrimeGameAssets>(ArtPath);
            if (art == null)
            {
                art = ScriptableObject.CreateInstance<PrimeGameAssets>();
                AssetDatabase.CreateAsset(art, ArtPath);
            }
            art.blue = Sprite("block_2x1_azul");
            art.mint = Sprite("block_2x1_menta");
            art.cream = Sprite("block_2x1_crema");
            art.coral = Sprite("block_1x1_coral");
            art.silhouetteWide = Sprite("silhouette_2x1");
            art.silhouetteSmall = Sprite("silhouette_1x1");
            art.logo = Sprite("PrimeGame_logo_sticker");
            art.soundOn = Sprite("btn_sound_on");
            art.soundOff = Sprite("btn_sound_off");
            art.restart = Sprite("btn_restart");
            art.star = Sprite("feedback_star");
            art.pickup = Audio("pickup_tick");
            art.snap = Audio("drop_snap");
            art.success = Audio("success_chime");
            art.ambient = Audio("ambient_loop");
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
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 800;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.runInBackground = false;
            AssetDatabase.SaveAssets();
            Debug.Log("PRIMEGAME_SETUP_OK: Level1 created with all original art and audio.");
        }

        [MenuItem("The PrimeGame/Compilar para Windows")]
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
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Windows build failed: " + report.summary.result);
            Debug.Log("PRIMEGAME_BUILD_OK: " + output);
        }

        private static Sprite Sprite(string name)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/" + name + ".png");
            if (sprite == null) throw new FileNotFoundException("Missing original sprite: " + name);
            return sprite;
        }

        private static AudioClip Audio(string name)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/audio/" + name + ".wav");
            if (clip == null) throw new FileNotFoundException("Missing original audio: " + name);
            return clip;
        }

        private static string Argument(string key)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++) if (args[i] == key) return args[i + 1];
            return null;
        }
    }
}
