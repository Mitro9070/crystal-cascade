using NeonSeven.Configs;
using NeonSeven.Gameplay;
using NeonSeven.Infrastructure.Services;
using NeonSeven.UI;
using UnityEngine;

namespace NeonSeven.Infrastructure
{
    public sealed class Bootstrap : MonoBehaviour
    {
        private GameplayCycle _gameplayCycle;

        private void Awake()
        {
            Application.targetFrameRate = 60;
            Screen.orientation = ScreenOrientation.Portrait;
            var gameConfig = Resources.Load<NeonSevenGameConfig>("Configs/NeonSevenGameConfig");
            var catalog = Resources.Load<LevelCatalogConfig>("Configs/LevelCatalogConfig");
            if (gameConfig == null || catalog == null)
            {
                Debug.LogError("Neon Seven configs are missing. Run Tools/Neon Seven/Rebuild Project Assets.");
                return;
            }

            BuildCamera(gameConfig);
            var services = new NeonSevenServices(gameObject, new LevelConfigService(catalog));
            var view = new GameObject("NeonSevenView").AddComponent<NeonSevenView>();
            _gameplayCycle = new GameplayCycle(gameConfig, services, view);
            _gameplayCycle.Prepare();
            _gameplayCycle.Launch();
        }

        private void Update()
        {
            _gameplayCycle?.Update(Time.deltaTime);
        }

        private void OnDestroy()
        {
            _gameplayCycle?.Dispose();
            _gameplayCycle = null;
        }

        private static void BuildCamera(NeonSevenGameConfig config)
        {
            var camera = Camera.main;
            if (camera == null)
            {
                var go = new GameObject("Main Camera");
                camera = go.AddComponent<Camera>();
                go.tag = "MainCamera";
            }

            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = config.BackgroundBottom;
            if (camera.GetComponent<AudioListener>() == null)
                camera.gameObject.AddComponent<AudioListener>();
        }
    }
}
