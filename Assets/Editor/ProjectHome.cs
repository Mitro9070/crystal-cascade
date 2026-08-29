#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace NeonSeven.Editor
{
    [InitializeOnLoad]
    public static class ProjectHome
    {
        public const string AsciiRoot = @"C:\Unity\Games\NeonSeven";
        public const string GameScene = "Assets/Scenes/Bootstrapper.unity";

        static ProjectHome()
        {
            EditorApplication.delayCall += Enforce;
        }

        [MenuItem("Tools/Neon Seven/Open Game Scene")]
        [MenuItem("Neon Seven/Open Game Scene")]
        public static void OpenGameScene()
        {
            if (!File.Exists(GameScene))
            {
                EditorUtility.DisplayDialog("Neon Seven", "Missing file " + GameScene, "OK");
                return;
            }

            EditorSceneManager.OpenScene(GameScene, OpenSceneMode.Single);
        }

        private static void Enforce()
        {
            if (IsForbiddenPath(Application.dataPath))
            {
                EditorUtility.DisplayDialog(
                    "Neon Seven",
                    "This Editor is opened from a forbidden folder:\n" + Application.dataPath +
                    "\n\nOnly project path:\n" + AsciiRoot +
                    "\n\nOpen it from Unity Hub or Запустить NeonSeven.bat.",
                    "Close");
                EditorApplication.Exit(0);
                return;
            }

            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path == GameScene)
                return;
            if (!File.Exists(GameScene))
                return;

            EditorSceneManager.OpenScene(GameScene, OpenSceneMode.Single);
        }

        public static bool IsForbiddenPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            if (path.IndexOf("OneDrive", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            if (path.IndexOf("Документы", StringComparison.Ordinal) >= 0)
                return true;

            for (int i = 0; i < path.Length; i++)
            {
                if (path[i] > 127)
                    return true;
            }

            return false;
        }
    }
}
#endif
