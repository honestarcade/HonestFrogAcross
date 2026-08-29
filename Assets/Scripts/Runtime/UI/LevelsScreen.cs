using FrogAcross.Levels;
using FrogAcross.Services;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FrogAcross.UI
{
    /// <summary>#56/#85: the levels grid — pagination (buttons AND swipe),
    /// lock states, medals, best times, filling the full reference canvas.</summary>
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

            UiKit.Button(root.transform, "‹", new Vector2(-890, 465), new Vector2(76, 76), shell.Back, fontSize: 36);
            UiKit.Label(root.transform, "Levels", 46, UiKit.White, new Vector2(-690, 465), new Vector2(320, 60), TextAnchor.MiddleLeft);

            // medal legend
            Legend(root.transform, "Gold", UiKit.Gold, new Vector2(390, 465));
            Legend(root.transform, "Silver", UiKit.Silver, new Vector2(590, 465));
            Legend(root.transform, "Bronze", UiKit.Bronze, new Vector2(790, 465));

            // progress bar
            int total = LevelCatalog.Count;
            int done = 0;
            for (int n = 1; n <= total; n++)
            {
                var rec = Progression.RecordFor(LevelCatalog.IdFor(n));
                if (rec != null && rec.bestSeconds >= 0) done++;
            }
            var barBg = UiKit.Panel(root.transform, "progress-bg", new Color(1f, 1f, 1f, 0.1f));
            barBg.rectTransform.sizeDelta = new Vector2(1700, 10);
            barBg.rectTransform.anchoredPosition = new Vector2(0, 400);
            var bar = UiKit.Panel(barBg.transform, "progress", UiKit.Mint);
            bar.rectTransform.anchorMin = Vector2.zero;
            bar.rectTransform.anchorMax = new Vector2(total > 0 ? done / (float)total : 0f, 1f);
            bar.rectTransform.offsetMin = bar.rectTransform.offsetMax = Vector2.zero;

            var pagesRoot = new GameObject("pages");
            pagesRoot.transform.SetParent(root.transform, false);
            var pagesRt = pagesRoot.AddComponent<RectTransform>();
            pagesRt.anchorMin = Vector2.zero; pagesRt.anchorMax = Vector2.one;
            pagesRt.offsetMin = pagesRt.offsetMax = Vector2.zero;
            // invisible raycast surface so the grid area receives swipes (#85)
            var swipeSurface = pagesRoot.AddComponent<Image>();
            swipeSurface.color = Color.clear;

            int pageCount = Mathf.Max(1, Mathf.CeilToInt(total / (float)PerPage));
            var state = new PageState { Page = 0 };
            var pageLabel = UiKit.Label(root.transform, "", 22, UiKit.TextDim, new Vector2(-790, 372), new Vector2(300, 30), TextAnchor.MiddleLeft);

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
                        new Vector2(-801 + (i % 10) * 178, 140 - (i / 10) * 390));
                }
            }

            void PageBy(int delta)
            {
                state.Page = Mathf.Clamp(state.Page + delta, 0, pageCount - 1);
                Rebuild();
            }

            var pager = pagesRoot.AddComponent<SwipePager>();
            pager.OnPage = PageBy;

            var prev = UiKit.Button(root.transform, "<", new Vector2(-915, -55), new Vector2(64, 220), () => PageBy(-1), fontSize: 40);
            prev.image.color = new Color(1f, 1f, 1f, 0.04f);
            var next = UiKit.Button(root.transform, ">", new Vector2(915, -55), new Vector2(64, 220), () => PageBy(+1), fontSize: 40);
            next.image.color = new Color(1f, 1f, 1f, 0.04f);

            Rebuild();
            return root;
        }

        private sealed class PageState { public int Page; }

        private static void Legend(Transform parent, string label, Color color, Vector2 pos)
        {
            var dot = UiKit.Panel(parent, $"legend-{label}", color);
            dot.rectTransform.sizeDelta = new Vector2(26, 26);
            dot.rectTransform.anchoredPosition = pos;
            UiKit.Label(parent, label, 24, UiKit.TextBlue, pos + new Vector2(90, 0), new Vector2(130, 30), TextAnchor.MiddleLeft);
        }

        private static void BuildCell(Transform parent, AppShell shell, int n, Vector2 pos)
        {
            string id = LevelCatalog.IdFor(n);
            bool unlocked = Progression.IsUnlocked(n);
            var rec = Progression.RecordFor(id);

            var cell = UiKit.Panel(parent, $"cell-{n}", unlocked ? new Color(1f, 1f, 1f, 0.07f) : new Color(1f, 1f, 1f, 0.025f));
            cell.rectTransform.sizeDelta = new Vector2(162, 360);
            cell.rectTransform.anchoredPosition = pos;

            var num = UiKit.Label(cell.transform, n.ToString(), 46,
                unlocked ? UiKit.White : new Color(1f, 1f, 1f, 0.25f), new Vector2(0, 50));
            num.fontStyle = FontStyle.Bold;
            UiKit.Label(cell.transform, rec != null && rec.bestSeconds >= 0 ? $"{rec.bestSeconds:0.0}s" : "—",
                24, UiKit.TextDim, new Vector2(0, -20));

            if (rec != null && rec.medal > 0)
            {
                var dot = UiKit.Panel(cell.transform, "medal", rec.medal switch
                {
                    3 => UiKit.Gold, 2 => UiKit.Silver, _ => UiKit.Bronze,
                });
                dot.rectTransform.sizeDelta = new Vector2(22, 22);
                dot.rectTransform.anchoredPosition = new Vector2(56, 150);
            }

            if (unlocked)
            {
                var btn = cell.gameObject.AddComponent<Button>();
                btn.onClick.AddListener(() => shell.LaunchLevel(id));
            }
        }
    }

    /// <summary>#85: horizontal swipe on the grid area pages the levels.
    /// Left swipe (drag left) = next page, matching a carousel.</summary>
    public sealed class SwipePager : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler
    {
        public const float ThresholdPixels = 110f;
        public System.Action<int> OnPage;
        private Vector2 _start;

        public void OnBeginDrag(PointerEventData eventData) => _start = eventData.position;

        public void OnDrag(PointerEventData eventData) { }

        public void OnEndDrag(PointerEventData eventData)
        {
            float dx = eventData.position.x - _start.x;
            if (Mathf.Abs(dx) < ThresholdPixels) return;
            OnPage?.Invoke(dx < 0 ? +1 : -1);
        }
    }
}
