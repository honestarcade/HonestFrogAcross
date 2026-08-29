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

        public void Build(float goldSeconds)
        {
            var canvas = UiKit.Canvas(transform, "hud", 50);

            var movePill = UiKit.Panel(canvas.transform, "move-pill", new Color(0.02f, 0.078f, 0.157f, 0.55f));
            movePill.rectTransform.anchorMin = movePill.rectTransform.anchorMax = new Vector2(0f, 1f);
            movePill.rectTransform.sizeDelta = new Vector2(380, 66);
            movePill.rectTransform.anchoredPosition = new Vector2(230, -56);
            _moveLabel = UiKit.Label(movePill.transform, "SWIPE ▲ FORWARD", 24, UiKit.TextBlue, Vector2.zero, new Vector2(370, 38));

            var timePill = UiKit.Panel(canvas.transform, "time-pill", new Color(0.02f, 0.078f, 0.157f, 0.55f));
            timePill.rectTransform.anchorMin = timePill.rectTransform.anchorMax = new Vector2(1f, 1f);
            timePill.rectTransform.sizeDelta = new Vector2(400, 66);
            timePill.rectTransform.anchoredPosition = new Vector2(-410, -56);
            _timer = UiKit.Label(timePill.transform, "0.0", 34, UiKit.White, new Vector2(-90, 0), new Vector2(170, 42));
            var dot = UiKit.Panel(timePill.transform, "gold-dot", UiKit.Gold);
            dot.rectTransform.sizeDelta = new Vector2(18, 18);
            dot.rectTransform.anchoredPosition = new Vector2(38, 0);
            UiKit.Label(timePill.transform, $"{goldSeconds:0.0}s", 24, UiKit.TextBlue, new Vector2(120, 0), new Vector2(140, 32));

            // corner buttons (owner amendment): dialogs freeze the sim
            var restart = UiKit.Button(canvas.transform, "↻", Vector2.zero, new Vector2(76, 76), () =>
            {
                if (FreezeGate != null && !FreezeGate()) return;
                SetFrozen?.Invoke(true);
                ConfirmDialog.Show(transform, "Restart level?",
                    "Bays and the clock reset — this attempt is abandoned.",
                    "Restart", () => { SetFrozen?.Invoke(false); OnRestartConfirmed?.Invoke(); },
                    () => SetFrozen?.Invoke(false));
            }, fontSize: 36);
            restart.image.rectTransform.anchorMin = restart.image.rectTransform.anchorMax = new Vector2(1f, 1f);
            restart.image.rectTransform.anchoredPosition = new Vector2(-146, -56);

            var menu = UiKit.Button(canvas.transform, "≡", Vector2.zero, new Vector2(76, 76), () =>
            {
                if (FreezeGate != null && !FreezeGate()) return;
                SetFrozen?.Invoke(true);
                ConfirmDialog.Show(transform, "Quit to menu?", Copy.Get("quitConfirm"),
                    "Quit", () => { SetFrozen?.Invoke(false); OnQuitConfirmed?.Invoke(); },
                    () => SetFrozen?.Invoke(false));
            }, fontSize: 36);
            menu.image.rectTransform.anchorMin = menu.image.rectTransform.anchorMax = new Vector2(1f, 1f);
            menu.image.rectTransform.anchoredPosition = new Vector2(-56, -56);
        }

        public void Tick(GameSim sim)
        {
            if (_timer == null) return;
            _timer.text = (sim.State.ClockTicks / (float)SimConfig.TicksPerSecond).ToString("0.0");
            _moveLabel.text = sim.State.Facing switch
            {
                Move.Back => "SWIPE ▼ BACK",
                Move.Left or Move.DiagForwardLeft or Move.DiagBackLeft => "SWIPE ◀ LEFT",
                Move.Right or Move.DiagForwardRight or Move.DiagBackRight => "SWIPE ▶ RIGHT",
                _ => "SWIPE ▲ FORWARD",
            };
        }
    }
}
