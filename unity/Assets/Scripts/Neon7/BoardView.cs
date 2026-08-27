using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Neon7
{
    /// <summary>
    /// Поле 7x7: сетка, подсветка колонки прицела, призрак посадки, шары, VFX, shake.
    /// boardRoot должен быть квадратным (aspect 1:1) с pivot/anchor в левом верхнем углу для детей.
    /// </summary>
    public class BoardView : MonoBehaviour
    {
        [SerializeField] private RectTransform boardRoot;   // квадрат, clip children
        [SerializeField] private RectTransform ballsLayer;
        [SerializeField] private RectTransform fxLayer;
        [SerializeField] private Image gridPrefab;          // UI/grid_cell.png
        [SerializeField] private Image aimColumn;           // градиентная подсветка колонки
        [SerializeField] private Image landingGhost;        // круг с dashed-обводкой
        [SerializeField] private BallView ballPrefab;
        [SerializeField] private Image ringPrefab;          // VFX/vfx_shockwave_ring.png
        [SerializeField] private Image sparkPrefab;         // VFX/vfx_spark_glow.png
        [SerializeField] private TMP_Text floatScorePrefab;

        private readonly Dictionary<int, BallView> _views = new Dictionary<int, BallView>();
        private Vector2 _boardHome;
        private Coroutine _shake;

        public float BoardWidth => boardRoot.rect.width;
        public float Cell => Metrics.Cell(BoardWidth);
        public float FontSize => Metrics.NumberFontSize(BoardWidth);

        private void Awake()
        {
            _boardHome = boardRoot.anchoredPosition;
            BuildGrid();
        }

        private void BuildGrid()
        {
            if (!gridPrefab) return;
            for (int r = 0; r < GameLogic.Size; r++)
                for (int c = 0; c < GameLogic.Size; c++)
                {
                    var cellImg = Instantiate(gridPrefab, ballsLayer.parent);
                    var rt = (RectTransform)cellImg.transform;
                    rt.SetAsFirstSibling();
                    rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
                    rt.pivot = new Vector2(0f, 1f);
                    rt.sizeDelta = new Vector2(Cell, Cell);
                    rt.anchoredPosition = new Vector2(c * Cell, -r * Cell);
                    cellImg.color = Palette.GridLine;
                }
        }

        // ---------- шары ----------

        public BallView Spawn(BallData data)
        {
            var v = Instantiate(ballPrefab, ballsLayer);
            var rt = (RectTransform)v.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            v.Init(data, Cell, FontSize);
            _views[data.Id] = v;
            return v;
        }

        public BallView Get(int id) => _views.TryGetValue(id, out var v) ? v : null;

        public void Sync(List<BallData> balls)
        {
            var alive = new HashSet<int>();
            foreach (var b in balls)
            {
                alive.Add(b.Id);
                var v = Get(b.Id) ?? Spawn(b);
                v.Data.Num = b.Num;
                v.Data.Cracks = b.Cracks;
                v.Refresh();
                if (v.Data.Col != b.Col || v.Data.Row != b.Row) v.SetCell(b.Col, b.Row, instant: false);
            }
            var stale = new List<int>();
            foreach (var kv in _views) if (!alive.Contains(kv.Key)) stale.Add(kv.Key);
            foreach (var id in stale)
            {
                if (_views[id]) Destroy(_views[id].gameObject);
                _views.Remove(id);
            }
        }

        public void ClearAll()
        {
            foreach (var kv in _views) if (kv.Value) Destroy(kv.Value.gameObject);
            _views.Clear();
        }

        // ---------- прицел ----------

        public void UpdateAim(int col, int landingRow, bool match)
        {
            if (aimColumn)
            {
                var rt = (RectTransform)aimColumn.transform;
                rt.sizeDelta = new Vector2(Cell, BoardWidth);
                StartCoroutine(TweenX(rt, col * Cell, Metrics.AimTween));
                aimColumn.color = match ? Palette.AimMatch : Palette.AimNormal;
            }
            if (landingGhost)
            {
                landingGhost.enabled = landingRow >= 0;
                if (landingRow >= 0)
                {
                    var rt = (RectTransform)landingGhost.transform;
                    rt.sizeDelta = new Vector2(Cell, Cell);
                    StartCoroutine(TweenPos(rt, new Vector2(col * Cell, -landingRow * Cell), Metrics.AimTween));
                    landingGhost.color = Palette.GhostBorder;
                }
            }
        }

        private IEnumerator TweenX(RectTransform rt, float x, float dur)
        {
            Vector2 from = rt.anchoredPosition;
            var to = new Vector2(x, from.y);
            for (float t = 0f; t < dur; t += Time.deltaTime)
            {
                rt.anchoredPosition = Vector2.Lerp(from, to, t / dur);
                yield return null;
            }
            rt.anchoredPosition = to;
        }

        private IEnumerator TweenPos(RectTransform rt, Vector2 to, float dur)
        {
            Vector2 from = rt.anchoredPosition;
            for (float t = 0f; t < dur; t += Time.deltaTime)
            {
                rt.anchoredPosition = Vector2.Lerp(from, to, t / dur);
                yield return null;
            }
            rt.anchoredPosition = to;
        }

        /// <summary>col = floor((x - left) / width * 7), clamp 0..6.</summary>
        public int ColumnFromScreen(Vector2 screenPos, Camera cam)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(boardRoot, screenPos, cam, out var local);
            float rel = (local.x - boardRoot.rect.xMin) / boardRoot.rect.width;
            return Mathf.Clamp(Mathf.FloorToInt(rel * GameLogic.Size), 0, GameLogic.Size - 1);
        }

        // ---------- VFX ----------

        public void PlayBurst(int col, int row, Color color)
        {
            Vector2 center = new Vector2(col * Cell + Cell * 0.5f, -row * Cell - Cell * 0.5f);

            if (ringPrefab)
            {
                var ring = Instantiate(ringPrefab, fxLayer);
                var rt = Prep(ring, center, new Vector2(Cell, Cell));
                ring.color = color;
                StartCoroutine(AnimRing(ring, rt));
            }

            if (!sparkPrefab) return;
            for (int i = 0; i < 7; i++)
            {
                float a = (i / 7f) * Mathf.PI * 2f + Random.value;
                float d = 40f + Random.value * 70f;
                var sp = Instantiate(sparkPrefab, fxLayer);
                var rt = Prep(sp, center, new Vector2(6f, 6f));
                sp.color = color;
                StartCoroutine(AnimSpark(sp, rt, center + new Vector2(Mathf.Cos(a) * d, Mathf.Sin(a) * d)));
            }
        }

        private RectTransform Prep(Graphic g, Vector2 pos, Vector2 size)
        {
            var rt = (RectTransform)g.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            return rt;
        }

        private IEnumerator AnimRing(Image img, RectTransform rt)
        {
            float dur = Metrics.Ring;
            Color c = img.color;
            for (float t = 0f; t < dur; t += Time.deltaTime)
            {
                float p = Easing.EaseOut(t / dur);
                rt.localScale = Vector3.one * Mathf.Lerp(0.2f, 2.6f, p);
                img.color = new Color(c.r, c.g, c.b, 1f - p);
                yield return null;
            }
            Destroy(img.gameObject);
        }

        private IEnumerator AnimSpark(Image img, RectTransform rt, Vector2 target)
        {
            float dur = Metrics.Spark;
            Vector2 from = rt.anchoredPosition;
            Color c = img.color;
            for (float t = 0f; t < dur; t += Time.deltaTime)
            {
                float p = Easing.Spark(t / dur);
                rt.anchoredPosition = Vector2.LerpUnclamped(from, target, p);
                rt.localScale = Vector3.one * Mathf.Lerp(1f, 0.2f, p);
                img.color = new Color(c.r, c.g, c.b, 1f - p);
                yield return null;
            }
            Destroy(img.gameObject);
        }

        public void PlayFloatScore(int col, int row, string text, Color color)
        {
            if (!floatScorePrefab) return;
            var t = Instantiate(floatScorePrefab, fxLayer);
            var rt = Prep(t, new Vector2(col * Cell + Cell * 0.5f, -row * Cell), new Vector2(200f, 24f));
            t.text = text;
            t.color = color;
            StartCoroutine(AnimFloat(t, rt));
        }

        private IEnumerator AnimFloat(TMP_Text txt, RectTransform rt)
        {
            float dur = Metrics.FloatScore;
            Vector2 home = rt.anchoredPosition;
            for (float time = 0f; time < dur; time += Time.deltaTime)
            {
                float p = Easing.EaseOut(time / dur);
                float y, scale, alpha;
                if (p < 0.25f)
                {
                    float k = p / 0.25f;
                    y = Mathf.Lerp(0f, 12f, k);
                    scale = Mathf.Lerp(0.7f, 1.15f, k);
                    alpha = k;
                }
                else
                {
                    float k = (p - 0.25f) / 0.75f;
                    y = Mathf.Lerp(12f, 58f, k);
                    scale = Mathf.Lerp(1.15f, 1f, k);
                    alpha = 1f - k;
                }
                rt.anchoredPosition = home + new Vector2(0f, y);
                rt.localScale = Vector3.one * scale;
                txt.alpha = alpha;
                yield return null;
            }
            Destroy(txt.gameObject);
        }

        /// <summary>Screen shake уровня 1..3 (см. README §5).</summary>
        public void Shake(int level)
        {
            level = Mathf.Clamp(level, 1, 3);
            if (_shake != null) StopCoroutine(_shake);
            _shake = StartCoroutine(ShakeRoutine(Metrics.ShakeDuration[level - 1], Metrics.ShakeAmplitude[level - 1]));
        }

        private IEnumerator ShakeRoutine(float dur, float amp)
        {
            for (float t = 0f; t < dur; t += Time.deltaTime)
            {
                float p = Easing.EaseInOut(t / dur);
                Vector2 off;
                float rot;
                if (p < 0.20f) { float k = p / 0.20f; off = Vector2.Lerp(Vector2.zero, new Vector2(-amp, 2f), k); rot = Mathf.Lerp(0f, -0.5f, k); }
                else if (p < 0.45f) { float k = (p - 0.20f) / 0.25f; off = Vector2.Lerp(new Vector2(-amp, 2f), new Vector2(amp, -2f), k); rot = Mathf.Lerp(-0.5f, 0.5f, k); }
                else if (p < 0.70f) { float k = (p - 0.45f) / 0.25f; off = Vector2.Lerp(new Vector2(amp, -2f), new Vector2(-amp * 0.6f, 1f), k); rot = Mathf.Lerp(0.5f, 0f, k); }
                else { float k = (p - 0.70f) / 0.30f; off = Vector2.Lerp(new Vector2(-amp * 0.6f, 1f), Vector2.zero, k); rot = 0f; }
                boardRoot.anchoredPosition = _boardHome + off;
                boardRoot.localRotation = Quaternion.Euler(0f, 0f, rot);
                yield return null;
            }
            boardRoot.anchoredPosition = _boardHome;
            boardRoot.localRotation = Quaternion.identity;
            _shake = null;
        }
    }
}
