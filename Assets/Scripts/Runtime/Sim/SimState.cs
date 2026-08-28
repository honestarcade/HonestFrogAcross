using System.Collections.Generic;
using System.Linq;

namespace FrogAcross.Sim
{
    public enum Move { Forward, Back, Left, Right, DiagForwardLeft, DiagForwardRight, DiagBackLeft, DiagBackRight }

    public enum DeathCause { None, Vehicle, Train, Water, Gator, EdgeDrift }

    /// <summary>
    /// The full mutable game state. Everything the sim knows lives here so the
    /// determinism guard can hash it and replays are exact.
    /// </summary>
    public sealed class SimState
    {
        public long Tick;

        // Player
        public int PlayerRow;
        public float PlayerX;          // continuous column position (grid center = integer)
        public Move Facing = Move.Forward;
        public bool Riding;            // attached to a rideable object
        public int RideTrain;          // train index within the row
        public int RideInstance;       // instance index within the train
        public float RideOffset;       // cells from the object's left edge
        public int StunTicksLeft;
        public int HopCooldown;
        public int RespawnDelay;       // > 0 while dead, counting down
        public DeathCause LastDeath = DeathCause.None;
        public int Deaths;

        // Run
        public bool ClockStarted;
        public long ClockTicks;
        public bool Completed;
        public readonly HashSet<int> BaysFilled = new();

        // Crashed rider instances: key = (row << 16) | (train << 8) | instance,
        // value = crash tick (position freezes there; wreck is passable+safe).
        public readonly Dictionary<int, long> CrashedAt = new();

        public readonly Queue<Move> MoveQueue = new();

        public static int CrashKey(int row, int train, int instance) => (row << 16) | (train << 8) | instance;

        /// <summary>Order-stable content hash for the determinism guard.</summary>
        public long StateHash()
        {
            unchecked
            {
                long h = 1469598103934665603;
                void Mix(long v) { h = (h ^ v) * 1099511628211; }
                Mix(Tick); Mix(PlayerRow); Mix((long)(PlayerX * 4096f)); Mix((int)Facing);
                Mix(Riding ? 1 : 0); Mix(RideTrain); Mix(RideInstance); Mix((long)(RideOffset * 4096f));
                Mix(StunTicksLeft); Mix(HopCooldown); Mix(RespawnDelay); Mix((int)LastDeath); Mix(Deaths);
                Mix(ClockStarted ? 1 : 0); Mix(ClockTicks); Mix(Completed ? 1 : 0);
                foreach (int b in BaysFilled.OrderBy(x => x)) Mix(b);
                foreach (var kv in CrashedAt.OrderBy(k => k.Key)) { Mix(kv.Key); Mix(kv.Value); }
                foreach (var m in MoveQueue) Mix((int)m);
                return h;
            }
        }
    }
}
