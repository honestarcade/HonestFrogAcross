using System;
using System.Collections;
using System.Collections.Generic;
using FrogAcross.Levels;
using FrogAcross.Pieces;
using FrogAcross.Services;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FrogAcross.UI
{
    /// <summary>
    /// #55: loading → menu → screens, one code-built canvas. Android back
    /// navigates up through the stack; screens register via Show/Push.
    /// Launching a level loads the Game scene with LaunchParams.
    /// </summary>
    public sealed class AppShell : MonoBehaviour
    {
        public static string PendingLevelId; // read by GameBootstrap on scene load

        /// <summary>Set before loading the Shell scene to land straight on the
        /// menu — returning from a level must not replay the boot beat.</summary>
        public static bool SkipBoot;

        private RectTransform _safe;

        private Canvas _canvas;
        private readonly Dictionary<string, GameObject> _screens = new();
        private readonly Stack<string> _stack = new();

        private void Start()
        {
            _canvas = UiKit.Canvas(transform, "shell-canvas");
            UiKit.Stretch(UiKit.Fill(_canvas.transform, "bg", UiKit.Navy));

            // every screen lives inside the device safe area
            var safeGo = new GameObject("safe-area");
            safeGo.transform.SetParent(_canvas.transform, false);
            _safe = safeGo.AddComponent<RectTransform>();
            _safe.anchorMin = Vector2.zero;
            _safe.anchorMax = Vector2.one;
            _safe.offsetMin = _safe.offsetMax = Vector2.zero;
            safeGo.AddComponent<SafeArea>();

            Register("loading", LoadingScreen.Build(_safe));
            Register("menu", MenuScreen.Build(_safe, this));
            Register("levels", LevelsScreen.Build(_safe, this));
            Register("character", CharacterScreen.Build(_safe, this));
            Register("about", StaticScreens.BuildAbout(_safe, this));
            Register("gameplay", StaticScreens.BuildGameplay(_safe, this));
            Register("studio", StaticScreens.BuildStudio(_safe, this));
            Register("settings", SettingsScreen.Build(_safe, this));

            FrogAcross.Audio.AudioDirector.Instance.PlayMusic(FrogAcross.Audio.MusicSlot.Menu);
            if (SkipBoot)
            {
                SkipBoot = false;
                Show("menu");
            }
            else
            {
                StartCoroutine(Boot());
            }
        }

        private static readonly WaitForSeconds BootBeat = new WaitForSeconds(0.9f);

        private IEnumerator Boot()
        {
            Show("loading");
            yield return BootBeat; // brief, honest load beat
            Show("menu");
        }

        private void Register(string name, GameObject root)
        {
            root.SetActive(false);
            _screens[name] = root;
        }

        public void Show(string name)
        {
            foreach (var kv in _screens) kv.Value.SetActive(kv.Key == name);
            _stack.Clear();
            _stack.Push(name);
        }

        public void Push(string name)
        {
            foreach (var kv in _screens) kv.Value.SetActive(kv.Key == name);
            _stack.Push(name);
            FrogAcross.Audio.AudioDirector.Instance.Play(FrogAcross.Audio.GameSound.UiNavigate);
        }

        public void Back()
        {
            if (_stack.Count > 1)
            {
                _stack.Pop();
                var target = _stack.Peek();
                foreach (var kv in _screens) kv.Value.SetActive(kv.Key == target);
                FrogAcross.Audio.AudioDirector.Instance.Play(FrogAcross.Audio.GameSound.UiNavigate);
            }
            // at the menu root: let the OS background us (no explicit action)
        }

        /// <summary>
        /// Rebuild the screen you are already on and stay there. Screens used
        /// to RebuildScreen + Push, which stacked a second copy of themselves —
        /// so Back popped the duplicate and appeared to do nothing the first
        /// time (owner: "had to hit it twice", 2026-08-29).
        /// </summary>
        public void Replace(string name, Func<Transform, AppShell, GameObject> builder)
        {
            // keep the reader where they were: flipping a setting halfway down
            // the page used to snap it back to the top
            float? scroll = null;
            if (_screens.TryGetValue(name, out var existing) && existing != null)
            {
                var previous = existing.GetComponentInChildren<ScrollRect>(true);
                if (previous != null) scroll = previous.verticalNormalizedPosition;
            }

            RebuildScreen(name, builder);
            foreach (var kv in _screens) kv.Value.SetActive(kv.Key == name);
            if (scroll.HasValue) StartCoroutine(RestoreScroll(name, scroll.Value));
        }

        private IEnumerator RestoreScroll(string name, float normalized)
        {
            yield return null; // let the rebuilt layout settle first
            if (!_screens.TryGetValue(name, out var screen) || screen == null) yield break;
            var scroll = screen.GetComponentInChildren<ScrollRect>(true);
            if (scroll != null) scroll.verticalNormalizedPosition = normalized;
        }

        public void RebuildScreen(string name, Func<Transform, AppShell, GameObject> builder)
        {
            if (_screens.TryGetValue(name, out var old)) Destroy(old);
            var built = builder(_safe, this);
            built.SetActive(false);
            _screens[name] = built;
        }

        /// <summary>
        /// #89: screens are built once at boot, so anything that mutates
        /// progression outside the Game scene (Reset all data) must rebuild
        /// the data-driven screens or they show stale values until restart.
        /// </summary>
        public void RefreshDataScreens()
        {
            RebuildScreen("menu", MenuScreen.Build);
            RebuildScreen("levels", LevelsScreen.Build);
            RebuildScreen("character", CharacterScreen.Build);
        }

        public void LaunchLevel(string levelId)
        {
            PendingLevelId = levelId;
            SceneManager.LoadScene("Game");
        }

        public void LaunchCurrent() => LaunchLevel(
            LevelCatalog.IdFor(Mathf.Min(Progression.HighestUnlocked, LevelCatalog.Count)));

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame) Back(); // Android back maps to Escape
        }
    }

    internal static class LoadingScreen
    {
        public static GameObject Build(Transform parent)
        {
            var root = new GameObject("loading");
            root.transform.SetParent(parent, false);
            var rrt = root.AddComponent<RectTransform>();
            rrt.anchorMin = Vector2.zero; rrt.anchorMax = Vector2.one;
            rrt.offsetMin = rrt.offsetMax = Vector2.zero;
            UiKit.Stretch(UiKit.Fill(root.transform, "bg", UiKit.Navy));
            UiKit.Logotype(root.transform, new Vector2(-560f, 60f), 116);
            var barBg = UiKit.Panel(root.transform, "bar-bg", new Color(1f, 1f, 1f, 0.12f), 8);
            barBg.rectTransform.sizeDelta = new Vector2(520, 12);
            barBg.rectTransform.anchoredPosition = new Vector2(0, -200);
            var bar = UiKit.Panel(barBg.transform, "bar", UiKit.Mint, 8);
            bar.rectTransform.anchorMin = new Vector2(0, 0);
            bar.rectTransform.anchorMax = new Vector2(0.85f, 1); // honest: nearly-instant load
            bar.rectTransform.offsetMin = bar.rectTransform.offsetMax = Vector2.zero;
            return root;
        }
    }

    internal static class MenuScreen
    {
        /// <summary>A full-height column pinned to one safe-area edge.</summary>
        private static Transform Side(Transform parent, bool left)
        {
            var go = new GameObject(left ? "brand-column" : "action-column");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(left ? 0f : 1f, 0.5f);
            rt.pivot = new Vector2(left ? 0f : 1f, 0.5f);
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = new Vector2(left ? UiKit.EdgePad : -UiKit.EdgePad, 0f);
            return go.transform;
        }

        private static void RightAlign(RectTransform rt, float rightX, float centreY, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = new Vector2(rightX, centreY);
        }

        /// <summary>Places a rect by its LEFT edge so a column can share one.</summary>
        private static void LeftAlign(RectTransform rt, float leftX, float centreY, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = new Vector2(leftX, centreY);
        }

        public static GameObject Build(Transform parent, AppShell shell)
        {
            var root = new GameObject("menu");
            root.transform.SetParent(parent, false);
            var rt = root.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            // Two columns pinned to the safe-area edges — fixed offsets from
            // the centre would fall off a 16:9 phone, where the canvas is only
            // 1920 wide.
            var brand = Side(root.transform, left: true);
            var actions = Side(root.transform, left: false);

            UiKit.Logotype(brand, new Vector2(0f, 200f), 112);

            int current = Mathf.Min(Progression.HighestUnlocked, LevelCatalog.Count);
            int medals = 0;
            foreach (var r in Progression.Data.records) if (r.medal > 0) medals++;

            var levelChip = UiKit.Panel(brand, "chip-level", new Color(1f, 1f, 1f, 0.07f));
            LeftAlign(levelChip.rectTransform, 0f, 10f, new Vector2(440, 100));
            UiKit.Label(levelChip.transform, $"Level {current} / {LevelCatalog.Count}", UiKit.Caption,
                UiKit.TextBlue, Vector2.zero, new Vector2(400, 48));
            var medalChip = UiKit.Panel(brand, "chip-medals", new Color(1f, 1f, 1f, 0.07f));
            LeftAlign(medalChip.rectTransform, 470f, 10f, new Vector2(340, 100));
            UiKit.Label(medalChip.transform, $"{medals} medals", UiKit.Caption,
                UiKit.TextBlue, Vector2.zero, new Vector2(300, 48));

            // Buttons: three rows of 160 from y=100 → the block bottom is -360,
            // and the promise line's bottom sits level with it.
            var continueBtn = UiKit.Button(actions, $"Continue — Level {current}", Vector2.zero,
                new Vector2(880, 160), shell.LaunchCurrent, primary: true, fontSize: UiKit.Title - 6);
            RightAlign(continueBtn.image.rectTransform, 0f, 300f, new Vector2(880, 160));

            var grid = new (string label, string screen)[]
            {
                ("Levels", "levels"), ("Character", "character"),
                ("About the game", "about"), ("Gameplay", "gameplay"),
                ("Settings", "settings"), ("Honest Arcade", "studio"),
            };
            for (int i = 0; i < grid.Length; i++)
            {
                var (label, screen) = grid[i];
                var btn = UiKit.Button(actions, label, Vector2.zero, new Vector2(432, 160),
                    () => shell.Push(screen), fontSize: UiKit.Heading);
                float right = i % 2 == 0 ? -448f : 0f;
                RightAlign(btn.image.rectTransform, right, 100f - (i / 2) * 190f, new Vector2(432, 160));
            }

            var promise = UiKit.Label(brand,
                $"v{Application.version}  ·  NO ADS  ·  NO TRACKING  ·  OPEN SOURCE",
                UiKit.Micro, UiKit.TextDim, Vector2.zero, new Vector2(1000, 44), TextAnchor.MiddleLeft);
            LeftAlign(promise.rectTransform, 0f, -360f + 22f, new Vector2(1000, 44));
            return root;
        }
    }
}
