using FrogAcross.Levels;
using FrogAcross.Services;
using UnityEngine;
using UnityEngine.UI;

namespace FrogAcross.UI
{
    /// <summary>#56: the levels grid — pagination, lock states, medals, best times.</summary>
    public static class LevelsScreen
    {
        private const int PerPage = 20;

        public static GameObject Build(Transform parent, AppShell shell)
        {
            var root = new GameObject("levels");
            root.transform.SetParent(parent, false);
            var rootRt = root.AddComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero; rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = rootRt.offsetMax = Vector2.zero;

            UiKit.Button(root.transform, "‹", new Vector2(-880, 460), new Vector2(56, 56), shell.Back);
            UiKit.Label(root.transform, "Levels", 34, UiKit.White, new Vector2(-700, 460), new Vector2(300, 48), TextAnchor.MiddleLeft);

            // medal legend
            Legend(root.transform, "Gold", UiKit.Gold, new Vector2(430, 460));
            Legend(root.transform, "Silver", UiKit.Silver, new Vector2(590, 460));
            Legend(root.transform, "Bronze", UiKit.Bronze, new Vector2(750, 460));

            // progress bar
            int total = LevelCatalog.Count;
            int done = 0;
            for (int n = 1; n <= total; n++)
            {
                var rec = Progression.RecordFor(LevelCatalog.IdFor(n));
                if (rec != null && rec.bestSeconds >= 0) done++;
            }
            var barBg = UiKit.Panel(root.transform, "progress-bg", new Color(1f, 1f, 1f, 0.1f));
            barBg.rectTransform.sizeDelta = new Vector2(1700, 8);
            barBg.rectTransform.anchoredPosition = new Vector2(0, 415);
            var bar = UiKit.Panel(barBg.transform, "progress", UiKit.Mint);
            bar.rectTransform.anchorMin = Vector2.zero;
            bar.rectTransform.anchorMax = new Vector2(total > 0 ? done / (float)total : 0f, 1f);
            bar.rectTransform.offsetMin = bar.rectTransform.offsetMax = Vector2.zero;

            var pagesRoot = new GameObject("pages");
            pagesRoot.transform.SetParent(root.transform, false);
            var pagesRt = pagesRoot.AddComponent<RectTransform>();
            pagesRt.anchorMin = Vector2.zero; pagesRt.anchorMax = Vector2.one;
            pagesRt.offsetMin = pagesRt.offsetMax = Vector2.zero;

            int pageCount = Mathf.Max(1, Mathf.CeilToInt(total / (float)PerPage));
            var state = new PageState { Page = 0 };
            var pageLabel = UiKit.Label(root.transform, "", 16, UiKit.TextDim, new Vector2(-700, 425), new Vector2(300, 24), TextAnchor.MiddleLeft);

            void Rebuild()
            {
                foreach (Transform child in pagesRoot.transform) Object.Destroy(child.gameObject);
                pageLabel.text = $"PAGE {state.Page + 1} / {pageCount}";
                int start = state.Page * PerPage + 1;
                for (int i = 0; i < PerPage; i++)
                {
                    int n = start + i;
                    if (n > total) break;
                    BuildCell(pagesRoot.transform, shell, n,
                        new Vector2(-765 + (i % 10) * 170, 250 - (i / 10) * 330));
                }
            }

            UiKit.Button(root.transform, "‹ prev", new Vector2(660, 425), new Vector2(110, 40), () =>
            {
                state.Page = Mathf.Max(0, state.Page - 1);
                Rebuild();
            }, fontSize: 16);
            UiKit.Button(root.transform, "next ›", new Vector2(790, 425), new Vector2(110, 40), () =>
            {
                state.Page = Mathf.Min(pageCount - 1, state.Page + 1);
                Rebuild();
            }, fontSize: 16);

            Rebuild();
            return root;
        }

        private sealed class PageState { public int Page; }

        private static void Legend(Transform parent, string label, Color color, Vector2 pos)
        {
            var dot = UiKit.Panel(parent, $"legend-{label}", color);
            dot.rectTransform.sizeDelta = new Vector2(18, 18);
            dot.rectTransform.anchoredPosition = pos;
            UiKit.Label(parent, label, 16, UiKit.TextBlue, pos + new Vector2(52, 0), new Vector2(90, 22), TextAnchor.MiddleLeft);
        }

        private static void BuildCell(Transform parent, AppShell shell, int n, Vector2 pos)
        {
            string id = LevelCatalog.IdFor(n);
            bool unlocked = Progression.IsUnlocked(n);
            var rec = Progression.RecordFor(id);

            var cell = UiKit.Panel(parent, $"cell-{n}", unlocked ? new Color(1f, 1f, 1f, 0.07f) : new Color(1f, 1f, 1f, 0.025f));
            cell.rectTransform.sizeDelta = new Vector2(150, 300);
            cell.rectTransform.anchoredPosition = pos;

            var num = UiKit.Label(cell.transform, n.ToString(), 34,
                unlocked ? UiKit.White : new Color(1f, 1f, 1f, 0.25f), new Vector2(0, 40));
            num.fontStyle = FontStyle.Bold;
            UiKit.Label(cell.transform, rec != null && rec.bestSeconds >= 0 ? $"{rec.bestSeconds:0.0}s" : "—",
                16, UiKit.TextDim, new Vector2(0, -10));

            if (rec != null && rec.medal > 0)
            {
                var dot = UiKit.Panel(cell.transform, "medal", rec.medal switch
                {
                    3 => UiKit.Gold, 2 => UiKit.Silver, _ => UiKit.Bronze,
                });
                dot.rectTransform.sizeDelta = new Vector2(16, 16);
                dot.rectTransform.anchoredPosition = new Vector2(52, 125);
            }

            if (unlocked)
            {
                var btn = cell.gameObject.AddComponent<Button>();
                btn.onClick.AddListener(() => shell.LaunchLevel(id));
            }
        }
    }
}
