using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Neon7
{
    /// <summary>
    /// Визуал одного шара: цвет по цифре, glow, цифра, трещины, анимации squash/pop/падение.
    /// Иерархия префаба: [RectTransform] Ball -> Image face, Image glow, Image cracks, TMP_Text num.
    /// </summary>
    public class BallView : MonoBehaviour
    {
        [SerializeField] private Image face;
        [SerializeField] private Image glow;
        [SerializeField] private Image cracks;
        [SerializeField] private TMP_Text numText;

        [Header("Спрайты (Assets/Textures/Balls)")]
        [SerializeField] private Sprite[] numberSprites = new Sprite[7];
        [SerializeField] private Sprite obsidianSprite;
        [SerializeField] private Sprite obsidianCrackedSprite;

        public BallData Data { get; private set; }

        private RectTransform _rt;
        private RectTransform _faceRt;
        private RectTransform _glowRt;
        private RectTransform _cracksRt;
        private RectTransform _numRt;
        private float _cell;
        private Coroutine _move;

        private void Awake()
        {
            _rt = (RectTransform)transform;
            _faceRt = (RectTransform)face.transform;
            _glowRt = glow ? (RectTransform)glow.transform : null;
            _cracksRt = cracks ? (RectTransform)cracks.transform : null;
            _numRt = numText ? (RectTransform)numText.transform : null;
        }

        public void Init(BallData data, float cell, float fontSize)
        {
            Data = data;
            _cell = cell;
            _rt.anchorMin = _rt.anchorMax = new Vector2(0f, 1f);
            _rt.pivot = new Vector2(0f, 1f);
            _rt.localScale = Vector3.one;
            _rt.localRotation = Quaternion.identity;
            _rt.sizeDelta = new Vector2(cell, cell);

            float d = Metrics.BallDiameter(cell);
            PlaceAtCellCenter(_faceRt, d);
            if (_glowRt) PlaceAtCellCenter(_glowRt, d + Metrics.BallGlowOuter);
            if (_cracksRt) PlaceAtCellCenter(_cracksRt, d);
            if (_numRt)
            {
                PlaceAtCellCenter(_numRt, d);
                numText.enableAutoSizing = false;
                numText.enableWordWrapping = false;
                numText.overflowMode = TextOverflowModes.Overflow;
                numText.alignment = TextAlignmentOptions.Center;
                numText.margin = Vector4.zero;
                numText.raycastTarget = false;
                numText.fontSize = fontSize;
                numText.color = Palette.NumInk;
            }
            face.preserveAspect = true;
            face.raycastTarget = false;
            if (glow) glow.raycastTarget = false;
            if (cracks) cracks.raycastTarget = false;
            Refresh();
            SetCell(data.Col, data.Row, instant: true);
        }

        private static void PlaceAtCellCenter(RectTransform rt, float size)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(size, size);
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
        }

        public void Refresh()
        {
            bool hidden = Data.Num == null;
            Color c = Palette.ForNumber(Data.Num);

            // ВАЖНО: спрайты в Assets/Textures/Balls уже окрашены (градиент + блик + тень),
            // поэтому tint = белый. Второй раз цвет не накладываем, иначе шар «пересвечен»
            // и не совпадает с веб-версией.
            face.sprite = hidden ? obsidianSprite : numberSprites[Data.Num.Value - 1];
            face.color = Color.white;

            if (glow)
            {
                glow.enabled = !hidden;                      // обсидиан не светится
                glow.color = new Color(c.r, c.g, c.b, 0.60f);
            }

            numText.text = hidden ? string.Empty : Data.Num.Value.ToString();

            if (cracks)
            {
                // трещины — отдельный слой ball_obsidian_cracked (лава уже в текстуре, blend Screen)
                cracks.enabled = hidden && Data.Cracks > 0;
                cracks.sprite = obsidianCrackedSprite;
                cracks.color = new Color(1f, 1f, 1f, 0.95f);
            }
        }

        public Vector2 CellToLocal(int col, int row) => new Vector2(col * _cell, -row * _cell);

        public void SetCell(int col, int row, bool instant)
        {
            Data.Col = col;
            Data.Row = row;
            if (instant)
            {
                _rt.anchoredPosition = CellToLocal(col, row);
                return;
            }
            if (_move != null) StopCoroutine(_move);
            _move = StartCoroutine(MoveTo(CellToLocal(col, row), Metrics.DropMove));
        }

        private IEnumerator MoveTo(Vector2 target, float dur)
        {
            Vector2 from = _rt.anchoredPosition;
            for (float t = 0f; t < dur; t += Time.deltaTime)
            {
                float k = Easing.Drop(t / dur);
                // top: 190ms drop-кривая, left: 140ms ease — как в CSS
                float kx = Easing.Ease(Mathf.Clamp01(t / Metrics.ColumnMove));
                _rt.anchoredPosition = new Vector2(
                    Mathf.LerpUnclamped(from.x, target.x, kx),
                    Mathf.LerpUnclamped(from.y, target.y, k));
                yield return null;
            }
            _rt.anchoredPosition = target;
            _move = null;
        }

        /// <summary>squash 260ms: scale(1.25,0.62) -> (0.90,1.12) -> (1,1).</summary>
        public IEnumerator PlaySquash()
        {
            float dur = Metrics.Squash;
            for (float t = 0f; t < dur; t += Time.deltaTime)
            {
                float p = Easing.SquashCurve(t / dur);
                Vector2 s;
                if (p < 0.55f)
                {
                    float k = p / 0.55f;
                    s = new Vector2(Mathf.Lerp(1.25f, 0.90f, k), Mathf.Lerp(0.62f, 1.12f, k));
                }
                else
                {
                    float k = (p - 0.55f) / 0.45f;
                    s = new Vector2(Mathf.Lerp(0.90f, 1f, k), Mathf.Lerp(1.12f, 1f, k));
                }
                _faceRt.localScale = new Vector3(s.x, s.y, 1f);
                yield return null;
            }
            _faceRt.localScale = Vector3.one;
        }

        /// <summary>
        /// pop 300ms: scale 1 -> 1.28 (brightness 2.2) -> 0.1, opacity -> 0.
        /// Спрайт уже окрашен, поэтому «brightness» имитируем засветкой в белый (Lerp к белому)
        /// и усилением glow — визуально это то же, что filter: brightness() в CSS.
        /// </summary>
        public IEnumerator PlayPop()
        {
            float dur = Metrics.Pop;
            Color glowBase = glow ? glow.color : Color.clear;
            for (float t = 0f; t < dur; t += Time.deltaTime)
            {
                float p = Easing.EaseOut(t / dur);
                float scale, bright, alpha;
                if (p < 0.35f)
                {
                    float k = p / 0.35f;
                    scale = Mathf.Lerp(1f, 1.28f, k);
                    bright = Mathf.Lerp(1f, 2.2f, k);
                    alpha = 1f;
                }
                else
                {
                    float k = (p - 0.35f) / 0.65f;
                    scale = Mathf.Lerp(1.28f, 0.1f, k);
                    bright = Mathf.Lerp(2.2f, 3f, k);
                    alpha = 1f - k;
                }
                float white = Mathf.InverseLerp(1f, 3f, bright);   // 0..1
                _faceRt.localScale = Vector3.one * scale;
                face.color = new Color(1f, 1f, 1f, alpha);
                if (numText) numText.alpha = alpha * (1f - white);
                if (glow)
                {
                    Color g = Color.Lerp(new Color(glowBase.r, glowBase.g, glowBase.b), Color.white, white);
                    glow.enabled = true;
                    glow.color = new Color(g.r, g.g, g.b, Mathf.Clamp01(0.6f + 0.4f * white) * alpha);
                    ((RectTransform)glow.transform).localScale = Vector3.one * Mathf.Lerp(1f, 1.35f, white);
                }
                yield return null;
            }
        }
    }
}
