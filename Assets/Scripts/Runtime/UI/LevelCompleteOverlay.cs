using System;
using FrogAcross.Levels;
using FrogAcross.Sim;
using UnityEngine;
using UnityEngine.UI;

namespace FrogAcross.UI
{
    /// <summary>
    /// #54: the medal overlay, design language (navy panel #0B3670 on scrim,
    /// mint accent, medal dot per threshold). Built in code (reviewable,
    /// idempotent). Reads thresholds from LevelDefinition; real progression
    /// data (best deltas, persistence) arrives with #60.
    /// </summary>
    public sealed class LevelCompleteOverlay : MonoBehaviour
    {
        public event Action OnNext;
        public event Action OnReplay;
        public event Action OnLevels;

        private GameObject _root;
        private Text _time;
        private Text _medal;
        private Image _medalDot;

        public static (string name, Color color) MedalFor(float seconds, LevelDefinition level)
        {
            if (seconds <= level.GoldSeconds) return ("GOLD", new Color(1f, 0.788f, 0.29f));
            if (seconds <= level.SilverSeconds) return ("SILVER", new Color(0.788f, 0.827f, 0.871f));
            if (seconds <= level.BronzeSeconds) return ("BRONZE", new Color(0.808f, 0.541f, 0.306f));
            return ("COMPLETE", new Color(0.44f, 0.57f, 0.69f));
        }

        public void Show(LevelDefinition level, long clockTicks)
        {
            if (_root == null) Build();
            float seconds = clockTicks / (float)SimConfig.TicksPerSecond;
            var (name, color) = MedalFor(seconds, level);
            _time.text = $"{seconds:0.0}s";
            _medal.text = name;
            _medalDot.color = color;
            _root.SetActive(true);
        }

        public void Hide()
        {
            if (_root != null) _root.SetActive(false);
        }

        private void Build()
        {
            _root = new GameObject("level-complete");
            _root.transform.SetParent(transform, false);
            var canvas = _root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            _root.AddComponent<GraphicRaycaster>();

            var scrim = Panel(_root.transform, new Color(0.012f, 0.055f, 0.125f, 0.78f));
            Stretch(scrim.rectTransform);

            var panel = Panel(_root.transform, new Color(0.043f, 0.212f, 0.439f)); // #0B3670
            var pr = panel.rectTransform;
            pr.anchorMin = pr.anchorMax = new Vector2(0.5f, 0.5f);
            pr.sizeDelta = new Vector2(520, 300);

            var title = Label(panel.transform, "Level complete", 30, new Vector2(0, 105));
            title.color = Color.white;
            _time = Label(panel.transform, "0.0s", 52, new Vector2(0, 40));
            _time.color = Color.white;

            var dotGo = new GameObject("medal-dot");
            dotGo.transform.SetParent(panel.transform, false);
            _medalDot = dotGo.AddComponent<Image>();
            var dr = _medalDot.rectTransform;
            dr.anchorMin = dr.anchorMax = new Vector2(0.5f, 0.5f);
            dr.sizeDelta = new Vector2(26, 26);
            dr.anchoredPosition = new Vector2(-52, -18);
            _medal = Label(panel.transform, "GOLD", 22, new Vector2(22, -18));
            _medal.color = new Color(0.62f, 0.76f, 0.93f);

            Btn(panel.transform, "Next level", new Vector2(-160, -105), () => OnNext?.Invoke(), true);
            Btn(panel.transform, "Replay", new Vector2(0, -105), () => OnReplay?.Invoke(), false);
            Btn(panel.transform, "Levels", new Vector2(160, -105), () => OnLevels?.Invoke(), false);

            _root.SetActive(false);
        }

        private static Image Panel(Transform parent, Color c)
        {
            var go = new GameObject("panel");
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = c;
            return img;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        private static Text Label(Transform parent, string text, int size, Vector2 pos)
        {
            var go = new GameObject("label");
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.text = text;
            t.fontSize = size;
            t.alignment = TextAnchor.MiddleCenter;
            var rt = t.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(480, size + 16);
            rt.anchoredPosition = pos;
            return t;
        }

        private static void Btn(Transform parent, string label, Vector2 pos, Action onClick, bool primary)
        {
            var go = new GameObject($"btn-{label}");
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = primary ? new Color(0f, 0.839f, 0.706f) : new Color(1f, 1f, 1f, 0.08f);
            var btn = go.AddComponent<Button>();
            btn.onClick.AddListener(() => onClick());
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(140, 52);
            rt.anchoredPosition = pos;
            var t = Label(go.transform, label, 20, Vector2.zero);
            t.color = primary ? new Color(0.02f, 0.157f, 0.373f) : Color.white;
            t.rectTransform.sizeDelta = new Vector2(140, 52);
        }
    }
}
