using UnityEngine;

namespace NeonSeven.Configs
{
    [CreateAssetMenu(menuName = "Neon Seven/Game Config", fileName = "NeonSevenGameConfig")]
    public sealed class NeonSevenGameConfig : ScriptableObject
    {
        [SerializeField, Min(7)] private int _boardSize = 7;
        [SerializeField, Min(1)] private int _classicRiseEveryMoves = 5;
        [SerializeField, Range(0f, 1f)] private float _hiddenPieceChance = 0.15f;
        [SerializeField, Min(0)] private int _boardClearBonus = 70000;
        [SerializeField] private Color _backgroundTop = new Color(0.13f, 0.06f, 0.24f, 1f);
        [SerializeField] private Color _backgroundBottom = new Color(0.03f, 0.02f, 0.08f, 1f);
        [SerializeField] private Color _glass = new Color(0.24f, 0.20f, 0.36f, 0.62f);
        [SerializeField] private Color _obsidian = new Color(0.035f, 0.032f, 0.055f, 1f);
        [SerializeField] private Color _lava = new Color(1f, 0.62f, 0.1f, 1f);
        [SerializeField]
        private Color[] _numberColors =
        {
            new Color(0.96f, 0.98f, 1f, 1f),
            new Color(0.12f, 0.84f, 1f, 1f),
            new Color(0.11f, 0.95f, 0.55f, 1f),
            new Color(1f, 0.86f, 0.22f, 1f),
            new Color(1f, 0.43f, 0.18f, 1f),
            new Color(1f, 0.18f, 0.62f, 1f),
            new Color(0.55f, 0.23f, 1f, 1f)
        };

        public int BoardSize => _boardSize;
        public int ClassicRiseEveryMoves => _classicRiseEveryMoves;
        public float HiddenPieceChance => _hiddenPieceChance;
        public int BoardClearBonus => _boardClearBonus;
        public Color BackgroundTop => _backgroundTop;
        public Color BackgroundBottom => _backgroundBottom;
        public Color Glass => _glass;
        public Color Obsidian => _obsidian;
        public Color Lava => _lava;
        public Color[] NumberColors => _numberColors;

        public Color ColorForNumber(int number)
        {
            if (number < 1 || number > _numberColors.Length)
                return Color.white;

            return _numberColors[number - 1];
        }
    }
}
