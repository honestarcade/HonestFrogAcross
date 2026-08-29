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
            UiKit.Label(root.transform, Copy.Get("aboutBody"), 28, UiKit.TextBlue,
                new Vector2(-440, 270), new Vector2(940, 170), TextAnchor.UpperLeft);
            Card(root.transform, "Swipe", Copy.Get("aboutSwipe"), new Vector2(-680, 40), false, 500, 230);
            Card(root.transform, "Diagonal 43–47°", Copy.Get("aboutDiagonal"), new Vector2(-160, 40), true, 500, 230);
            UiKit.Label(root.transform, Copy.Get("aboutFooter"), 22, UiKit.TextDim, new Vector2(-440, -440),
                new Vector2(940, 32), TextAnchor.MiddleLeft);

            string[] laneKeys = { "ruleRoad", "ruleRiver", "ruleSwamp", "ruleTracks", "ruleBike", "ruleWalkway", "ruleMedians", "ruleBays" };
            for (int i = 0; i < laneKeys.Length; i++)
                UiKit.Label(root.transform, "· " + Copy.Get(laneKeys[i]), 23, UiKit.TextBlue,
                    new Vector2(530, 330 - i * 106), new Vector2(800, 100), TextAnchor.UpperLeft);
            return root.gameObject;
        }

        public static GameObject BuildGameplay(Transform parent, AppShell shell)
        {
            var root = Screen(parent, shell, "gameplay", "Gameplay");
            Card(root.transform, "THE GOAL", Copy.Get("goal"), new Vector2(-460, 230), true, 920, 240);
            Card(root.transform, "LEVELS & TIMING", Copy.Get("levelsTiming"), new Vector2(-460, -75), false, 920, 280);
            Card(root.transform, "SWIPING & TAPPING", Copy.Get("swiping"), new Vector2(-460, -400), false, 920, 320);
            string[] laneKeys = { "ruleRoad", "ruleRiver", "ruleSwamp", "ruleTracks", "ruleBike", "ruleWalkway", "ruleMedians", "ruleBays" };
            UiKit.Label(root.transform, "WHAT'S ON THE BOARD", 22, UiKit.TextDim, new Vector2(530, 400),
                new Vector2(800, 30), TextAnchor.MiddleLeft);
            for (int i = 0; i < laneKeys.Length; i++)
                UiKit.Label(root.transform, "· " + Copy.Get(laneKeys[i]), 23, UiKit.TextBlue,
                    new Vector2(530, 340 - i * 106), new Vector2(800, 100), TextAnchor.UpperLeft);
            return root.gameObject;
        }

        public static GameObject BuildStudio(Transform parent, AppShell shell)
        {
            // #91: the standard Honest Arcade studio screen (from the Honest
            // Sudoku design), adapted to landscape: promises left, support +
            // identity right.
            var root = Screen(parent, shell, "studio", "About Honest Arcade");

            // left column: OUR PROMISES
            UiKit.Label(root.transform, "OUR PROMISES", 22, UiKit.TextDim, new Vector2(-660, 390), new Vector2(400, 30), TextAnchor.MiddleLeft);
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
                var row = UiKit.Panel(root.transform, $"promise-{i}", new Color(1f, 1f, 1f, 0.05f));
                row.rectTransform.sizeDelta = new Vector2(880, 96);
                row.rectTransform.anchoredPosition = new Vector2(-470, 320 - i * 108);
                UiKit.Label(row.transform, "✓", 26, promises[i].c, new Vector2(-405, 0), new Vector2(40, 40));
                var k = UiKit.Label(row.transform, promises[i].k, 25, UiKit.White, new Vector2(30, 22), new Vector2(790, 32), TextAnchor.MiddleLeft);
                k.fontStyle = FontStyle.Bold;
                UiKit.Label(row.transform, promises[i].v, 19, UiKit.Hex("9FC3EE"), new Vector2(30, -22), new Vector2(790, 40), TextAnchor.MiddleLeft);
            }

            // right column: identity, support card, chips, links
            UiKit.Label(root.transform, Copy.Get("studioBody"), 26, UiKit.Hex("C6DAF0"),
                new Vector2(490, 330), new Vector2(820, 150), TextAnchor.UpperLeft);
            UiKit.Label(root.transform, Copy.Get("studioTagline"), 24, UiKit.Hex("7FA6D8"),
                new Vector2(490, 205), new Vector2(820, 60), TextAnchor.UpperLeft);

            var support = UiKit.Panel(root.transform, "support-card", new Color(0f, 0.839f, 0.706f, 0.10f));
            support.rectTransform.sizeDelta = new Vector2(840, 220);
            support.rectTransform.anchoredPosition = new Vector2(490, 40);
            UiKit.Label(support.transform, "SUPPORT HONEST ARCADE", 20, UiKit.Mint, new Vector2(0, 75), new Vector2(780, 28), TextAnchor.MiddleLeft);
            UiKit.Label(support.transform, Copy.Get("studioSupport"), 22, UiKit.Hex("C6DAF0"),
                new Vector2(0, 0), new Vector2(780, 90), TextAnchor.UpperLeft);
            var link = UiKit.Label(support.transform, "honestarcade.app/contribute →", 24, UiKit.White, new Vector2(0, -75), new Vector2(780, 32), TextAnchor.MiddleLeft);
            link.fontStyle = FontStyle.Bold;

            var chips = new (string text, Color c)[]
            {
                ("NO ADS", UiKit.Mint), ("NO TRACKING", UiKit.Hex("6FB4FF")),
                ("NO ACCOUNTS", UiKit.Hex("B48CFF")), ("NO PURCHASES", UiKit.Mint),
                ("NO PERMISSIONS", UiKit.Hex("6FB4FF")), ("OPEN SOURCE", UiKit.Hex("B48CFF")),
                ("WORKS OFFLINE", UiKit.Mint),
            };
            for (int i = 0; i < chips.Length; i++)
            {
                float x = 490 + (i % 4 - 1.5f) * 210;
                float y = -140 - (i / 4) * 62;
                Chip(root.transform, chips[i].text, chips[i].c, new Vector2(x, y));
            }

            UiKit.Label(root.transform, "HONESTARCADE.APP  ·  SOURCE ON GITHUB", 20, UiKit.Hex("7FA6D8"),
                new Vector2(490, -330), new Vector2(840, 30));
            return root.gameObject;
        }

        private static RectTransform Screen(Transform parent, AppShell shell, string name, string title)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            var rt = root.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            UiKit.Button(root.transform, "‹", new Vector2(-890, 465), new Vector2(76, 76), shell.Back, fontSize: 36);
            UiKit.Label(root.transform, title, 46, UiKit.White, new Vector2(-590, 465), new Vector2(540, 60), TextAnchor.MiddleLeft);
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
            UiKit.Label(card.transform, title, 22, accent ? UiKit.Mint : UiKit.TextDim,
                new Vector2(0, h / 2f - 32), new Vector2(w - 40, 30), TextAnchor.MiddleLeft);
            UiKit.Label(card.transform, body, 24, UiKit.TextBlue,
                new Vector2(0, -18), new Vector2(w - 40, h - 76), TextAnchor.UpperLeft);
        }

        private static void Chip(Transform parent, string text, Color color, Vector2 pos)
        {
            var chip = UiKit.Panel(parent, $"chip-{text}", new Color(color.r, color.g, color.b, 0.14f));
            chip.rectTransform.sizeDelta = new Vector2(200, 52);
            chip.rectTransform.anchoredPosition = pos;
            UiKit.Label(chip.transform, text, 17, color, Vector2.zero, new Vector2(200, 52));
        }
    }
}
