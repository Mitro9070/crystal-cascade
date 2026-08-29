using UnityEngine;

namespace NeonSeven.Infrastructure.Services
{
    public sealed class NeonSevenServices
    {
        public NeonSevenServices(GameObject root, LevelConfigService levels)
        {
            Levels = levels;
            SaveData = new SaveDataService();
            SaveData.Load();
            Audio = new AudioService(root, 32);
            Audio.SetMuted(SaveData.IsMuted);
            VFX = new VFXService();
            Haptics = new HapticService();
        }

        public LevelConfigService Levels { get; }
        public SaveDataService SaveData { get; }
        public AudioService Audio { get; }
        public VFXService VFX { get; }
        public HapticService Haptics { get; }
    }
}
