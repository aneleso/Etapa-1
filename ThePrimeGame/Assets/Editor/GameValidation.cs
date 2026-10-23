using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace ThePrimeGame.Editor
{
    [InitializeOnLoad]
    public static class GameValidation
    {
        private const string PhaseKey = "ThePrimeGame.ValidationPhase";
        private static readonly TestRunnerApi Api;
        private static string ResultsPath => Path.GetFullPath(Path.Combine(Application.dataPath, "../TestResults"));

        static GameValidation()
        {
            Api = ScriptableObject.CreateInstance<TestRunnerApi>();
            Api.RegisterCallbacks(new Results());
        }

        [MenuItem("The PrimeGame/Validar juego (EditMode y PlayMode)", false, 20)]
        public static void RunAll()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            ProjectSetup.CreateGameScene();
            Directory.CreateDirectory(ResultsPath);
            File.WriteAllText(Path.Combine(ResultsPath, "validation-status.txt"), "Running EditMode tests.\n");
            Execute(TestMode.EditMode);
        }

        private static void Execute(TestMode mode)
        {
            SessionState.SetString(PhaseKey, mode.ToString());
            Api.Execute(new ExecutionSettings(new Filter
            {
                testMode = mode,
                assemblyNames = new[] { "ThePrimeGame." + mode + "Tests" }
            }));
        }

        private sealed class Results : ICallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun) { }
            public void TestStarted(ITestAdaptor test) { }
            public void TestFinished(ITestResultAdaptor result) { }

            public void RunFinished(ITestResultAdaptor result)
            {
                string phase = SessionState.GetString(PhaseKey, "");
                if (string.IsNullOrEmpty(phase)) return;
                SessionState.EraseString(PhaseKey);
                Directory.CreateDirectory(ResultsPath);
                TestRunnerApi.SaveResultToFile(result, Path.Combine(ResultsPath, phase + ".xml"));
                string summary = phase + ": passed=" + result.PassCount + ", failed=" + result.FailCount
                    + ", skipped=" + result.SkipCount;
                File.AppendAllText(Path.Combine(ResultsPath, "validation-status.txt"), summary + "\n");
                Debug.Log("PRIMEGAME_TESTS: " + summary);
                if (phase == "EditMode" && result.FailCount == 0 && result.PassCount >= 6)
                    EditorApplication.delayCall += () => Execute(TestMode.PlayMode);
            }
        }
    }
}
