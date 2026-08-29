using System;
using System.Collections;
using NeonSeven.Configs;
using NeonSeven.Core;
using NeonSeven.Infrastructure.Services;
using NeonSeven.UI;
using UnityEngine;

namespace NeonSeven.Gameplay
{
    public enum GameplayPhase
    {
        WaitingForInput = 0,
        DropBallAnimation = 1,
        CheckMatches = 2,
        DestroyMatches = 3,
        ApplyGravity = 4,
        InterWaveDelay = 5,
        RiseChecks = 6,
        EndChecks = 7
    }

    public sealed class GameplayCycle : IDisposable
    {
        private readonly NeonSevenGameConfig _gameConfig;
        private readonly NeonSevenServices _services;
        private readonly NeonSevenView _view;
        private GameMode _mode;
        private GameModeType _currentModeType;
        private LevelConfig _currentLevel;
        private int _levelIndex;
        private GameplayPhase _phase = GameplayPhase.WaitingForInput;
        private Coroutine _turnRoutine;

        public GameplayCycle(NeonSevenGameConfig gameConfig, NeonSevenServices services, NeonSevenView view)
        {
            _gameConfig = gameConfig;
            _services = services;
            _view = view;
        }

        public GameplayPhase Phase => _phase;

        public void Prepare()
        {
            _view.Initialize(_gameConfig, DropColumn, PreviewColumn, _services.Audio.Move, StartMode, StartCampaignLevel, OnRestart, ToggleSound, OnTutorialCompleted, _services.SaveData.IsMuted);
        }

        public void Launch()
        {
            _view.ShowMenu(_services.Levels.Count, !_services.SaveData.HasCompletedTutorial);
        }

        public void Update(float deltaTime)
        {
            _mode?.Tick(deltaTime, _phase == GameplayPhase.WaitingForInput);
            _view.Tick(deltaTime);
        }

        public void Dispose()
        {
            DisposeMode();
        }

        private void StartMode(GameModeType modeType)
        {
            if (_mode != null && IsLiveEndlessSwitch(_currentModeType, modeType))
            {
                _currentModeType = modeType;
                _mode.SwitchEndlessMode(modeType);
                return;
            }

            if (modeType == GameModeType.Campaign)
                StartCampaignLevel(_levelIndex);
            else
                StartGame(null, modeType);
        }

        private void StartCampaignLevel(int levelIndex)
        {
            _levelIndex = levelIndex;
            StartGame(_services.Levels.GetLevel(levelIndex), GameModeType.Campaign);
        }

        private void StartGame(LevelConfig level, GameModeType modeType)
        {
            DisposeMode();
            _currentLevel = level;
            _currentModeType = modeType;
            _mode = new GameMode(_gameConfig, level, modeType, _services.SaveData);
            _mode.StateChanged += OnStateChanged;
            _mode.Won += OnWon;
            _mode.Lost += OnLost;
            _view.ShowGame();
            _view.SetInputLocked(false);
            _mode.Start();
            _phase = GameplayPhase.WaitingForInput;
        }

        private void DropColumn(int column)
        {
            if (_mode == null || _phase != GameplayPhase.WaitingForInput || _turnRoutine != null)
            {
                if (_mode == null)
                    _view.SetInputLocked(false);
                return;
            }

            _view.SetInputLocked(true);
            _turnRoutine = _view.PlayRoutine(RunTurn(column));
        }

        private PreviewInfo PreviewColumn(int column)
        {
            return _mode == null ? new PreviewInfo(-1, 0, 0, false) : _mode.Preview(column);
        }

        private IEnumerator RunTurn(int column)
        {
            _phase = GameplayPhase.DropBallAnimation;
            if (!_mode.TryPlaceCurrentBall(column, out BallData dropped))
            {
                FinishTurn(unlock: !_mode.IsTerminal);
                yield break;
            }

            yield return _view.AnimateDrop(dropped, dropped.Column, dropped.Row, OnDropLanded);

            yield return ResolveWaves();
            if (_mode.IsTerminal)
            {
                FinishTurn(unlock: false);
                yield break;
            }

            _mode.AdvanceBallQueue();

            _phase = GameplayPhase.RiseChecks;
            RiseResult rise = _mode.TryHandleRise();
            if (rise == RiseResult.Overflow)
            {
                FinishTurn(unlock: false);
                yield break;
            }

            if (rise == RiseResult.Rose)
            {
                _services.Audio.Rise();
                _services.Haptics.Rise();
                yield return _view.AnimateRise();
                yield return ResolveWaves();
                if (_mode.IsTerminal)
                {
                    FinishTurn(unlock: false);
                    yield break;
                }
            }

            _phase = GameplayPhase.EndChecks;
            _mode.EvaluateEndConditions();
            FinishTurn(unlock: !_mode.IsTerminal);
        }

        private IEnumerator ResolveWaves()
        {
            int wave = 1;
            while (true)
            {
                _phase = GameplayPhase.CheckMatches;
                var matches = _mode.PeekMatches();
                if (matches.Count == 0)
                    break;

                _phase = GameplayPhase.DestroyMatches;
                for (int i = 0; i < matches.Count; i++)
                    _services.Audio.Pop(wave, i);
                _services.Haptics.Pop(wave);
                yield return _view.AnimateMatchPop(matches, wave);
                ResolutionStep step = _mode.DestroyMatches(wave, matches);
                _view.PlayResolutionFeedback(step);
                if (step.HiddenHits > 0)
                    _services.Audio.Crack();

                _phase = GameplayPhase.ApplyGravity;
                _mode.ApplyGravity();
                yield return _view.AnimateGravity();

                wave++;
            }

            if (_mode.TryApplyBoardClearBonus(out int bonus))
            {
                _services.Audio.Clear();
                _services.Haptics.Clear();
                yield return _view.AnimateBoardClear(bonus);
            }
        }

        private void FinishTurn(bool unlock)
        {
            _turnRoutine = null;
            _phase = GameplayPhase.WaitingForInput;
            if (unlock)
                _view.SetInputLocked(false);
        }

        private void OnDropLanded()
        {
            _services.Audio.Drop();
            _services.Haptics.Drop();
        }

        private void OnStateChanged(GameModeSnapshot snapshot)
        {
            _view.Render(snapshot, _mode.Preview(_view.AimColumn));
        }

        private void OnWon()
        {
            _view.ShowResult(true, _levelIndex + 1 < _services.Levels.Count, OnNextLevel, OnRestart, OnMenu);
        }

        private void OnLost()
        {
            _services.Audio.Over();
            _view.ShowResult(false, false, OnNextLevel, OnRestart, OnMenu);
        }

        private void OnNextLevel()
        {
            if (_levelIndex + 1 < _services.Levels.Count)
                StartCampaignLevel(_levelIndex + 1);
        }

        private void OnRestart()
        {
            if (_currentModeType == GameModeType.Campaign)
                StartCampaignLevel(_levelIndex);
            else
                StartGame(_currentLevel, _currentModeType);
        }

        private void OnMenu()
        {
            ShowMenu();
        }

        private void ShowMenu()
        {
            DisposeMode();
            _view.ShowMenu(_services.Levels.Count, false);
        }

        private void ToggleSound()
        {
            bool muted = !_services.SaveData.IsMuted;
            _services.SaveData.SetMuted(muted);
            _services.Audio.SetMuted(muted);
            _view.SetSoundMuted(muted);
        }

        private void OnTutorialCompleted()
        {
            _services.SaveData.CompleteTutorial();
        }

        private void DisposeMode()
        {
            StopTurn();
            if (_mode == null)
                return;

            _mode.StateChanged -= OnStateChanged;
            _mode.Won -= OnWon;
            _mode.Lost -= OnLost;
            _mode.Dispose();
            _mode = null;
        }

        private void StopTurn()
        {
            if (_turnRoutine != null)
            {
                _view.StopRoutine(_turnRoutine);
                _turnRoutine = null;
            }

            _phase = GameplayPhase.WaitingForInput;
            _view.SetInputLocked(false);
        }

        private static bool IsLiveEndlessSwitch(GameModeType current, GameModeType next)
        {
            bool currentSwitchable = current == GameModeType.Classic || current == GameModeType.Zen;
            bool nextSwitchable = next == GameModeType.Classic || next == GameModeType.Zen;
            return currentSwitchable && nextSwitchable && current != next;
        }
    }
}
