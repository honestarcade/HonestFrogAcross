using System;
using UnityEngine;
using UnityEngine.UI;

namespace FrogAcross.UI
{
    /// <summary>Design-language UI builders (navy/mint palette, code-built = reviewable).</summary>
    public static class UiKit
    {
        public static readonly Color Navy = Hex("05285F");
        public static readonly Color NavyDeep = Hex("031634");
        public static readonly Color PanelNavy = Hex("0B3670");
        public static readonly Color Mint = Hex("00D6B4");
        public static readonly Color TextBlue = Hex("9FC3EE");
        public static readonly Color TextDim = Hex("6E93C4");
        public static readonly Color White = Color.white;
        public static readonly Color Gold = Hex("FFC94A");
        public static readonly Color Silver = Hex("C9D3DE");
        public static readonly Color Bronze = Hex("CE8A4E");
        public static readonly Color Danger = Hex("E05A4E");

        public static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out var c);
            return c;
        }

        public static Font DefaultFont => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        /// <summary>Corner radius in reference pixels — the design rounds everything.</summary>
        public const int Radius = 28;
        public const int PillRadius = 999;

        private static readonly System.Collections.Generic.Dictionary<int, Sprite> RoundedCache = new();

        /// <summary>
        /// A 9-sliced rounded-rect sprite, generated once per radius. Unity UI
        /// has no corner-radius property, so every panel and button borrows
        /// this sprite in Sliced mode and keeps its corners at any size.
        /// </summary>
        public static Sprite Rounded(int radius = Radius)
        {
            if (RoundedCache.TryGetValue(radius, out var cached) && cached != null) return cached;
            int r = Mathf.Clamp(radius, 2, 64);
            int size = r * 2 + 4; // 2px of straight edge in the middle for stretching
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = $"rounded-{r}",
            };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                // distance outside the rounded rect, antialiased over one pixel
                float dx = Mathf.Max(r - x - 0.5f, x + 0.5f - (size - r), 0f);
                float dy = Mathf.Max(r - y - 0.5f, y + 0.5f - (size - r), 0f);
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(r - d + 0.5f);
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f),
                100f, 0, SpriteMeshType.FullRect, new Vector4(r, r, r, r));
            sprite.name = $"rounded-{r}";
            RoundedCache[radius] = sprite;
            return sprite;
        }

        /// <summary>Applies the rounded sprite to an existing Image.</summary>
        public static Image Round(Image img, int radius = Radius)
        {
            img.sprite = Rounded(radius);
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 1f;
            return img;
        }

        public static Sprite Logo => Resources.Load<Sprite>("UI/logo-frog");

        public static Canvas Canvas(Transform parent, string name, int order = 0)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = order;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            // Landscape-only: match HEIGHT so the canvas is always 1080 tall and
            // at least 1920 wide (wider phones just get more room). Matching
            // 50/50 shrank the canvas to ~1662 units on a 21:9 panel, which
            // silently clipped layouts authored against the 1920 reference.
            scaler.matchWidthOrHeight = 1f;
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        public static Image Panel(Transform parent, string name, Color color, int radius = Radius)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            if (radius > 0) Round(img, radius);
            return img;
        }

        /// <summary>Square-cornered fill (backgrounds, scrims, progress fills).</summary>
        public static Image Fill(Transform parent, string name, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            return img;
        }

        public static Image Stretch(Image img)
        {
            var rt = img.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return img;
        }

        public static Text Label(Transform parent, string text, int size, Color color,
            Vector2 pos, Vector2? sizeDelta = null, TextAnchor align = TextAnchor.MiddleCenter)
        {
            var go = new GameObject("label");
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font = DefaultFont;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            var rt = t.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = sizeDelta ?? new Vector2(600, size + 14);
            rt.anchoredPosition = pos;
            return t;
        }

        public static Button Button(Transform parent, string label, Vector2 pos, Vector2 size,
            Action onClick, bool primary = false, int fontSize = 22)
        {
            var go = new GameObject($"btn-{label}");
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = primary ? Mint : new Color(1f, 1f, 1f, 0.10f);
            Round(img);
            var btn = go.AddComponent<Button>();
            btn.onClick.AddListener(() => FrogAcross.Audio.AudioDirector.Instance.Play(FrogAcross.Audio.GameSound.UiTap));
            if (onClick != null) btn.onClick.AddListener(() => onClick());
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            var t = Label(go.transform, label, fontSize, primary ? Navy : White, Vector2.zero, size);
            t.raycastTarget = false;
            return btn;
        }

        public static Toggle Toggle(Transform parent, Vector2 pos, bool value, Action<bool> onChange)
        {
            var go = new GameObject("toggle");
            go.transform.SetParent(parent, false);
            var bg = go.AddComponent<Image>();
            bg.color = new Color(1f, 1f, 1f, 0.15f);
            var rt = bg.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(72, 40);
            rt.anchoredPosition = pos;

            var knobGo = new GameObject("knob");
            knobGo.transform.SetParent(go.transform, false);
            var knob = knobGo.AddComponent<Image>();
            knob.color = White;
            var krt = knob.rectTransform;
            krt.sizeDelta = new Vector2(32, 32);

            var toggle = go.AddComponent<Toggle>();
            toggle.targetGraphic = bg;
            toggle.isOn = value;
            void Sync(bool v)
            {
                bg.color = v ? Mint : new Color(1f, 1f, 1f, 0.15f);
                krt.anchoredPosition = new Vector2(v ? 16 : -16, 0);
            }
            Sync(value);
            toggle.onValueChanged.AddListener(v => { Sync(v); onChange?.Invoke(v); });
            return toggle;
        }

        /// <summary>
        /// The full logo: frog mark above the two-tone spaced wordmark
        /// ("Frog" white + " Across" mint — owner decision).
        /// </summary>
        public static Transform Logotype(Transform parent, Vector2 pos, int size, bool withMark = true)
        {
            var go = new GameObject("logotype");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;

            if (withMark && Logo != null)
            {
                var markGo = new GameObject("mark");
                markGo.transform.SetParent(go.transform, false);
                var mark = markGo.AddComponent<Image>();
                mark.sprite = Logo;
                mark.preserveAspect = true;
                mark.raycastTarget = false;
                float m = size * 2.3f;
                mark.rectTransform.sizeDelta = new Vector2(m, m);
                mark.rectTransform.anchoredPosition = new Vector2(0, size * 1.35f);
            }
            Lockup(go.transform, Vector2.zero, size);
            return go.transform;
        }

        /// <summary>The two-tone spaced wordmark alone.</summary>
        public static Transform Lockup(Transform parent, Vector2 pos, int size)
        {
            var go = new GameObject("lockup");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            // widths sized to the glyphs at any point size ("Frog" ≈ 2.3em,
            // " Across" ≈ 3.6em bold) so large lockups neither wrap nor overlap
            float frogW = size * 2.6f, acrossW = size * 4.0f;
            var frog = Label(go.transform, "Frog", size, White,
                new Vector2(-frogW / 2f - size * 0.05f, 0), new Vector2(frogW, size + 16), TextAnchor.MiddleRight);
            frog.fontStyle = FontStyle.Bold;
            var across = Label(go.transform, " Across", size, Mint,
                new Vector2(acrossW / 2f + size * 0.05f, 0), new Vector2(acrossW, size + 16), TextAnchor.MiddleLeft);
            across.fontStyle = FontStyle.Bold;
            return go.transform;
        }

        /// <summary>Comfortable top inset: clear of the status bar and camera cutout.</summary>
        public const float HeaderTop = -96f;
        public const float EdgePad = 84f;
        public const float TapTarget = 96f;

        /// <summary>
        /// The shared screen header: an oversized rounded back button and the
        /// title, anchored top-left so every screen sits the same distance
        /// from the edge. Returned transform is raised to last sibling — a
        /// screen's own content can never cover the back button (#6/#..).
        /// </summary>
        public static RectTransform Header(Transform parent, string title, Action onBack, string subtitle = null)
        {
            var go = new GameObject("header");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.offsetMin = new Vector2(0f, 0f);
            rt.offsetMax = new Vector2(0f, 0f);
            rt.sizeDelta = new Vector2(0f, 200f);
            rt.anchoredPosition = Vector2.zero;

            var back = Button(go.transform, "‹", Vector2.zero, new Vector2(TapTarget, TapTarget), onBack, fontSize: 44);
            var brt = back.image.rectTransform;
            brt.anchorMin = brt.anchorMax = new Vector2(0f, 1f);
            brt.pivot = new Vector2(0f, 1f);
            brt.anchoredPosition = new Vector2(EdgePad, HeaderTop);

            var titleLabel = Label(go.transform, title, 52, White, Vector2.zero,
                new Vector2(760, 66), TextAnchor.MiddleLeft);
            titleLabel.fontStyle = FontStyle.Bold;
            var trt = titleLabel.rectTransform;
            trt.anchorMin = trt.anchorMax = new Vector2(0f, 1f);
            trt.pivot = new Vector2(0f, 1f);
            trt.anchoredPosition = new Vector2(EdgePad + TapTarget + 36f, HeaderTop - 12f);

            if (!string.IsNullOrEmpty(subtitle))
            {
                var sub = Label(go.transform, subtitle, 24, TextDim, Vector2.zero,
                    new Vector2(760, 32), TextAnchor.MiddleLeft);
                var srt = sub.rectTransform;
                srt.anchorMin = srt.anchorMax = new Vector2(0f, 1f);
                srt.pivot = new Vector2(0f, 1f);
                srt.anchoredPosition = new Vector2(EdgePad + TapTarget + 40f, HeaderTop - 74f);
            }

            go.transform.SetAsLastSibling();
            return rt;
        }

        /// <summary>
        /// A vertical scroll region. Returns the content RectTransform (top-
        /// anchored, pivot at top) — place children at negative Y and set
        /// content height with SetContentHeight.
        /// </summary>
        public static RectTransform ScrollArea(Transform parent, Vector2 topLeftInset, Vector2 bottomRightInset)
        {
            var viewGo = new GameObject("scroll-view");
            viewGo.transform.SetParent(parent, false);
            var view = viewGo.AddComponent<RectTransform>();
            view.anchorMin = Vector2.zero;
            view.anchorMax = Vector2.one;
            view.offsetMin = new Vector2(bottomRightInset.x, bottomRightInset.y);
            view.offsetMax = new Vector2(-topLeftInset.x, -topLeftInset.y);
            viewGo.AddComponent<RectMask2D>();

            var contentGo = new GameObject("content");
            contentGo.transform.SetParent(viewGo.transform, false);
            var content = contentGo.AddComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = new Vector2(0f, 0f);
            content.offsetMax = new Vector2(0f, 0f);
            content.anchoredPosition = Vector2.zero;

            // Children anchor to their parent's CENTRE by default, so on a tall
            // page they land half a page too low. This zero-sized node pins the
            // anchor to the top, letting callers author plain "-Y from the top".
            var originGo = new GameObject("origin");
            originGo.transform.SetParent(contentGo.transform, false);
            var origin = originGo.AddComponent<RectTransform>();
            origin.anchorMin = origin.anchorMax = new Vector2(0.5f, 1f);
            origin.pivot = new Vector2(0.5f, 1f);
            origin.sizeDelta = Vector2.zero;
            origin.anchoredPosition = Vector2.zero;

            var scroll = viewGo.AddComponent<ScrollRect>();
            scroll.content = content;
            scroll.viewport = view;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.elasticity = 0.08f;
            scroll.scrollSensitivity = 40f;
            scroll.inertia = true;
            scroll.decelerationRate = 0.12f;
            return origin;
        }

        /// <summary>Sets the scrollable height (pass the transform ScrollArea returned).</summary>
        public static void SetContentHeight(RectTransform origin, float height)
        {
            var content = ScrollContent(origin);
            content.sizeDelta = new Vector2(content.sizeDelta.x, height);
        }

        /// <summary>The sized, scrolling surface behind a ScrollArea origin.</summary>
        public static RectTransform ScrollContent(RectTransform origin) => (RectTransform)origin.parent;
    }

    /// <summary>
    /// Insets a full-screen RectTransform to the device's safe area, so no
    /// control ever lands under a camera cutout or the gesture bar.
    /// </summary>
    public sealed class SafeArea : MonoBehaviour
    {
        private Rect _applied;

        private void OnEnable() => Apply();

        private void Update()
        {
            if (Screen.safeArea != _applied) Apply();
        }

        private void Apply()
        {
            var rt = transform as RectTransform;
            if (rt == null || Screen.width == 0 || Screen.height == 0) return;
            var safe = Screen.safeArea;
            _applied = safe;
            var min = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
            var max = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }
}
