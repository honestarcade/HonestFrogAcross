using System;
using System.Collections.Generic;
using System.Linq;
using FrogAcross.Levels;
using FrogAcross.Sim;

namespace FrogAcross.Editor.Solver
{
    public sealed class SolveResult
    {
        public bool Solved;
        public long MinTicks;                       // clock ticks at completion
        public List<(long tick, Move move)> Script = new();
        public int NodesExpanded;
        public string FailReason = "";
    }

    /// <summary>
    /// A* over the REAL sim (snapshot/restore) — the trust anchor for shipped
    /// content. Diagonal-free mode exists so medal calibration (#63) proves
    /// every level for tap-region players.
    /// </summary>
    public static class LevelSolver
    {
        private const int WaitTicks = 6;

        private sealed class Node
        {
            public SimState State;
            public Node Parent;
            public long MoveTick = -1;
            public Move Move;
            public long G;      // state tick
            public long F;      // G + heuristic
        }

        public static SolveResult Solve(LevelDefinition level, bool allowDiagonals,
            int nodeBudget = 250_000, long tickBudget = 10_800 /* 3 min */)
        {
            var sim = new GameSim(level);
            var result = new SolveResult();

            Move[] moves = allowDiagonals
                ? new[] { Move.Forward, Move.Left, Move.Right, Move.Back,
                          Move.DiagForwardLeft, Move.DiagForwardRight, Move.DiagBackLeft, Move.DiagBackRight }
                : new[] { Move.Forward, Move.Left, Move.Right, Move.Back };

            long Heuristic(SimState s)
            {
                int trips = level.BayColumns.Count - s.BaysFilled.Count;
                if (trips <= 0) return 0;
                // each remaining trip needs ≥ bankRow forward hops at ≥10 ticks each;
                // the current trip is partially done.
                return (s.PlayerRow + Math.Max(0, trips - 1) * level.BankRow) * (SimConfig.HopCooldownTicks + 1);
            }

            long Key(SimState s)
            {
                unchecked
                {
                    long h = 17;
                    h = h * 31 + s.Tick / WaitTicks;
                    h = h * 31 + s.PlayerRow;
                    h = h * 31 + (long)MathF.Round(s.PlayerX * 4f);
                    h = h * 31 + (s.Riding ? 1 + s.RideTrain * 64 + s.RideInstance : 0);
                    foreach (int b in s.BaysFilled.OrderBy(x => x)) h = h * 31 + b + 1;
                    h = h * 31 + (s.StunTicksLeft > 0 ? 1 : 0);
                    h = h * 31 + (s.RespawnDelay > 0 ? 1 : 0);
                    return h;
                }
            }

            // Advance until the player can act (or terminal).
            static bool AdvanceToDecision(GameSim sim, long tickBudget)
            {
                var s = sim.State;
                while (!s.Completed && s.Tick < tickBudget
                    && (s.HopCooldown > 0 || s.StunTicksLeft > 0 || s.RespawnDelay > 0 || s.MoveQueue.Count > 0))
                    sim.Tick();
                return !s.Completed && s.Tick < tickBudget;
            }

            var open = new SortedSet<(long f, long seq, Node node)>(
                Comparer<(long f, long seq, Node node)>.Create((a, b) =>
                    a.f != b.f ? a.f.CompareTo(b.f) : a.seq.CompareTo(b.seq)));
            var best = new Dictionary<long, long>();
            long seq = 0;

            AdvanceToDecision(sim, tickBudget);
            var root = new Node { State = sim.CaptureState(), G = sim.State.Tick };
            root.F = root.G + Heuristic(root.State);
            open.Add((root.F, seq++, root));

            while (open.Count > 0)
            {
                var (_, _, node) = open.Min;
                open.Remove(open.Min);
                result.NodesExpanded++;
                if (result.NodesExpanded > nodeBudget)
                {
                    result.FailReason = $"node budget {nodeBudget} exhausted";
                    return result;
                }

                foreach (var action in moves.Cast<Move?>().Append(null))
                {
                    sim.RestoreState(node.State);
                    long moveTick = sim.State.Tick;
                    if (action.HasValue)
                    {
                        if (!sim.EnqueueMove(action.Value)) continue;
                        sim.Tick(); // consume
                    }
                    else
                    {
                        for (int i = 0; i < WaitTicks; i++) sim.Tick();
                    }

                    if (sim.State.Completed)
                    {
                        result.Solved = true;
                        result.MinTicks = sim.State.ClockTicks;
                        var chain = new List<(long, Move)>();
                        if (action.HasValue) chain.Add((moveTick, action.Value));
                        for (var n = node; n != null; n = n.Parent)
                            if (n.MoveTick >= 0) chain.Add((n.MoveTick, n.Move));
                        chain.Reverse();
                        result.Script = chain;
                        return result;
                    }

                    if (!AdvanceToDecision(sim, tickBudget)) continue;

                    var childState = sim.CaptureState();
                    long key = Key(childState);
                    if (best.TryGetValue(key, out long seenTick) && seenTick <= childState.Tick) continue;
                    best[key] = childState.Tick;

                    // Wait nodes carry no move (MoveTick -1); the script rebuild
                    // walks the parent chain and keeps only real moves.
                    var child = new Node
                    {
                        State = childState,
                        Parent = node,
                        MoveTick = action.HasValue ? moveTick : -1,
                        Move = action ?? default,
                        G = childState.Tick,
                    };
                    child.F = child.G + Heuristic(childState);
                    open.Add((child.F, seq++, child));
                }
            }

            result.FailReason = result.FailReason == "" ? "search space exhausted (unsolvable within budgets)" : result.FailReason;
            return result;
        }

        /// <summary>Replay a script on a fresh sim; true if it completes.</summary>
        public static bool Replay(LevelDefinition level, List<(long tick, Move move)> script, out long clockTicks)
        {
            var sim = new GameSim(level);
            int i = 0;
            long limit = (script.Count > 0 ? script[^1].tick : 0) + 3600;
            while (!sim.State.Completed && sim.State.Tick < limit)
            {
                while (i < script.Count && script[i].tick == sim.State.Tick)
                    sim.EnqueueMove(script[i++].move);
                sim.Tick();
            }
            clockTicks = sim.State.ClockTicks;
            return sim.State.Completed;
        }
    }
}
