using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SlotGame.Editor
{
    /// <summary>
    /// Reproducible WebGL build into <c>Build/WebGL</c> (the path the submission requires).
    /// Menu: <b>Slot Game ▸ Build WebGL</b>.
    /// CLI: <c>Unity -batchmode -quit -projectPath . -buildTarget WebGL -executeMethod SlotGame.Editor.BuildScript.BuildWebGL</c>
    /// </summary>
    public static class BuildScript
    {
        public const string OutputPath = "Build/WebGL";

        /// <summary>Output of the QA build (kept out of git by .gitignore).</summary>
        public const string QaOutputPath = "Build/QA";

        /// <summary>Scripting define that lets a QA build replay outcomes from a <c>?seed=</c> URL parameter.</summary>
        public const string QaSeedsDefine = "SLOT_QA_SEEDS";

        /// <summary>Release build: the cryptographic RNG only, URL seeding compiled out.</summary>
        [MenuItem("Slot Game/Build WebGL", priority = 20)]
        public static void BuildWebGL()
        {
            Build(OutputPath, Array.Empty<string>());
        }

        /// <summary>QA build: identical, plus <c>?seed=</c> replay for reproducing bugs and recording demos.</summary>
        [MenuItem("Slot Game/Build WebGL (QA, seedable)", priority = 21)]
        public static void BuildQaWebGL()
        {
            Build(QaOutputPath, new[] { QaSeedsDefine });
        }

        private static void Build(string outputPath, string[] extraDefines)
        {
            ApplyPlayerSettings();

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
            {
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);
            }

            string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0)
            {
                throw new InvalidOperationException("No scenes in Build Settings. Run Slot Game > Rebuild Project Assets first.");
            }

            if (Directory.Exists(outputPath))
            {
                Directory.Delete(outputPath, true);
            }

            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.WebGL,
                options = BuildOptions.None,
                extraScriptingDefines = extraDefines,
            });

            BuildSummary summary = report.summary;
            Debug.Log($"[BuildScript] {outputPath}: {summary.result}, {summary.totalSize / (1024f * 1024f):F1} MB, " +
                      $"{summary.totalErrors} errors, {summary.totalWarnings} warnings, {summary.totalTime}.");

            if (summary.result != BuildResult.Succeeded && Application.isBatchMode)
            {
                EditorApplication.Exit(1);
            }
        }

        /// <summary>CI entry point: regenerate the generated assets, then build.</summary>
        public static void RebuildAssetsAndBuildWebGL()
        {
            SlotProjectBuilder.BuildAll();
            BuildWebGL();
        }

        /// <summary>CI entry point: regenerate assets, then build both the release and the QA player.</summary>
        public static void RebuildAssetsAndBuildAll()
        {
            SlotProjectBuilder.BuildAll();
            BuildQaWebGL();
            BuildWebGL();
        }

        /// <summary>Player settings tuned for a small, fast-loading WebGL build that works on any static host.</summary>
        public static void ApplyPlayerSettings()
        {
            PlayerSettings.companyName = "Praneeth Rayalu Repalle";
            PlayerSettings.productName = "Lucky 7 Slots";
            PlayerSettings.bundleVersion = "1.0.0";
            PlayerSettings.colorSpace = ColorSpace.Gamma;
            PlayerSettings.runInBackground = true;
            PlayerSettings.defaultWebScreenWidth = 1280;
            PlayerSettings.defaultWebScreenHeight = 720;
            PlayerSettings.SplashScreen.showUnityLogo = false;
            PlayerSettings.SplashScreen.show = false;

            PlayerSettings.WebGL.template = "PROJECT:SlotMachine";

            // Gzip + decompression fallback: small downloads that also run on hosts that do not
            // send Content-Encoding headers (GitHub Pages, simple static servers).
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
            PlayerSettings.WebGL.nameFilesAsHashes = false;

            PlayerSettings.stripEngineCode = true;
            PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.WebGL, ManagedStrippingLevel.Low);
            PlayerSettings.SetApiCompatibilityLevel(BuildTargetGroup.WebGL, ApiCompatibilityLevel.NET_Standard);
            AssetDatabase.SaveAssets();
        }
    }
}
