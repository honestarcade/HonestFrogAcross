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
            UiKit.SetContentHeight(content, 1500f);

            // Both columns flow: sound subtitles wrap on narrow phones and the
            // fixed offsets underneath them collided (owner: CONTROLS ran into
            // the Interface row; Show-regions sat on the stored-data card).
            var left = UiKit.Column(content, new Vector2(-900, -60), 820f, 26f);
            SoundRow(left, "All sound", "Master switch for every sound the game makes.",
                () => SoundSettings.Master, v => SoundSettings.Master = v);
            SoundRow(left, "Music", "Menu and gameplay music.",
                () => SoundSettings.Music, v => SoundSettings.Music = v);
            SoundRow(left, "Effects", "Hops, crashes, splashes, medals.",
                () => SoundSettings.Effects, v => SoundSettings.Effects = v);
            SoundRow(left, "Interface", "Taps and navigation.",
                () => SoundSettings.Ui, v => SoundSettings.Ui = v);

            UiKit.Label(left, "CONTROLS", UiKit.Caption, UiKit.TextDim,
                Vector2.zero, new Vector2(820, 48), TextAnchor.MiddleLeft);
            bool regions = ControlSchemeSetting.Current == ControlScheme.TapRegions;
            var schemeRow = UiKit.Row(left, 124f);
            UiKit.Button(schemeRow, regions ? "Swipe" : "Swipe ✓", Vector2.zero, new Vector2(330, 124), () =>
            {
                ControlSchemeSetting.Current = ControlScheme.Swipe;
                Refresh(shell);
            }, primary: !regions, fontSize: UiKit.Heading);
            UiKit.Button(schemeRow, regions ? "Tap regions ✓" : "Tap regions", Vector2.zero, new Vector2(380, 124), () =>
            {
                ControlSchemeSetting.Current = ControlScheme.TapRegions;
                Refresh(shell);
            }, primary: regions, fontSize: UiKit.Heading);
            if (regions)
            {
                var link = UiKit.Button(left, "Show regions", Vector2.zero, new Vector2(420, 124),
                    () => ShowRegionsPreview(shell.transform), fontSize: UiKit.Heading);
                link.image.color = new Color(0f, 0.839f, 0.706f, 0.22f);
                link.gameObject.AddComponent<LayoutElement>().preferredHeight = 124f;
            }

            var right = UiKit.Column(content, new Vector2(120, -60), 840f, 30f);
            UiKit.Label(right, "DATA", UiKit.Caption, UiKit.TextDim,
                Vector2.zero, new Vector2(840, 48), TextAnchor.MiddleLeft);

            var dataCard = UiKit.Panel(right, "data-card", new Color(1f, 1f, 1f, 0.06f));
            dataCard.gameObject.AddComponent<LayoutElement>().preferredHeight = 400f;
            UiKit.Label(dataCard.transform, "Progress", UiKit.Heading, UiKit.White,
                new Vector2(0, 132), new Vector2(760, 58), TextAnchor.MiddleLeft);
            UiKit.Label(dataCard.transform, "Levels, medals, best times and settings.", UiKit.Body, UiKit.TextBlue,
                new Vector2(0, 62), new Vector2(760, 50), TextAnchor.MiddleLeft);
            var resetBtn = UiKit.Button(dataCard.transform, "Reset all data", new Vector2(0, -105),
                new Vector2(760, 124), () => ConfirmReset(shell), fontSize: UiKit.Heading);
            resetBtn.image.color = UiKit.Danger; // 20% alpha read as disabled
            foreach (var t in resetBtn.GetComponentsInChildren<Text>()) t.color = UiKit.White;

            var storedCard = UiKit.Panel(right, "stored-card", new Color(1f, 1f, 1f, 0.045f));
            storedCard.gameObject.AddComponent<LayoutElement>().preferredHeight = 400f;
            UiKit.Label(storedCard.transform, "STORED ON DEVICE ONLY", UiKit.Caption, UiKit.Mint,
                new Vector2(0, 140), new Vector2(760, 44), TextAnchor.MiddleLeft);
            UiKit.Label(storedCard.transform, Copy.Get("storedOnDevice"), UiKit.Body, UiKit.TextBlue,
                new Vector2(0, -20), new Vector2(760, 240), TextAnchor.UpperLeft);

            UiKit.Label(right, $"Frog Across v{Application.version}", UiKit.Caption, UiKit.TextDim,
                Vector2.zero, new Vector2(840, 48), TextAnchor.MiddleLeft);

            UiKit.Header(root.transform, "Settings", shell.Back);
            return root;
        }

        private static void Refresh(AppShell shell)
        {
            shell.Replace("settings", Build); // Push here stacked a second copy
        }

        private static void SoundRow(Transform parent, string title, string sub,
            System.Func<bool> get, System.Action<bool> set)
        {
            var row = UiKit.Panel(parent, $"row-{title}", new Color(1f, 1f, 1f, 0.06f));
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 150f;
            UiKit.Label(row.transform, title, UiKit.Heading, UiKit.White, new Vector2(-70, 34),
                new Vector2(560, 56), TextAnchor.MiddleLeft);
            UiKit.Label(row.transform, sub, UiKit.Caption, UiKit.TextBlue, new Vector2(-70, -32),
                new Vector2(560, 48), TextAnchor.MiddleLeft);
            UiKit.Toggle(row.transform, new Vector2(330, 0), get(), set);
        }

        /// <summary>Full-screen preview of the four tap zones; one tap dismisses (#74/#59 amendment).</summary>
        public static void ShowRegionsPreview(Transform parent)
        {
            var canvas = UiKit.Canvas(parent, "regions-preview", 200);
            var scrim = UiKit.Stretch(UiKit.Fill(canvas.transform, "scrim", UiKit.NavyDeep));

            void Zone(Vector2 aMin, Vector2 aMax, string arrow, string label, Color zoneColor)
            {
                var z = UiKit.Fill(canvas.transform, $"zone-{label}", zoneColor); // opaque (owner ruling)
                var rt = z.rectTransform;
                rt.anchorMin = aMin; rt.anchorMax = aMax;
                rt.offsetMin = new Vector2(6, 6); rt.offsetMax = new Vector2(-6, -6);
                UiKit.Label(z.transform, arrow, 160, UiKit.NavyDeep, new Vector2(0, 60));
                UiKit.Label(z.transform, label, UiKit.Title, UiKit.NavyDeep, new Vector2(0, -120));
            }
            float side = TapRegionMapper.SideFraction;
            Zone(new Vector2(0f, 0f), new Vector2(side, 1f), "◀", "Left", UiKit.Mint);
            Zone(new Vector2(1f - side, 0f), new Vector2(1f, 1f), "▶", "Right", UiKit.Mint);
            Zone(new Vector2(side, 0.5f), new Vector2(1f - side, 1f), "▲", "Forward", UiKit.Hex("6FB4FF"));
            Zone(new Vector2(side, 0f), new Vector2(1f - side, 0.5f), "▼", "Back", UiKit.Hex("B48CFF"));

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
            panel.rectTransform.sizeDelta = new Vector2(980, 520);
            var t = UiKit.Label(panel.transform, title, UiKit.Title, UiKit.White, new Vector2(0, 150), new Vector2(820, 78));
            t.fontStyle = FontStyle.Bold;
            UiKit.Label(panel.transform, body, UiKit.Body, UiKit.TextBlue, new Vector2(0, 20), new Vector2(800, 180), TextAnchor.UpperLeft);
            UiKit.Button(panel.transform, "Cancel", new Vector2(-210, -170), new Vector2(370, 116), () =>
            {
                Object.Destroy(canvas.gameObject);
                onCancel?.Invoke();
            });
            var confirm = UiKit.Button(panel.transform, confirmLabel, new Vector2(210, -170), new Vector2(370, 116), () =>
            {
                Object.Destroy(canvas.gameObject);
                onConfirm();
            });
            confirm.image.color = UiKit.Danger;
            return canvas.gameObject;
        }
    }
}
