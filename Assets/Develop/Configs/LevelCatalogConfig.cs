using UnityEngine;

namespace NeonSeven.Configs
{
    [CreateAssetMenu(menuName = "Neon Seven/Level Catalog", fileName = "LevelCatalogConfig")]
    public sealed class LevelCatalogConfig : ScriptableObject
    {
        [SerializeField] private LevelConfig[] _levels;

        public int Count => _levels == null ? 0 : _levels.Length;
        public LevelConfig[] Levels => _levels;

        public LevelConfig GetLevel(int index)
        {
            if (_levels == null || _levels.Length == 0)
                return null;

            return _levels[Mathf.Clamp(index, 0, _levels.Length - 1)];
        }

#if UNITY_EDITOR
        public void EditorSetLevels(LevelConfig[] levels)
        {
            _levels = levels;
        }
#endif
    }
}
