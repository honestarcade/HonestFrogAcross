using FrogAcross.Levels;
using FrogAcross.Services;
using UnityEngine;
using UnityEngine.UI;

namespace FrogAcross.UI
{
    /// <summary>
    /// The levels grid: every level on one scrolling surface under a header
    /// that stays put (back button, progress, medal legend). Pagination is
    /// gone — scrolling is the navigation (owner ruling, 2026-08-29).
    /// </summary>
    public static class LevelsScreen
    {
        private const int Columns = 10;
        private const float Gap = 20f;

        public static GameObject Build(Transform parent, AppShell shell)
        {
            var root = new GameObject("levels");
            root.transform.SetParent(parent, false);
            var rootRt = root.AddComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero; rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = rootRt.offsetMax = Vector2.zero;

            int total = LevelCatalog.Count;
            int done = 0;
            for (int n = 1; n <= total; n++)
            {
                var rec = Progression.RecordFor(LevelCatalog.IdFor(n));
                if (rec != null && rec.bestSeconds >= 0) done++;
            }

            // ---- scrolling grid (built first; the header sits above it) ----
            var content = UiKit.ScrollArea(root.transform,
                topLeftInset: new Vector2(UiKit.EdgePad, 268f),
                bottomRightInset: new Vector2(UiKit.EdgePad, 40f));
            var surface = UiKit.ScrollContent(content);
            var grid = surface.gameObject.AddComponent<GridLayoutGroup>();
            grid.spacing = new Vector2(Gap, Gap);
            grid.padding = new RectOffset(0, 0, 0, (int)Gap); // no dead band above row one
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = Columns;
            var fitter = surface.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var gridFitter = surface.gameObject.AddComponent<GridFitter>();
            gridFitter.columns = Columns;
            gridFitter.aspect = 1.12f; // room for the medal disc plus the time under it

            for (int n = 1; n <= total; n++) BuildCell(surface, shell, n, root.transform);

            // ---- fixed header band ----
            var band = UiKit.Fill(root.transform, "header-band", UiKit.Navy);
            var brt = band.rectTransform;
            brt.anchorMin = new Vector2(0f, 1f);
            brt.anchorMax = new Vector2(1f, 1f);
            brt.pivot = new Vector2(0.5f, 1f);
            brt.sizeDelta = new Vector2(0f, 268f);
            brt.anchoredPosition = Vector2.zero;

            var barBg = UiKit.Panel(band.transform, "progress-bg", new Color(1f, 1f, 1f, 0.12f), 8);
            barBg.rectTransform.anchorMin = barBg.rectTransform.anchorMax = new Vector2(0f, 1f);
            barBg.rectTransform.pivot = new Vector2(0f, 1f);
            barBg.rectTransform.sizeDelta = new Vector2(900, 14);
            barBg.rectTransform.anchoredPosition = new Vector2(UiKit.EdgePad, -212f);
            var bar = UiKit.Panel(barBg.transform, "progress", UiKit.Mint, 8);
            bar.rectTransform.anchorMin = Vector2.zero;
            bar.rectTransform.anchorMax = new Vector2(total > 0 ? done / (float)total : 0f, 1f);
            bar.rectTransform.offsetMin = bar.rectTransform.offsetMax = Vector2.zero;
            var pct = UiKit.Label(band.transform, $"{done} / {total} COMPLETE", UiKit.Caption, UiKit.TextDim,
                Vector2.zero, new Vector2(420, 42), TextAnchor.MiddleLeft);
            pct.rectTransform.anchorMin = pct.rectTransform.anchorMax = new Vector2(0f, 1f);
            pct.rectTransform.pivot = new Vector2(0f, 1f);
            pct.rectTransform.anchoredPosition = new Vector2(UiKit.EdgePad + 920f, -220f);

            Legend(band.transform, "Gold", UiKit.Gold, -940f);
            Legend(band.transform, "Silver", UiKit.Silver, -530f);
            Legend(band.transform, "Bronze", UiKit.Bronze, -110f);

            UiKit.Header(root.transform, "Levels", shell.Back);
            return root;
        }

        private static void Legend(Transform band, string label, Color color, float rightOffset)
        {
            var dot = UiKit.Panel(band, $"legend-{label}", color, UiKit.PillRadius);
            var drt = dot.rectTransform;
            drt.anchorMin = drt.anchorMax = new Vector2(1f, 1f);
            drt.pivot = new Vector2(1f, 1f);
            drt.sizeDelta = new Vector2(64, 64);
            drt.anchoredPosition = new Vector2(rightOffset - 360f, -92f);

            var text = UiKit.Label(band, label, UiKit.Heading + 12, UiKit.TextBlue, Vector2.zero,
                new Vector2(280, 76), TextAnchor.MiddleLeft);
            var trt = text.rectTransform;
            trt.anchorMin = trt.anchorMax = new Vector2(1f, 1f);
            trt.pivot = new Vector2(1f, 1f);
            trt.anchoredPosition = new Vector2(rightOffset, -92f);
        }

        private static void BuildCell(Transform parent, AppShell shell, int n, Transform screen)
        {
            string id = LevelCatalog.IdFor(n);
            bool unlocked = Progression.IsUnlocked(n);
            var rec = Progression.RecordFor(id);

            var cell = UiKit.Panel(parent, $"cell-{n}",
                unlocked ? new Color(1f, 1f, 1f, 0.09f) : new Color(1f, 1f, 1f, 0.03f));

            // The medal is the disc behind the number (owner ruling): one size
            // for every level so "1" and "100" match, wide enough for three
            // digits, and the number is outlined so it reads on gold.
            var disc = UiKit.Panel(cell.transform, "medal", rec != null && rec.medal > 0
                ? rec.medal switch { 3 => UiKit.Gold, 2 => UiKit.Silver, _ => UiKit.Bronze }
                : new Color(1f, 1f, 1f, unlocked ? 0.10f : 0.05f), UiKit.PillRadius);
            disc.rectTransform.anchorMin = disc.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            disc.rectTransform.pivot = new Vector2(0.5f, 1f);
            disc.rectTransform.sizeDelta = new Vector2(DiscSize, DiscSize);
            disc.rectTransform.anchoredPosition = new Vector2(0f, -DiscTop);

            bool onMedal = rec != null && rec.medal > 0;
            var num = UiKit.Label(disc.transform, n.ToString(), UiKit.Heading,
                onMedal ? UiKit.NavyDeep : unlocked ? UiKit.White : new Color(1f, 1f, 1f, 0.3f),
                Vector2.zero, new Vector2(DiscSize - 8f, DiscSize * 0.62f));
            num.fontStyle = FontStyle.Bold;
            var outline = num.gameObject.AddComponent<Outline>();
            outline.effectColor = onMedal ? new Color(1f, 1f, 1f, 0.85f) : new Color(0f, 0f, 0f, 0.55f);
            outline.effectDistance = new Vector2(2f, -2f);

            bool played = rec != null && rec.bestSeconds >= 0;
            var time = UiKit.Label(cell.transform, played ? $"{rec.bestSeconds:0.0}s" : "—",
                UiKit.Caption, played ? UiKit.TextBlue : UiKit.TextDim, Vector2.zero, new Vector2(160, 44));
            time.rectTransform.anchorMin = time.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            time.rectTransform.pivot = new Vector2(0.5f, 0f);
            time.rectTransform.anchoredPosition = new Vector2(0f, 12f);

            // Press and hold any cell — locked included — to see what the
            // medals cost. Knowing the target before you unlock is useful, and
            // hiding it serves nothing (#123).
            var press = cell.gameObject.AddComponent<LongPress>();
            press.OnHold = () => ShowTimes(screen, cell.rectTransform, n, id);
            press.OnRelease = () => HideTimes(screen);

            if (unlocked)
            {
                var btn = cell.gameObject.AddComponent<Button>();
                btn.onClick.AddListener(() =>
                {
                    // OnPointerUp runs before the click, so a completed hold is
                    // visible here — a preview must never also start the level.
                    if (press.Held) return;
                    FrogAcross.Audio.AudioDirector.Instance.Play(FrogAcross.Audio.GameSound.UiTap);
                    shell.LaunchLevel(id);
                });
            }
        }

        public const string BubbleName = "medal-times";

        /// <summary>The medal deadlines for one level, floating above its cell.</summary>
        private static void ShowTimes(Transform screen, RectTransform cell, int n, string id)
        {
            HideTimes(screen);
            var level = LevelLoader.LoadFromResources(id, FrogAcross.Pieces.PieceRegistry.Load());

            var bubble = UiKit.Panel(screen, BubbleName, UiKit.PanelNavy);
            bubble.rectTransform.sizeDelta = new Vector2(300, 240);
            // parented to the SCREEN, not the cell: the grid lives in a masked
            // scroll view and a child bubble would be clipped at the edges
            bubble.rectTransform.position = cell.position + new Vector3(0f, cell.rect.height * 0.75f, 0f);

            UiKit.Label(bubble.transform, $"LEVEL {n}", UiKit.Micro, UiKit.TextDim,
                new Vector2(0, 88), new Vector2(260, 34));

            var rows = new[]
            {
                ("GOLD", Medals.Gold, level.GoldSeconds),
                ("SILVER", Medals.Silver, level.SilverSeconds),
                ("BRONZE", Medals.Bronze, level.BronzeSeconds),
            };
            float y = 30f;
            foreach (var (name, color, seconds) in rows)
            {
                var dot = UiKit.Panel(bubble.transform, $"dot-{name}", color, UiKit.PillRadius);
                dot.rectTransform.sizeDelta = new Vector2(26, 26);
                dot.rectTransform.anchoredPosition = new Vector2(-104, y);
                UiKit.Label(bubble.transform, name, UiKit.Micro, UiKit.TextBlue,
                    new Vector2(-30, y), new Vector2(120, 34), TextAnchor.MiddleLeft);
                UiKit.Label(bubble.transform, $"{seconds:0.0}s", UiKit.Caption, UiKit.White,
                    new Vector2(80, y), new Vector2(120, 38), TextAnchor.MiddleRight);
                y -= 52f;
            }
        }

        private static void HideTimes(Transform screen)
        {
            var existing = screen.Find(BubbleName);
            if (existing != null) Object.Destroy(existing.gameObject);
        }

        /// <summary>Medal disc: identical on every cell, sized for three digits.</summary>
        public const float DiscSize = 112f;
        private const float DiscTop = 14f;
    }
}
