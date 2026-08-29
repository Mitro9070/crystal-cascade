#if UNITY_EDITOR
using System;
using System.IO;
using NeonSeven.Configs;
using NeonSeven.Infrastructure;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NeonSeven.Editor
{
    public static class NeonSevenProjectSetup
    {
        private const string ConfigPath = "Assets/Resources/Configs/NeonSevenGameConfig.asset";
        private const string CatalogPath = "Assets/Resources/Configs/LevelCatalogConfig.asset";
        private const string LevelsFolder = "Assets/Resources/Configs/Levels";
        private const string BootstrapperScenePath = "Assets/Scenes/Bootstrapper.unity";
        private const string GameplayScenePath = "Assets/Scenes/Gameplay.unity";
        private const string LegacyScenePath = "Assets/Scenes/NeonSeven.unity";

        [MenuItem("Tools/Neon Seven/Rebuild Project Assets")]
        public static void RebuildProjectAssets()
        {
            EnsureFolders();
            ApplyPlayerSettings();
            AssetDatabase.Refresh();
            ConfigureTextureImports();
            var gameConfig = EnsureAsset<NeonSevenGameConfig>(ConfigPath);
            EditorUtility.SetDirty(gameConfig);
            var levels = CreateLevels();
            var catalog = EnsureAsset<LevelCatalogConfig>(CatalogPath);
            catalog.EditorSetLevels(levels);
            EditorUtility.SetDirty(catalog);
            CreateScenes();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void ApplyPlayerSettings()
        {
            PlayerSettings.companyName = "Astreya";
            PlayerSettings.productName = "Neon Seven";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
        }

        private static void ConfigureTextureImports()
        {
            const string texturesRoot = "Assets/Resources/Textures";
            if (!Directory.Exists(texturesRoot))
                return;

            var files = Directory.GetFiles(texturesRoot, "*.*", SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
            {
                string path = files[i].Replace("\\", "/");
                string extension = Path.GetExtension(path).ToLowerInvariant();
                if (extension != ".png" && extension != ".jpg" && extension != ".jpeg")
                    continue;

                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                    continue;

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = extension == ".png";
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                importer.maxTextureSize = path.Contains("/Backgrounds/") ? 2048 : 512;

                if (path.Contains("/UI/panel_glass") || path.Contains("/UI/grid_cell") || path.Contains("/UI/button_candy"))
                    importer.spriteBorder = new Vector4(32f, 32f, 32f, 32f);

                importer.SaveAndReimport();
            }
        }

        private static LevelConfig[] CreateLevels()
        {
            var levels = new LevelConfig[50];
            for (int i = 0; i < levels.Length; i++)
            {
                int number = i + 1;
                string path = $"{LevelsFolder}/Level_{number:00}.asset";
                var level = EnsureAsset<LevelConfig>(path);
                var objective = (LevelObjective)(i % 4);
                int tier = i / 5;
                int moveLimit = Mathf.Max(10, 20 - tier + (i % 3));
                int targetScore = 7000 + number * 1800;
                int targetObsidian = 4 + tier * 2 + (i % 4);
                int targetCombo = 2 + Mathf.Min(4, tier / 2);
                int riseEvery = i < 8 ? 0 : Mathf.Max(4, 8 - tier / 2);
                int initialRows = Mathf.Clamp(1 + tier / 3, 1, 4);
                int[] initialMatrix = BuildLevelMatrix(number, objective, tier);
                level.EditorSet(number, objective, moveLimit, targetScore, targetObsidian, targetCombo, riseEvery, initialRows, 7000 + number * 37, initialMatrix);
                EditorUtility.SetDirty(level);
                levels[i] = level;
            }

            return levels;
        }

        private static void CreateScenes()
        {
            CreateBootstrapperScene(BootstrapperScenePath);
            CreateGameplayScene();
            CreateBootstrapperScene(LegacyScenePath);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(BootstrapperScenePath, true),
                new EditorBuildSettingsScene(GameplayScenePath, true),
                new EditorBuildSettingsScene(LegacyScenePath, true)
            };
        }

        private static void CreateBootstrapperScene(string path)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var bootstrap = new GameObject("Bootstrap");
            bootstrap.transform.position = Vector3.zero;
            bootstrap.AddComponent<Bootstrap>();
            var cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            var camera = cameraGo.AddComponent<Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.03f, 0.02f, 0.08f);
            cameraGo.AddComponent<AudioListener>();
            EditorSceneManager.SaveScene(scene, path);
        }

        private static void CreateGameplayScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var marker = new GameObject("GameplayScene");
            marker.transform.position = Vector3.zero;
            EditorSceneManager.SaveScene(scene, GameplayScenePath);
        }

        private static T EnsureAsset<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
                return asset;

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void EnsureFolders()
        {
            Directory.CreateDirectory("Assets/Resources/Configs/Levels");
            Directory.CreateDirectory("Assets/Scenes");
        }

        private static int[] BuildLevelMatrix(int levelNumber, LevelObjective objective, int tier)
        {
            string[] rows = SelectTemplate(levelNumber, objective, tier);
            var rng = new System.Random(10000 + levelNumber * 97 + tier * 13);
            var matrix = new int[49];
            for (int i = 0; i < matrix.Length; i++)
                matrix[i] = -1;

            for (int row = 0; row < 7; row++)
            {
                string pattern = rows[row];
                for (int column = 0; column < 7; column++)
                {
                    matrix[row * 7 + column] = EncodeCell(pattern[column], row, column, levelNumber, tier, rng);
                }
            }

            return matrix;
        }

        private static string[] SelectTemplate(int levelNumber, LevelObjective objective, int tier)
        {
            if (levelNumber == 1)
            {
                return new[]
                {
                    ".......",
                    ".......",
                    ".......",
                    ".......",
                    ".......",
                    "2......",
                    "#....67"
                };
            }

            if (levelNumber == 2)
            {
                return new[]
                {
                    ".......",
                    ".......",
                    ".......",
                    ".......",
                    "..2....",
                    "..#..3.",
                    "1....#2"
                };
            }

            if (levelNumber == 3)
            {
                return new[]
                {
                    ".......",
                    ".......",
                    ".......",
                    "...3...",
                    "..*#*..",
                    ".2...2.",
                    "#1...1#"
                };
            }

            if (levelNumber == 4)
            {
                return new[]
                {
                    ".......",
                    ".......",
                    ".......",
                    "..#.#..",
                    "..*4*..",
                    ".2...2.",
                    "1#...#1"
                };
            }

            string[][] templates = objective switch
            {
                LevelObjective.TargetScore => new[]
                {
                    new[] { ".......", ".......", "...*...", "..***..", ".**#**.", ".*****.", "**#*#**" },
                    new[] { ".......", ".......", "..***..", ".**.**.", ".#***#.", ".*****.", "*#***#*" },
                    new[] { ".......", "...*...", "..***..", "..#.#..", ".*****.", ".**.**.", "*#*.*#*" },
                    new[] { ".......", ".......", ".**.**.", ".*****.", "..###..", ".*****.", "**.*.**" }
                },
                LevelObjective.BreakObsidian => new[]
                {
                    new[] { ".......", ".......", "...#...", "..###..", ".#*#*#.", ".*****.", "##*#*##" },
                    new[] { ".......", ".......", "..#.#..", ".##*##.", ".#***#.", "##*#*##", ".#...#." },
                    new[] { ".......", ".......", "..###..", ".#*#*#.", ".##.##.", "##***##", ".#*#*#." },
                    new[] { ".......", ".......", "...#...", ".#.#.#.", ".##*##.", "##***##", "##.#.##" }
                },
                LevelObjective.ReachCombo => new[]
                {
                    new[] { ".......", ".......", ".......", "...2...", "..232..", ".1.#.1.", "#1...1#" },
                    new[] { ".......", ".......", "...3...", "..232..", ".1...1.", ".#2#2#.", "1.....1" },
                    new[] { ".......", ".......", "..2.2..", "...3...", ".2#1#2.", ".1...1.", "#..2..#" },
                    new[] { ".......", ".......", "...2...", "..343..", ".2...2.", ".#1#1#.", "1.....1" }
                },
                _ => new[]
                {
                    new[] { ".......", ".......", ".......", ".......", "..*.*..", ".2#3#2.", ".1...1." },
                    new[] { ".......", ".......", ".......", "..#.#..", "..*.*..", ".2...2.", "1#...#1" },
                    new[] { ".......", ".......", ".......", "...*...", "..#.#..", ".3...3.", "1..#..1" },
                    new[] { ".......", ".......", ".......", ".......", ".2...2.", "..#.#..", "1#...#1" }
                }
            };

            string[] baseTemplate = templates[(levelNumber + tier) % templates.Length];
            string[] output = new string[7];
            for (int row = 0; row < 7; row++)
                output[row] = DensifyRow(baseTemplate[row], objective, tier, row);

            return output;
        }

        private static string DensifyRow(string rowPattern, LevelObjective objective, int tier, int row)
        {
            if (tier <= 1 || row < 2)
                return rowPattern;

            char[] chars = rowPattern.ToCharArray();
            int passes = Mathf.Min(2, tier / 3);
            for (int pass = 0; pass < passes; pass++)
            {
                int column = (row + tier + pass * 2) % chars.Length;
                if (chars[column] == '.')
                    chars[column] = objective == LevelObjective.BreakObsidian && (row + pass) % 2 == 0 ? '#' : '*';
            }

            return new string(chars);
        }

        private static int EncodeCell(char token, int row, int column, int levelNumber, int tier, System.Random rng)
        {
            return token switch
            {
                '.' => -1,
                '#' => 0,
                '*' => 1 + rng.Next(7),
                >= '1' and <= '7' => token - '0',
                _ => 1 + ((levelNumber + row * 2 + column + tier) % 7)
            };
        }
    }
}
#endif
