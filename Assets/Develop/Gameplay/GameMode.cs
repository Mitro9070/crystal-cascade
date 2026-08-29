using System;
using System.Collections.Generic;
using NeonSeven.Configs;
using NeonSeven.Core;
using NeonSeven.Infrastructure.Services;
using NeonSeven.PrototypePort.Neon7;
using UnityEngine;

namespace NeonSeven.Gameplay
{
    public sealed class GameMode : IDisposable
    {
        private const int BlitzDurationSeconds = 120;

        private readonly NeonSevenGameConfig _config;
        private readonly LevelConfig _level;
        private GameModeType _mode;
        private readonly SaveDataService _saveData;
        private readonly BoardModel _board;
        private readonly float _hiddenChance;
        private int _currentNumber;
        private int _nextNumber;
        private int _score;
        private int _movesLeftToRise;
        private int _movesUsed;
        private int _maxCombo;
        private int _obsidianHits;
        private float _secondsRemaining;
        private int _lastNotifiedSeconds;
        private bool _isGameOver;
        private bool _isWon;

        public GameMode(NeonSevenGameConfig config, LevelConfig level, GameModeType mode, SaveDataService saveData)
        {
            _config = config;
            _level = level;
            _mode = mode;
            _saveData = saveData;
            int seed = level == null ? Environment.TickCount : level.Seed;
            _board = new BoardModel(mode == GameModeType.Campaign ? config.BoardSize : PrototypeMetrics.Size, seed);
            _hiddenChance = mode == GameModeType.Campaign ? 0.20f : PrototypeMetrics.HiddenPieceChance;
            _movesLeftToRise = RiseEveryMoves;
            _secondsRemaining = mode == GameModeType.Blitz ? BlitzDurationSeconds : 0f;
            _lastNotifiedSeconds = RemainingSeconds;
            if (mode == GameModeType.Campaign && level != null)
            {
                if (level.HasInitialMatrix)
                    _board.FillFromMatrix(level.InitialMatrix);
                else
                    _board.FillStartRows(level.InitialRows, _hiddenChance);
            }
            else
                _board.FillPrototypeStartBalls(PrototypeMetrics.HiddenStartChance);
            _currentNumber = _board.RollNumber(_hiddenChance);
            _nextNumber = _board.RollNumber(_hiddenChance);
        }

        public event Action<GameModeSnapshot> StateChanged;
        public event Action Won;
        public event Action Lost;
        public bool IsTerminal => _isGameOver || _isWon;

        public int Size => _board.Size;
        public int RemainingSeconds => _mode == GameModeType.Blitz ? Mathf.Max(0, Mathf.CeilToInt(_secondsRemaining)) : 0;

        public int RiseEveryMoves
        {
            get
            {
                if (_mode == GameModeType.Blitz)
                    return 0;
                if (_mode == GameModeType.Classic)
                    return PrototypeMetrics.RiseEvery;

                return _level == null ? 0 : _level.RiseEveryMoves;
            }
        }

        public PreviewInfo Preview(int column)
        {
            return _board.PreviewDrop(column, _currentNumber);
        }

        public void Start()
        {
            Notify(StatusText());
        }

        public bool TryPlaceCurrentBall(int column, out BallData dropped)
        {
            dropped = null;
            if (_isGameOver || _isWon)
                return false;

            if (!_board.TryDrop(column, _currentNumber, out dropped))
            {
                Notify("Колонка заполнена.");
                return false;
            }

            _movesUsed++;
            Notify(StatusText());
            return true;
        }

        public IReadOnlyList<BallData> PeekMatches()
        {
            return _board.PeekMatches();
        }

        public ResolutionStep DestroyMatches(int wave, IReadOnlyList<BallData> matches)
        {
            var step = _board.CommitDestroy(matches, wave);
            if (step.Matches.Count == 0)
                return step;

            _score += step.Score;
            _obsidianHits += step.HiddenHits;
            _maxCombo = Mathf.Max(_maxCombo, step.Wave);
            SaveBest();
            Notify(StatusText());
            return step;
        }

        public void ApplyGravity()
        {
            _board.ApplyGravity();
            Notify(StatusText());
        }

        public bool TryApplyBoardClearBonus(out int bonus)
        {
            bonus = 0;
            if (_board.CountBalls() != 0)
                return false;

            bonus = _mode == GameModeType.Campaign ? _config.BoardClearBonus : PrototypeMetrics.BoardClearBonus;
            _score += bonus;
            SaveBest();
            Notify("ПОЛЕ ОЧИЩЕНО!");
            return true;
        }

        public void AdvanceBallQueue()
        {
            _currentNumber = _nextNumber;
            _nextNumber = _board.RollNumber(_hiddenChance);
            Notify(StatusText());
        }

        public RiseResult TryHandleRise()
        {
            if (_mode == GameModeType.Zen || _mode == GameModeType.Blitz || RiseEveryMoves <= 0)
                return RiseResult.Skipped;

            _movesLeftToRise--;
            Notify(StatusText());
            if (_movesLeftToRise > 0)
                return RiseResult.Skipped;

            _movesLeftToRise = RiseEveryMoves;
            if (!_board.TryRiseHiddenRow())
            {
                Lose("The obsidian floor reached the top.");
                return RiseResult.Overflow;
            }

            Notify(StatusText());
            return RiseResult.Rose;
        }

        public void Tick(float deltaTime, bool allowFinish)
        {
            if (_mode != GameModeType.Blitz || _isGameOver || _isWon || deltaTime <= 0f)
                return;

            _secondsRemaining = Mathf.Max(0f, _secondsRemaining - deltaTime);
            int seconds = RemainingSeconds;
            if (seconds != _lastNotifiedSeconds)
            {
                _lastNotifiedSeconds = seconds;
                Notify(StatusText());
            }

            if (allowFinish && _secondsRemaining <= 0f)
                EvaluateEndConditions();
        }

        public void SwitchEndlessMode(GameModeType mode)
        {
            if (_isGameOver || _isWon)
                return;
            if ((_mode != GameModeType.Classic && _mode != GameModeType.Zen) || (mode != GameModeType.Classic && mode != GameModeType.Zen))
                return;

            _mode = mode;
            _movesLeftToRise = RiseEveryMoves;
            Notify(StatusText());
        }

        public void EvaluateEndConditions()
        {
            CheckEndConditions();
        }

        public void Dispose()
        {
            StateChanged = null;
            Won = null;
            Lost = null;
        }

        private void CheckEndConditions()
        {
            if (_mode == GameModeType.Blitz && _secondsRemaining <= 0f)
            {
                _isWon = true;
                Notify("Время вышло.");
                Won?.Invoke();
                return;
            }

            if (_board.IsFull())
            {
                Lose("Нет свободных колонок.");
                return;
            }

            if (_mode != GameModeType.Campaign || _level == null)
                return;

            bool achieved = false;
            switch (_level.Objective)
            {
                case LevelObjective.TargetScore:
                    achieved = _score >= _level.TargetScore;
                    break;
                case LevelObjective.BreakObsidian:
                    achieved = _obsidianHits >= _level.TargetObsidianBreaks;
                    break;
                case LevelObjective.ReachCombo:
                    achieved = _maxCombo >= _level.TargetCombo;
                    break;
                case LevelObjective.BoardClear:
                    achieved = _board.CountBalls() == 0;
                    break;
            }

            if (achieved)
            {
                _isWon = true;
                _saveData?.CompleteLevel(_level.LevelNumber, CalculateStars());
                Notify("Уровень пройден!");
                Won?.Invoke();
                return;
            }

            if (_movesUsed >= _level.MoveLimit)
                Lose("Ходы закончились.");
        }

        private void Lose(string reason)
        {
            _isGameOver = true;
            Notify(reason);
            Lost?.Invoke();
        }

        private void SaveBest()
        {
            _saveData?.SetBestScore(_score);
        }

        private int CalculateStars()
        {
            if (_level == null || _level.MoveLimit <= 0)
                return 1;

            float movesRatio = 1f - (float)_movesUsed / _level.MoveLimit;
            if (movesRatio >= 0.45f)
                return 3;
            if (movesRatio >= 0.20f)
                return 2;

            return 1;
        }

        private void Notify(string status)
        {
            StateChanged?.Invoke(new GameModeSnapshot(
                _mode,
                _level,
                _board.Balls,
                _score,
                _saveData == null ? 0 : _saveData.BestScore,
                _currentNumber,
                _nextNumber,
                _movesLeftToRise,
                _movesUsed,
                _level == null ? 0 : _level.MoveLimit,
                _maxCombo,
                _obsidianHits,
                _isGameOver,
                _isWon,
                RemainingSeconds,
                status));
        }

        private string StatusText()
        {
            if (_mode == GameModeType.Blitz)
                return $"Блиц: {RemainingSeconds / 60:00}:{RemainingSeconds % 60:00}";

            return "Длина линии должна совпадать с числом шара.";
        }
    }

    public enum RiseResult
    {
        Skipped = 0,
        Rose = 1,
        Overflow = 2
    }
}
