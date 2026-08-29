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
            UiKit.Logotype(root.transform, new Vector2(0, 40), 96);
            UiKit.Label(root.transform, "BY HONEST ARCADE", 26, UiKit.TextDim, new Vector2(0, -110));
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
        public static GameObject Build(Transform parent, AppShell shell)
        {
            var root = new GameObject("menu");
            root.transform.SetParent(parent, false);
            var rt = root.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            // brand column left, actions right — both filling the safe area
            UiKit.Logotype(root.transform, new Vector2(-470, 120), 110);
            UiKit.Label(root.transform, "BY HONEST ARCADE", 28, UiKit.TextDim, new Vector2(-470, -40));

            int current = Mathf.Min(Progression.HighestUnlocked, LevelCatalog.Count);
            int medals = 0;
            foreach (var r in Progression.Data.records) if (r.medal > 0) medals++;
            var stat = UiKit.Panel(root.transform, "stat", new Color(1f, 1f, 1f, 0.06f));
            stat.rectTransform.sizeDelta = new Vector2(620, 92);
            stat.rectTransform.anchoredPosition = new Vector2(-470, -170);
            UiKit.Label(stat.transform, $"Level {current} / {LevelCatalog.Count}    ·    {medals} medals",
                32, UiKit.TextBlue, Vector2.zero, new Vector2(580, 44));

            UiKit.Button(root.transform, $"Continue — Level {current}", new Vector2(450, 250),
                new Vector2(760, 132), shell.LaunchCurrent, primary: true, fontSize: 42);
            var grid = new (string label, string screen)[]
            {
                ("Levels", "levels"), ("Character", "character"),
                ("About the game", "about"), ("Gameplay", "gameplay"),
                ("Settings", "settings"), ("Honest Arcade", "studio"),
            };
            for (int i = 0; i < grid.Length; i++)
            {
                var (label, screen) = grid[i];
                float x = 450 + (i % 2 == 0 ? -196 : 196);
                float y = 80 - (i / 2) * 150;
                UiKit.Button(root.transform, label, new Vector2(x, y), new Vector2(372, 126),
                    () => shell.Push(screen), fontSize: 30);
            }
            UiKit.Label(root.transform, $"v{Application.version} · NO ADS · NO TRACKING", 22, UiKit.TextDim,
                new Vector2(-470, -350), new Vector2(700, 30));
            return root;
        }
    }
}
