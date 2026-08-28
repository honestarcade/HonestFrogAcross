using FrogAcross.Sim;
using UnityEngine;

namespace FrogAcross.Input
{
    /// <summary>
    /// Pure gesture → move mapping (unit-tested table). Spec:
    /// - clear tap = FORWARD hop (owner amendment 2026-08-28)
    /// - dead zone between tap ceiling and swipe floor does nothing
    /// - swipe at 43–47° from horizontal in any quadrant = diagonal double-hop
    /// - other swipes snap to the nearest cardinal
    /// Distances are centimeters (dpi-normalized by the driver) so thresholds
    /// feel identical across devices.
    /// </summary>
    public static class SwipeClassifier
    {
        public const float TapMaxCm = 0.4f;
        public const float TapMaxSeconds = 0.25f;
        public const float SwipeMinCm = 0.8f;
        public const float DiagBandDeg = 2f; // 45° ± 2 → the 43..47 spec band

        /// <summary>deltaCm: gesture vector in cm, +y = screen up. Null = no move.</summary>
        public static Move? Classify(Vector2 deltaCm, float durationSeconds)
        {
            float dist = deltaCm.magnitude;

            if (dist <= TapMaxCm)
                return durationSeconds <= TapMaxSeconds ? Move.Forward : (Move?)null;

            if (dist < SwipeMinCm) return null; // dead zone

            float angle = Mathf.Atan2(deltaCm.y, deltaCm.x) * Mathf.Rad2Deg;
            if (angle < 0f) angle += 360f;

            float offAxis45 = Mathf.Abs(((angle % 90f) + 90f) % 90f - 45f);
            if (offAxis45 <= DiagBandDeg)
            {
                return angle switch
                {
                    < 90f => Move.DiagForwardRight,   // ~45°
                    < 180f => Move.DiagForwardLeft,   // ~135°
                    < 270f => Move.DiagBackLeft,      // ~225°
                    _ => Move.DiagBackRight,          // ~315°
                };
            }

            // nearest cardinal
            if (angle >= 315f || angle < 45f) return Move.Right;
            if (angle < 135f) return Move.Forward;
            if (angle < 225f) return Move.Left;
            return Move.Back;
        }
    }
}
