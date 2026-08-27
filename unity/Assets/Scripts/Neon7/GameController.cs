using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Neon7
{
    public enum GameMode { Classic, Zen }

    /// <summary>
    /// Цикл хода: сброс -> каскад волн -> подъём дна -> проверка Game Over.
    /// Точный порт src/routes/index.tsx.
    /// </summary>
    public class GameController : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [Header("Ссылки")]
        [SerializeField] private BoardView board;
        [SerializeField] private Sfx sfx;

        [Header("HUD")]
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text bestText;
        [SerializeField] private TMP_Text movesText;
        [SerializeField] private TMP_Text movesLabel;
        [SerializeField] private TMP_Text aimText;
        [SerializeField] private BallView currentPreview;   // 48x48
        [SerializeField] private BallView nextPreview;      // 32x32
        [SerializeField] private TMP_Text muteButtonLabel;

        [Header("Оверлеи")]
        [SerializeField] private CanvasGroup bannerGroup;
        [SerializeField] private TMP_Text bannerText;
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private TMP_Text gameOverScore;

        private const string BestKey = "neon7-best";
        private static readonly CultureInfo Ru = new CultureInfo("ru-RU");

        private List<BallData> _balls = new List<BallData>();
        private int? _current, _next;
        private int _aim = 3, _score, _best, _movesLeft = GameLogic.RiseEvery;
        private bool _busy, _over;
        private GameMode _mode = GameMode.Classic;

        private void Start()
        {
            _best = PlayerPrefs.GetInt(BestKey, 0);
            Restart();
        }

        public void Restart()
        {
            board.ClearAll();
            GameLogic.ResetIds();
            _balls = GameLogic.StartBalls();
            _current = GameLogic.RollPiece();
            _next = GameLogic.RollPiece();
            _score = 0;
            _movesLeft = GameLogic.RiseEvery;
            _over = false;
            _busy = false;
            if (gameOverPanel) gameOverPanel.SetActive(false);
            board.Sync(_balls);
            RefreshHud();
        }

        public void SetMode(int mode)
        {
            _mode = (GameMode)mode;
            _movesLeft = GameLogic.RiseEvery;
            RefreshHud();
        }

        public void ToggleMute()
        {
            sfx.Muted = !sfx.Muted;
            if (muteButtonLabel) muteButtonLabel.text = sfx.Muted ? "🔇" : "🔊";
        }

        // ---------- ввод ----------

        public void OnPointerDown(PointerEventData e) => Aim(e, playSound: true);
        public void OnDrag(PointerEventData e) => Aim(e, playSound: true, withHaptic: true);
        public void OnPointerUp(PointerEventData e)
        {
            if (_busy || _over) return;
            StartCoroutine(Drop(_aim));
        }

        private void Aim(PointerEventData e, bool playSound, bool withHaptic = false)
        {
            int c = board.ColumnFromScreen(e.position, e.pressEventCamera);
            if (c == _aim) return;
            _aim = c;
            if (playSound) sfx.Move();
            if (withHaptic) Sfx.Haptic(6);
            RefreshAim();
        }

        // ---------- ход ----------

        private IEnumerator Drop(int col)
        {
            int row = GameLogic.LandingRow(_balls, col);
            if (row < 0) yield break;
            _busy = true;

            var ball = GameLogic.MakeBall(col, row, _current);
            var spawn = ball.Clone();
            spawn.Row = -1;                       // появляется над полем
            _balls.Add(ball);
            var view = board.Spawn(spawn);
            yield return new WaitForSeconds(Metrics.SpawnDelay);

            view.SetCell(col, row, instant: false);
            sfx.Drop();
            Sfx.Haptic(14);
            yield return new WaitForSeconds(Metrics.DropMove);
            yield return view.PlaySquash();

            yield return Resolve();

            _current = _next;
            _next = GameLogic.RollPiece();

            if (_mode == GameMode.Classic)
            {
                int left = _movesLeft - 1;
                if (left <= 0)
                {
                    _movesLeft = GameLogic.RiseEvery;
                    var (arr, dead) = GameLogic.Rise(_balls);
                    if (dead)
                    {
                        GameOver();
                        yield break;
                    }
                    _balls = arr;
                    sfx.Rise();
                    Sfx.Haptic(10, 20, 10);
                    board.Sync(_balls);
                    yield return new WaitForSeconds(Metrics.Rise);
                    yield return Resolve();
                }
                else _movesLeft = left;
            }

            if (GameLogic.BoardFull(_balls)) GameOver();
            _busy = false;
            RefreshHud();
        }

        /// <summary>Каскад волн: взрыв -> урон соседям -> удаление -> гравитация -> проверка.</summary>
        private IEnumerator Resolve()
        {
            int wave = 1;
            while (true)
            {
                var matches = GameLogic.FindMatches(_balls);
                if (matches.Count == 0) break;
                var hidden = GameLogic.DamagedNeighbours(_balls, matches);
                int gained = GameLogic.ScoreFor(matches.Count, wave);

                for (int i = 0; i < matches.Count; i++)
                {
                    var m = matches[i];
                    sfx.Pop(wave, i);
                    board.PlayBurst(m.Col, m.Row, Palette.ForNumber(m.Num));
                    var v = board.Get(m.Id);
                    if (v) StartCoroutine(v.PlayPop());
                }
                if (hidden.Count > 0) sfx.Crack();

                var anchor = matches[0];
                board.PlayFloatScore(anchor.Col, anchor.Row,
                    wave > 1 ? $"+{gained} COMBO x{wave}!" : $"+{gained}",
                    wave > 1 ? Palette.Numbers[3] : Palette.Numbers[1]);
                board.Shake(Mathf.Min(3, wave));
                if (wave > 1) Sfx.Haptic(18, 30, 24); else Sfx.Haptic(16);
                AddScore(gained);
                if (wave >= 3) ShowBanner($"WAVE {wave} • x{1 << (wave - 1)}");

                var dead = new HashSet<int>(matches.Select(m => m.Id));
                var dmg = new HashSet<int>(hidden.Select(h => h.Id));
                foreach (var b in _balls)
                {
                    if (dead.Contains(b.Id)) b.Dying = true;
                    else if (dmg.Contains(b.Id))
                    {
                        if (b.Cracks == 0) b.Cracks = 1;
                        else { b.Cracks = 0; b.Num = GameLogic.RandNum(); }
                    }
                }
                foreach (var b in _balls.Where(b => dmg.Contains(b.Id)))
                {
                    var v = board.Get(b.Id);
                    if (v) { v.Data.Num = b.Num; v.Data.Cracks = b.Cracks; v.Refresh(); }
                }
                yield return new WaitForSeconds(Metrics.Pop);

                _balls = GameLogic.ApplyGravity(_balls.Where(b => !b.Dying).ToList());
                board.Sync(_balls);
                yield return new WaitForSeconds(Metrics.Gravity);
                wave++;
            }

            if (_balls.Count == 0)
            {
                sfx.Clear();
                Sfx.Haptic(30, 40, 30, 60);
                ShowBanner("BOARD CLEAR! +70,000");
                AddScore(GameLogic.BoardClearBonus);
                board.Shake(3);
                yield return new WaitForSeconds(Metrics.ClearPause);
            }
        }

        private void GameOver()
        {
            sfx.Over();
            _over = true;
            _busy = false;
            if (gameOverPanel) gameOverPanel.SetActive(true);
            if (gameOverScore) gameOverScore.text = _score.ToString("N0", Ru);
        }

        // ---------- HUD ----------

        private void AddScore(int v)
        {
            _score += v;
            if (_score > _best)
            {
                _best = _score;
                PlayerPrefs.SetInt(BestKey, _best);
            }
            RefreshHud();
        }

        private void RefreshHud()
        {
            if (scoreText) scoreText.text = _score.ToString("N0", Ru);
            if (bestText) bestText.text = _best.ToString("N0", Ru);
            if (movesLabel) movesLabel.text = _mode == GameMode.Classic ? "До подъёма" : "Дзен";
            if (movesText) movesText.text = _mode == GameMode.Classic ? _movesLeft.ToString() : "∞";
            if (currentPreview) currentPreview.Init(GameLogic.MakeBall(0, 0, _current), 48f, 20f);
            if (nextPreview) nextPreview.Init(GameLogic.MakeBall(0, 0, _next), 32f, 14f);
            RefreshAim();
        }

        private void RefreshAim()
        {
            int landing = GameLogic.LandingRow(_balls, _aim);
            int v = 0, h = 0;
            bool match = false;
            if (landing >= 0)
            {
                var probe = _balls.Select(b => b.Clone()).ToList();
                probe.Add(GameLogic.MakeBall(_aim, landing, _current));
                var g = GameLogic.ToGrid(probe);
                (v, h) = GameLogic.RunLengths(g, _aim, landing);
                match = _current.HasValue && (v == _current.Value || h == _current.Value);
            }
            if (aimText)
            {
                aimText.text = $"↕ {v} · ↔ {h}{(match ? " · ВЗРЫВ!" : "")}";
                aimText.color = match ? Palette.Numbers[2] : Palette.InkDim;
            }
            board.UpdateAim(_aim, landing, match);
        }

        private Coroutine _banner;

        private void ShowBanner(string text)
        {
            if (!bannerGroup || !bannerText) return;
            bannerText.text = text;
            bannerText.color = Palette.Numbers[3];
            if (_banner != null) StopCoroutine(_banner);
            _banner = StartCoroutine(BannerRoutine());
        }

        /// <summary>banner 1500ms: 0.6/0 -> 18% 1.06/1 -> 75% 1.0/1 -> 100% 1.10/0.</summary>
        private IEnumerator BannerRoutine()
        {
            var rt = (RectTransform)bannerGroup.transform;
            float dur = Metrics.Banner;
            for (float t = 0f; t < dur; t += Time.deltaTime)
            {
                float p = Easing.EaseOut(t / dur);
                float s, a;
                if (p < 0.18f) { float k = p / 0.18f; s = Mathf.Lerp(0.6f, 1.06f, k); a = k; }
                else if (p < 0.75f) { float k = (p - 0.18f) / 0.57f; s = Mathf.Lerp(1.06f, 1f, k); a = 1f; }
                else { float k = (p - 0.75f) / 0.25f; s = Mathf.Lerp(1f, 1.10f, k); a = 1f - k; }
                rt.localScale = Vector3.one * s;
                bannerGroup.alpha = a;
                yield return null;
            }
            bannerGroup.alpha = 0f;
            _banner = null;
        }
    }
}
