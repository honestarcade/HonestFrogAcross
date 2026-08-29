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
            var c = Screen(parent, shell, "about", "About Frog Across", 1180f, out var root);
            UiKit.Label(c, Copy.Get("aboutBody"), 30, UiKit.TextBlue,
                new Vector2(-380, -110), new Vector2(1000, 190), TextAnchor.UpperLeft);
            Card(c, "Swipe", Copy.Get("aboutSwipe"), new Vector2(-620, -330), false, 540, 250);
            Card(c, "Diagonal 43–47°", Copy.Get("aboutDiagonal"), new Vector2(-60, -330), true, 540, 250);

            string[] laneKeys = { "ruleRoad", "ruleRiver", "ruleSwamp", "ruleTracks", "ruleBike", "ruleWalkway", "ruleMedians", "ruleBays" };
            UiKit.Label(c, "WHAT'S ON THE BOARD", 24, UiKit.TextDim, new Vector2(560, -80),
                new Vector2(760, 32), TextAnchor.MiddleLeft);
            for (int i = 0; i < laneKeys.Length; i++)
                UiKit.Label(c, "· " + Copy.Get(laneKeys[i]), 25, UiKit.TextBlue,
                    new Vector2(560, -160 - i * 116), new Vector2(760, 110), TextAnchor.UpperLeft);

            UiKit.Label(c, Copy.Get("aboutFooter"), 24, UiKit.TextDim, new Vector2(-380, -620),
                new Vector2(1000, 34), TextAnchor.MiddleLeft);
            return root;
        }

        public static GameObject BuildGameplay(Transform parent, AppShell shell)
        {
            var c = Screen(parent, shell, "gameplay", "Gameplay", 1480f, out var root);
            Card(c, "THE GOAL", Copy.Get("goal"), new Vector2(-420, -180), true, 940, 270);
            Card(c, "LEVELS & TIMING", Copy.Get("levelsTiming"), new Vector2(-420, -500), false, 940, 300);
            Card(c, "SWIPING & TAPPING", Copy.Get("swiping"), new Vector2(-420, -850), false, 940, 340);

            string[] laneKeys = { "ruleRoad", "ruleRiver", "ruleSwamp", "ruleTracks", "ruleBike", "ruleWalkway", "ruleMedians", "ruleBays" };
            UiKit.Label(c, "WHAT'S ON THE BOARD", 24, UiKit.TextDim, new Vector2(560, -80),
                new Vector2(760, 32), TextAnchor.MiddleLeft);
            for (int i = 0; i < laneKeys.Length; i++)
                UiKit.Label(c, "· " + Copy.Get(laneKeys[i]), 25, UiKit.TextBlue,
                    new Vector2(560, -160 - i * 116), new Vector2(760, 110), TextAnchor.UpperLeft);
            return root;
        }

        public static GameObject BuildStudio(Transform parent, AppShell shell)
        {
            // #91: the standard Honest Arcade studio screen (from the Honest
            // Sudoku design), landscape: promises left, support + identity right.
            var c = Screen(parent, shell, "studio", "About Honest Arcade", 1240f, out var root);

            UiKit.Label(c, "OUR PROMISES", 24, UiKit.TextDim, new Vector2(-560, -70),
                new Vector2(400, 32), TextAnchor.MiddleLeft);
            var promises = new (string k, string v, Color c)[]
            {
                ("No ads. Ever.", Copy.Get("promiseAds"), UiKit.Mint),
                ("No tracking, no analytics", Copy.Get("promiseTracking"), UiKit.Hex("6FB4FF")),
                ("No accounts, no sign-in", Copy.Get("promiseAccounts"), UiKit.Hex("B48CFF")),
                ("No in-app purchases", Copy.Get("promisePurchases"), UiKit.Mint),
                ("No permissions", Copy.Get("promisePermissions"), UiKit.Hex("6FB4FF")),
                ("Open source", Copy.Get("promiseOpenSource"), UiKit.Hex("B48CFF")),
                ("Works offline, stays small", Copy.Get("promiseOffline"), UiKit.Mint),
            };
            for (int i = 0; i < promises.Length; i++)
            {
                var row = UiKit.Panel(c, $"promise-{i}", new Color(1f, 1f, 1f, 0.06f));
                row.rectTransform.sizeDelta = new Vector2(900, 116);
                row.rectTransform.anchoredPosition = new Vector2(-430, -190 - i * 132);
                UiKit.Label(row.transform, "✓", 32, promises[i].c, new Vector2(-410, 0), new Vector2(50, 44));
                var k = UiKit.Label(row.transform, promises[i].k, 29, UiKit.White, new Vector2(40, 26),
                    new Vector2(800, 38), TextAnchor.MiddleLeft);
                k.fontStyle = FontStyle.Bold;
                UiKit.Label(row.transform, promises[i].v, 22, UiKit.Hex("9FC3EE"), new Vector2(40, -26),
                    new Vector2(800, 44), TextAnchor.MiddleLeft);
            }

            UiKit.Label(c, Copy.Get("studioBody"), 29, UiKit.Hex("C6DAF0"),
                new Vector2(560, -170), new Vector2(760, 190), TextAnchor.UpperLeft);
            UiKit.Label(c, Copy.Get("studioTagline"), 26, UiKit.Hex("7FA6D8"),
                new Vector2(560, -330), new Vector2(760, 80), TextAnchor.UpperLeft);

            var support = UiKit.Panel(c, "support-card", new Color(0f, 0.839f, 0.706f, 0.12f));
            support.rectTransform.sizeDelta = new Vector2(780, 260);
            support.rectTransform.anchoredPosition = new Vector2(560, -520);
            UiKit.Label(support.transform, "SUPPORT HONEST ARCADE", 22, UiKit.Mint, new Vector2(0, 88),
                new Vector2(700, 30), TextAnchor.MiddleLeft);
            UiKit.Label(support.transform, Copy.Get("studioSupport"), 24, UiKit.Hex("C6DAF0"),
                new Vector2(0, 0), new Vector2(700, 110), TextAnchor.UpperLeft);
            var link = UiKit.Label(support.transform, "honestarcade.app/contribute →", 26, UiKit.White,
                new Vector2(0, -88), new Vector2(700, 36), TextAnchor.MiddleLeft);
            link.fontStyle = FontStyle.Bold;

            var chips = new (string text, Color c)[]
            {
                ("NO ADS", UiKit.Mint), ("NO TRACKING", UiKit.Hex("6FB4FF")),
                ("NO ACCOUNTS", UiKit.Hex("B48CFF")), ("NO PURCHASES", UiKit.Mint),
                ("NO PERMISSIONS", UiKit.Hex("6FB4FF")), ("OPEN SOURCE", UiKit.Hex("B48CFF")),
                ("WORKS OFFLINE", UiKit.Mint),
            };
            for (int i = 0; i < chips.Length; i++)
                Chip(c, chips[i].text, chips[i].c,
                    new Vector2(300 + (i % 3) * 230, -700 - (i / 3) * 76));

            UiKit.Label(c, "HONESTARCADE.APP  ·  SOURCE ON GITHUB", 22, UiKit.Hex("7FA6D8"),
                new Vector2(560, -940), new Vector2(780, 32), TextAnchor.MiddleLeft);
            return root;
        }

        /// <summary>
        /// Screen scaffold: fixed header + a scrolling content surface
        /// (owner: these pages must scroll, 2026-08-29). Returns the content
        /// transform — place children at negative Y from the top.
        /// </summary>
        private static RectTransform Screen(Transform parent, AppShell shell, string name, string title,
            float contentHeight, out GameObject root)
        {
            root = new GameObject(name);
            root.transform.SetParent(parent, false);
            var rt = root.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            var content = UiKit.ScrollArea(root.transform,
                topLeftInset: new Vector2(UiKit.EdgePad, 210f),
                bottomRightInset: new Vector2(UiKit.EdgePad, 40f));
            UiKit.SetContentHeight(content, contentHeight);
            UiKit.Header(root.transform, title, shell.Back);
            return content;
        }

        private static void Card(Transform parent, string title, string body, Vector2 pos, bool accent,
            float w = 420, float h = 170)
        {
            var card = UiKit.Panel(parent, $"card-{title}", accent
                ? new Color(0f, 0.839f, 0.706f, 0.12f)
                : new Color(1f, 1f, 1f, 0.05f));
            card.rectTransform.sizeDelta = new Vector2(w, h);
            card.rectTransform.anchoredPosition = pos;
            UiKit.Label(card.transform, title, 24, accent ? UiKit.Mint : UiKit.TextDim,
                new Vector2(0, h / 2f - 38), new Vector2(w - 56, 32), TextAnchor.MiddleLeft);
            UiKit.Label(card.transform, body, 26, UiKit.TextBlue,
                new Vector2(0, -20), new Vector2(w - 56, h - 92), TextAnchor.UpperLeft);
        }

        private static void Chip(Transform parent, string text, Color color, Vector2 pos)
        {
            var chip = UiKit.Panel(parent, $"chip-{text}", new Color(color.r, color.g, color.b, 0.16f), UiKit.PillRadius);
            chip.rectTransform.sizeDelta = new Vector2(216, 58);
            chip.rectTransform.anchoredPosition = pos;
            UiKit.Label(chip.transform, text, 19, color, Vector2.zero, new Vector2(216, 58));
        }
    }
}
