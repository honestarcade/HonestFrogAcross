using FrogAcross.Levels;
using UnityEngine;

namespace FrogAcross.UI
{
    /// <summary>
    /// The one time→medal rule. The HUD reads it every frame to show what is
    /// still reachable, and the completion panel reads it once to show what was
    /// earned; a second copy of the thresholds is how those two start
    /// disagreeing (#122).
    /// </summary>
    public static class Medals
    {
        // One palette, shared with the grid discs and the legend. These used to
        // be separate float literals that differed from UiKit's hex by ~0.0002
        // per channel: visually identical, not Equals-equal, so they could not
        // be swapped in an assertion (#131).
        public static readonly Color Gold = UiKit.Gold;
        public static readonly Color Silver = UiKit.Silver;
        public static readonly Color Bronze = UiKit.Bronze;
        public static readonly Color Spent = new Color(0.44f, 0.57f, 0.69f);

        /// <summary>
        /// Where a run stands at <paramref name="seconds"/>: the medal it would
        /// take right now, its colour, and the deadline still worth chasing.
        /// Past bronze there is nothing left to chase, so the target is 0.
        /// </summary>
        public static (string name, Color color, float target) Standing(float seconds, LevelDefinition level)
        {
            return MedalRule.IndexFor(seconds, level) switch
            {
                3 => ("GOLD", Gold, level.GoldSeconds),
                2 => ("SILVER", Silver, level.SilverSeconds),
                1 => ("BRONZE", Bronze, level.BronzeSeconds),
                _ => ("COMPLETE", Spent, 0f),
            };
        }
    }
}
