using System;
using UnityEngine;

namespace FrogAcross.Editor.Generator
{
    /// <summary>
    /// One contiguous stretch of the difficulty curve. Values marked
    /// start/end interpolate linearly across the band's levels, so pressure
    /// rises inside a band, not just between bands.
    /// </summary>
    [Serializable]
    public sealed class CurveBand
    {
        public string label;
        public int startLevel = 1;
        public int endLevel = 10;

        [Tooltip("Lane kind ids drawn for middle rows (weights by repetition).")]
        public string[] laneKindPool = { "road", "grass" };

        [Tooltip("Kinds every level in this band must contain (the mechanic it teaches).")]
        public string[] requiredKinds = Array.Empty<string>();

        public Vector2Int middleRowsStartEnd = new(3, 5);
        public Vector2Int bayCountStartEnd = new(2, 3);
        public Vector2Int columnsRange = new(9, 11);

        public Vector2 deadlySpeedStart = new(1.2f, 1.6f);
        public Vector2 deadlySpeedEnd = new(1.5f, 2.0f);
        public Vector2 waterSpeedStart = new(0.9f, 1.2f);
        public Vector2 waterSpeedEnd = new(1.0f, 1.5f);
        public Vector2 crashSpeedStart = new(1.8f, 2.2f);
        public Vector2 crashSpeedEnd = new(2.0f, 2.6f);
        public Vector2 conveyorSpeedStart = new(1.0f, 1.3f);
        public Vector2 conveyorSpeedEnd = new(1.2f, 1.7f);

        [Tooltip("Extra spacing beyond size+1.2, in cells — SHRINKS as difficulty rises.")]
        public Vector2 spacingSlackStart = new(2.5f, 4.0f);
        public Vector2 spacingSlackEnd = new(1.8f, 3.2f);

        public float obstructionChanceStart;
        public float obstructionChanceEnd = 0.1f;

        [Tooltip("Reject candidates the solver needs more than this many moves for (0 = unlimited).")]
        public int maxSolverMoves;
    }

    /// <summary>
    /// #61: the parameter schedule for the shipped 100 levels. The asset is
    /// the single source of truth — levels are reproducible from it plus the
    /// per-level seeds, so hand-edited JSON drift shows up in regeneration
    /// diffs (and in #62's fixture guard).
    /// </summary>
    public sealed class DifficultyCurve : ScriptableObject
    {
        public int levelCount = 100;
        public int baseSeed = 5000;
        public CurveBand[] bands = Array.Empty<CurveBand>();

        /// <summary>Effective generator params for one 1-based level number.</summary>
        public GeneratorParams ParamsForLevel(int level)
        {
            var band = Array.Find(bands, b => level >= b.startLevel && level <= b.endLevel)
                ?? throw new InvalidOperationException($"no curve band covers level {level}");
            float t = band.endLevel == band.startLevel
                ? 1f
                : (level - band.startLevel) / (float)(band.endLevel - band.startLevel);

            var p = ScriptableObject.CreateInstance<GeneratorParams>();
            p.count = 1;
            p.laneKindPool = band.laneKindPool;
            p.columns = band.columnsRange;
            int rows = Mathf.RoundToInt(Mathf.Lerp(band.middleRowsStartEnd.x, band.middleRowsStartEnd.y, t));
            p.middleRows = new Vector2Int(rows, rows);
            int bays = Mathf.RoundToInt(Mathf.Lerp(band.bayCountStartEnd.x, band.bayCountStartEnd.y, t));
            p.bayCount = new Vector2Int(bays, bays);
            p.deadlySpeed = Vector2.Lerp(band.deadlySpeedStart, band.deadlySpeedEnd, t);
            p.waterSpeed = Vector2.Lerp(band.waterSpeedStart, band.waterSpeedEnd, t);
            p.crashSpeed = Vector2.Lerp(band.crashSpeedStart, band.crashSpeedEnd, t);
            p.conveyorSpeed = Vector2.Lerp(band.conveyorSpeedStart, band.conveyorSpeedEnd, t);
            p.spacingSlack = Vector2.Lerp(band.spacingSlackStart, band.spacingSlackEnd, t);
            p.obstructionChance = Mathf.Lerp(band.obstructionChanceStart, band.obstructionChanceEnd, t);
            return p;
        }
    }
}
