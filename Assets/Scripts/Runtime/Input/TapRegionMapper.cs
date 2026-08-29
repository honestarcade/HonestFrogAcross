using FrogAcross.Sim;
using UnityEngine;

namespace FrogAcross.Input
{
    /// <summary>
    /// Owner's tap-region layout: narrow left/right bands → Left/Right, and
    /// the wide middle column splits top → Forward, bottom → Back. Bands were
    /// thirds until the 2026-08-29 device test — the owner cut them to 20% so
    /// forward/back (the moves you make constantly) get the room. Pure
    /// geometry; the driver feeds normalized screen points. No diagonals in
    /// this scheme (owner decision; medal calibration is diagonal-free).
    /// </summary>
    public static class TapRegionMapper
    {
        /// <summary>Width of each side band as a fraction of the screen.</summary>
        public const float SideFraction = 0.20f;

        public const float SideBand = SideFraction;
        // Region taps are a touch more lenient than swipe-mode taps.
        public const float RegionTapMaxCm = 0.6f;
        public const float RegionTapMaxSeconds = 0.35f;

        /// <summary>p: normalized screen point (0..1, +y up).</summary>
        public static Move Map(Vector2 p)
        {
            if (p.x < SideFraction) return Move.Left;
            if (p.x > 1f - SideFraction) return Move.Right;
            return p.y > 0.5f ? Move.Forward : Move.Back;
        }

        public static bool IsRegionTap(Vector2 deltaCm, float durationSeconds) =>
            deltaCm.magnitude <= RegionTapMaxCm && durationSeconds <= RegionTapMaxSeconds;
    }
}
