using FrogAcross.View;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FrogAcross.Input
{
    /// <summary>
    /// Primary pointer gestures → sim moves, routed by the persisted control
    /// scheme (#74): swipe (classifier incl. tap-forward and diagonals) or tap
    /// regions (four zones, no diagonals). Touches that begin over UI belong to
    /// the UI (UiGuard) in both schemes.
    /// </summary>
    public sealed class TouchInputDriver : MonoBehaviour
    {
        public GameBootstrap bootstrap;

        private Vector2 _downPos;
        private double _downTime;
        private bool _tracking;
        private bool _downOverUi;

        private float PixelsPerCm => (Screen.dpi > 1f ? Screen.dpi : 160f) / 2.54f;

        private void Update()
        {
            var pointer = Pointer.current;
            if (pointer == null || bootstrap == null || bootstrap.Sim == null || bootstrap.Frozen) return;

            if (pointer.press.wasPressedThisFrame)
            {
                _tracking = true;
                _downPos = pointer.position.ReadValue();
                _downTime = Time.realtimeSinceStartupAsDouble;
                _downOverUi = UiGuard.IsPointOverUi(_downPos);
            }
            else if (_tracking && pointer.press.wasReleasedThisFrame)
            {
                _tracking = false;
                if (_downOverUi) return; // the UI owns this touch

                Vector2 deltaCm = (pointer.position.ReadValue() - _downPos) / PixelsPerCm;
                float duration = (float)(Time.realtimeSinceStartupAsDouble - _downTime);

                if (ControlSchemeSetting.Current == ControlScheme.TapRegions)
                {
                    if (TapRegionMapper.IsRegionTap(deltaCm, duration))
                    {
                        var n = new Vector2(_downPos.x / Screen.width, _downPos.y / Screen.height);
                        bootstrap.Sim.EnqueueMove(TapRegionMapper.Map(n));
                    }
                    return; // swipes do nothing in region mode (owner rule)
                }

                var move = SwipeClassifier.Classify(deltaCm, duration);
                if (move.HasValue) bootstrap.Sim.EnqueueMove(move.Value);
            }
        }
    }
}
