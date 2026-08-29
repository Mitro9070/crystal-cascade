#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace NeonSeven.Editor
{
    public static class NeonSevenAndroidBuild
    {
        public const string ScenePath = "Assets/Scenes/Bootstrapper.unity";
        private const string PackageName = "ru.astreya.neonseven";

        [MenuItem("Tools/Neon Seven/Build Android APK")]
        public static void BuildApk()
        {
            if (!HasAndroidModule())
                throw new InvalidOperationException("Android Build Support не установлен для этого редактора.");

            NeonSevenProjectSetup.RebuildProjectAssets();
            PrepareBuildEnvironment();
            ApplyAndroidSettings();
            SetPhoneScene();

            var apk = ApkAbs();
            Directory.CreateDirectory(Path.GetDirectoryName(apk));
            var report = BuildPipeline.BuildPlayer(new[] { ScenePath }, apk, BuildTarget.Android, BuildOptions.None);
            bool ok = report.summary.result == BuildResult.Succeeded && File.Exists(apk) && new FileInfo(apk).Length > 1024;
            if (!ok)
                throw new InvalidOperationException("Сборка APK завершилась без валидного файла.");
        }

        public static void BuildApkBatch()
        {
            if (!HasAndroidModule())
            {
                Debug.LogError("Android Build Support is not installed.");
                EditorApplication.Exit(2);
                return;
            }

            NeonSevenProjectSetup.RebuildProjectAssets();
            PrepareBuildEnvironment();
            ApplyAndroidSettings();
            SetPhoneScene();

            var apk = ApkAbs();
            Directory.CreateDirectory(Path.GetDirectoryName(apk));
            var report = BuildPipeline.BuildPlayer(new[] { ScenePath }, apk, BuildTarget.Android, BuildOptions.None);
            bool ok = report.summary.result == BuildResult.Succeeded && File.Exists(apk) && new FileInfo(apk).Length > 1024;
            EditorApplication.Exit(ok ? 0 : 1);
        }

        private static void PrepareBuildEnvironment()
        {
            Environment.SetEnvironmentVariable("BEE_JOBS", "1");
            Environment.SetEnvironmentVariable("UNITY_IL2CPP_JOBS", "1");
            Environment.SetEnvironmentVariable("IL2CPP_JOBS", "1");

            const string gradleHome = @"C:\g";
            Directory.CreateDirectory(gradleHome);
            Environment.SetEnvironmentVariable("GRADLE_USER_HOME", gradleHome);
            Environment.SetEnvironmentVariable("GRADLE_USER_HOME", gradleHome, EnvironmentVariableTarget.Process);

            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            DeleteIfExists(Path.Combine(projectRoot, ".utmp"));
            DeleteIfExists(Path.Combine(projectRoot, "Library", "Bee", "Android", "Prj", "IL2CPP", "Gradle", "unityLibrary", "build", "intermediates", "cxx"));
            DeleteIfExists(Path.Combine(projectRoot, "Library", "PlayerDataCache", "Android"));
        }

        private static void DeleteIfExists(string path)
        {
            if (Directory.Exists(path))
                Directory.Delete(path, true);
        }

        private static bool HasAndroidModule()
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
                return false;

            var player = Path.Combine(EditorApplication.applicationContentsPath, "PlaybackEngines", "AndroidPlayer");
            return Directory.Exists(player);
        }

        private static void ApplyAndroidSettings()
        {
            PlayerSettings.companyName = "Astreya";
            PlayerSettings.productName = "Neon Seven";
            PlayerSettings.bundleVersion = "1.0";
            PlayerSettings.Android.bundleVersionCode = 1;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, PackageName);
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetIl2CppCompilerConfiguration(NamedBuildTarget.Android, Il2CppCompilerConfiguration.Release);
            PlayerSettings.SetIl2CppCodeGeneration(NamedBuildTarget.Android, Il2CppCodeGeneration.OptimizeSize);
            PlayerSettings.SetIl2CppStacktraceInformation(NamedBuildTarget.Android, Il2CppStacktraceInformation.MethodOnly);
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Android, ManagedStrippingLevel.High);
            PlayerSettings.stripEngineCode = true;
            PlayerSettings.SetAdditionalIl2CppArgs("--jobs=1");
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            EditorUserBuildSettings.buildAppBundle = false;
            EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;
            EditorUserBuildSettings.development = false;
            EditorUserBuildSettings.connectProfiler = false;
            EditorUserBuildSettings.allowDebugging = false;
            EditorUserBuildSettings.buildWithDeepProfilingSupport = false;
        }

        private static void SetPhoneScene()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };
        }

        private static string ApkAbs()
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Builds", "Android", "NeonSeven.apk"));
        }
    }
}
#endif
