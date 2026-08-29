using System;
using System.Collections.Generic;

namespace FrogAcross.Levels
{
    /// <summary>
    /// Lane-object spacing rules, shared by the generator (which places
    /// objects) and the guards (which prove none overlap).
    ///
    /// Every object in a row rides the same wrap loop at the same speed, so
    /// their relative positions never change: overlap is a static property of
    /// the level data. Objects tile the loop at an exact pitch — if the pitch
    /// did not divide the loop, the wrap seam would overlap the first object
    /// (the bug behind the "cars stacked on top of each other" report).
    /// </summary>
    public static class LaneGeometry
    {
        /// <summary>Clear cells required between one object's tail and the next one's nose.</summary>
        public const float MinGapCells = 0.5f;

        public static float MarginFor(float maxSizeCells) => (float)Math.Ceiling(maxSizeCells) + 1f;

        public static float LoopFor(int columns, float maxSizeCells) => columns + 2f * MarginFor(maxSizeCells);

        /// <summary>
        /// Smallest gap between consecutive objects in a row, in cells
        /// (negative = overlap). Rows without moving objects return
        /// float.MaxValue. Positions are taken modulo the row's loop, which is
        /// where the wrap-seam collisions hide.
        /// </summary>
        public static float SmallestGap(RowDefinition row, int columns)
        {
            if (row.Trains == null || row.Trains.Count == 0) return float.MaxValue;

            float maxSize = 0f;
            foreach (var t in row.Trains) maxSize = Math.Max(maxSize, t.Def.sizeCells);
            float loop = LoopFor(columns, maxSize);

            // every instance the sim will spawn, reduced onto the loop circle
            var spans = new List<(float start, float size)>();
            foreach (var train in row.Trains)
            {
                if (train.SpacingCells <= 0f)
                {
                    spans.Add((Mod(train.OffsetCells, loop), train.Def.sizeCells));
                    continue;
                }
                int count = Math.Max(1, (int)Math.Ceiling(loop / train.SpacingCells));
                for (int k = 0; k < count; k++)
                    spans.Add((Mod(train.OffsetCells + k * train.SpacingCells, loop), train.Def.sizeCells));
            }
            if (spans.Count < 2) return spans.Count == 1 ? loop - spans[0].size : float.MaxValue;

            spans.Sort((a, b) => a.start.CompareTo(b.start));
            float smallest = float.MaxValue;
            for (int i = 0; i < spans.Count; i++)
            {
                var cur = spans[i];
                var next = spans[(i + 1) % spans.Count];
                float nextStart = i + 1 < spans.Count ? next.start : next.start + loop;
                smallest = Math.Min(smallest, nextStart - (cur.start + cur.size));
            }
            return smallest;
        }

        /// <summary>
        /// Exact placement for a row: N objects tiling the loop at pitch
        /// loop/N, dealt round-robin to the row's trains. Returns per-train
        /// (offset, spacing); every train ends up with N/trainCount instances.
        /// </summary>
        public static (float offset, float spacing)[] Place(
            int trainCount, float loop, float maxSizeCells, float desiredPitch)
        {
            int maxSlots = (int)Math.Floor(loop / Math.Max(0.01f, maxSizeCells + MinGapCells));
            int slots = Math.Clamp((int)Math.Floor(loop / Math.Max(0.01f, desiredPitch)), 1, Math.Max(1, maxSlots));
            int trains = Math.Clamp(trainCount, 1, slots);
            slots -= slots % trains; // whole instances per train keeps the pitch exact
            if (slots < trains) slots = trains;

            float pitch = loop / slots;
            var result = new (float, float)[trains];
            for (int t = 0; t < trains; t++)
                result[t] = (t * pitch, trains * pitch);
            return result;
        }

        private static float Mod(float v, float m) => (v % m + m) % m;
    }
}
