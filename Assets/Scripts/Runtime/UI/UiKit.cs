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
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        public static Image Panel(Transform parent, string name, Color color)
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
            img.color = primary ? Mint : new Color(1f, 1f, 1f, 0.06f);
            var btn = go.AddComponent<Button>();
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

        /// <summary>The two-tone spaced lockup: "Frog" white + "Across" mint (owner decision).</summary>
        public static Transform Lockup(Transform parent, Vector2 pos, int size)
        {
            var go = new GameObject("lockup");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            float half = size * 1.62f;
            var frog = Label(go.transform, "Frog", size, White, new Vector2(-half * 0.62f, 0), new Vector2(half * 1.5f, size + 12), TextAnchor.MiddleRight);
            frog.fontStyle = FontStyle.Bold;
            var across = Label(go.transform, " Across", size, Mint, new Vector2(half * 0.78f, 0), new Vector2(half * 1.8f, size + 12), TextAnchor.MiddleLeft);
            across.fontStyle = FontStyle.Bold;
            return go.transform;
        }
    }
}
