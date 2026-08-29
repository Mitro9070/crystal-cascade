using System.Collections.Generic;
using UnityEngine;

namespace NeonSeven.Configs
{
    [CreateAssetMenu(menuName = "Neon Seven/Level Config", fileName = "LevelConfig")]
    public sealed class LevelConfig : ScriptableObject
    {
        private const int BoardCellCount = 49;

        [SerializeField, Min(1)] private int _levelNumber = 1;
        [SerializeField] private LevelObjective _objective = LevelObjective.TargetScore;
        [SerializeField, Min(1)] private int _moveLimit = 18;
        [SerializeField, Min(0)] private int _targetScore = 12000;
        [SerializeField, Min(0)] private int _targetObsidianBreaks = 8;
        [SerializeField, Min(1)] private int _targetCombo = 2;
        [SerializeField, Min(0)] private int _riseEveryMoves = 0;
        [SerializeField, Range(0, 4)] private int _initialRows = 1;
        [SerializeField] private int _seed = 1001;
        [SerializeField] private int[] _initialMatrix = new int[BoardCellCount];

        public int LevelNumber => _levelNumber;
        public LevelObjective Objective => _objective;
        public int MoveLimit => _moveLimit;
        public int TargetScore => _targetScore;
        public int TargetObsidianBreaks => _targetObsidianBreaks;
        public int TargetCombo => _targetCombo;
        public int RiseEveryMoves => _riseEveryMoves;
        public int InitialRows => _initialRows;
        public int Seed => _seed;
        public IReadOnlyList<int> InitialMatrix => _initialMatrix;
        public bool HasInitialMatrix => _initialMatrix != null && _initialMatrix.Length == BoardCellCount;

#if UNITY_EDITOR
        public void EditorSet(int levelNumber, LevelObjective objective, int moveLimit, int targetScore, int targetObsidianBreaks, int targetCombo, int riseEveryMoves, int initialRows, int seed, int[] initialMatrix)
        {
            _levelNumber = levelNumber;
            _objective = objective;
            _moveLimit = moveLimit;
            _targetScore = targetScore;
            _targetObsidianBreaks = targetObsidianBreaks;
            _targetCombo = targetCombo;
            _riseEveryMoves = riseEveryMoves;
            _initialRows = initialRows;
            _seed = seed;
            if (initialMatrix != null && initialMatrix.Length == BoardCellCount)
                _initialMatrix = (int[])initialMatrix.Clone();
        }
#endif
    }
}
