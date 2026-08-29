using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NeonSeven.UI
{
    public static class UiFactory
    {
        private static Font _font;
        private static Sprite _circle;
        private static Sprite _glow;
        private static Sprite _verticalGlow;
        private static Sprite _dashedRing;
        private static Sprite _softSquare;
        private static Sprite _button;

        public static Font Font
        {
            get
            {
                if (_font == null)
                    _font = Font.CreateDynamicFontFromOSFont(new[] { "Segoe UI Semibold", "Segoe UI", "Arial" }, 16)
                        ?? Resources.GetBuiltinResource<Font>("Arial.ttf")
                        ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

                return _font;
            }
        }

        public static Sprite Circle => _circle == null ? _circle = MakeCircleSprite() : _circle;
        public static Sprite Glow => _glow == null ? _glow = MakeGlowSprite() : _glow;
        public static Sprite VerticalGlow => _verticalGlow == null ? _verticalGlow = MakeVerticalGlowSprite() : _verticalGlow;
        public static Sprite DashedRing => _dashedRing == null ? _dashedRing = MakeDashedRingSprite() : _dashedRing;
        public static Sprite SoftSquare => _softSquare == null ? _softSquare = MakeSoftSquareSprite() : _softSquare;
        public static Sprite ButtonSprite => _button == null ? _button = Resources.Load<Sprite>("Textures/UI/button_candy_violet") : _button;

        public static Canvas CreateCanvas(string name, int sortingOrder)
        {
            var canvasGo = new GameObject(name);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(420f, 900f);
            scaler.matchWidthOrHeight = 0f;
            canvasGo.AddComponent<GraphicRaycaster>();

            if (Object.FindAnyObjectByType<EventSystem>() == null)
            {
                var eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<EventSystem>();
                eventSystem.AddComponent<StandaloneInputModule>();
            }

            return canvas;
        }

        public static Image Panel(Transform parent, string name, Color color, Sprite sprite = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.color = color;
            image.sprite = sprite ?? SoftSquare;
            image.type = Image.Type.Sliced;
            return image;
        }

        public static Text Label(Transform parent, string name, string text, int size, Color color, TextAnchor anchor, bool addShadow = true)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var label = go.AddComponent<Text>();
            label.font = Font;
            label.text = text;
            label.fontSize = size;
            label.fontStyle = FontStyle.Normal;
            label.color = color;
            label.alignment = anchor;
            label.resizeTextForBestFit = false;
            if (addShadow)
            {
                var shadow = go.AddComponent<Shadow>();
                shadow.effectColor = new Color(0f, 0f, 0f, 0.65f);
                shadow.effectDistance = new Vector2(1.6f, -1.6f);
            }

            return label;
        }

        public static Image Block(Transform parent, string name, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        public static Button Button(Transform parent, string name, string text, Color color)
        {
            var image = Panel(parent, name, color, ButtonSprite);
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var label = Label(image.transform, "Text", text, 12, Color.white, TextAnchor.MiddleCenter);
            label.fontStyle = FontStyle.Bold;
            Stretch(label.rectTransform);
            var outline = label.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.10f, 0.05f, 0.22f, 0.80f);
            outline.effectDistance = new Vector2(1.2f, -1.2f);
            return button;
        }

        public static Button GlassIconButton(Transform parent, string name, string text, out Text label)
        {
            var image = Panel(parent, name, new Color(0.24f, 0.22f, 0.48f, 0.55f), SoftSquare);
            image.type = Image.Type.Sliced;
            image.raycastTarget = true;
            var outline = image.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.80f, 0.78f, 1f, 0.18f);
            outline.effectDistance = new Vector2(1.3f, -1.3f);
            var shadow = image.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.45f);
            shadow.effectDistance = new Vector2(0f, -8f);
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            label = Label(image.transform, "Text", text, 16, Color.white, TextAnchor.MiddleCenter);
            Stretch(label.rectTransform);
            return button;
        }

        public static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        public static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static Sprite MakeCircleSprite()
        {
            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            float radius = size * 0.48f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center) / radius;
                    float alpha = Mathf.Clamp01(1f - Mathf.SmoothStep(0.92f, 1.02f, distance));
                    float highlight = Mathf.Clamp01(1f - Vector2.Distance(new Vector2(x, y), new Vector2(size * 0.34f, size * 0.70f)) / (size * 0.30f));
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha * (0.72f + highlight * 0.28f)));
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private static Sprite MakeGlowSprite()
        {
            const int size = 256;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            float radius = size * 0.48f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center) / radius;
                    float alpha = Mathf.Clamp01(1f - distance);
                    alpha = Mathf.Pow(alpha, 1.45f) * 0.95f;
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private static Sprite MakeVerticalGlowSprite()
        {
            const int width = 64;
            const int height = 256;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            for (int y = 0; y < height; y++)
            {
                float vertical = 1f - (float)y / (height - 1);
                float fade = Mathf.SmoothStep(0f, 1f, vertical);
                for (int x = 0; x < width; x++)
                {
                    float center = 1f - Mathf.Abs((x + 0.5f) / width - 0.5f) * 2f;
                    float alpha = fade * (0.38f + center * 0.42f);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f);
        }

        private static Sprite MakeDashedRingSprite()
        {
            const int size = 160;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            float radius = size * 0.39f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    var point = new Vector2(x, y);
                    float distance = Mathf.Abs(Vector2.Distance(point, center) - radius);
                    float stroke = Mathf.Clamp01(1f - distance / 3.2f);
                    float angle = Mathf.Atan2(point.y - center.y, point.x - center.x);
                    if (angle < 0f)
                        angle += Mathf.PI * 2f;
                    float dash = Mathf.Repeat(angle / (Mathf.PI * 2f) * 18f, 1f);
                    float alpha = dash < 0.58f ? stroke * 0.72f : 0f;
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private static Sprite MakeSoftSquareSprite()
        {
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float edge = Mathf.Min(Mathf.Min(x, y), Mathf.Min(size - 1 - x, size - 1 - y));
                    float alpha = Mathf.Clamp01(edge / 10f);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(14, 14, 14, 14));
        }
    }
}
