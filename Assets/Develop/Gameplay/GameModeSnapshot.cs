using System.Collections.Generic;
using NeonSeven.Configs;
using NeonSeven.Core;

namespace NeonSeven.Gameplay
{
    public sealed class GameModeSnapshot
    {
        public GameModeSnapshot(GameModeType mode, LevelConfig level, IReadOnlyList<BallData> balls, int score, int bestScore, int currentNumber, int nextNumber, int movesLeftToRise, int movesUsed, int moveLimit, int maxCombo, int obsidianHits, bool isGameOver, bool isWon, int remainingSeconds, string status)
        {
            Mode = mode;
            Level = level;
            Balls = balls;
            Score = score;
            BestScore = bestScore;
            CurrentNumber = currentNumber;
            NextNumber = nextNumber;
            MovesLeftToRise = movesLeftToRise;
            MovesUsed = movesUsed;
            MoveLimit = moveLimit;
            MaxCombo = maxCombo;
            ObsidianHits = obsidianHits;
            IsGameOver = isGameOver;
            IsWon = isWon;
            RemainingSeconds = remainingSeconds;
            Status = status;
        }

        public GameModeType Mode { get; }
        public LevelConfig Level { get; }
        public IReadOnlyList<BallData> Balls { get; }
        public int Score { get; }
        public int BestScore { get; }
        public int CurrentNumber { get; }
        public int NextNumber { get; }
        public int MovesLeftToRise { get; }
        public int MovesUsed { get; }
        public int MoveLimit { get; }
        public int MaxCombo { get; }
        public int ObsidianHits { get; }
        public bool IsGameOver { get; }
        public bool IsWon { get; }
        public int RemainingSeconds { get; }
        public string Status { get; }
    }
}
