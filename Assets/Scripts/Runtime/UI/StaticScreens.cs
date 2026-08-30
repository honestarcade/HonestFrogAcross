using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
        public const string SupportUrl = "https://honestarcade.app/contribute";

        public static GameObject BuildAbout(Transform parent, AppShell shell)
        {
            var c = Screen(parent, shell, "about", "About Frog Across", 1500f, out var root);

            var left = UiKit.Column(c, new Vector2(-880, -70), 900f, 30f);
            UiKit.Label(left, Copy.Get("aboutBody"), UiKit.Body, UiKit.TextBlue,
                Vector2.zero, new Vector2(900, 0), TextAnchor.UpperLeft);
            Card(left, "SWIPE", Copy.Get("aboutSwipe"), false, 900f);
            Card(left, "DIAGONAL 43–47°", Copy.Get("aboutDiagonal"), true, 900f);
            UiKit.Label(left, $"v{Application.version}  ·  {FrogAcross.Levels.LevelCatalog.Count} LEVELS  ·  6 CHARACTERS  ·  NO ADS",
                UiKit.Caption, UiKit.TextDim, Vector2.zero, new Vector2(900, 48), TextAnchor.MiddleLeft);

            BoardRules(c, new Vector2(80, -70));
            return root;
        }

        public static GameObject BuildGameplay(Transform parent, AppShell shell)
        {
            var c = Screen(parent, shell, "gameplay", "Gameplay", 1900f, out var root);

            var left = UiKit.Column(c, new Vector2(-880, -70), 900f, 30f);
            Card(left, "THE GOAL", Copy.Get("goal"), true, 900f);
            Card(left, "LEVELS & TIMING", Copy.Get("levelsTiming"), false, 900f);
            Card(left, "SWIPING & TAPPING", Copy.Get("swiping"), false, 900f);

            BoardRules(c, new Vector2(80, -70));
            return root;
        }

        public static GameObject BuildStudio(Transform parent, AppShell shell)
        {
            // #91: the standard Honest Arcade studio screen (from the Honest
            // Sudoku design), landscape: promises left, support + identity right.
            var c = Screen(parent, shell, "studio", "About Honest Arcade", 1900f, out var root);

            UiKit.Label(c, "OUR PROMISES", UiKit.Caption, UiKit.TextDim, new Vector2(-600, -80),
                new Vector2(500, 44), TextAnchor.MiddleLeft);
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
            var promiseColumn = UiKit.Column(c, new Vector2(-880, -140), 940f, 22f);
            for (int i = 0; i < promises.Length; i++)
            {
                var row = UiKit.Panel(promiseColumn, $"promise-{i}", new Color(1f, 1f, 1f, 0.06f));
                var rowLayout = row.gameObject.AddComponent<VerticalLayoutGroup>();
                rowLayout.padding = new RectOffset(84, 32, 22, 22);
                rowLayout.spacing = 8;
                rowLayout.childControlHeight = true;
                rowLayout.childControlWidth = true;
                rowLayout.childForceExpandHeight = false;
                var rowFitter = row.gameObject.AddComponent<ContentSizeFitter>();
                rowFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                var k = UiKit.Label(row.transform, promises[i].k, UiKit.Heading, UiKit.White,
                    Vector2.zero, new Vector2(820, 0), TextAnchor.MiddleLeft);
                k.fontStyle = FontStyle.Bold;
                UiKit.Label(row.transform, promises[i].v, UiKit.Caption, UiKit.Hex("9FC3EE"),
                    Vector2.zero, new Vector2(820, 0), TextAnchor.UpperLeft);

                var tick = UiKit.Label(row.transform, "✓", UiKit.Heading, promises[i].c,
                    new Vector2(-424, 0), new Vector2(64, 64));
                tick.rectTransform.anchorMin = tick.rectTransform.anchorMax = new Vector2(0f, 1f);
                tick.rectTransform.pivot = new Vector2(0f, 1f);
                tick.rectTransform.anchoredPosition = new Vector2(20, -24);
                tick.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            }

            UiKit.Label(c, Copy.Get("studioBody"), UiKit.Body, UiKit.Hex("C6DAF0"),
                new Vector2(540, -190), new Vector2(840, 260), TextAnchor.UpperLeft);
            UiKit.Label(c, Copy.Get("studioTagline"), UiKit.Body, UiKit.Hex("7FA6D8"),
                new Vector2(540, -420), new Vector2(840, 100), TextAnchor.UpperLeft);

            var support = UiKit.Panel(c, "support-card", new Color(0f, 0.839f, 0.706f, 0.14f));
            var supportBtn = support.gameObject.AddComponent<Button>();
            supportBtn.targetGraphic = support;
            supportBtn.onClick.AddListener(() =>
                FrogAcross.Audio.AudioDirector.Instance.Play(FrogAcross.Audio.GameSound.UiTap));
            // hands off to the browser: no network access of our own (invariant 1)
            supportBtn.onClick.AddListener(() => Application.OpenURL(SupportUrl));
            support.rectTransform.sizeDelta = new Vector2(860, 360);
            support.rectTransform.anchoredPosition = new Vector2(540, -680);
            UiKit.Label(support.transform, "SUPPORT HONEST ARCADE", UiKit.Caption, UiKit.Mint, new Vector2(0, 128),
                new Vector2(780, 44), TextAnchor.MiddleLeft);
            UiKit.Label(support.transform, Copy.Get("studioSupport"), UiKit.Body, UiKit.Hex("C6DAF0"),
                new Vector2(0, 0), new Vector2(780, 160), TextAnchor.UpperLeft);
            var link = UiKit.Label(support.transform, "honestarcade.app/contribute →", UiKit.Heading, UiKit.White,
                new Vector2(0, -128), new Vector2(780, 52), TextAnchor.MiddleLeft);
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
                    new Vector2(300 + (i % 3) * 268, -960 - (i / 3) * 92));

            return root;
        }

        /// <summary>The lane rundown, flowed so wrapped lines never collide.</summary>
        private static void BoardRules(Transform parent, Vector2 topLeft)
        {
            string[] laneKeys = { "ruleRoad", "ruleRiver", "ruleSwamp", "ruleTracks", "ruleBike", "ruleWalkway", "ruleMedians", "ruleBays" };
            var column = UiKit.Column(parent, topLeft, 900f, 26f);
            UiKit.Label(column, "WHAT'S ON THE BOARD", UiKit.Caption, UiKit.TextDim,
                Vector2.zero, new Vector2(900, 44), TextAnchor.MiddleLeft);
            foreach (var key in laneKeys)
                UiKit.Label(column, "· " + Copy.Get(key), UiKit.Body, UiKit.TextBlue,
                    Vector2.zero, new Vector2(900, 0), TextAnchor.UpperLeft);
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

        /// <summary>
        /// A copy card that grows to whatever its text needs. Lives inside a
        /// UiKit.Column, which sizes it from this layout's preferred height —
        /// a fixed height let "Swiping &amp; tapping" spill out of its box.
        /// </summary>
        private static void Card(Transform parent, string title, string body, bool accent, float width)
        {
            var card = UiKit.Panel(parent, $"card-{title}", accent
                ? new Color(0f, 0.839f, 0.706f, 0.12f)
                : new Color(1f, 1f, 1f, 0.06f));

            var layout = card.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(40, 40, 32, 34);
            layout.spacing = 16;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            UiKit.Label(card.transform, title, UiKit.Caption, accent ? UiKit.Mint : UiKit.TextDim,
                Vector2.zero, new Vector2(width - 80, 0), TextAnchor.MiddleLeft);
            UiKit.Label(card.transform, body, UiKit.Body, UiKit.TextBlue,
                Vector2.zero, new Vector2(width - 80, 0), TextAnchor.UpperLeft);
        }

        private static void Chip(Transform parent, string text, Color color, Vector2 pos)
        {
            var chip = UiKit.Panel(parent, $"chip-{text}", new Color(color.r, color.g, color.b, 0.16f), UiKit.PillRadius);
            chip.rectTransform.sizeDelta = new Vector2(252, 72);
            chip.rectTransform.anchoredPosition = pos;
            UiKit.Label(chip.transform, text, UiKit.Micro, color, Vector2.zero, new Vector2(252, 72));
        }
    }
}
