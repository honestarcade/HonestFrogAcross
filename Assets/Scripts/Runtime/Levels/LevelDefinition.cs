using System.Collections.Generic;
using FrogAcross.Pieces;

namespace FrogAcross.Levels
{
    /// <summary>Validated, def-resolved level model — the sim's sole input.</summary>
    public sealed class LevelDefinition
    {
        public string Id;
        public string Name;
        public int Columns;
        public float GoldSeconds, SilverSeconds, BronzeSeconds;
        public int StartColumn;
        public IReadOnlyList<int> BayColumns;
        public IReadOnlyList<RowDefinition> Rows; // index 0 = goal row, last = bank

        public int BankRow => Rows.Count - 1;
    }

    public sealed class RowDefinition
    {
        public LaneKindDef Kind;
        public int DirSign;             // +1 right, -1 left, 0 static
        public float SpeedCellsPerSec;
        public IReadOnlyList<ObjectTrain> Trains;
        public IReadOnlyList<PlacedObstruction> Obstructions;
    }

    public sealed class ObjectTrain
    {
        public LaneObjectDef Def;
        public float OffsetCells;
        public float SpacingCells;      // 0 = single instance
        public int PhaseTicks;
    }

    public sealed class PlacedObstruction
    {
        public ObstructionDef Def;
        public int Column;
    }
}
