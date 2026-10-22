using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Cardboard.EditorTools
{
    public static class ProjectSetup
    {
        private const string ScenePath = "Assets/Scenes/Game.unity";

        [MenuItem("Cardboard/Setup Project")]
        public static void Setup()
        {
            PlayerSettings.productName = "Картонное подземелье";
            PlayerSettings.companyName = "Mopch";
            PlayerSettings.bundleVersion = "1.0.0";
            PlayerSettings.defaultScreenWidth = 1600;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
            PlayerSettings.visibleInBackground = true;
            PlayerSettings.allowFullscreenSwitch = true;
            PlayerSettings.usePlayerLog = true;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            PlayerSettings.SetApiCompatibilityLevel(NamedBuildTarget.Standalone, ApiCompatibilityLevel.NET_Standard);
            PlayerSettings.colorSpace = ColorSpace.Gamma;

            Directory.CreateDirectory("Assets/Scenes");
            if (!File.Exists(ScenePath))
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, ScenePath);
            }

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Cardboard] Project setup complete");
        }

        [MenuItem("Cardboard/Run Self Tests")]
        public static void RunSelfTests()
        {
            bool ok = SelfTest.RunAll();
            if (!ok)
            {
                throw new Exception("[Cardboard] Self tests failed");
            }
            Debug.Log("[Cardboard] Self tests passed");
        }

        public static void BuildWindows()
        {
            Setup();
            EditorUserBuildSettings.development = false;
            EditorUserBuildSettings.allowDebugging = false;
            EditorUserBuildSettings.connectProfiler = false;
            Directory.CreateDirectory("Build");
            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = "Build/CardboardDungeon.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new Exception("[Cardboard] Build failed: " + report.summary.result);
            }
            Debug.Log("[Cardboard] Build OK: " + report.summary.outputPath + " size=" + report.summary.totalSize);
        }

        public static void BuildQA()
        {
            Setup();
            Directory.CreateDirectory("Build/qa");
            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = "Build/qa/CardboardDungeonQA.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new Exception("[Cardboard] QA build failed: " + report.summary.result);
            }
            Debug.Log("[Cardboard] QA build OK");
        }
    }
}
