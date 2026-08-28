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

        private Canvas _canvas;
        private readonly Dictionary<string, GameObject> _screens = new();
        private readonly Stack<string> _stack = new();

        private void Start()
        {
            _canvas = UiKit.Canvas(transform, "shell-canvas");
            UiKit.Stretch(UiKit.Panel(_canvas.transform, "bg", UiKit.Navy));

            Register("loading", LoadingScreen.Build(_canvas.transform));
            Register("menu", MenuScreen.Build(_canvas.transform, this));
            Register("levels", LevelsScreen.Build(_canvas.transform, this));
            Register("character", CharacterScreen.Build(_canvas.transform, this));
            Register("about", StaticScreens.BuildAbout(_canvas.transform, this));
            Register("gameplay", StaticScreens.BuildGameplay(_canvas.transform, this));
            Register("studio", StaticScreens.BuildStudio(_canvas.transform, this));
            Register("settings", SettingsScreen.Build(_canvas.transform, this));

            FrogAcross.Audio.AudioDirector.Instance.PlayMusic(FrogAcross.Audio.MusicSlot.Menu);
            StartCoroutine(Boot());
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
            var built = builder(_canvas.transform, this);
            built.SetActive(false);
            _screens[name] = built;
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
            UiKit.Stretch(UiKit.Panel(root.transform, "bg", UiKit.Navy));
            UiKit.Lockup(root.transform, new Vector2(0, 60), 64);
            UiKit.Label(root.transform, "BY HONEST ARCADE", 18, UiKit.TextDim, new Vector2(0, -20));
            var barBg = UiKit.Panel(root.transform, "bar-bg", new Color(1f, 1f, 1f, 0.12f));
            barBg.rectTransform.sizeDelta = new Vector2(340, 8);
            barBg.rectTransform.anchoredPosition = new Vector2(0, -90);
            var bar = UiKit.Panel(barBg.transform, "bar", UiKit.Mint);
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

            UiKit.Lockup(root.transform, new Vector2(-420, 140), 52);
            UiKit.Label(root.transform, "BY HONEST ARCADE", 15, UiKit.TextDim, new Vector2(-420, 78));

            int current = Mathf.Min(Progression.HighestUnlocked, LevelCatalog.Count);
            int medals = 0;
            foreach (var r in Progression.Data.records) if (r.medal > 0) medals++;
            UiKit.Label(root.transform, $"Level {current} / {LevelCatalog.Count}    ·    {medals} medals",
                20, UiKit.TextBlue, new Vector2(-420, 20));

            UiKit.Button(root.transform, $"Continue — Level {current}", new Vector2(330, 150),
                new Vector2(480, 76), shell.LaunchCurrent, primary: true, fontSize: 26);
            var grid = new (string label, string screen)[]
            {
                ("Levels", "levels"), ("Character", "character"),
                ("About the game", "about"), ("Gameplay", "gameplay"),
                ("Settings", "settings"), ("Honest Arcade", "studio"),
            };
            for (int i = 0; i < grid.Length; i++)
            {
                var (label, screen) = grid[i];
                float x = 330 + (i % 2 == 0 ? -122 : 122);
                float y = 60 - (i / 2) * 84;
                UiKit.Button(root.transform, label, new Vector2(x, y), new Vector2(230, 68),
                    () => shell.Push(screen));
            }
            return root;
        }
    }
}
