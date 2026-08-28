using FrogAcross.Sim;
using UnityEngine;

namespace FrogAcross.Input
{
    /// <summary>
    /// Owner's tap-region layout (2026-08-28): left third → Left, right third →
    /// Right, middle column top half → Forward, bottom half → Back. Pure
    /// geometry — the driver feeds normalized screen points. No diagonals in
    /// this scheme (owner decision; medal calibration is diagonal-free).
    /// </summary>
    public static class TapRegionMapper
    {
        public const float SideBand = 1f / 3f;
        // Region taps are a touch more lenient than swipe-mode taps.
        public const float RegionTapMaxCm = 0.6f;
        public const float RegionTapMaxSeconds = 0.35f;

        /// <summary>p: normalized screen point (0..1, +y up).</summary>
        public static Move Map(Vector2 p)
        {
            if (p.x < SideBand) return Move.Left;
            if (p.x > 1f - SideBand) return Move.Right;
            return p.y > 0.5f ? Move.Forward : Move.Back;
        }

        public static bool IsRegionTap(Vector2 deltaCm, float durationSeconds) =>
            deltaCm.magnitude <= RegionTapMaxCm && durationSeconds <= RegionTapMaxSeconds;
    }
}
