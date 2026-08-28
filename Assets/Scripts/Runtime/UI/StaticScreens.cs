using System;
using System.Collections.Generic;
using UnityEngine;

namespace FrogAcross.UI
{
    /// <summary>Copy loader: Resources/copy.json — text changes need no code edits (#58 AC).</summary>
    public static class Copy
    {
        private static Dictionary<string, string> _map;

        public static string Get(string key)
        {
            if (_map == null)
            {
                _map = new Dictionary<string, string>();
                var text = Resources.Load<TextAsset>("copy");
                if (text != null)
                {
                    // flat string map; parse with JsonUtility via wrapper-free scan
                    foreach (var line in text.text.Split('\n'))
                    {
                        int c = line.IndexOf("\":", StringComparison.Ordinal);
                        if (c < 0) continue;
                        string k = line.Substring(0, c).Trim().Trim('{', ' ', '"');
                        int vStart = line.IndexOf('"', c + 2);
                        int vEnd = line.LastIndexOf('"');
                        if (vStart < 0 || vEnd <= vStart) continue;
                        _map[k] = line.Substring(vStart + 1, vEnd - vStart - 1).Replace("\\u2019", "'");
                    }
                }
            }
            return _map.TryGetValue(key, out var v) ? v : $"[{key}]";
        }

        public static void Invalidate() => _map = null;
    }

    /// <summary>#58: About / Gameplay / Studio — teaching the SHIPPED rules.</summary>
    public static class StaticScreens
    {
        public static GameObject BuildAbout(Transform parent, AppShell shell)
        {
            var root = Screen(parent, shell, "about", "About Frog Across");
            UiKit.Label(root.transform, Copy.Get("aboutBody"), 22, UiKit.TextBlue,
                new Vector2(-440, 280), new Vector2(820, 140), TextAnchor.UpperLeft);
            Card(root.transform, "Swipe", Copy.Get("aboutSwipe"), new Vector2(-660, 90), false);
            Card(root.transform, "Diagonal 43–47°", Copy.Get("aboutDiagonal"), new Vector2(-220, 90), true);
            UiKit.Label(root.transform, Copy.Get("aboutFooter"), 16, UiKit.TextDim, new Vector2(-440, -420),
                new Vector2(820, 26), TextAnchor.MiddleLeft);

            string[] laneKeys = { "ruleRoad", "ruleRiver", "ruleSwamp", "ruleTracks", "ruleBike", "ruleWalkway", "ruleMedians", "ruleBays" };
            for (int i = 0; i < laneKeys.Length; i++)
                UiKit.Label(root.transform, "· " + Copy.Get(laneKeys[i]), 18, UiKit.TextBlue,
                    new Vector2(510, 300 - i * 88), new Vector2(760, 84), TextAnchor.UpperLeft);
            return root.gameObject;
        }

        public static GameObject BuildGameplay(Transform parent, AppShell shell)
        {
            var root = Screen(parent, shell, "gameplay", "Gameplay");
            Card(root.transform, "THE GOAL", Copy.Get("goal"), new Vector2(-460, 220), true, 860, 190);
            Card(root.transform, "LEVELS & TIMING", Copy.Get("levelsTiming"), new Vector2(-460, -40), false, 860, 220);
            Card(root.transform, "SWIPING & TAPPING", Copy.Get("swiping"), new Vector2(-460, -320), false, 860, 250);
            string[] laneKeys = { "ruleRoad", "ruleRiver", "ruleSwamp", "ruleTracks", "ruleBike", "ruleWalkway", "ruleMedians", "ruleBays" };
            UiKit.Label(root.transform, "WHAT'S ON THE BOARD", 16, UiKit.TextDim, new Vector2(510, 400),
                new Vector2(760, 24), TextAnchor.MiddleLeft);
            for (int i = 0; i < laneKeys.Length; i++)
                UiKit.Label(root.transform, "· " + Copy.Get(laneKeys[i]), 18, UiKit.TextBlue,
                    new Vector2(510, 340 - i * 92), new Vector2(760, 88), TextAnchor.UpperLeft);
            return root.gameObject;
        }

        public static GameObject BuildStudio(Transform parent, AppShell shell)
        {
            var root = Screen(parent, shell, "studio", "Honest Arcade");
            UiKit.Label(root.transform, Copy.Get("studioBody"), 24, UiKit.TextBlue,
                new Vector2(-380, 180), new Vector2(900, 220), TextAnchor.UpperLeft);
            UiKit.Label(root.transform, Copy.Get("studioTagline"), 24, UiKit.TextDim,
                new Vector2(-380, 0), new Vector2(900, 80), TextAnchor.UpperLeft);
            Chip(root.transform, "NO ADS", UiKit.Mint, new Vector2(-700, -140));
            Chip(root.transform, "NO TRACKING", UiKit.Hex("6FB4FF"), new Vector2(-460, -140));
            Chip(root.transform, "OPEN SOURCE", UiKit.Hex("B48CFF"), new Vector2(-200, -140));
            return root.gameObject;
        }

        private static RectTransform Screen(Transform parent, AppShell shell, string name, string title)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            var rt = root.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            UiKit.Button(root.transform, "‹", new Vector2(-880, 460), new Vector2(56, 56), shell.Back);
            UiKit.Label(root.transform, title, 34, UiKit.White, new Vector2(-620, 460), new Vector2(460, 48), TextAnchor.MiddleLeft);
            return rt;
        }

        private static void Card(Transform parent, string title, string body, Vector2 pos, bool accent,
            float w = 420, float h = 170)
        {
            var card = UiKit.Panel(parent, $"card-{title}", accent
                ? new Color(0f, 0.839f, 0.706f, 0.12f)
                : new Color(1f, 1f, 1f, 0.05f));
            card.rectTransform.sizeDelta = new Vector2(w, h);
            card.rectTransform.anchoredPosition = pos;
            UiKit.Label(card.transform, title, 17, accent ? UiKit.Mint : UiKit.TextDim,
                new Vector2(0, h / 2f - 26), new Vector2(w - 30, 24), TextAnchor.MiddleLeft);
            UiKit.Label(card.transform, body, 19, UiKit.TextBlue,
                new Vector2(0, -14), new Vector2(w - 30, h - 60), TextAnchor.UpperLeft);
        }

        private static void Chip(Transform parent, string text, Color color, Vector2 pos)
        {
            var chip = UiKit.Panel(parent, $"chip-{text}", new Color(color.r, color.g, color.b, 0.14f));
            chip.rectTransform.sizeDelta = new Vector2(210, 44);
            chip.rectTransform.anchoredPosition = pos;
            UiKit.Label(chip.transform, text, 16, color, Vector2.zero, new Vector2(210, 44));
        }
    }
}
