using System;
using System.Linq;
using FrogAcross.Levels;
using FrogAcross.Pieces;

namespace FrogAcross.Sim
{
    public static class SimConfig
    {
        public const int TicksPerSecond = 60;
        public const int HopCooldownTicks = 9;    // ~150ms between hops; queue chains through it
        public const int RespawnDelayTicks = 30;  // death feedback window (< 1s incl. art, per #54)
        public const int QueueCap = 2;            // owner decision: current hop + 2 buffered
        public const float AttachGrace = 0.20f;   // cells of forgiveness at platform edges
    }

    /// <summary>
    /// The deterministic fixed-step core (invariant 4). Pure logic: constructed
    /// from LevelDefinition, advanced by Tick(), fed by EnqueueMove. No RNG, no
    /// wall clock, no Unity scene objects. Rendering interpolates outside.
    /// </summary>
    public sealed class GameSim
    {
        public readonly LevelDefinition Level;
        public readonly SimState State = new();

        // Per-row precomputed kinematics.
        private readonly float[] _margin;
        private readonly float[] _loop;
        private readonly int[][] _instanceCounts; // per row, per train

        public event Action<DeathCause> OnDeath;
        public event Action<int> OnBayFilled;
        public event Action OnCompleted;
        public event Action<int, int, int> OnRiderCrashed; // row, train, instance

        public GameSim(LevelDefinition level)
        {
            Level = level;
            int rows = level.Rows.Count;
            _margin = new float[rows];
            _loop = new float[rows];
            _instanceCounts = new int[rows][];
            for (int r = 0; r < rows; r++)
            {
                float maxSize = 1f;
                var trains = level.Rows[r].Trains;
                foreach (var t in trains) maxSize = Math.Max(maxSize, t.Def.sizeCells);
                _margin[r] = (float)Math.Ceiling(maxSize) + 1f;
                _loop[r] = level.Columns + 2f * _margin[r];
                _instanceCounts[r] = new int[trains.Count];
                for (int t = 0; t < trains.Count; t++)
                    _instanceCounts[r][t] = trains[t].SpacingCells <= 0f
                        ? 1
                        : Math.Max(1, (int)Math.Ceiling(_loop[r] / trains[t].SpacingCells));
            }
            ResetToBank();
        }

        public void ResetToBank()
        {
            State.PlayerRow = Level.BankRow;
            State.PlayerX = Level.StartColumn;
            State.Riding = false;
            State.Facing = Move.Forward;
            State.StunTicksLeft = 0;
            State.HopCooldown = 0;
            State.MoveQueue.Clear();
        }

        public bool EnqueueMove(Move move)
        {
            if (State.Completed || State.RespawnDelay > 0 || State.StunTicksLeft > 0) return false;
            if (State.MoveQueue.Count >= SimConfig.QueueCap) return false; // drop newest (owner cap)
            State.MoveQueue.Enqueue(move);
            if (!State.ClockStarted) State.ClockStarted = true;
            return true;
        }

        /// <summary>Left-edge X of a train instance at a tick (cells; wrapped).</summary>
        public float ObjectLeftX(int row, int train, int instance, long tick)
        {
            var r = Level.Rows[row];
            var t = r.Trains[train];
            int key = SimState.CrashKey(row, train, instance);
            long effectiveTick = State.CrashedAt.TryGetValue(key, out long crashTick) ? crashTick : tick;
            float travel = r.DirSign * r.SpeedCellsPerSec * (effectiveTick / (float)SimConfig.TicksPerSecond);
            float raw = t.OffsetCells + instance * t.SpacingCells + travel;
            float loop = _loop[row];
            float wrapped = ((raw + _margin[row]) % loop + loop) % loop - _margin[row];
            return wrapped;
        }

        public int InstanceCount(int row, int train) => _instanceCounts[row][train];

        public void Tick()
        {
            if (State.Completed) return;
            State.Tick++;
            if (State.ClockStarted) State.ClockTicks++;

            if (State.RespawnDelay > 0)
            {
                if (--State.RespawnDelay == 0) ResetToBank();
                return;
            }

            if (State.StunTicksLeft > 0)
            {
                State.StunTicksLeft--;
                State.MoveQueue.Clear(); // stun drops buffered swipes (owner rule)
            }

            if (State.HopCooldown > 0) State.HopCooldown--;

            // Carried motion: riding objects, or conveyor ground.
            var rowDef = Level.Rows[State.PlayerRow];
            if (State.Riding)
            {
                State.PlayerX = ObjectLeftX(State.PlayerRow, State.RideTrain, State.RideInstance, State.Tick) + State.RideOffset;
                var def = rowDef.Trains[State.RideTrain].Def;
                if (!def.IsRideableAtTick(State.Tick, rowDef.Trains[State.RideTrain].PhaseTicks))
                {
                    Kill(def.inactiveKills ? DeathCause.Gator : DeathCause.Water);
                    return;
                }
            }
            else if (rowDef.Kind.semantics == LaneSemantics.Conveyor)
            {
                State.PlayerX += rowDef.DirSign * rowDef.SpeedCellsPerSec / SimConfig.TicksPerSecond;
            }

            // Drift off the board kills (riding or conveyor).
            if (State.PlayerX < -0.5f || State.PlayerX > Level.Columns - 0.5f)
            {
                Kill(DeathCause.EdgeDrift);
                return;
            }

            // Consume a queued move.
            if (State.StunTicksLeft == 0 && State.HopCooldown == 0 && State.MoveQueue.Count > 0)
                ExecuteMove(State.MoveQueue.Dequeue());

            if (State.RespawnDelay > 0 || State.Completed) return;

            // Contact rules on the current row (post-move).
            CheckRowContact();
        }

        private void ExecuteMove(Move move)
        {
            State.Facing = move;
            (int dRow, int dCol) = move switch
            {
                Move.Forward => (-1, 0),
                Move.Back => (1, 0),
                Move.Left => (0, -1),
                Move.Right => (0, 1),
                Move.DiagForwardLeft => (-1, -1),
                Move.DiagForwardRight => (-1, 1),
                Move.DiagBackLeft => (1, -1),
                Move.DiagBackRight => (1, 1),
                _ => (0, 0),
            };

            int targetRow = State.PlayerRow + dRow;
            if (targetRow < 0 || targetRow > Level.BankRow) return;

            // Landing column: grid from grounded; nearest-column from drift (owner rule).
            int fromCol = NearestColumn(State.PlayerX);
            int targetCol = fromCol + dCol;
            if (targetCol < 0 || targetCol >= Level.Columns) return;

            var target = Level.Rows[targetRow];

            // Obstructions & bays block landing (diagonals: landing square only — owner rule).
            if (IsObstructed(targetRow, targetCol)) return;

            if (target.Kind.semantics == LaneSemantics.Goal)
            {
                if (!Level.BayColumns.Contains(targetCol) || State.BaysFilled.Contains(targetCol)) return;
                State.BaysFilled.Add(targetCol);
                OnBayFilled?.Invoke(targetCol);
                State.HopCooldown = SimConfig.HopCooldownTicks;
                if (State.BaysFilled.Count >= Level.BayColumns.Count)
                {
                    State.Completed = true;
                    OnCompleted?.Invoke();
                }
                else
                {
                    ResetToBank();
                }
                return;
            }

            State.PlayerRow = targetRow;
            State.HopCooldown = SimConfig.HopCooldownTicks;
            ResolveLanding(targetRow, targetCol);
        }

        /// <summary>Landing resolution — ONE path for grounded and riding moves (#42).</summary>
        public bool IsObstructed(int row, int col)
        {
            var rowDef = Level.Rows[row];
            foreach (var ob in rowDef.Obstructions)
                if (ob.Column == col && ob.Def.blocksTile) return true;
            return false;
        }

        private void ResolveLanding(int row, int targetCol)
        {
            var rowDef = Level.Rows[row];
            float landX = State.Riding || rowDef.Kind.semantics == LaneSemantics.Conveyor
                ? targetCol // nearest-column landing already snapped via NearestColumn
                : targetCol;
            State.Riding = false;
            State.PlayerX = landX;

            if (rowDef.Kind.semantics == LaneSemantics.Water)
            {
                if (TryAttach(row, landX)) return;
                Kill(DeathCause.Water);
            }
        }

        private bool TryAttach(int row, float x)
        {
            var rowDef = Level.Rows[row];
            for (int t = 0; t < rowDef.Trains.Count; t++)
            {
                var train = rowDef.Trains[t];
                var def = train.Def;
                if (def.role is not (ObjectRole.Rideable or ObjectRole.StaticSafe)) continue;
                for (int k = 0; k < _instanceCounts[row][t]; k++)
                {
                    float left = ObjectLeftX(row, t, k, State.Tick);
                    if (x < left - SimConfig.AttachGrace || x > left + def.sizeCells + SimConfig.AttachGrace) continue;

                    if (def.role == ObjectRole.StaticSafe)
                    {
                        State.PlayerX = left + def.sizeCells * 0.5f; // center on the pad
                        return true;
                    }

                    // Rideable: zone + cycle rules (owner's gator law lives in def data).
                    float frac = Math.Clamp((x - left) / def.sizeCells, 0f, 1f);
                    bool inZone = frac >= def.rideableZoneStart && frac <= def.rideableZoneEnd;
                    bool active = def.IsRideableAtTick(State.Tick, train.PhaseTicks);
                    if (!active && def.inactiveKills) { Kill(DeathCause.Gator); return true; }
                    if (!active || !inZone) continue; // not a platform here: fall through (water)

                    State.Riding = true;
                    State.RideTrain = t;
                    State.RideInstance = k;
                    State.RideOffset = x - left;
                    State.PlayerX = x;
                    return true;
                }
            }
            return false;
        }

        private void CheckRowContact()
        {
            var rowDef = Level.Rows[State.PlayerRow];
            if (State.Riding) return;
            switch (rowDef.Kind.semantics)
            {
                case LaneSemantics.DeadlyTraffic:
                    if (FindOverlap(State.PlayerRow, out _, out _, out var def1) && def1.role == ObjectRole.Kill)
                        Kill(def1.warnLeadTicks > 0 ? DeathCause.Train : DeathCause.Vehicle);
                    break;
                case LaneSemantics.CrashTraffic:
                    if (FindOverlap(State.PlayerRow, out int t, out int k, out var def2)
                        && def2.role == ObjectRole.Crashable
                        && !State.CrashedAt.ContainsKey(SimState.CrashKey(State.PlayerRow, t, k)))
                    {
                        State.CrashedAt[SimState.CrashKey(State.PlayerRow, t, k)] = State.Tick;
                        State.StunTicksLeft = def2.stunTicks;
                        State.MoveQueue.Clear();
                        OnRiderCrashed?.Invoke(State.PlayerRow, t, k);
                    }
                    break;
                case LaneSemantics.Water:
                    // Standing in water unattached: only possible transiently; kill.
                    Kill(DeathCause.Water);
                    break;
            }
        }

        private bool FindOverlap(int row, out int train, out int instance, out LaneObjectDef def)
        {
            var rowDef = Level.Rows[row];
            for (int t = 0; t < rowDef.Trains.Count; t++)
            {
                var d = rowDef.Trains[t].Def;
                for (int k = 0; k < _instanceCounts[row][t]; k++)
                {
                    float left = ObjectLeftX(row, t, k, State.Tick);
                    // hitbox inset: 0.15 cells each side (tunable at M3 art pass)
                    if (State.PlayerX > left + 0.15f && State.PlayerX < left + d.sizeCells - 0.15f)
                    {
                        train = t; instance = k; def = d;
                        return true;
                    }
                }
            }
            train = instance = -1; def = null;
            return false;
        }

        private void Kill(DeathCause cause)
        {
            State.LastDeath = cause;
            State.Deaths++;
            State.Riding = false;
            State.RespawnDelay = SimConfig.RespawnDelayTicks;
            State.MoveQueue.Clear();
            OnDeath?.Invoke(cause);
        }

        public static int NearestColumn(float x) => (int)Math.Floor(x + 0.5f);
    }
}
