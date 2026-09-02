using System;
using FrogAcross.Sim;
using UnityEngine;
using UnityEngine.UI;

namespace FrogAcross.UI
{
    /// <summary>
    /// #60 (amended): the in-game HUD — move label, live timer, gold-target
    /// chip, and the Restart / Menu corner buttons whose confirm dialogs
    /// freeze the sim ("no pause" = no dedicated pause screen).
    /// </summary>
    public sealed class GameHud : MonoBehaviour
    {
        public event Action OnRestartConfirmed;
        public event Action OnQuitConfirmed;
        public Func<bool> FreezeGate;      // returns true when a dialog may open
        public Action<bool> SetFrozen;

        private Text _moveLabel;
        private Text _timer;
        private GameObject _canvasGo;

        /// <summary>Corner buttons: bigger than a normal tap target, because
        /// they are pressed mid-run with a thumb (owner, 2026-08-30).</summary>
        public const float ButtonSize = 148f;

        /// <summary>The board's wedges are shallow, so the HUD hugs the edge
        /// tighter than a menu screen does (it is already inside the safe area).</summary>
        private const float Inset = 40f;
        private const float ChipHeight = 80f;

        public void Build(float goldSeconds)
        {
            // Rebuilt per level: the gold target on the chip belongs to the
            // level being played, and a rebuild is how the HUD comes back after
            // a restart.
            if (_canvasGo != null) Destroy(_canvasGo);
            var canvas = UiKit.Canvas(transform, "hud", 50);
            _canvasGo = canvas.gameObject;
            var safeGo = new GameObject("safe-area");
            safeGo.transform.SetParent(canvas.transform, false);
            var safe = safeGo.AddComponent<RectTransform>();
            safe.anchorMin = Vector2.zero; safe.anchorMax = Vector2.one;
            safe.offsetMin = safe.offsetMax = Vector2.zero;
            safeGo.AddComponent<SafeArea>();

            // The board rolls -8°, so on screen BOTH its edges rise left to
            // right: the free wedges are above its low left end and below its
            // high right end. Everything the HUD draws goes in one of those
            // two corners — the timer used to sit top-right, where the goal
            // row reaches highest, and swallowed landing pads on a wide board
            // (owner, 2026-09-02). Sizes are on the type scale since 2026-08-30.
            var timePill = UiKit.Panel(safe, "time-pill", new Color(0.02f, 0.078f, 0.157f, 0.72f), UiKit.PillRadius);
            timePill.rectTransform.anchorMin = timePill.rectTransform.anchorMax = new Vector2(0f, 1f);
            timePill.rectTransform.sizeDelta = new Vector2(540, ChipHeight);
            timePill.rectTransform.anchoredPosition = new Vector2(Inset + 270, -(Inset + ChipHeight / 2f));
            _timer = UiKit.Label(timePill.transform, "0.0", UiKit.Title, UiKit.White,
                new Vector2(-110, 0), new Vector2(260, 78));
            var dot = UiKit.Panel(timePill.transform, "gold-dot", UiKit.Gold, UiKit.PillRadius);
            dot.rectTransform.sizeDelta = new Vector2(26, 26);
            dot.rectTransform.anchoredPosition = new Vector2(48, 0);
            UiKit.Label(timePill.transform, $"{goldSeconds:0.0}s", UiKit.Caption, UiKit.TextBlue,
                new Vector2(160, 0), new Vector2(180, 44));

            // The wedge is a triangle: widest along the very top edge and
            // narrowing as you descend, so the hint goes BESIDE the clock, not
            // under it — stacked, its second row ran into the goal row.
            var movePill = UiKit.Panel(safe, "move-pill", new Color(0.02f, 0.078f, 0.157f, 0.72f), UiKit.PillRadius);
            movePill.rectTransform.anchorMin = movePill.rectTransform.anchorMax = new Vector2(0f, 1f);
            movePill.rectTransform.sizeDelta = new Vector2(340, ChipHeight);
            movePill.rectTransform.anchoredPosition = new Vector2(Inset + 564 + 170, -(Inset + ChipHeight / 2f));
            _moveLabel = UiKit.Label(movePill.transform, "▲ FORWARD", UiKit.Caption, UiKit.TextBlue,
                Vector2.zero, new Vector2(320, 56));

            // corner buttons (owner amendment): dialogs freeze the sim
            var restart = UiKit.Button(safe, "↻", Vector2.zero, new Vector2(ButtonSize, ButtonSize), () =>
            {
                if (FreezeGate != null && !FreezeGate()) return;
                SetFrozen?.Invoke(true);
                ConfirmDialog.Show(transform, "Restart level?",
                    "Bays and the clock reset — this attempt is abandoned.",
                    "Restart", () => { SetFrozen?.Invoke(false); OnRestartConfirmed?.Invoke(); },
                    () => SetFrozen?.Invoke(false));
            }, fontSize: UiKit.Title);
            CornerChrome(restart);
            // lower right, thumb-height (owner, 2026-08-30)
            restart.image.rectTransform.anchorMin = restart.image.rectTransform.anchorMax = new Vector2(1f, 0f);
            restart.image.rectTransform.anchoredPosition =
                new Vector2(-(UiKit.EdgePad + ButtonSize * 1.5f + 24f), UiKit.EdgePad + ButtonSize / 2f);

            var menu = UiKit.Button(safe, "≡", Vector2.zero, new Vector2(ButtonSize, ButtonSize), () =>
            {
                if (FreezeGate != null && !FreezeGate()) return;
                SetFrozen?.Invoke(true);
                ConfirmDialog.Show(transform, "Quit to menu?", Copy.Get("quitConfirm"),
                    "Quit", () => { SetFrozen?.Invoke(false); OnQuitConfirmed?.Invoke(); },
                    () => SetFrozen?.Invoke(false));
            }, fontSize: UiKit.Title);
            CornerChrome(menu);
            menu.image.rectTransform.anchorMin = menu.image.rectTransform.anchorMax = new Vector2(1f, 0f);
            menu.image.rectTransform.anchoredPosition =
                new Vector2(-(UiKit.EdgePad + ButtonSize / 2f), UiKit.EdgePad + ButtonSize / 2f);
        }

        /// <summary>Same dark chip as the HUD pills: 10%-white over a bright
        /// board read as a disabled control.</summary>
        private static void CornerChrome(Button b)
        {
            b.image.color = new Color(0.02f, 0.078f, 0.157f, 0.82f);
            foreach (var t in b.GetComponentsInChildren<Text>()) t.color = UiKit.Mint;
        }

        public void Tick(GameSim sim)
        {
            if (_timer == null) return;
            _timer.text = (sim.State.ClockTicks / (float)SimConfig.TicksPerSecond).ToString("0.0");
            _moveLabel.text = sim.State.Facing switch
            {
                Move.Back => "▼ BACK",
                Move.Left or Move.DiagForwardLeft or Move.DiagBackLeft => "◀ LEFT",
                Move.Right or Move.DiagForwardRight or Move.DiagBackRight => "▶ RIGHT",
                _ => "▲ FORWARD",
            }; // it used to say SWIPE even with tap regions selected
        }
    }
}
