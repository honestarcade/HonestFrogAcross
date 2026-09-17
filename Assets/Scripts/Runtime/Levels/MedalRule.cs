namespace FrogAcross.Levels
{
    /// <summary>
    /// The one time→medal rule, in the data layer so every surface can reach it
    /// without depending on the UI. Three surfaces read it: the HUD chip (what
    /// is still reachable), the completion panel (what was earned) and
    /// Progression (what gets persisted and drawn on the levels grid). They
    /// used to hold three copies of the same comparisons (#131).
    /// </summary>
    public static class MedalRule
    {
        /// <summary>3 gold, 2 silver, 1 bronze, 0 none.</summary>
        public static int IndexFor(float seconds, float gold, float silver, float bronze)
        {
            if (seconds <= gold) return 3;
            if (seconds <= silver) return 2;
            if (seconds <= bronze) return 1;
            return 0;
        }

        public static int IndexFor(float seconds, LevelDefinition level) =>
            IndexFor(seconds, level.GoldSeconds, level.SilverSeconds, level.BronzeSeconds);
    }
}
