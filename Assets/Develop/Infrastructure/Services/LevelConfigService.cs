using NeonSeven.Configs;

namespace NeonSeven.Infrastructure.Services
{
    public sealed class LevelConfigService
    {
        private readonly LevelCatalogConfig _catalog;

        public LevelConfigService(LevelCatalogConfig catalog)
        {
            _catalog = catalog;
        }

        public int Count => _catalog.Count;

        public LevelConfig GetLevel(int index)
        {
            return _catalog.GetLevel(index);
        }
    }
}
