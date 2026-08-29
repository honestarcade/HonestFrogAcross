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
        private const float CellW = 158f, CellH = 196f, Gap = 18f;

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
            grid.cellSize = new Vector2(CellW, CellH);
            grid.spacing = new Vector2(Gap, Gap);
            grid.padding = new RectOffset(0, 0, (int)Gap, (int)Gap);
            grid.childAlignment = TextAnchor.UpperCenter;
            var fitter = surface.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            for (int n = 1; n <= total; n++) BuildCell(surface, shell, n);

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
            var pct = UiKit.Label(band.transform, $"{done} / {total} COMPLETE", 22, UiKit.TextDim,
                Vector2.zero, new Vector2(340, 30), TextAnchor.MiddleLeft);
            pct.rectTransform.anchorMin = pct.rectTransform.anchorMax = new Vector2(0f, 1f);
            pct.rectTransform.pivot = new Vector2(0f, 1f);
            pct.rectTransform.anchoredPosition = new Vector2(UiKit.EdgePad + 920f, -220f);

            Legend(band.transform, "Gold", UiKit.Gold, -540f);
            Legend(band.transform, "Silver", UiKit.Silver, -330f);
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
            drt.sizeDelta = new Vector2(26, 26);
            drt.anchoredPosition = new Vector2(rightOffset - 172f, -96f);

            var text = UiKit.Label(band, label, 26, UiKit.TextBlue, Vector2.zero,
                new Vector2(150, 34), TextAnchor.MiddleLeft);
            var trt = text.rectTransform;
            trt.anchorMin = trt.anchorMax = new Vector2(1f, 1f);
            trt.pivot = new Vector2(1f, 1f);
            trt.anchoredPosition = new Vector2(rightOffset, -92f);
        }

        private static void BuildCell(Transform parent, AppShell shell, int n)
        {
            string id = LevelCatalog.IdFor(n);
            bool unlocked = Progression.IsUnlocked(n);
            var rec = Progression.RecordFor(id);

            var cell = UiKit.Panel(parent, $"cell-{n}",
                unlocked ? new Color(1f, 1f, 1f, 0.09f) : new Color(1f, 1f, 1f, 0.03f));

            var num = UiKit.Label(cell.transform, n.ToString(), 52,
                unlocked ? UiKit.White : new Color(1f, 1f, 1f, 0.28f), new Vector2(0, 26),
                new Vector2(CellW - 16, 64));
            num.fontStyle = FontStyle.Bold;

            bool played = rec != null && rec.bestSeconds >= 0;
            UiKit.Label(cell.transform, played ? $"{rec.bestSeconds:0.0}s" : "—",
                30, played ? UiKit.TextBlue : UiKit.TextDim, new Vector2(0, -40), new Vector2(CellW - 16, 40));

            if (rec != null && rec.medal > 0)
            {
                var dot = UiKit.Panel(cell.transform, "medal", rec.medal switch
                {
                    3 => UiKit.Gold, 2 => UiKit.Silver, _ => UiKit.Bronze,
                }, UiKit.PillRadius);
                dot.rectTransform.sizeDelta = new Vector2(26, 26);
                dot.rectTransform.anchoredPosition = new Vector2(CellW / 2f - 26, CellH / 2f - 26);
            }

            if (unlocked)
            {
                var btn = cell.gameObject.AddComponent<Button>();
                btn.onClick.AddListener(() =>
                    FrogAcross.Audio.AudioDirector.Instance.Play(FrogAcross.Audio.GameSound.UiTap));
                btn.onClick.AddListener(() => shell.LaunchLevel(id));
            }
        }
    }
}
