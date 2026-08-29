using System;
using System.Collections;
using System.Collections.Generic;
using NeonSeven.Configs;
using NeonSeven.Core;
using NeonSeven.Gameplay;
using NeonSeven.PrototypePort.Neon7;
using UnityEngine;
using UnityEngine.UI;

namespace NeonSeven.UI
{
    public sealed class NeonSevenView : MonoBehaviour
    {
        private const float IdleHintDelay = 7f;

        private NeonSevenGameConfig _config;
        private Action<int> _dropRequested;
        private Func<int, PreviewInfo> _previewRequested;
        private Action _aimSoundRequested;
        private Action<GameModeType> _modeRequested;
        private Action<int> _levelRequested;
        private Action _restartRequested;
        private Action _soundRequested;
        private Action _tutorialCompletedRequested;
        private RectTransform _safeRoot;
        private RectTransform _board;
        private RectTransform _gridRoot;
        private RectTransform _aimRoot;
        private RectTransform _ballsRoot;
        private RectTransform _ghostRoot;
        private RectTransform _fxRoot;
        private RectTransform _columnGlow;
        private BoardPointerInput _pointerInput;
        private Image _ghostGlow;
        private Image _ghostBall;
        private Image _currentBall;
        private Image _nextBall;
        private Text _currentBallText;
        private Text _nextBallText;
        private Text _score;
        private Text _best;
        private Text _moves;
        private Text _hint;
        private Text _objective;
        private Text _progress;
        private Text _banner;
        private Text _soundText;
        private GameObject _boostersPanel;
        private GameObject _mainMenu;
        private GameObject _levelMap;
        private GameObject _levelPopup;
        private Text _levelPopupTitle;
        private Text _levelPopupGoal;
        private Button _levelFightButton;
        private GameObject _game;
        private GameObject _pause;
        private GameObject _result;
        private Text _resultTitle;
        private Text _resultText;
        private Button _nextButton;
        private Button _restartButton;
        private Button _menuButton;
        private GameObject _tutorialOverlay;
        private RectTransform _tutorialDemoBoard;
        private RectTransform _tutorialColumnGlow;
        private RectTransform _tutorialGhost;
        private RectTransform _tutorialArrow;
        private RectTransform _tutorialLaunchBall;
        private Image _tutorialLaunchBallBody;
        private Image _tutorialCurrentBall;
        private Image _tutorialNextBall;
        private Text _tutorialLaunchBallText;
        private Text _tutorialCurrentText;
        private Text _tutorialNextText;
        private Text _tutorialTitle;
        private Text _tutorialBody;
        private Text _tutorialCounter;
        private Text _tutorialRule;
        private Text _tutorialMetricCaption;
        private Text _tutorialMetricValue;
        private Button _tutorialNextButton;
        private Text _tutorialNextLabel;
        private Sprite[] _ballSprites;
        private Sprite _obsidianSprite;
        private Sprite _obsidianCrackedSprite;
        private Sprite _backgroundSprite;
        private Sprite _panelSprite;
        private Sprite _cellSprite;
        private Sprite _shockwaveSprite;
        private int _size;
        private int _currentNumber;
        private int _aimColumn = 3;
        private GameModeType _currentModeType = GameModeType.Classic;
        private int _pendingLevelIndex;
        private float _idleTimer;
        private bool _tutorialVisible = true;
        private Coroutine _unlockInputRoutine;
        private Coroutine _aimTween;
        private Coroutine _ghostTween;
        private Coroutine _shakeRoutine;
        private bool _isInputLocked;
        private bool _gridBuilt;
        private bool _tutorialActive;
        private PreviewInfo _lastPreview;
        private Vector2 _boardHome;
        private readonly List<GameObject> _ballViews = new List<GameObject>();
        private readonly List<GameObject> _fxViews = new List<GameObject>();
        private readonly List<RectTransform> _gridCells = new List<RectTransform>();
        private readonly List<RectTransform> _tutorialGridCells = new List<RectTransform>();
        private readonly List<GameObject> _tutorialBallViews = new List<GameObject>();
        private readonly List<RectTransform> _tutorialPulseTargets = new List<RectTransform>();
        private readonly HashSet<int> _tutorialPulseBallIds = new HashSet<int>();
        private readonly Dictionary<int, Coroutine> _ballMoves = new Dictionary<int, Coroutine>();
        private readonly List<Text> _boosterIcons = new List<Text>();
        private int _tutorialStepIndex = -1;
        private int _tutorialHighlightColumn = -1;
        private int _tutorialGhostColumn = -1;
        private int _tutorialGhostRow = -1;
        private int _tutorialLaunchBallNumber;
        private int _tutorialLaunchBallColumn;
        private bool _tutorialLaunchBallVisible;

        public int AimColumn => _aimColumn;

        public void Initialize(NeonSevenGameConfig config, Action<int> dropRequested, Func<int, PreviewInfo> previewRequested, Action aimSoundRequested, Action<GameModeType> modeRequested, Action<int> levelRequested, Action restartRequested = null, Action soundRequested = null, Action tutorialCompletedRequested = null, bool muted = false)
        {
            _config = config;
            _dropRequested = dropRequested;
            _previewRequested = previewRequested;
            _aimSoundRequested = aimSoundRequested;
            _modeRequested = modeRequested;
            _levelRequested = levelRequested;
            _restartRequested = restartRequested;
            _soundRequested = soundRequested;
            _tutorialCompletedRequested = tutorialCompletedRequested;
            _size = PrototypeMetrics.Size;
            LoadTexturePack();
            Build();
            SetSoundMuted(muted);
            ShowMenu(50, false);
        }

        public void Tick(float deltaTime)
        {
            if (_tutorialActive)
            {
                AnimateTutorial();
                return;
            }

            if (_game == null || !_game.activeSelf || _pause.activeSelf || _result.activeSelf)
                return;

            _idleTimer += deltaTime;
            if (_idleTimer >= IdleHintDelay)
            {
                _idleTimer = 0f;
                StartCoroutine(PulseHint());
            }

            AnimateBoosterIcons();
        }

        public void ShowGame()
        {
            SetScreen(_game);
            _pause.SetActive(false);
            _result.SetActive(false);
            _objective.gameObject.SetActive(false);
            Canvas.ForceUpdateCanvases();
            if (!_gridBuilt)
            {
                BuildBoardGrid(_gridRoot);
                _gridBuilt = true;
            }
            RefreshBoardMetrics();
            RenderAim(_lastPreview, _currentNumber);
        }

        public void ShowMenu(int levelCount, bool autoShowTutorial = false)
        {
            SetScreen(_mainMenu);
            if (autoShowTutorial)
                StartCoroutine(OpenTutorialNextFrame());
        }

        public void Render(GameModeSnapshot snapshot, PreviewInfo preview)
        {
            _currentModeType = snapshot.Mode;
            _currentNumber = snapshot.CurrentNumber;
            _lastPreview = preview;
            _score.text = snapshot.Score.ToString("N0");
            _best.text = snapshot.BestScore.ToString("N0");
            _moves.text = MovesText(snapshot);
            _objective.text = ObjectiveText(snapshot);
            _objective.gameObject.SetActive(false);
            _progress.text = ProgressText(snapshot);
            bool campaignHud = snapshot.Mode == GameModeType.Campaign;
            _hint.text = snapshot.Status;
            _progress.gameObject.SetActive(false);
            if (_boostersPanel != null)
                _boostersPanel.SetActive(campaignHud);
            SetBallIcon(_currentBall, _currentBallText, snapshot.CurrentNumber);
            SetBallIcon(_nextBall, _nextBallText, snapshot.NextNumber);
            RenderBalls(snapshot.Balls);
            RenderAim(preview, snapshot.CurrentNumber);
        }

        public void SetSoundMuted(bool muted)
        {
            if (_soundText != null)
                _soundText.text = muted ? "×" : "♪";
        }

        public void SetInputLocked(bool locked)
        {
            if (_unlockInputRoutine != null)
            {
                StopCoroutine(_unlockInputRoutine);
                _unlockInputRoutine = null;
            }

            _isInputLocked = locked;
            _pointerInput?.SetLocked(locked);
            if (locked)
                HideGhost();
        }

        public void LockInputFor(float seconds)
        {
            SetInputLocked(true);
            _unlockInputRoutine = StartCoroutine(UnlockInputAfter(seconds));
        }

        public Coroutine PlayRoutine(IEnumerator routine)
        {
            return StartCoroutine(routine);
        }

        public void StopRoutine(Coroutine routine)
        {
            if (routine != null)
                StopCoroutine(routine);
        }

        public IEnumerator WaitSeconds(float seconds)
        {
            yield return new WaitForSeconds(seconds);
        }

        public IEnumerator AnimateDrop(BallData dropped, int column, int row, Action landed)
        {
            HideGhost();
            yield return new WaitForSeconds(PrototypeMetrics.SpawnDelay);
            var go = dropped == null ? null : FindBallView(dropped.Id);
            if (go == null)
            {
                yield return new WaitForSeconds(PrototypeMetrics.DropMove);
                yield break;
            }

            var rect = go.GetComponent<RectTransform>();
            SetCellRect(rect, column, -1f, 0f);
            float time = 0f;
            while (time < PrototypeMetrics.DropMove)
            {
                time += Time.deltaTime;
                float t = Mathf.Clamp01(time / PrototypeMetrics.DropMove);
                SetCellRect(rect, column, Mathf.LerpUnclamped(-1f, row, PrototypeEasing.Drop(t)), 0f);
                yield return null;
            }

            SetCellRect(rect, column, row, 0f);
            landed?.Invoke();

            var body = go.transform.Find("Body");
            time = 0f;
            while (time < PrototypeMetrics.Squash)
            {
                time += Time.deltaTime;
                float p = PrototypeEasing.Squash(time / PrototypeMetrics.Squash);
                Vector2 scale;
                if (p < 0.55f)
                {
                    float k = p / 0.55f;
                    scale = new Vector2(Mathf.Lerp(1.25f, 0.90f, k), Mathf.Lerp(0.62f, 1.12f, k));
                }
                else
                {
                    float k = (p - 0.55f) / 0.45f;
                    scale = new Vector2(Mathf.Lerp(0.90f, 1f, k), Mathf.Lerp(1.12f, 1f, k));
                }
                body.localScale = new Vector3(scale.x, scale.y, 1f);
                yield return null;
            }

            body.localScale = Vector3.one;
        }

        public IEnumerator AnimateResolutionWave(ResolutionStep step)
        {
            PlayResolutionFeedback(step);
            yield return new WaitForSeconds(0.30f);
        }

        public IEnumerator AnimateMatchPop(IReadOnlyList<BallData> matches, int wave)
        {
            if (matches == null || matches.Count == 0)
                yield break;

            for (int i = 0; i < matches.Count; i++)
                StartCoroutine(PlayCellShockwave(matches[i].Column, matches[i].Row, GlowColorForNumber(matches[i].Number, 1f)));

            float time = 0f;
            while (time < PrototypeMetrics.Pop)
            {
                time += Time.deltaTime;
                float t = PrototypeEasing.EaseOut(time / PrototypeMetrics.Pop);
                float scale = t < 0.35f ? Mathf.Lerp(1f, 1.28f, t / 0.35f) : Mathf.Lerp(1.28f, 0.08f, (t - 0.35f) / 0.65f);
                for (int i = 0; i < matches.Count; i++)
                {
                    var go = FindBallView(matches[i].Id);
                    if (go != null)
                        go.transform.localScale = Vector3.one * scale;
                }

                yield return null;
            }
        }

        public IEnumerator AnimateGravity()
        {
            yield return new WaitForSeconds(PrototypeMetrics.Gravity);
        }

        public IEnumerator AnimateRise()
        {
            yield return new WaitForSeconds(PrototypeMetrics.Rise);
        }

        public IEnumerator AnimateBoardClear(int bonus)
        {
            StartCoroutine(ShowBanner($"ПОЛЕ ОЧИЩЕНО! +{bonus:N0}"));
            yield return new WaitForSeconds(PrototypeMetrics.ClearPause);
        }

        public void PlayResolutionFeedback(IReadOnlyList<ResolutionStep> steps)
        {
            if (steps == null || steps.Count == 0)
                return;

            for (int i = 0; i < steps.Count; i++)
                PlayResolutionFeedback(steps[i]);
        }

        public void PlayResolutionFeedback(ResolutionStep step)
        {
            if (step == null || step.Matches == null || step.Matches.Count == 0)
                return;

            string floatText = step.Wave > 1 ? $"+{step.Score:N0} COMBO x{step.Wave}!" : $"+{step.Score:N0}";
            Color floatColor = step.Wave > 1 ? _config.ColorForNumber(4) : _config.ColorForNumber(2);
            PlayFloatingScore(step.Matches[0].Column, step.Matches[0].Row, floatText, floatColor);
            if (step.Wave >= 3)
                StartCoroutine(ShowBanner($"ВОЛНА {step.Wave}  x{1 << (step.Wave - 1)}"));

            Shake(Mathf.Min(3, step.Wave));
        }

        public void ShowResult(bool won, bool hasNext, Action next, Action restart, Action menu)
        {
            _result.SetActive(true);
            _resultTitle.text = won ? "УРОВЕНЬ ПРОЙДЕН" : "ИГРА ОКОНЧЕНА";
            _resultText.text = won ? "Каскад собран. Звезды и прогресс сохранены." : "Поле переполнено или закончились ходы.";
            _nextButton.gameObject.SetActive(won && hasNext);
            _nextButton.onClick.RemoveAllListeners();
            _restartButton.onClick.RemoveAllListeners();
            _menuButton.onClick.RemoveAllListeners();
            _nextButton.onClick.AddListener(() => next?.Invoke());
            _restartButton.onClick.AddListener(() => restart?.Invoke());
            _menuButton.onClick.AddListener(() => menu?.Invoke());
        }

        private void Build()
        {
            var canvas = UiFactory.CreateCanvas("NeonSevenCanvas", 0);
            var root = canvas.GetComponent<RectTransform>();
            var background = UiFactory.Block(root, "Background", new Color(0.025f, 0.025f, 0.075f, 1f));
            UiFactory.Stretch(background.rectTransform);
            AddBackgroundGlow(root, "TopVioletGlow", new Vector2(-260f, 1040f), new Vector2(1600f, 1220f), new Color(0.50f, 0.22f, 0.92f, 0.40f));
            AddBackgroundGlow(root, "BottomCyanGlow", new Vector2(-560f, -820f), new Vector2(1180f, 900f), new Color(0.04f, 0.76f, 1f, 0.22f));

            _safeRoot = new GameObject("SafeArea").AddComponent<RectTransform>();
            _safeRoot.transform.SetParent(root, false);
            UiFactory.Stretch(_safeRoot);
            ApplySafeArea(_safeRoot);

            BuildGame(_safeRoot);
            BuildMainMenu(_safeRoot);
            BuildLevelMap(_safeRoot);
            BuildLevelPopup(_safeRoot);
            BuildPause(_safeRoot);
            BuildResult(_safeRoot);
            BuildTutorialOverlay(_safeRoot);
        }

        private void BuildGame(Transform parent)
        {
            _game = new GameObject("GameplayUI");
            _game.transform.SetParent(parent, false);
            UiFactory.Stretch(_game.AddComponent<RectTransform>());

            var title = UiFactory.Label(_game.transform, "Logo", "NEON <color=#00DFF2>SEVEN</color>", 20, Color.white, TextAnchor.UpperLeft, false);
            title.supportRichText = true;
            title.fontStyle = FontStyle.Bold;
            UiFactory.SetRect(title.rectTransform, new Vector2(12f / 420f, 0.925f), new Vector2(0.54f, 0.985f), Vector2.zero, Vector2.zero);
            var subtitle = UiFactory.Label(_game.transform, "Subtitle", "линия ровно N — взрыв", 11, new Color(0.72f, 0.70f, 0.88f, 0.9f), TextAnchor.UpperLeft);
            UiFactory.SetRect(subtitle.rectTransform, new Vector2(0.06f, 0.895f), new Vector2(0.60f, 0.925f), Vector2.zero, Vector2.zero);
            subtitle.gameObject.SetActive(false);

            var sound = UiFactory.GlassIconButton(_game.transform, "Sound", "♪", out _soundText);
            var help = UiFactory.GlassIconButton(_game.transform, "Help", "?", out _);
            UiFactory.SetRect(help.GetComponent<RectTransform>(), new Vector2(0.665f, 0.915f), new Vector2(0.755f, 0.985f), Vector2.zero, Vector2.zero);
            help.onClick.AddListener(OpenTutorial);
            UiFactory.SetRect(sound.GetComponent<RectTransform>(), new Vector2(0.775f, 0.915f), new Vector2(0.865f, 0.985f), Vector2.zero, Vector2.zero);
            sound.onClick.AddListener(() => _soundRequested?.Invoke());
            var restart = UiFactory.GlassIconButton(_game.transform, "QuickRestart", "↻", out _);
            UiFactory.SetRect(restart.GetComponent<RectTransform>(), new Vector2(0.885f, 0.915f), new Vector2(0.975f, 0.985f), Vector2.zero, Vector2.zero);
            restart.onClick.AddListener(() => _restartRequested?.Invoke());

            var scorePanel = UiFactory.Panel(_game.transform, "ScorePanel", _config.Glass, _panelSprite);
            UiFactory.SetRect(scorePanel.rectTransform, new Vector2(0.06f, 0.805f), new Vector2(0.94f, 0.885f), Vector2.zero, Vector2.zero);
            AddHudMetric(scorePanel.transform, "СЧЁТ", out _score, new Vector2(0.04f, 0.10f), new Vector2(0.28f, 0.84f), _config.ColorForNumber(2), TextAnchor.MiddleCenter);
            AddHudMetric(scorePanel.transform, "РЕКОРД", out _best, new Vector2(0.36f, 0.10f), new Vector2(0.64f, 0.84f), Color.white, TextAnchor.MiddleCenter);
            AddHudMetric(scorePanel.transform, "ДО ПОДЪЁМА", out _moves, new Vector2(0.70f, 0.10f), new Vector2(0.96f, 0.84f), _config.ColorForNumber(5), TextAnchor.MiddleCenter);

            var spawner = UiFactory.Panel(_game.transform, "SpawnerPanel", new Color(0.26f, 0.20f, 0.45f, 0.76f), _panelSprite);
            UiFactory.SetRect(spawner.rectTransform, new Vector2(0.035f, 0.730f), new Vector2(0.375f, 0.800f), Vector2.zero, Vector2.zero);
            BuildBallIcon(spawner.transform, "CurrentBall", new Vector2(0.05f, 0.10f), new Vector2(0.45f, 0.92f), out _currentBall, out _currentBallText);
            var nextLabel = UiFactory.Label(spawner.transform, "NextLabel", "СЛЕД.", 10, new Color(0.75f, 0.73f, 0.90f, 0.82f), TextAnchor.MiddleCenter);
            UiFactory.SetRect(nextLabel.rectTransform, new Vector2(0.42f, 0.20f), new Vector2(0.70f, 0.82f), Vector2.zero, Vector2.zero);
            BuildBallIcon(spawner.transform, "NextBall", new Vector2(0.68f, 0.18f), new Vector2(0.95f, 0.84f), out _nextBall, out _nextBallText);

            var switcher = UiFactory.Panel(_game.transform, "ModeSwitcher", new Color(0.19f, 0.18f, 0.39f, 0.86f), _panelSprite);
            UiFactory.SetRect(switcher.rectTransform, new Vector2(0.66f, 0.744f), new Vector2(0.955f, 0.792f), Vector2.zero, Vector2.zero);
            var classic = UiFactory.Button(switcher.transform, "Classic", "Классика", new Color(0.42f, 0.39f, 0.66f, 0.92f));
            UiFactory.SetRect(classic.GetComponent<RectTransform>(), new Vector2(0.03f, 0.10f), new Vector2(0.57f, 0.90f), Vector2.zero, Vector2.zero);
            classic.onClick.AddListener(() => _modeRequested?.Invoke(GameModeType.Classic));
            var zen = UiFactory.Button(switcher.transform, "Zen", "Дзен", new Color(0f, 0f, 0f, 0f));
            UiFactory.SetRect(zen.GetComponent<RectTransform>(), new Vector2(0.55f, 0.10f), new Vector2(0.97f, 0.90f), Vector2.zero, Vector2.zero);
            zen.onClick.AddListener(() => _modeRequested?.Invoke(GameModeType.Zen));

            _hint = UiFactory.Label(_game.transform, "Status", "", 9, new Color(0.72f, 0.70f, 0.88f, 0.90f), TextAnchor.MiddleLeft, false);
            UiFactory.SetRect(_hint.rectTransform, new Vector2(0.06f, 0.892f), new Vector2(0.70f, 0.922f), Vector2.zero, Vector2.zero);

            var boardImage = UiFactory.Panel(_game.transform, "Board", new Color(0.08f, 0.09f, 0.26f, 0.70f), _panelSprite);
            _board = boardImage.rectTransform;
            _board.anchorMin = new Vector2(12f / PrototypeMetrics.RefWidth, 0.417f);
            _board.anchorMax = new Vector2((PrototypeMetrics.RefWidth - 12f) / PrototypeMetrics.RefWidth, 0.417f);
            _board.pivot = new Vector2(0.5f, 0.5f);
            _board.sizeDelta = Vector2.zero;
            _board.sizeDelta = new Vector2(0f, 396f);
            boardImage.color = new Color(1f, 1f, 1f, 0.001f);
            var boardBackdrop = UiFactory.Panel(_board, "BoardBackdrop", new Color(0.08f, 0.09f, 0.26f, 0.70f), _panelSprite);
            boardBackdrop.raycastTarget = false;
            UiFactory.SetRect(boardBackdrop.rectTransform, Vector2.zero, Vector2.one, new Vector2(-20f, -20f), new Vector2(20f, 20f));
            boardBackdrop.rectTransform.SetAsFirstSibling();
            _pointerInput = boardImage.gameObject.AddComponent<BoardPointerInput>();
            _pointerInput.Initialize(_size, OnAimed, OnDropped);

            _gridRoot = CreateLayer(_board, "GridLayer");

            _aimRoot = CreateLayer(_board, "AimLayer");
            _columnGlow = UiFactory.Block(_aimRoot, "AimColumn", PrototypePalette.AimNormal).rectTransform;
            _columnGlow.GetComponent<Image>().sprite = UiFactory.VerticalGlow;
            _columnGlow.GetComponent<Image>().type = Image.Type.Simple;

            _ballsRoot = CreateLayer(_board, "BallsLayer");
            _ghostRoot = CreateLayer(_board, "GhostLayer");
            _fxRoot = CreateLayer(_board, "FxLayer");

            _ghostGlow = new GameObject("GhostGlow").AddComponent<Image>();
            _ghostGlow.transform.SetParent(_ghostRoot, false);
            _ghostGlow.sprite = UiFactory.Glow;
            _ghostGlow.raycastTarget = false;
            _ghostGlow.gameObject.SetActive(false);

            _ghostBall = new GameObject("GhostBall").AddComponent<Image>();
            _ghostBall.transform.SetParent(_ghostRoot, false);
            _ghostBall.sprite = UiFactory.DashedRing;
            _ghostBall.raycastTarget = false;
            _ghostBall.gameObject.SetActive(false);

            _progress = UiFactory.Label(_game.transform, "Progress", "", 11, _config.ColorForNumber(2), TextAnchor.MiddleCenter);
            UiFactory.SetRect(_progress.rectTransform, new Vector2(0.10f, 0.145f), new Vector2(0.90f, 0.175f), Vector2.zero, Vector2.zero);
            _objective = UiFactory.Label(_game.transform, "Objective", "Нажмите на шар сверху, чтобы бросить его в колонку", 11, new Color(0.80f, 0.78f, 0.92f, 0.9f), TextAnchor.MiddleCenter);
            UiFactory.SetRect(_objective.rectTransform, new Vector2(0.08f, 0.140f), new Vector2(0.92f, 0.190f), Vector2.zero, Vector2.zero);
            _objective.gameObject.SetActive(false);

            BuildBoosters(_game.transform);
        }

        private void BuildMainMenu(Transform parent)
        {
            _mainMenu = new GameObject("MainMenu");
            _mainMenu.transform.SetParent(parent, false);
            UiFactory.Stretch(_mainMenu.AddComponent<RectTransform>());

            var panel = UiFactory.Panel(_mainMenu.transform, "Panel", _config.Glass, _panelSprite);
            UiFactory.SetRect(panel.rectTransform, new Vector2(0.07f, 0.14f), new Vector2(0.93f, 0.88f), Vector2.zero, Vector2.zero);
            UiFactory.Label(panel.transform, "Title", "NEON SEVEN", 64, Color.white, TextAnchor.UpperCenter).rectTransform.offsetMax = new Vector2(0f, -70f);
            var subtitle = UiFactory.Label(panel.transform, "Subtitle", "Неоновая головоломка. Линия ровно N — взрыв.", 28, _config.ColorForNumber(2), TextAnchor.UpperCenter);
            UiFactory.SetRect(subtitle.rectTransform, new Vector2(0.08f, 0.78f), new Vector2(0.92f, 0.86f), Vector2.zero, Vector2.zero);

            AddMenuButton(panel.transform, "Campaign", "Кампания", 0.63f, () => ShowLevelMap(50));
            AddMenuButton(panel.transform, "Classic", "Классика", 0.52f, () => _modeRequested?.Invoke(GameModeType.Classic));
            AddMenuButton(panel.transform, "Blitz", "Блиц", 0.41f, () => _modeRequested?.Invoke(GameModeType.Blitz));
            AddMenuButton(panel.transform, "Zen", "Дзен", 0.30f, () => _modeRequested?.Invoke(GameModeType.Zen));
            AddMenuButton(panel.transform, "Settings", "Настройки", 0.19f, () => StartCoroutine(ShowBanner("Настройки звука и вибрации включены")));
            var help = UiFactory.GlassIconButton(panel.transform, "Help", "?", out _);
            UiFactory.SetRect(help.GetComponent<RectTransform>(), new Vector2(0.83f, 0.89f), new Vector2(0.94f, 0.97f), Vector2.zero, Vector2.zero);
            help.onClick.AddListener(OpenTutorial);
        }

        private void BuildLevelMap(Transform parent)
        {
            _levelMap = new GameObject("LevelSelectMap");
            _levelMap.transform.SetParent(parent, false);
            UiFactory.Stretch(_levelMap.AddComponent<RectTransform>());

            var panel = UiFactory.Panel(_levelMap.transform, "Panel", _config.Glass, _panelSprite);
            UiFactory.SetRect(panel.rectTransform, new Vector2(0.05f, 0.06f), new Vector2(0.95f, 0.94f), Vector2.zero, Vector2.zero);
            UiFactory.Label(panel.transform, "Title", "КАРТА УРОВНЕЙ", 44, Color.white, TextAnchor.UpperCenter).rectTransform.offsetMax = new Vector2(0f, -24f);

            var back = UiFactory.Button(panel.transform, "Back", "←", new Color(0.22f, 0.22f, 0.48f, 0.85f));
            UiFactory.SetRect(back.GetComponent<RectTransform>(), new Vector2(0.04f, 0.90f), new Vector2(0.15f, 0.97f), Vector2.zero, Vector2.zero);
            back.onClick.AddListener(() => SetScreen(_mainMenu));

            var scrollGo = new GameObject("ScrollRect");
            scrollGo.transform.SetParent(panel.transform, false);
            var scrollRectTransform = scrollGo.AddComponent<RectTransform>();
            UiFactory.SetRect(scrollRectTransform, new Vector2(0.06f, 0.08f), new Vector2(0.94f, 0.88f), Vector2.zero, Vector2.zero);
            var scroll = scrollGo.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            var maskImage = scrollGo.AddComponent<Image>();
            maskImage.color = new Color(0f, 0f, 0f, 0.01f);
            scrollGo.AddComponent<Mask>().showMaskGraphic = false;

            var content = new GameObject("Content").AddComponent<RectTransform>();
            content.transform.SetParent(scrollGo.transform, false);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.sizeDelta = new Vector2(0f, 1350f);
            scroll.content = content;

            for (int i = 0; i < 50; i++)
            {
                int index = i;
                int col = i % 5;
                int row = i / 5;
                var button = UiFactory.Button(content, $"Level_{i + 1}", $"{i + 1}\n★★★", new Color(0.10f + 0.012f * row, 0.26f, 0.46f + 0.015f * col, 0.88f));
                var rect = button.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(135f, 96f);
                rect.anchoredPosition = new Vector2(92f + col * 170f, -80f - row * 126f);
                button.onClick.AddListener(() => ShowLevelPopup(index));
            }
        }

        private void BuildLevelPopup(Transform parent)
        {
            _levelPopup = new GameObject("LevelPopup");
            _levelPopup.transform.SetParent(parent, false);
            UiFactory.Stretch(_levelPopup.AddComponent<RectTransform>());
            var overlay = UiFactory.Panel(_levelPopup.transform, "Overlay", new Color(0f, 0f, 0f, 0.62f), null);
            UiFactory.Stretch(overlay.rectTransform);
            var panel = UiFactory.Panel(_levelPopup.transform, "Panel", _config.Glass, _panelSprite);
            UiFactory.SetRect(panel.rectTransform, new Vector2(0.10f, 0.31f), new Vector2(0.90f, 0.69f), Vector2.zero, Vector2.zero);
            _levelPopupTitle = UiFactory.Label(panel.transform, "Title", "УРОВЕНЬ 1", 44, Color.white, TextAnchor.UpperCenter);
            UiFactory.SetRect(_levelPopupTitle.rectTransform, new Vector2(0.06f, 0.72f), new Vector2(0.94f, 0.94f), Vector2.zero, Vector2.zero);
            _levelPopupGoal = UiFactory.Label(panel.transform, "Goal", "Разбей камни за 20 ходов", 28, _config.ColorForNumber(2), TextAnchor.MiddleCenter);
            UiFactory.SetRect(_levelPopupGoal.rectTransform, new Vector2(0.08f, 0.43f), new Vector2(0.92f, 0.70f), Vector2.zero, Vector2.zero);
            UiFactory.Label(panel.transform, "Boosters", "Бустеры: Бомба · Радуга · Смена шара", 22, new Color(0.85f, 0.82f, 1f, 0.88f), TextAnchor.MiddleCenter).rectTransform.offsetMin = new Vector2(0f, -40f);
            _levelFightButton = UiFactory.Button(panel.transform, "Fight", "В бой!", new Color(0.10f, 0.62f, 0.78f, 0.95f));
            UiFactory.SetRect(_levelFightButton.GetComponent<RectTransform>(), new Vector2(0.18f, 0.10f), new Vector2(0.82f, 0.28f), Vector2.zero, Vector2.zero);
        }

        private void BuildPause(Transform parent)
        {
            _pause = new GameObject("PausePopup");
            _pause.transform.SetParent(parent, false);
            UiFactory.Stretch(_pause.AddComponent<RectTransform>());
            var overlay = UiFactory.Panel(_pause.transform, "Overlay", new Color(0f, 0f, 0f, 0.55f), null);
            UiFactory.Stretch(overlay.rectTransform);
            var panel = UiFactory.Panel(_pause.transform, "Panel", _config.Glass, _panelSprite);
            UiFactory.SetRect(panel.rectTransform, new Vector2(0.14f, 0.36f), new Vector2(0.86f, 0.64f), Vector2.zero, Vector2.zero);
            UiFactory.Label(panel.transform, "Title", "ПАУЗА", 44, Color.white, TextAnchor.UpperCenter).rectTransform.offsetMax = new Vector2(0f, -28f);
            var resume = UiFactory.Button(panel.transform, "Resume", "Продолжить", new Color(0.12f, 0.62f, 0.75f, 0.92f));
            UiFactory.SetRect(resume.GetComponent<RectTransform>(), new Vector2(0.16f, 0.38f), new Vector2(0.84f, 0.56f), Vector2.zero, Vector2.zero);
            resume.onClick.AddListener(() => _pause.SetActive(false));
            var menu = UiFactory.Button(panel.transform, "Menu", "Меню", new Color(0.38f, 0.25f, 0.58f, 0.92f));
            UiFactory.SetRect(menu.GetComponent<RectTransform>(), new Vector2(0.16f, 0.14f), new Vector2(0.84f, 0.32f), Vector2.zero, Vector2.zero);
            menu.onClick.AddListener(() => ShowMenu(50, false));
            _pause.SetActive(false);
        }

        private void BuildResult(RectTransform root)
        {
            _result = new GameObject("ResultPopup");
            _result.transform.SetParent(root, false);
            UiFactory.Stretch(_result.AddComponent<RectTransform>());
            var overlay = UiFactory.Panel(_result.transform, "Overlay", new Color(0f, 0f, 0f, 0.68f), null);
            UiFactory.Stretch(overlay.rectTransform);
            var panel = UiFactory.Panel(_result.transform, "Panel", _config.Glass, _panelSprite);
            UiFactory.SetRect(panel.rectTransform, new Vector2(0.10f, 0.33f), new Vector2(0.90f, 0.67f), Vector2.zero, Vector2.zero);
            _resultTitle = UiFactory.Label(panel.transform, "Title", "GAME OVER", 44, _config.ColorForNumber(6), TextAnchor.UpperCenter);
            UiFactory.SetRect(_resultTitle.rectTransform, new Vector2(0.06f, 0.66f), new Vector2(0.94f, 0.94f), Vector2.zero, Vector2.zero);
            _resultText = UiFactory.Label(panel.transform, "Text", "", 26, Color.white, TextAnchor.MiddleCenter);
            UiFactory.SetRect(_resultText.rectTransform, new Vector2(0.06f, 0.42f), new Vector2(0.94f, 0.66f), Vector2.zero, Vector2.zero);
            _nextButton = UiFactory.Button(panel.transform, "Next", "Дальше", new Color(0.12f, 0.72f, 0.38f, 0.9f));
            UiFactory.SetRect(_nextButton.GetComponent<RectTransform>(), new Vector2(0.08f, 0.12f), new Vector2(0.33f, 0.32f), Vector2.zero, Vector2.zero);
            _restartButton = UiFactory.Button(panel.transform, "Restart", "Заново", new Color(0.12f, 0.52f, 0.72f, 0.9f));
            UiFactory.SetRect(_restartButton.GetComponent<RectTransform>(), new Vector2(0.38f, 0.12f), new Vector2(0.63f, 0.32f), Vector2.zero, Vector2.zero);
            _menuButton = UiFactory.Button(panel.transform, "Menu", "Меню", new Color(0.60f, 0.34f, 0.15f, 0.9f));
            UiFactory.SetRect(_menuButton.GetComponent<RectTransform>(), new Vector2(0.68f, 0.12f), new Vector2(0.92f, 0.32f), Vector2.zero, Vector2.zero);
            _banner = UiFactory.Label(root, "Banner", "", 54, _config.ColorForNumber(4), TextAnchor.MiddleCenter);
            UiFactory.SetRect(_banner.rectTransform, new Vector2(0.06f, 0.44f), new Vector2(0.94f, 0.56f), Vector2.zero, Vector2.zero);
            _banner.gameObject.SetActive(false);
            _result.SetActive(false);
        }

        private void BuildTutorialOverlay(Transform parent)
        {
            _tutorialOverlay = new GameObject("TutorialOverlay");
            _tutorialOverlay.transform.SetParent(parent, false);
            UiFactory.Stretch(_tutorialOverlay.AddComponent<RectTransform>());

            var overlay = UiFactory.Panel(_tutorialOverlay.transform, "Overlay", new Color(0.01f, 0.01f, 0.05f, 0.88f), null);
            UiFactory.Stretch(overlay.rectTransform);

            var panel = UiFactory.Panel(_tutorialOverlay.transform, "Panel", new Color(0.12f, 0.11f, 0.23f, 0.92f), _panelSprite);
            UiFactory.SetRect(panel.rectTransform, new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.95f), Vector2.zero, Vector2.zero);

            _tutorialTitle = UiFactory.Label(panel.transform, "Title", "КАК ИГРАТЬ", 20, Color.white, TextAnchor.MiddleCenter, false);
            UiFactory.SetRect(_tutorialTitle.rectTransform, new Vector2(0.10f, 0.90f), new Vector2(0.76f, 0.97f), Vector2.zero, Vector2.zero);
            _tutorialCounter = UiFactory.Label(panel.transform, "Counter", "1 / 4", 11, new Color(0.78f, 0.76f, 0.92f, 0.88f), TextAnchor.MiddleRight);
            UiFactory.SetRect(_tutorialCounter.rectTransform, new Vector2(0.72f, 0.905f), new Vector2(0.88f, 0.965f), Vector2.zero, Vector2.zero);

            var skip = UiFactory.Button(panel.transform, "Skip", "Пропустить", new Color(0.45f, 0.26f, 0.56f, 0.92f));
            UiFactory.SetRect(skip.GetComponent<RectTransform>(), new Vector2(0.65f, 0.075f), new Vector2(0.92f, 0.15f), Vector2.zero, Vector2.zero);
            skip.onClick.AddListener(() => CloseTutorial(true));

            var demoHud = UiFactory.Panel(panel.transform, "DemoHud", new Color(0.22f, 0.18f, 0.36f, 0.82f), _panelSprite);
            UiFactory.SetRect(demoHud.rectTransform, new Vector2(0.08f, 0.77f), new Vector2(0.92f, 0.87f), Vector2.zero, Vector2.zero);
            BuildBallIcon(demoHud.transform, "TutorialCurrentBall", new Vector2(0.04f, 0.12f), new Vector2(0.16f, 0.88f), out _tutorialCurrentBall, out _tutorialCurrentText);
            BuildBallIcon(demoHud.transform, "TutorialNextBall", new Vector2(0.31f, 0.22f), new Vector2(0.41f, 0.78f), out _tutorialNextBall, out _tutorialNextText);
            var nextLabel = UiFactory.Label(demoHud.transform, "TutorialNextLabel", "СЛЕД.", 10, new Color(0.76f, 0.74f, 0.90f, 0.82f), TextAnchor.MiddleCenter);
            UiFactory.SetRect(nextLabel.rectTransform, new Vector2(0.17f, 0.20f), new Vector2(0.29f, 0.78f), Vector2.zero, Vector2.zero);
            _tutorialMetricCaption = UiFactory.Label(demoHud.transform, "TutorialMetricCaption", "ПРАВИЛО", 10, new Color(0.78f, 0.76f, 0.92f, 0.84f), TextAnchor.MiddleLeft);
            UiFactory.SetRect(_tutorialMetricCaption.rectTransform, new Vector2(0.47f, 0.18f), new Vector2(0.61f, 0.82f), Vector2.zero, Vector2.zero);
            _tutorialMetricValue = UiFactory.Label(demoHud.transform, "TutorialMetricValue", "↕ 2 · ↔ 1", 16, _config.ColorForNumber(2), TextAnchor.MiddleRight, false);
            UiFactory.SetRect(_tutorialMetricValue.rectTransform, new Vector2(0.62f, 0.15f), new Vector2(0.93f, 0.85f), Vector2.zero, Vector2.zero);

            _tutorialDemoBoard = new GameObject("TutorialBoard").AddComponent<RectTransform>();
            _tutorialDemoBoard.transform.SetParent(panel.transform, false);
            _tutorialDemoBoard.anchorMin = _tutorialDemoBoard.anchorMax = new Vector2(0.5f, 0.76f);
            _tutorialDemoBoard.pivot = new Vector2(0.5f, 1f);
            _tutorialDemoBoard.sizeDelta = new Vector2(336f, 336f);
            _tutorialDemoBoard.anchoredPosition = Vector2.zero;

            for (int row = 0; row < 7; row++)
            {
                for (int column = 0; column < 7; column++)
                {
                    var cell = new GameObject("tutorial_grid_cell").AddComponent<Image>();
                    cell.transform.SetParent(_tutorialDemoBoard, false);
                    cell.sprite = _cellSprite;
                    cell.type = _cellSprite == null ? Image.Type.Simple : Image.Type.Sliced;
                    cell.color = new Color(1f, 1f, 1f, 0.12f);
                    cell.raycastTarget = false;
                    var rect = cell.rectTransform;
                    rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    _tutorialGridCells.Add(rect);
                }
            }

            _tutorialColumnGlow = UiFactory.Block(_tutorialDemoBoard, "TutorialColumnGlow", PrototypePalette.AimNormal).rectTransform;
            _tutorialColumnGlow.GetComponent<Image>().sprite = UiFactory.VerticalGlow;
            _tutorialColumnGlow.GetComponent<Image>().color = PrototypePalette.AimNormal;
            _tutorialColumnGlow.pivot = new Vector2(0f, 1f);
            _tutorialColumnGlow.anchorMin = _tutorialColumnGlow.anchorMax = new Vector2(0f, 1f);

            _tutorialGhost = new GameObject("TutorialGhost").AddComponent<Image>().rectTransform;
            _tutorialGhost.transform.SetParent(_tutorialDemoBoard, false);
            _tutorialGhost.GetComponent<Image>().sprite = UiFactory.DashedRing;
            _tutorialGhost.GetComponent<Image>().color = PrototypePalette.GhostBorder;
            _tutorialGhost.GetComponent<Image>().raycastTarget = false;
            _tutorialGhost.anchorMin = _tutorialGhost.anchorMax = new Vector2(0f, 1f);
            _tutorialGhost.pivot = new Vector2(0.5f, 0.5f);

            _tutorialArrow = UiFactory.Label(_tutorialDemoBoard, "TutorialArrow", "↓", 24, _config.ColorForNumber(2), TextAnchor.MiddleCenter, false).rectTransform;
            _tutorialArrow.anchorMin = _tutorialArrow.anchorMax = new Vector2(0f, 1f);
            _tutorialArrow.pivot = new Vector2(0.5f, 0.5f);

            var launchRoot = new GameObject("TutorialLaunchBall").AddComponent<RectTransform>();
            launchRoot.transform.SetParent(_tutorialDemoBoard, false);
            launchRoot.anchorMin = launchRoot.anchorMax = new Vector2(0f, 1f);
            launchRoot.pivot = new Vector2(0.5f, 0.5f);
            _tutorialLaunchBall = launchRoot;
            var launchGlow = new GameObject("Glow").AddComponent<Image>();
            launchGlow.transform.SetParent(launchRoot, false);
            launchGlow.sprite = UiFactory.Glow;
            launchGlow.raycastTarget = false;
            UiFactory.Stretch(launchGlow.rectTransform);
            _tutorialLaunchBallBody = new GameObject("Body").AddComponent<Image>();
            _tutorialLaunchBallBody.transform.SetParent(launchRoot, false);
            _tutorialLaunchBallBody.raycastTarget = false;
            UiFactory.Stretch(_tutorialLaunchBallBody.rectTransform);
            _tutorialLaunchBallText = UiFactory.Label(launchRoot, "Text", string.Empty, 18, PrototypePalette.NumInk, TextAnchor.MiddleCenter, false);
            UiFactory.Stretch(_tutorialLaunchBallText.rectTransform);

            var textCard = UiFactory.Panel(panel.transform, "TutorialTextCard", new Color(0.18f, 0.18f, 0.28f, 0.92f), _panelSprite);
            UiFactory.SetRect(textCard.rectTransform, new Vector2(0.08f, 0.537f), new Vector2(0.92f, 0.697f), Vector2.zero, Vector2.zero);
            _tutorialRule = UiFactory.Label(textCard.transform, "Rule", "Линия ровно N - взрыв", 12, _config.ColorForNumber(4), TextAnchor.MiddleCenter, false);
            UiFactory.SetRect(_tutorialRule.rectTransform, new Vector2(0.08f, 0.68f), new Vector2(0.92f, 0.90f), Vector2.zero, Vector2.zero);
            _tutorialBody = UiFactory.Label(textCard.transform, "Body", "", 12, Color.white, TextAnchor.MiddleCenter, false);
            UiFactory.SetRect(_tutorialBody.rectTransform, new Vector2(0.10f, 0.16f), new Vector2(0.90f, 0.64f), Vector2.zero, Vector2.zero);

            _tutorialNextButton = UiFactory.Button(panel.transform, "TutorialNext", "Дальше", new Color(0.10f, 0.62f, 0.78f, 0.95f));
            UiFactory.SetRect(_tutorialNextButton.GetComponent<RectTransform>(), new Vector2(0.18f, 0.075f), new Vector2(0.58f, 0.15f), Vector2.zero, Vector2.zero);
            _tutorialNextButton.onClick.AddListener(AdvanceTutorial);
            _tutorialNextLabel = _tutorialNextButton.transform.Find("Text").GetComponent<Text>();

            for (int i = 0; i < _tutorialGridCells.Count; i++)
                SetTutorialCellRect(_tutorialGridCells[i], i % 7, i / 7);

            _tutorialOverlay.SetActive(false);
        }

        private void BuildBoosters(Transform parent)
        {
            var panel = UiFactory.Panel(parent, "Boosters", new Color(0f, 0f, 0f, 0f), null);
            _boostersPanel = panel.gameObject;
            UiFactory.SetRect(panel.rectTransform, new Vector2(0.06f, 0.020f), new Vector2(0.94f, 0.095f), Vector2.zero, Vector2.zero);
            AddBooster(panel.transform, "БОМБА", "✦", 0f);
            AddBooster(panel.transform, "ДОЖДЬ", "⋮", 0.345f);
            AddBooster(panel.transform, "ОБМЕН", "⇄", 0.69f);
            _boostersPanel.SetActive(false);
        }

        private void AddBackgroundGlow(RectTransform parent, string name, Vector2 offsetMin, Vector2 offsetMax, Color color)
        {
            var glow = new GameObject(name).AddComponent<Image>();
            glow.transform.SetParent(parent, false);
            glow.sprite = UiFactory.Glow;
            glow.color = color;
            glow.raycastTarget = false;
            glow.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            glow.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            glow.rectTransform.offsetMin = offsetMin;
            glow.rectTransform.offsetMax = offsetMax;
        }

        private void BuildBoardGrid(RectTransform parent)
        {
            _gridCells.Clear();
            for (int row = 0; row < 7; row++)
            {
                for (int column = 0; column < 7; column++)
                {
                    var cell = new GameObject("grid_cell").AddComponent<Image>();
                    cell.transform.SetParent(parent, false);
                    cell.sprite = _cellSprite;
                    cell.type = _cellSprite == null ? Image.Type.Simple : Image.Type.Sliced;
                    cell.color = new Color(
                        PrototypePalette.GridLine.r,
                        PrototypePalette.GridLine.g,
                        PrototypePalette.GridLine.b,
                        0.12f);
                    cell.raycastTarget = false;
                    var rect = cell.rectTransform;
                    rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    _gridCells.Add(rect);
                }
            }

            RefreshBoardMetrics();
        }

        private static RectTransform CreateLayer(Transform parent, string name)
        {
            var layer = new GameObject(name).AddComponent<RectTransform>();
            layer.transform.SetParent(parent, false);
            UiFactory.Stretch(layer);
            return layer;
        }

        private void RefreshBoardMetrics()
        {
            if (_board == null)
                return;

            float boardWidth = _board.rect.width;
            if (boardWidth <= 0f)
                return;

            float cell = PrototypeMetrics.Cell(boardWidth);
            ConfigureBoardLayer(_gridRoot);
            ConfigureBoardLayer(_aimRoot);
            ConfigureBoardLayer(_ballsRoot);
            ConfigureBoardLayer(_ghostRoot);
            ConfigureBoardLayer(_fxRoot);
            for (int i = 0; i < _gridCells.Count; i++)
            {
                int row = i / 7;
                int column = i % 7;
                var rect = _gridCells[i];
                rect.sizeDelta = new Vector2(cell, cell);
                rect.anchoredPosition = new Vector2((column + 0.5f) * cell, -(row + 0.5f) * cell);
            }

            if (_columnGlow != null)
            {
                _columnGlow.anchorMin = _columnGlow.anchorMax = new Vector2(0f, 1f);
                _columnGlow.pivot = new Vector2(0f, 1f);
                _columnGlow.sizeDelta = new Vector2(cell, boardWidth);
                _columnGlow.anchoredPosition = new Vector2(_aimColumn * cell, 0f);
            }
        }

        private void ConfigureBoardLayer(RectTransform layer)
        {
            if (layer == null)
                return;

            layer.anchorMin = layer.anchorMax = new Vector2(0f, 1f);
            layer.pivot = new Vector2(0f, 1f);
            layer.sizeDelta = new Vector2(_board.rect.width, _board.rect.height);
            layer.anchoredPosition = Vector2.zero;
        }

        private void AddBooster(Transform parent, string labelText, string iconGlyph, float x)
        {
            var button = UiFactory.Button(parent, "Booster", labelText, new Color(0.18f, 0.18f, 0.38f, 0.82f));
            UiFactory.SetRect(button.GetComponent<RectTransform>(), new Vector2(x, 0f), new Vector2(x + 0.30f, 1f), Vector2.zero, Vector2.zero);
            var label = button.transform.Find("Text")?.GetComponent<Text>();
            if (label != null)
            {
                label.fontSize = 10;
                UiFactory.SetRect(label.rectTransform, new Vector2(0.18f, 0.14f), new Vector2(0.82f, 0.48f), Vector2.zero, Vector2.zero);
            }

            var icon = UiFactory.Label(button.transform, "Icon", iconGlyph, 18, new Color(0.93f, 0.90f, 1f, 0.92f), TextAnchor.MiddleCenter, false);
            icon.fontStyle = FontStyle.Bold;
            UiFactory.SetRect(icon.rectTransform, new Vector2(0.18f, 0.48f), new Vector2(0.82f, 0.88f), Vector2.zero, Vector2.zero);
            _boosterIcons.Add(icon);
            button.onClick.AddListener(() => StartCoroutine(ShowBanner("Бустер будет доступен в следующем шаге")));
        }

        private void AnimateBoosterIcons()
        {
            float time = Time.unscaledTime;
            for (int i = 0; i < _boosterIcons.Count; i++)
            {
                var icon = _boosterIcons[i];
                if (icon == null)
                    continue;

                float phase = time * 2.6f + i * 0.9f;
                float scale = 1f + 0.08f * Mathf.Sin(phase);
                float tilt = 6f * Mathf.Sin(phase * 0.8f);
                float alpha = 0.82f + 0.18f * (0.5f + 0.5f * Mathf.Sin(phase + 0.4f));
                icon.rectTransform.localScale = new Vector3(scale, scale, 1f);
                icon.rectTransform.localRotation = Quaternion.Euler(0f, 0f, tilt);
                icon.color = new Color(0.93f, 0.90f, 1f, alpha);
            }
        }

        private void AddHudMetric(Transform parent, string caption, out Text value, Vector2 min, Vector2 max, Color valueColor, TextAnchor valueAnchor)
        {
            var label = UiFactory.Label(parent, caption + "Label", caption, 10, new Color(0.74f, 0.72f, 0.88f, 0.82f), TextAnchor.UpperCenter);
            UiFactory.SetRect(label.rectTransform, new Vector2(min.x, 0.64f), new Vector2(max.x, 0.96f), Vector2.zero, Vector2.zero);
            value = UiFactory.Label(parent, caption + "Value", "0", 18, valueColor, valueAnchor, false);
            UiFactory.SetRect(value.rectTransform, new Vector2(min.x, 0.08f), new Vector2(max.x, 0.62f), Vector2.zero, Vector2.zero);
            value.fontStyle = FontStyle.Bold;
        }

        private void AddMenuButton(Transform parent, string name, string text, float y, Action action)
        {
            var button = UiFactory.Button(parent, name, text, new Color(0.16f, 0.42f, 0.62f, 0.88f));
            UiFactory.SetRect(button.GetComponent<RectTransform>(), new Vector2(0.16f, y), new Vector2(0.84f, y + 0.075f), Vector2.zero, Vector2.zero);
            var label = button.transform.Find("Text")?.GetComponent<Text>();
            if (label != null)
                label.fontSize = 20;
            button.onClick.AddListener(() => action?.Invoke());
        }

        private void BuildBallIcon(Transform parent, string name, Vector2 min, Vector2 max, out Image image, out Text label)
        {
            var root = new GameObject(name).AddComponent<RectTransform>();
            root.transform.SetParent(parent, false);
            UiFactory.SetRect(root, min, max, Vector2.zero, Vector2.zero);

            var glow = new GameObject("Glow").AddComponent<Image>();
            glow.transform.SetParent(root, false);
            glow.sprite = UiFactory.Glow;
            glow.raycastTarget = false;
            UiFactory.Stretch(glow.rectTransform);
            glow.rectTransform.offsetMin = new Vector2(-34f, -34f);
            glow.rectTransform.offsetMax = new Vector2(34f, 34f);

            image = new GameObject("Body").AddComponent<Image>();
            image.transform.SetParent(root, false);
            image.raycastTarget = false;
            UiFactory.Stretch(image.rectTransform);
            label = UiFactory.Label(root, "Number", "", 20, new Color(0.04f, 0.03f, 0.08f), TextAnchor.MiddleCenter, false);
            UiFactory.Stretch(label.rectTransform);
        }

        private void ShowLevelMap(int levelCount)
        {
            SetScreen(_levelMap);
        }

        private IEnumerator OpenTutorialNextFrame()
        {
            yield return null;
            OpenTutorial();
        }

        private void OpenTutorial()
        {
            if (_tutorialOverlay == null || _tutorialActive)
                return;

            _tutorialActive = true;
            _tutorialOverlay.SetActive(true);
            _tutorialOverlay.transform.SetAsLastSibling();
            RefreshTutorialBoardMetrics();
            _tutorialStepIndex = -1;
            AdvanceTutorial();
        }

        private void AdvanceTutorial()
        {
            _aimSoundRequested?.Invoke();
            _tutorialStepIndex++;
            if (_tutorialStepIndex >= 4)
            {
                CloseTutorial(true);
                return;
            }

            ApplyTutorialStep(_tutorialStepIndex);
            _tutorialCounter.text = $"Шаг {_tutorialStepIndex + 1} из 4";
            _tutorialNextLabel.text = _tutorialStepIndex == 3 ? "Завершить" : "Дальше";
        }

        private void CloseTutorial(bool completed)
        {
            _tutorialActive = false;
            _tutorialOverlay?.SetActive(false);
            _tutorialVisible = false;
            if (completed)
                _tutorialCompletedRequested?.Invoke();
        }

        private void ApplyTutorialStep(int stepIndex)
        {
            switch (stepIndex)
            {
                case 0:
                    _tutorialTitle.text = "КАК СДЕЛАТЬ ХОД";
                    _tutorialRule.text = "Нажмите на шар сверху. Он упадёт в выбранную колонку.";
                    _tutorialBody.text = "Шар летит по лучу и занимает нижнюю свободную клетку. Следующий шар уже показан рядом.";
                    _tutorialMetricCaption.gameObject.SetActive(false);
                    _tutorialMetricValue.gameObject.SetActive(false);
                    SetBallIcon(_tutorialCurrentBall, _tutorialCurrentText, 2);
                    SetBallIcon(_tutorialNextBall, _tutorialNextText, 6);
                    _tutorialHighlightColumn = 0;
                    _tutorialGhostColumn = 0;
                    _tutorialGhostRow = 4;
                    SetTutorialLaunchBall(2, 0, true);
                    SetTutorialPulseBallIds(1, 2);
                    RenderTutorialBalls(
                        new BallData(1, 0, 5, 2, 0),
                        new BallData(2, 0, 6, 0, 0),
                        new BallData(3, 5, 5, 6, 0),
                        new BallData(4, 6, 5, 7, 0));
                    break;
                case 1:
                    _tutorialTitle.text = "КОГДА ШАР ВЗРЫВАЕТСЯ";
                    _tutorialRule.text = "Шар исчезает, если длина линии равна его числу.";
                    _tutorialBody.text = "Здесь двойка стоит в линии из двух шаров. Поэтому сработает взрыв. Если линия короче или длиннее, ничего не произойдёт.";
                    _tutorialMetricCaption.gameObject.SetActive(true);
                    _tutorialMetricValue.gameObject.SetActive(true);
                    _tutorialMetricCaption.text = "ЛИНИЯ";
                    _tutorialMetricValue.text = "Два шара.";
                    SetBallIcon(_tutorialCurrentBall, _tutorialCurrentText, 2);
                    SetBallIcon(_tutorialNextBall, _tutorialNextText, 3);
                    _tutorialHighlightColumn = 0;
                    _tutorialGhostColumn = -1;
                    _tutorialGhostRow = -1;
                    SetTutorialLaunchBall(0, 0, false);
                    SetTutorialPulseBallIds(10, 11);
                    RenderTutorialBalls(
                        new BallData(10, 0, 5, 2, 0),
                        new BallData(11, 0, 6, 2, 0),
                        new BallData(12, 5, 5, 6, 0),
                        new BallData(13, 6, 5, 7, 0));
                    break;
                case 2:
                    _tutorialTitle.text = "КАК РАБОТАЮТ КАМНИ";
                    _tutorialRule.text = "Камень раскрывается после двух соседних взрывов.";
                    _tutorialBody.text = "Первый взрыв оставляет трещину. Второй взрыв открывает спрятанный шар.";
                    _tutorialMetricCaption.gameObject.SetActive(true);
                    _tutorialMetricValue.gameObject.SetActive(true);
                    _tutorialMetricCaption.text = "КАМЕНЬ";
                    _tutorialMetricValue.text = "Нужен ещё один удар.";
                    SetBallIcon(_tutorialCurrentBall, _tutorialCurrentText, 4);
                    SetBallIcon(_tutorialNextBall, _tutorialNextText, 1);
                    _tutorialHighlightColumn = 3;
                    _tutorialGhostColumn = -1;
                    _tutorialGhostRow = -1;
                    SetTutorialLaunchBall(0, 0, false);
                    SetTutorialPulseBallIds(20, 21, 22);
                    RenderTutorialBalls(
                        new BallData(20, 2, 4, 0, 1),
                        new BallData(21, 3, 4, 4, 0),
                        new BallData(22, 4, 4, 0, 0),
                        new BallData(23, 3, 5, 1, 0),
                        new BallData(24, 5, 4, 5, 0));
                    break;
                default:
                    _tutorialTitle.text = "КОМБО И РЕЖИМЫ";
                    _tutorialRule.text = "После взрыва шары падают и могут собрать новую линию.";
                    _tutorialBody.text = "Так появляются комбо и растут очки. В Классике поле поднимается, в Блице время ограничено, а в Дзене можно играть спокойно.";
                    _tutorialMetricCaption.gameObject.SetActive(true);
                    _tutorialMetricValue.gameObject.SetActive(true);
                    _tutorialMetricCaption.text = "РЕЖИМ";
                    _tutorialMetricValue.text = "Блиц. 02:00.";
                    SetBallIcon(_tutorialCurrentBall, _tutorialCurrentText, 3);
                    SetBallIcon(_tutorialNextBall, _tutorialNextText, 5);
                    _tutorialHighlightColumn = 2;
                    _tutorialGhostColumn = 2;
                    _tutorialGhostRow = 3;
                    SetTutorialLaunchBall(3, 2, true);
                    SetTutorialPulseBallIds(30, 31, 32);
                    RenderTutorialBalls(
                        new BallData(30, 2, 4, 3, 0),
                        new BallData(31, 2, 5, 1, 0),
                        new BallData(32, 2, 6, 2, 0),
                        new BallData(33, 4, 5, 5, 0),
                        new BallData(34, 5, 5, 6, 0),
                        new BallData(35, 6, 5, 7, 0));
                    break;
            }
        }

        private void RenderTutorialBalls(params BallData[] balls)
        {
            _tutorialPulseTargets.Clear();
            RefreshTutorialBoardMetrics();
            float cell = TutorialBoardCellSize();
            float diameter = cell * 0.84f;
            for (int i = 0; i < balls.Length; i++)
            {
                var ball = balls[i];
                var go = GetTutorialBallView(i);
                go.SetActive(true);
                var rect = go.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(cell, cell);
                rect.anchoredPosition = TutorialCellPosition(ball.Column, ball.Row);

                var glow = go.transform.Find("Glow").GetComponent<Image>();
                var body = go.transform.Find("Body").GetComponent<Image>();
                var cracks = go.transform.Find("Cracks").GetComponent<Image>();
                var text = go.transform.Find("Text").GetComponent<Text>();

                body.sprite = SpriteFor(ball);
                body.color = Color.white;
                body.rectTransform.anchorMin = body.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                body.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                body.rectTransform.sizeDelta = new Vector2(diameter, diameter);

                glow.enabled = !ball.IsHidden;
                glow.color = GlowColorForNumber(ball.Number, 0.56f);
                glow.rectTransform.anchorMin = glow.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                glow.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                glow.rectTransform.sizeDelta = new Vector2(diameter + 26f, diameter + 26f);

                cracks.enabled = ball.IsHidden && ball.Cracks > 0;
                cracks.sprite = _obsidianCrackedSprite;
                cracks.color = Color.white;
                cracks.rectTransform.anchorMin = cracks.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                cracks.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                cracks.rectTransform.sizeDelta = new Vector2(diameter, diameter);

                text.text = ball.IsHidden ? string.Empty : ball.Number.ToString();
                text.fontSize = 18;
                text.color = PrototypePalette.NumInk;
                if (_tutorialPulseBallIds.Contains(ball.Id) || _tutorialHighlightColumn == ball.Column)
                    _tutorialPulseTargets.Add(rect);
            }

            for (int i = balls.Length; i < _tutorialBallViews.Count; i++)
                _tutorialBallViews[i].SetActive(false);

            UpdateTutorialGuideVisuals();
        }

        private void SetTutorialPulseBallIds(params int[] ids)
        {
            _tutorialPulseBallIds.Clear();
            for (int i = 0; i < ids.Length; i++)
                _tutorialPulseBallIds.Add(ids[i]);
        }

        private void SetTutorialLaunchBall(int number, int column, bool visible)
        {
            if (_tutorialLaunchBall == null || _tutorialLaunchBallBody == null || _tutorialLaunchBallText == null)
                return;

            _tutorialLaunchBallNumber = number;
            _tutorialLaunchBallColumn = column;
            _tutorialLaunchBallVisible = visible;
            _tutorialLaunchBall.gameObject.SetActive(visible);
            if (!visible)
                return;

            float cell = TutorialBoardCellSize();
            float diameter = cell * 0.88f;
            _tutorialLaunchBall.sizeDelta = new Vector2(cell, cell);
            _tutorialLaunchBall.anchoredPosition = new Vector2(TutorialColumnCenter(column), -0.28f * cell);
            _tutorialLaunchBallBody.sprite = SpriteForNumber(number);
            _tutorialLaunchBallBody.color = Color.white;
            _tutorialLaunchBallBody.rectTransform.anchorMin = _tutorialLaunchBallBody.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _tutorialLaunchBallBody.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            _tutorialLaunchBallBody.rectTransform.sizeDelta = new Vector2(diameter, diameter);

            var glow = _tutorialLaunchBall.Find("Glow").GetComponent<Image>();
            glow.color = GlowColorForNumber(number, 0.62f);
            glow.rectTransform.anchorMin = glow.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            glow.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            glow.rectTransform.sizeDelta = new Vector2(diameter + 28f, diameter + 28f);

            _tutorialLaunchBallText.text = number.ToString();
            _tutorialLaunchBallText.fontSize = 20;
            _tutorialLaunchBallText.color = PrototypePalette.NumInk;
        }

        private GameObject GetTutorialBallView(int index)
        {
            while (_tutorialBallViews.Count <= index)
            {
                var go = new GameObject("TutorialBall");
                var rect = go.AddComponent<RectTransform>();
                rect.transform.SetParent(_tutorialDemoBoard, false);
                rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0.5f, 0.5f);

                var glow = new GameObject("Glow").AddComponent<Image>();
                glow.transform.SetParent(go.transform, false);
                glow.sprite = UiFactory.Glow;
                glow.raycastTarget = false;

                var body = new GameObject("Body").AddComponent<Image>();
                body.transform.SetParent(go.transform, false);
                body.raycastTarget = false;

                var cracks = new GameObject("Cracks").AddComponent<Image>();
                cracks.transform.SetParent(go.transform, false);
                cracks.raycastTarget = false;

                var text = UiFactory.Label(go.transform, "Text", string.Empty, 18, PrototypePalette.NumInk, TextAnchor.MiddleCenter, false);
                UiFactory.Stretch(text.rectTransform);

                _tutorialBallViews.Add(go);
            }

            return _tutorialBallViews[index];
        }

        private void ShowLevelPopup(int levelIndex)
        {
            _pendingLevelIndex = levelIndex;
            _levelPopupTitle.text = $"УРОВЕНЬ {levelIndex + 1}";
            _levelPopupGoal.text = "Цель: набери очки, разбей камни или собери комбо за лимит ходов.";
            _levelFightButton.onClick.RemoveAllListeners();
            _levelFightButton.onClick.AddListener(() => _levelRequested?.Invoke(_pendingLevelIndex));
            _levelPopup.SetActive(true);
        }

        private void SetScreen(GameObject active)
        {
            _mainMenu.SetActive(active == _mainMenu);
            _levelMap.SetActive(active == _levelMap);
            _game.SetActive(active == _game);
            _levelPopup.SetActive(false);
            _pause.SetActive(false);
            _result.SetActive(false);
        }

        private void RenderBalls(IReadOnlyList<BallData> balls)
        {
            RefreshBoardMetrics();
            var alive = new HashSet<string>();

            for (int i = 0; i < balls.Count; i++)
            {
                var ball = balls[i];
                string ballName = $"Ball_{ball.Id}";
                alive.Add(ballName);
                var go = FindBallView(ball.Id) ?? GetFreeBallView();
                bool wasActive = go.activeSelf;
                go.name = $"Ball_{ball.Id}";
                go.SetActive(true);
                go.transform.localScale = Vector3.one;
                var glow = go.transform.Find("Glow").GetComponent<Image>();
                var image = go.transform.Find("Body").GetComponent<Image>();
                var cracks = go.transform.Find("Cracks")?.GetComponent<Image>();
                image.sprite = SpriteFor(ball);
                image.color = Color.white;
                glow.enabled = !ball.IsHidden;
                glow.color = GlowColorForNumber(ball.Number, 0.60f);
                if (cracks != null)
                {
                    cracks.enabled = ball.IsHidden && ball.Cracks > 0;
                    cracks.sprite = _obsidianCrackedSprite;
                    cracks.color = Color.white;
                }
                var rect = go.GetComponent<RectTransform>();
                Vector2 target = CellPosition(ball.Column, ball.Row);
                if (wasActive && Vector2.Distance(rect.anchoredPosition, target) > 0.01f)
                {
                    if (_ballMoves.TryGetValue(ball.Id, out var previousMove) && previousMove != null)
                        StopCoroutine(previousMove);
                    _ballMoves[ball.Id] = StartCoroutine(AnimateBallPosition(ball.Id, rect, target, PrototypeMetrics.Gravity));
                }
                else
                {
                    if (_ballMoves.TryGetValue(ball.Id, out var previousMove) && previousMove != null)
                        StopCoroutine(previousMove);
                    _ballMoves.Remove(ball.Id);
                    SetCellRect(rect, ball.Column, ball.Row, 0f);
                }
                ApplyBallMetrics(go);
                var text = go.transform.Find("Text").GetComponent<Text>();
                text.text = ball.IsHidden ? string.Empty : ball.Number.ToString();
                text.color = new Color(0.04f, 0.03f, 0.08f);
            }

            for (int i = 0; i < _ballViews.Count; i++)
            {
                if (_ballViews[i].activeSelf && !alive.Contains(_ballViews[i].name))
                {
                    if (int.TryParse(_ballViews[i].name.Substring("Ball_".Length), out int staleId) &&
                        _ballMoves.TryGetValue(staleId, out var staleMove) && staleMove != null)
                        StopCoroutine(staleMove);
                    if (int.TryParse(_ballViews[i].name.Substring("Ball_".Length), out staleId))
                        _ballMoves.Remove(staleId);
                    _ballViews[i].SetActive(false);
                }
            }
        }

        private GameObject GetFreeBallView()
        {
            for (int i = 0; i < _ballViews.Count; i++)
            {
                if (!_ballViews[i].activeSelf)
                    return _ballViews[i];
            }

            return GetBallView(_ballViews.Count);
        }

        private GameObject GetBallView(int index)
        {
            while (_ballViews.Count <= index)
            {
                var go = new GameObject("Ball");
                go.transform.SetParent(_ballsRoot, false);
                go.AddComponent<RectTransform>();
                var glow = new GameObject("Glow").AddComponent<Image>();
                glow.transform.SetParent(go.transform, false);
                glow.sprite = UiFactory.Glow;
                glow.raycastTarget = false;
                UiFactory.Stretch(glow.rectTransform);
                glow.rectTransform.offsetMin = new Vector2(-46f, -46f);
                glow.rectTransform.offsetMax = new Vector2(46f, 46f);

                var image = new GameObject("Body").AddComponent<Image>();
                image.transform.SetParent(go.transform, false);
                image.raycastTarget = false;
                UiFactory.Stretch(image.rectTransform);
                var cracks = new GameObject("Cracks").AddComponent<Image>();
                cracks.transform.SetParent(go.transform, false);
                cracks.raycastTarget = false;
                cracks.gameObject.SetActive(false);
                UiFactory.Stretch(cracks.rectTransform);
                var text = UiFactory.Label(go.transform, "Text", "", 44, new Color(0.04f, 0.03f, 0.08f), TextAnchor.MiddleCenter, false);
                UiFactory.Stretch(text.rectTransform);
                go.SetActive(false);
                _ballViews.Add(go);
            }

            return _ballViews[index];
        }

        private GameObject FindBallView(int id)
        {
            string targetName = $"Ball_{id}";
            for (int i = 0; i < _ballViews.Count; i++)
            {
                if (_ballViews[i].activeSelf && _ballViews[i].name == targetName)
                    return _ballViews[i];
            }

            return null;
        }

        private void RenderAim(PreviewInfo preview, int currentNumber)
        {
            RefreshBoardMetrics();
            float cell = PrototypeMetrics.Cell(_board.rect.width);
            _columnGlow.sizeDelta = new Vector2(cell, _board.rect.width);
            if (_aimTween != null)
                StopCoroutine(_aimTween);
            _aimTween = StartCoroutine(TweenAnchoredPosition(
                _columnGlow,
                new Vector2(_aimColumn * cell, 0f),
                PrototypeMetrics.AimTween));
            _columnGlow.GetComponent<Image>().color = preview.WillMatch ? PrototypePalette.AimMatch : PrototypePalette.AimNormal;

            if (_isInputLocked)
            {
                HideGhost();
                return;
            }

            if (preview.Row >= 0)
            {
                _ghostGlow.gameObject.SetActive(false);
                _ghostBall.sprite = UiFactory.DashedRing;
                _ghostBall.color = PrototypePalette.GhostBorder;
                _ghostBall.rectTransform.anchorMin = _ghostBall.rectTransform.anchorMax = new Vector2(0f, 1f);
                _ghostBall.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                _ghostBall.rectTransform.sizeDelta = new Vector2(cell, cell);
                if (_ghostTween != null)
                    StopCoroutine(_ghostTween);
                _ghostTween = StartCoroutine(TweenAnchoredPosition(
                    _ghostBall.rectTransform,
                    CellPosition(_aimColumn, preview.Row),
                    PrototypeMetrics.AimTween));
                _ghostBall.gameObject.SetActive(true);
            }
            else
            {
                _ghostGlow.gameObject.SetActive(false);
                _ghostBall.gameObject.SetActive(false);
            }
        }

        private void OnAimed(int column)
        {
            if (_aimColumn == column)
                return;

            _aimColumn = column;
            _idleTimer = 0f;
            _aimSoundRequested?.Invoke();
            _lastPreview = PreviewForCurrentAim();
            RenderAim(_lastPreview, _currentNumber);
        }

        private void OnDropped(int column)
        {
            _tutorialVisible = false;
            _idleTimer = 0f;
            _pointerInput?.SetLocked(true);
            _dropRequested?.Invoke(column);
        }

        private void AnimateTutorial()
        {
            if (_tutorialOverlay == null || !_tutorialOverlay.activeSelf)
                return;

            RefreshTutorialBoardMetrics();
            float time = Time.unscaledTime;
            if (_tutorialColumnGlow != null && _tutorialHighlightColumn >= 0)
            {
                var image = _tutorialColumnGlow.GetComponent<Image>();
                Color baseColor = _tutorialStepIndex == 1 ? PrototypePalette.AimMatch : PrototypePalette.AimNormal;
                image.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0.22f + Mathf.Sin(time * 3.6f) * 0.08f);
            }

            if (_tutorialGhost != null && _tutorialGhost.gameObject.activeSelf)
                _tutorialGhost.localScale = Vector3.one * (1f + Mathf.Sin(time * 4.2f) * 0.045f);

            if (_tutorialArrow != null && _tutorialArrow.gameObject.activeSelf)
            {
                var pos = _tutorialArrow.anchoredPosition;
                pos.y = -12f + Mathf.Sin(time * 4.4f) * 6f;
                _tutorialArrow.anchoredPosition = pos;
            }

            for (int i = 0; i < _tutorialPulseTargets.Count; i++)
            {
                if (_tutorialPulseTargets[i] != null)
                {
                    float pulse = 0.5f + 0.5f * Mathf.Sin(time * 3.8f + i * 0.45f);
                    _tutorialPulseTargets[i].localScale = Vector3.one * (1f + pulse * 0.045f);
                    var body = _tutorialPulseTargets[i].Find("Body")?.GetComponent<Image>();
                    if (body != null)
                        body.color = new Color(1f, 1f, 1f, 0.72f + pulse * 0.28f);

                    var glow = _tutorialPulseTargets[i].Find("Glow")?.GetComponent<Image>();
                    if (glow != null && glow.enabled)
                    {
                        var color = glow.color;
                        glow.color = new Color(color.r, color.g, color.b, 0.34f + pulse * 0.36f);
                    }
                }
            }

            if (_tutorialLaunchBall != null && _tutorialLaunchBall.gameObject.activeSelf)
            {
                float pulse = 0.5f + 0.5f * Mathf.Sin(time * 3.3f);
                _tutorialLaunchBall.localScale = Vector3.one * (1f + pulse * 0.04f);
                var position = _tutorialLaunchBall.anchoredPosition;
                position.y = -TutorialBoardCellSize() * 0.28f + Mathf.Sin(time * 3.6f) * 4f;
                _tutorialLaunchBall.anchoredPosition = position;
                _tutorialLaunchBallBody.color = new Color(1f, 1f, 1f, 0.78f + pulse * 0.22f);
                var glow = _tutorialLaunchBall.Find("Glow")?.GetComponent<Image>();
                if (glow != null)
                {
                    var color = glow.color;
                    glow.color = new Color(color.r, color.g, color.b, 0.42f + pulse * 0.30f);
                }
            }
        }

        private IEnumerator UnlockInputAfter(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            _unlockInputRoutine = null;
            SetInputLocked(false);
        }

        private void HideGhost()
        {
            if (_ghostGlow != null)
                _ghostGlow.gameObject.SetActive(false);
            if (_ghostBall != null)
                _ghostBall.gameObject.SetActive(false);
        }

        private void SetBallIcon(Image image, Text text, int number)
        {
            image.sprite = SpriteForNumber(number);
            bool current = image == _currentBall || image == _tutorialCurrentBall;
            float diameter = current ? 48f : 32f;
            text.fontSize = current ? 20 : 14;
            var body = image.rectTransform;
            body.anchorMin = body.anchorMax = new Vector2(0.5f, 0.5f);
            body.pivot = new Vector2(0.5f, 0.5f);
            body.sizeDelta = new Vector2(diameter, diameter);
            image.color = image.sprite == UiFactory.Circle ? (number == 0 ? _config.Obsidian : _config.ColorForNumber(number)) : Color.white;
            var glow = image.transform.parent.Find("Glow")?.GetComponent<Image>();
            if (glow != null)
            {
                glow.rectTransform.anchorMin = glow.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                glow.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                glow.rectTransform.sizeDelta = new Vector2(diameter + PrototypeMetrics.BallGlowOuter, diameter + PrototypeMetrics.BallGlowOuter);
                glow.color = number == 0 ? new Color(0.05f, 0.04f, 0.08f, 0.58f) : GlowColorForNumber(number, 0.62f);
            }
            text.text = number == 0 ? string.Empty : number.ToString();
        }

        private Color GlowColorForNumber(int number, float alpha)
        {
            if (number == 0)
                return new Color(0.10f, 0.08f, 0.16f, alpha);

            var color = _config.ColorForNumber(number);
            return new Color(color.r, color.g, color.b, alpha);
        }

        private Sprite SpriteFor(BallData ball)
        {
            if (ball.IsHidden)
            {
                if (ball.Cracks > 0 && _obsidianCrackedSprite != null)
                    return _obsidianCrackedSprite;

                return _obsidianSprite != null ? _obsidianSprite : UiFactory.Circle;
            }

            return SpriteForNumber(ball.Number);
        }

        private Sprite SpriteForNumber(int number)
        {
            if (number == 0)
                return _obsidianSprite != null ? _obsidianSprite : UiFactory.Circle;

            if (_ballSprites != null && number >= 1 && number <= _ballSprites.Length && _ballSprites[number - 1] != null)
                return _ballSprites[number - 1];

            return UiFactory.Circle;
        }

        private void SetCellRect(RectTransform rect, int column, int row, float padding)
        {
            SetCellRect(rect, column, (float)row, padding);
        }

        private void SetCellRect(RectTransform rect, int column, float row, float padding)
        {
            float cell = PrototypeMetrics.Cell(_board.rect.width);
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(cell, cell);
            rect.anchoredPosition = CellPosition(column, row);
        }

        private Vector2 CellPosition(int column, float row)
        {
            float cell = PrototypeMetrics.Cell(_board.rect.width);
            return new Vector2((column + 0.5f) * cell, -(row + 0.5f) * cell);
        }

        private void SetTutorialCellRect(RectTransform rect, int column, int row)
        {
            float cell = TutorialBoardCellSize();
            rect.sizeDelta = new Vector2(cell, cell);
            rect.anchoredPosition = TutorialCellPosition(column, row);
        }

        private Vector2 TutorialCellPosition(int column, int row)
        {
            float cell = TutorialBoardCellSize();
            return new Vector2(TutorialColumnCenter(column), -(row + 0.5f) * cell);
        }

        private void UpdateTutorialGuideVisuals()
        {
            float cell = TutorialBoardCellSize();
            if (_tutorialColumnGlow != null)
            {
                _tutorialColumnGlow.sizeDelta = new Vector2(cell, _tutorialDemoBoard.rect.height);
                _tutorialColumnGlow.anchoredPosition = _tutorialHighlightColumn >= 0
                    ? new Vector2(TutorialColumnLeft(_tutorialHighlightColumn), 0f)
                    : new Vector2(-2000f, 0f);
                _tutorialColumnGlow.gameObject.SetActive(_tutorialHighlightColumn >= 0);
            }

            if (_tutorialGhost != null)
            {
                _tutorialGhost.sizeDelta = new Vector2(cell, cell);
                bool showGhost = _tutorialGhostColumn >= 0 && _tutorialGhostRow >= 0;
                _tutorialGhost.anchoredPosition = showGhost
                    ? TutorialCellPosition(_tutorialGhostColumn, _tutorialGhostRow)
                    : new Vector2(-2000f, -2000f);
                _tutorialGhost.gameObject.SetActive(showGhost);
            }

            if (_tutorialArrow != null)
            {
                bool showArrow = _tutorialHighlightColumn >= 0;
                _tutorialArrow.anchoredPosition = showArrow
                    ? new Vector2(TutorialColumnCenter(_tutorialHighlightColumn), -12f)
                    : new Vector2(-2000f, -2000f);
                _tutorialArrow.gameObject.SetActive(showArrow);
            }
        }

        private void RefreshTutorialBoardMetrics()
        {
            if (_tutorialDemoBoard == null)
                return;

            for (int i = 0; i < _tutorialGridCells.Count; i++)
                SetTutorialCellRect(_tutorialGridCells[i], i % 7, i / 7);

            if (_tutorialLaunchBallVisible)
                SetTutorialLaunchBall(_tutorialLaunchBallNumber, _tutorialLaunchBallColumn, true);
        }

        private float TutorialBoardCellSize()
        {
            if (_tutorialDemoBoard == null)
                return 36f;

            return Mathf.Max(252f, _tutorialDemoBoard.rect.width) / 7f;
        }

        private float TutorialColumnLeft(int column)
        {
            return column * TutorialBoardCellSize();
        }

        private float TutorialColumnCenter(int column)
        {
            return (column + 0.5f) * TutorialBoardCellSize();
        }

        private IEnumerator AnimateBallPosition(int ballId, RectTransform rect, Vector2 target, float duration)
        {
            Vector2 start = rect.anchoredPosition;
            for (float time = 0f; time < duration; time += Time.deltaTime)
            {
                rect.anchoredPosition = Vector2.LerpUnclamped(start, target, PrototypeEasing.Drop(time / duration));
                yield return null;
            }

            rect.anchoredPosition = target;
            if (_ballMoves.TryGetValue(ballId, out var move) && move != null)
                _ballMoves.Remove(ballId);
        }

        private IEnumerator TweenAnchoredPosition(RectTransform rect, Vector2 target, float duration)
        {
            Vector2 start = rect.anchoredPosition;
            for (float time = 0f; time < duration; time += Time.deltaTime)
            {
                rect.anchoredPosition = Vector2.Lerp(start, target, time / duration);
                yield return null;
            }

            rect.anchoredPosition = target;
        }

        private void ApplyBallMetrics(GameObject ball)
        {
            float cell = PrototypeMetrics.Cell(_board.rect.width);
            float diameter = PrototypeMetrics.BallDiameter(cell);
            var body = ball.transform.Find("Body").GetComponent<RectTransform>();
            body.anchorMin = body.anchorMax = new Vector2(0.5f, 0.5f);
            body.pivot = new Vector2(0.5f, 0.5f);
            body.sizeDelta = new Vector2(diameter, diameter);
            body.anchoredPosition = Vector2.zero;

            var cracksImage = ball.transform.Find("Cracks")?.GetComponent<Image>();
            if (cracksImage != null)
            {
                var cracks = cracksImage.rectTransform;
                cracks.anchorMin = cracks.anchorMax = new Vector2(0.5f, 0.5f);
                cracks.pivot = new Vector2(0.5f, 0.5f);
                cracks.sizeDelta = new Vector2(diameter, diameter);
                cracks.anchoredPosition = Vector2.zero;
            }

            var glow = ball.transform.Find("Glow").GetComponent<RectTransform>();
            glow.anchorMin = glow.anchorMax = new Vector2(0.5f, 0.5f);
            glow.pivot = new Vector2(0.5f, 0.5f);
            glow.sizeDelta = new Vector2(diameter + PrototypeMetrics.BallGlowOuter, diameter + PrototypeMetrics.BallGlowOuter);
            glow.anchoredPosition = Vector2.zero;

            var text = ball.transform.Find("Text").GetComponent<Text>();
            text.fontSize = Mathf.RoundToInt(PrototypeMetrics.NumberFontSize(_board.rect.width));
            text.color = PrototypePalette.NumInk;
        }

        private PreviewInfo PreviewForCurrentAim()
        {
            return _previewRequested == null ? _lastPreview : _previewRequested(_aimColumn);
        }

        private string ObjectiveText(GameModeSnapshot snapshot)
        {
            if (_tutorialVisible)
                return "Нажмите на шар сверху, чтобы бросить его в колонку";

            if (snapshot.Mode == GameModeType.Classic)
                return $"Классика: рекорд. Combo x{snapshot.MaxCombo}";
            if (snapshot.Mode == GameModeType.Zen)
                return "Дзен: без подъёма поля и таймера.";
            if (snapshot.Mode == GameModeType.Blitz)
                return $"Блиц: {FormatTime(snapshot.RemainingSeconds)} без подъёма поля.";
            if (snapshot.Level == null)
                return string.Empty;

            return $"Уровень {snapshot.Level.LevelNumber}: {GoalText(snapshot)}";
        }

        private string ProgressText(GameModeSnapshot snapshot)
        {
            if (snapshot.Level == null)
                return snapshot.Status;

            return GoalText(snapshot) + $" · Ходы {snapshot.MovesUsed}/{snapshot.MoveLimit}";
        }

        private static string MovesText(GameModeSnapshot snapshot)
        {
            if (snapshot.Mode == GameModeType.Zen)
                return "∞";
            if (snapshot.Mode == GameModeType.Blitz)
                return FormatTime(snapshot.RemainingSeconds);

            return snapshot.MovesLeftToRise.ToString();
        }

        private static string FormatTime(int totalSeconds)
        {
            int clamped = Mathf.Max(0, totalSeconds);
            return $"{clamped / 60:00}:{clamped % 60:00}";
        }

        public void Shake(int level)
        {
            if (_board == null)
                return;

            _boardHome = _board.anchoredPosition;
            level = Mathf.Clamp(level, 1, 3);
            if (_shakeRoutine != null)
                StopCoroutine(_shakeRoutine);
            _shakeRoutine = StartCoroutine(ShakeRoutine(level));
        }

        private string GoalText(GameModeSnapshot snapshot)
        {
            switch (snapshot.Level.Objective)
            {
                case LevelObjective.TargetScore:
                    return $"Очки {snapshot.Score}/{snapshot.Level.TargetScore}";
                case LevelObjective.BreakObsidian:
                    return $"Камни {snapshot.ObsidianHits}/{snapshot.Level.TargetObsidianBreaks}";
                case LevelObjective.ReachCombo:
                    return $"Комбо x{snapshot.Level.TargetCombo}. Лучшее x{snapshot.MaxCombo}";
                default:
                    return "Очисти поле";
            }
        }

        private IEnumerator ShowBanner(string text)
        {
            _banner.text = text;
            _banner.gameObject.SetActive(true);
            yield return new WaitForSeconds(PrototypeMetrics.Banner);
            _banner.gameObject.SetActive(false);
        }

        private IEnumerator ShakeRoutine(int level)
        {
            float duration = level == 1 ? 0.16f : level == 2 ? 0.20f : 0.24f;
            float amplitude = level == 1 ? 5f : level == 2 ? 8f : 11f;

            for (float time = 0f; time < duration; time += Time.deltaTime)
            {
                float p = time / duration;
                float fade = 1f - p;
                float x = Mathf.Sin(time * 85f) * amplitude * fade;
                float y = Mathf.Cos(time * 62f) * (amplitude * 0.28f) * fade;
                float rot = Mathf.Sin(time * 70f) * 0.45f * fade;
                _board.anchoredPosition = _boardHome + new Vector2(x, y);
                _board.localRotation = Quaternion.Euler(0f, 0f, rot);
                yield return null;
            }

            _board.anchoredPosition = _boardHome;
            _board.localRotation = Quaternion.identity;
            _shakeRoutine = null;
        }

        private IEnumerator PulseHint()
        {
            if (_columnGlow == null)
                yield break;

            var image = _columnGlow.GetComponent<Image>();
            for (int i = 0; i < 6; i++)
            {
                image.color = i % 2 == 0 ? new Color(1f, 0.72f, 0.16f, 0.30f) : new Color(0.12f, 0.84f, 1f, 0.16f);
                yield return new WaitForSeconds(0.18f);
            }
        }

        private IEnumerator PlayCellShockwave(int column, int row)
        {
            yield return PlayCellShockwave(column, row, new Color(1f, 1f, 1f, 0.88f));
        }

        private IEnumerator PlayCellShockwave(int column, int row, Color color)
        {
            if (_shockwaveSprite == null)
                yield break;

            var go = new GameObject("Shockwave");
            go.transform.SetParent(_fxRoot, false);
            var image = go.AddComponent<Image>();
            image.sprite = _shockwaveSprite;
            image.color = new Color(color.r, color.g, color.b, 0.88f);
            image.raycastTarget = false;
            SetCellRect(image.rectTransform, column, row, -0.05f);
            _fxViews.Add(go);

            for (int i = 0; i < 7; i++)
                StartCoroutine(PlaySpark(column, row, color, i));

            float time = 0f;
            while (time < PrototypeMetrics.Ring)
            {
                time += Time.deltaTime;
                float t = PrototypeEasing.EaseOut(time / PrototypeMetrics.Ring);
                image.color = new Color(color.r, color.g, color.b, 1f - t);
                image.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.2f, 2.6f, t);
                yield return null;
            }

            _fxViews.Remove(go);
            Destroy(go);
        }

        private IEnumerator PlaySpark(int column, int row, Color color, int index)
        {
            var go = new GameObject("Spark");
            go.transform.SetParent(_fxRoot, false);
            var image = go.AddComponent<Image>();
            image.sprite = UiFactory.Glow;
            image.color = new Color(color.r, color.g, color.b, 0.90f);
            image.raycastTarget = false;
            SetCellCenter(image.rectTransform, column, row, 18f);
            _fxViews.Add(go);

            float angle = (index / 7f) * Mathf.PI * 2f + UnityEngine.Random.Range(-0.18f, 0.18f);
            float distance = UnityEngine.Random.Range(44f, 92f);
            var target = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
            float time = 0f;
            while (time < PrototypeMetrics.Spark)
            {
                time += Time.deltaTime;
                float t = PrototypeEasing.Spark(time / PrototypeMetrics.Spark);
                image.rectTransform.anchoredPosition = Vector2.LerpUnclamped(Vector2.zero, target, t);
                image.rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, 0.20f, t);
                image.color = new Color(color.r, color.g, color.b, 1f - t);
                yield return null;
            }

            _fxViews.Remove(go);
            Destroy(go);
        }

        private void PlayFloatingScore(int column, int row, string text, Color color)
        {
            StartCoroutine(PlayFloatingScoreRoutine(column, row, text, color));
        }

        private IEnumerator PlayFloatingScoreRoutine(int column, int row, string text, Color color)
        {
            var label = UiFactory.Label(_fxRoot, "FloatingScore", text, 30, color, TextAnchor.MiddleCenter);
            SetCellCenter(label.rectTransform, column, row, 240f);
            _fxViews.Add(label.gameObject);

            var start = label.rectTransform.anchoredPosition;
            float time = 0f;
            while (time < PrototypeMetrics.FloatScore)
            {
                time += Time.deltaTime;
                float t = PrototypeEasing.EaseOut(time / PrototypeMetrics.FloatScore);
                float y;
                float scale;
                float alpha;
                if (t < 0.25f)
                {
                    float k = t / 0.25f;
                    y = Mathf.Lerp(0f, 12f, k);
                    scale = Mathf.Lerp(0.7f, 1.15f, k);
                    alpha = k;
                }
                else
                {
                    float k = (t - 0.25f) / 0.75f;
                    y = Mathf.Lerp(12f, 58f, k);
                    scale = Mathf.Lerp(1.15f, 1f, k);
                    alpha = 1f - k;
                }
                label.rectTransform.anchoredPosition = start + new Vector2(0f, y);
                label.rectTransform.localScale = Vector3.one * scale;
                label.color = new Color(color.r, color.g, color.b, alpha);
                yield return null;
            }

            _fxViews.Remove(label.gameObject);
            Destroy(label.gameObject);
        }

        private void SetCellCenter(RectTransform rect, int column, int row, float size)
        {
            float cell = 1f / _size;
            rect.anchorMin = new Vector2((column + 0.5f) * cell, 1f - (row + 0.5f) * cell);
            rect.anchorMax = rect.anchorMin;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(size, size);
            rect.anchoredPosition = Vector2.zero;
        }

        private void ApplySafeArea(RectTransform rect)
        {
            var area = Screen.safeArea;
            var min = area.position;
            var max = area.position + area.size;
            min.x /= Screen.width;
            min.y /= Screen.height;
            max.x /= Screen.width;
            max.y /= Screen.height;
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private void LoadTexturePack()
        {
            _ballSprites = new[]
            {
                Resources.Load<Sprite>("Textures/Balls/ball_1_white"),
                Resources.Load<Sprite>("Textures/Balls/ball_2_cyan"),
                Resources.Load<Sprite>("Textures/Balls/ball_3_emerald"),
                Resources.Load<Sprite>("Textures/Balls/ball_4_amber"),
                Resources.Load<Sprite>("Textures/Balls/ball_5_coral"),
                Resources.Load<Sprite>("Textures/Balls/ball_6_magenta"),
                Resources.Load<Sprite>("Textures/Balls/ball_7_violet")
            };
            _obsidianSprite = Resources.Load<Sprite>("Textures/Balls/ball_obsidian");
            _obsidianCrackedSprite = Resources.Load<Sprite>("Textures/Balls/ball_obsidian_cracked");
            _backgroundSprite = Resources.Load<Sprite>("Textures/Backgrounds/bg_deep_indigo");
            _panelSprite = Resources.Load<Sprite>("Textures/UI/panel_glass");
            _cellSprite = Resources.Load<Sprite>("Textures/UI/grid_cell");
            _shockwaveSprite = Resources.Load<Sprite>("Textures/VFX/vfx_shockwave_ring");
        }
    }
}
