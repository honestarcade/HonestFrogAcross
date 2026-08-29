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

            UiKit.Button(root.transform, "‹", new Vector2(-890, 465), new Vector2(76, 76), shell.Back, fontSize: 36);
            UiKit.Label(root.transform, "Settings", 46, UiKit.White, new Vector2(-670, 465), new Vector2(380, 60), TextAnchor.MiddleLeft);

            // -------- sound --------
            SoundRow(root.transform, "All sound", "Master switch for every sound the game makes.",
                new Vector2(-460, 320), () => SoundSettings.Master, v => SoundSettings.Master = v);
            SoundRow(root.transform, "Music", "Menu and gameplay music.",
                new Vector2(-460, 185), () => SoundSettings.Music, v => SoundSettings.Music = v);
            SoundRow(root.transform, "Effects", "Hops, crashes, splashes, medals.",
                new Vector2(-460, 50), () => SoundSettings.Effects, v => SoundSettings.Effects = v);
            SoundRow(root.transform, "Interface", "Taps and navigation.",
                new Vector2(-460, -85), () => SoundSettings.Ui, v => SoundSettings.Ui = v);

            // -------- controls --------
            UiKit.Label(root.transform, "CONTROLS", 22, UiKit.TextDim, new Vector2(-750, -210), new Vector2(240, 30), TextAnchor.MiddleLeft);
            bool regions = ControlSchemeSetting.Current == ControlScheme.TapRegions;
            UiKit.Button(root.transform, regions ? "Swipe" : "Swipe ✓", new Vector2(-650, -300), new Vector2(280, 88), () =>
            {
                ControlSchemeSetting.Current = ControlScheme.Swipe;
                Refresh(shell);
            }, primary: !regions, fontSize: 26);
            UiKit.Button(root.transform, regions ? "Tap regions ✓" : "Tap regions", new Vector2(-340, -300), new Vector2(300, 88), () =>
            {
                ControlSchemeSetting.Current = ControlScheme.TapRegions;
                Refresh(shell);
            }, primary: regions, fontSize: 26);
            if (regions)
            {
                var link = UiKit.Button(root.transform, "Show regions", new Vector2(-20, -300), new Vector2(280, 88),
                    () => ShowRegionsPreview(shell.transform), fontSize: 26);
                link.image.color = new Color(0f, 0.839f, 0.706f, 0.15f);
            }

            // -------- data --------
            UiKit.Label(root.transform, "DATA", 22, UiKit.TextDim, new Vector2(400, 400), new Vector2(240, 30), TextAnchor.MiddleLeft);
            var dataCard = UiKit.Panel(root.transform, "data-card", new Color(1f, 1f, 1f, 0.05f));
            dataCard.rectTransform.sizeDelta = new Vector2(760, 310);
            dataCard.rectTransform.anchoredPosition = new Vector2(520, 210);
            UiKit.Label(dataCard.transform, "Progress", 32, UiKit.White, new Vector2(0, 105), new Vector2(700, 42), TextAnchor.MiddleLeft);
            UiKit.Label(dataCard.transform, "Levels, medals, best times and settings.", 24, UiKit.TextBlue,
                new Vector2(0, 55), new Vector2(700, 36), TextAnchor.MiddleLeft);
            var resetBtn = UiKit.Button(dataCard.transform, "Reset all data", new Vector2(0, -70), new Vector2(700, 88),
                () => ConfirmReset(shell), fontSize: 30);
            resetBtn.image.color = new Color(0.878f, 0.353f, 0.306f, 0.2f);

            var storedCard = UiKit.Panel(root.transform, "stored-card", new Color(1f, 1f, 1f, 0.035f));
            storedCard.rectTransform.sizeDelta = new Vector2(760, 260);
            storedCard.rectTransform.anchoredPosition = new Vector2(520, -120);
            UiKit.Label(storedCard.transform, "STORED ON DEVICE ONLY", 22, UiKit.TextDim, new Vector2(0, 88), new Vector2(700, 30), TextAnchor.MiddleLeft);
            UiKit.Label(storedCard.transform, Copy.Get("storedOnDevice"), 24, UiKit.TextBlue,
                new Vector2(0, -24), new Vector2(700, 150), TextAnchor.UpperLeft);

            UiKit.Label(root.transform, "v1.0", 20, UiKit.TextDim, new Vector2(520, -320), new Vector2(760, 28), TextAnchor.MiddleLeft);
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
            var row = UiKit.Panel(parent, $"row-{title}", new Color(1f, 1f, 1f, 0.05f));
            row.rectTransform.sizeDelta = new Vector2(720, 120);
            row.rectTransform.anchoredPosition = pos;
            UiKit.Label(row.transform, title, 30, UiKit.White, new Vector2(-90, 22), new Vector2(500, 38), TextAnchor.MiddleLeft);
            UiKit.Label(row.transform, sub, 22, UiKit.TextBlue, new Vector2(-90, -24), new Vector2(500, 32), TextAnchor.MiddleLeft);
            UiKit.Toggle(row.transform, new Vector2(300, 0), get(), set);
        }

        /// <summary>Full-screen preview of the four tap zones; one tap dismisses (#74/#59 amendment).</summary>
        public static void ShowRegionsPreview(Transform parent)
        {
            var canvas = UiKit.Canvas(parent, "regions-preview", 200);
            var scrim = UiKit.Stretch(UiKit.Panel(canvas.transform, "scrim", new Color(0f, 0f, 0f, 0.55f)));

            void Zone(Vector2 aMin, Vector2 aMax, string arrow, string label)
            {
                var z = UiKit.Panel(canvas.transform, $"zone-{label}", new Color(0f, 0.839f, 0.706f, 0.12f));
                var rt = z.rectTransform;
                rt.anchorMin = aMin; rt.anchorMax = aMax;
                rt.offsetMin = new Vector2(6, 6); rt.offsetMax = new Vector2(-6, -6);
                UiKit.Label(z.transform, arrow, 110, UiKit.Mint, new Vector2(0, 40));
                UiKit.Label(z.transform, label, 34, UiKit.White, new Vector2(0, -80));
            }
            Zone(new Vector2(0f, 0f), new Vector2(1f / 3f, 1f), "◀", "Left");
            Zone(new Vector2(2f / 3f, 0f), new Vector2(1f, 1f), "▶", "Right");
            Zone(new Vector2(1f / 3f, 0.5f), new Vector2(2f / 3f, 1f), "▲", "Forward");
            Zone(new Vector2(1f / 3f, 0f), new Vector2(2f / 3f, 0.5f), "▼", "Back");

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
            UiKit.Stretch(UiKit.Panel(canvas.transform, "scrim", new Color(0.012f, 0.055f, 0.125f, 0.78f)));
            var panel = UiKit.Panel(canvas.transform, "panel", UiKit.PanelNavy);
            panel.rectTransform.sizeDelta = new Vector2(840, 420);
            var t = UiKit.Label(panel.transform, title, 40, UiKit.White, new Vector2(0, 140), new Vector2(760, 54));
            t.fontStyle = FontStyle.Bold;
            UiKit.Label(panel.transform, body, 27, UiKit.TextBlue, new Vector2(0, 25), new Vector2(760, 150), TextAnchor.UpperLeft);
            UiKit.Button(panel.transform, "Cancel", new Vector2(-190, -145), new Vector2(330, 84), () =>
            {
                Object.Destroy(canvas.gameObject);
                onCancel?.Invoke();
            });
            var confirm = UiKit.Button(panel.transform, confirmLabel, new Vector2(190, -145), new Vector2(330, 84), () =>
            {
                Object.Destroy(canvas.gameObject);
                onConfirm();
            });
            confirm.image.color = UiKit.Danger;
            return canvas.gameObject;
        }
    }
}
