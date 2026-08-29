using FrogAcross.Input;
using FrogAcross.Services;
using UnityEngine;
using UnityEngine.UI;

namespace FrogAcross.UI
{
    /// <summary>
    /// #59: sound toggles (persisted; #65 binds buses), Controls (scheme
    /// selector + Show-regions preview), Reset-all (single wipe path), and the
    /// stored-on-device statement.
    /// </summary>
    public static class SettingsScreen
    {
        public static GameObject Build(Transform parent, AppShell shell)
        {
            var root = new GameObject("settings");
            root.transform.SetParent(parent, false);
            var rt = root.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            var content = UiKit.ScrollArea(root.transform,
                topLeftInset: new Vector2(UiKit.EdgePad, 210f),
                bottomRightInset: new Vector2(UiKit.EdgePad, 40f));
            UiKit.SetContentHeight(content, 1160f);

            // -------- sound --------
            SoundRow(content, "All sound", "Master switch for every sound the game makes.",
                new Vector2(-450, -90), () => SoundSettings.Master, v => SoundSettings.Master = v);
            SoundRow(content, "Music", "Menu and gameplay music.",
                new Vector2(-450, -230), () => SoundSettings.Music, v => SoundSettings.Music = v);
            SoundRow(content, "Effects", "Hops, crashes, splashes, medals.",
                new Vector2(-450, -370), () => SoundSettings.Effects, v => SoundSettings.Effects = v);
            SoundRow(content, "Interface", "Taps and navigation.",
                new Vector2(-450, -510), () => SoundSettings.Ui, v => SoundSettings.Ui = v);

            // -------- controls --------
            UiKit.Label(content, "CONTROLS", 24, UiKit.TextDim, new Vector2(-700, -620), new Vector2(300, 32), TextAnchor.MiddleLeft);
            bool regions = ControlSchemeSetting.Current == ControlScheme.TapRegions;
            UiKit.Button(content, regions ? "Swipe" : "Swipe ✓", new Vector2(-620, -720), new Vector2(300, 104), () =>
            {
                ControlSchemeSetting.Current = ControlScheme.Swipe;
                Refresh(shell);
            }, primary: !regions, fontSize: 26);
            UiKit.Button(content, regions ? "Tap regions ✓" : "Tap regions", new Vector2(-290, -720), new Vector2(330, 104), () =>
            {
                ControlSchemeSetting.Current = ControlScheme.TapRegions;
                Refresh(shell);
            }, primary: regions, fontSize: 26);
            if (regions)
            {
                var link = UiKit.Button(content, "Show regions", new Vector2(60, -720), new Vector2(300, 104),
                    () => ShowRegionsPreview(shell.transform), fontSize: 26);
                link.image.color = new Color(0f, 0.839f, 0.706f, 0.22f);
            }

            // -------- data --------
            UiKit.Label(content, "DATA", 24, UiKit.TextDim, new Vector2(400, -60), new Vector2(300, 32), TextAnchor.MiddleLeft);
            var dataCard = UiKit.Panel(content, "data-card", new Color(1f, 1f, 1f, 0.05f));
            dataCard.rectTransform.sizeDelta = new Vector2(780, 330);
            dataCard.rectTransform.anchoredPosition = new Vector2(520, -240);
            UiKit.Label(dataCard.transform, "Progress", 32, UiKit.White, new Vector2(0, 105), new Vector2(700, 42), TextAnchor.MiddleLeft);
            UiKit.Label(dataCard.transform, "Levels, medals, best times and settings.", 24, UiKit.TextBlue,
                new Vector2(0, 55), new Vector2(700, 36), TextAnchor.MiddleLeft);
            var resetBtn = UiKit.Button(dataCard.transform, "Reset all data", new Vector2(0, -70), new Vector2(700, 88),
                () => ConfirmReset(shell), fontSize: 30);
            resetBtn.image.color = UiKit.Danger;
            foreach (var t in resetBtn.GetComponentsInChildren<Text>()) t.color = UiKit.White;

            var storedCard = UiKit.Panel(content, "stored-card", new Color(1f, 1f, 1f, 0.035f));
            storedCard.rectTransform.sizeDelta = new Vector2(780, 300);
            storedCard.rectTransform.anchoredPosition = new Vector2(520, -610);
            UiKit.Label(storedCard.transform, "STORED ON DEVICE ONLY", 22, UiKit.TextDim, new Vector2(0, 88), new Vector2(700, 30), TextAnchor.MiddleLeft);
            UiKit.Label(storedCard.transform, Copy.Get("storedOnDevice"), 24, UiKit.TextBlue,
                new Vector2(0, -24), new Vector2(700, 150), TextAnchor.UpperLeft);

            UiKit.Label(content, $"Frog Across v{Application.version}", 22, UiKit.TextDim, new Vector2(520, -800), new Vector2(780, 30), TextAnchor.MiddleLeft);
            UiKit.Header(root.transform, "Settings", shell.Back);
            return root;
        }

        private static void Refresh(AppShell shell)
        {
            shell.RebuildScreen("settings", Build);
            shell.Push("settings");
        }

        private static void SoundRow(Transform parent, string title, string sub, Vector2 pos,
            System.Func<bool> get, System.Action<bool> set)
        {
            var row = UiKit.Panel(parent, $"row-{title}", new Color(1f, 1f, 1f, 0.06f));
            row.rectTransform.sizeDelta = new Vector2(760, 124);
            row.rectTransform.anchoredPosition = pos;
            UiKit.Label(row.transform, title, 30, UiKit.White, new Vector2(-90, 22), new Vector2(500, 38), TextAnchor.MiddleLeft);
            UiKit.Label(row.transform, sub, 22, UiKit.TextBlue, new Vector2(-90, -24), new Vector2(500, 32), TextAnchor.MiddleLeft);
            UiKit.Toggle(row.transform, new Vector2(300, 0), get(), set);
        }

        /// <summary>Full-screen preview of the four tap zones; one tap dismisses (#74/#59 amendment).</summary>
        public static void ShowRegionsPreview(Transform parent)
        {
            var canvas = UiKit.Canvas(parent, "regions-preview", 200);
            var scrim = UiKit.Stretch(UiKit.Fill(canvas.transform, "scrim", new Color(0f, 0f, 0f, 0.55f)));

            void Zone(Vector2 aMin, Vector2 aMax, string arrow, string label)
            {
                var z = UiKit.Fill(canvas.transform, $"zone-{label}", new Color(0f, 0.839f, 0.706f, 0.12f));
                var rt = z.rectTransform;
                rt.anchorMin = aMin; rt.anchorMax = aMax;
                rt.offsetMin = new Vector2(6, 6); rt.offsetMax = new Vector2(-6, -6);
                UiKit.Label(z.transform, arrow, 110, UiKit.Mint, new Vector2(0, 40));
                UiKit.Label(z.transform, label, 34, UiKit.White, new Vector2(0, -80));
            }
            float side = TapRegionMapper.SideFraction;
            Zone(new Vector2(0f, 0f), new Vector2(side, 1f), "◀", "Left");
            Zone(new Vector2(1f - side, 0f), new Vector2(1f, 1f), "▶", "Right");
            Zone(new Vector2(side, 0.5f), new Vector2(1f - side, 1f), "▲", "Forward");
            Zone(new Vector2(side, 0f), new Vector2(1f - side, 0.5f), "▼", "Back");

            var dismiss = scrim.gameObject.AddComponent<Button>();
            dismiss.onClick.AddListener(() => Object.Destroy(canvas.gameObject));
            foreach (var img in canvas.GetComponentsInChildren<Image>())
                if (img != scrim) img.raycastTarget = false; // one tap anywhere dismisses
        }

        private static void ConfirmReset(AppShell shell)
        {
            ConfirmDialog.Show(shell.transform, "Reset all data?", Copy.Get("resetConfirm"),
                "Reset everything", () =>
                {
                    DataWipe.WipeAll();
                    shell.RefreshDataScreens(); // #89: menu/levels/character were stale
                    Refresh(shell);
                });
        }
    }

    /// <summary>Shared confirm dialog (#55/#60 amendment): in-game callers freeze the sim while open.</summary>
    public static class ConfirmDialog
    {
        public static GameObject Show(Transform parent, string title, string body,
            string confirmLabel, System.Action onConfirm, System.Action onCancel = null)
        {
            var canvas = UiKit.Canvas(parent, "confirm-dialog", 300);
            UiKit.Stretch(UiKit.Fill(canvas.transform, "scrim", new Color(0.012f, 0.055f, 0.125f, 0.78f)));
            var panel = UiKit.Panel(canvas.transform, "panel", UiKit.PanelNavy);
            panel.rectTransform.sizeDelta = new Vector2(900, 460);
            var t = UiKit.Label(panel.transform, title, 44, UiKit.White, new Vector2(0, 150), new Vector2(800, 58));
            t.fontStyle = FontStyle.Bold;
            UiKit.Label(panel.transform, body, 28, UiKit.TextBlue, new Vector2(0, 30), new Vector2(800, 160), TextAnchor.UpperLeft);
            UiKit.Button(panel.transform, "Cancel", new Vector2(-200, -160), new Vector2(350, 96), () =>
            {
                Object.Destroy(canvas.gameObject);
                onCancel?.Invoke();
            });
            var confirm = UiKit.Button(panel.transform, confirmLabel, new Vector2(200, -160), new Vector2(350, 96), () =>
            {
                Object.Destroy(canvas.gameObject);
                onConfirm();
            });
            confirm.image.color = UiKit.Danger;
            return canvas.gameObject;
        }
    }
}
