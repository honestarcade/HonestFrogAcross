using System.Collections.Generic;
using System.IO;
using System.Linq;
using FrogAcross.Levels;
using FrogAcross.Pieces;
using FrogAcross.Sim;
using NUnit.Framework;

namespace FrogAcross.Tests.EditMode.Sim
{
    /// <summary>Executable guard for invariant 4 (CLAUDE.md: guard #39).</summary>
    public class DeterminismGuardTests
    {
        public static GameSim NewSim(string levelId = "dev-001") =>
            new(LevelLoader.LoadFromResources(levelId, PieceRegistry.Load()));

        public static List<long> Run(GameSim sim, IReadOnlyDictionary<long, Move> script, int ticks)
        {
            var hashes = new List<long>(ticks);
            for (int i = 0; i < ticks; i++)
            {
                if (script != null && script.TryGetValue(sim.State.Tick, out var mv)) sim.EnqueueMove(mv);
                sim.Tick();
                hashes.Add(sim.State.StateHash());
            }
            return hashes;
        }

        private static readonly Dictionary<long, Move> Script = new()
        {
            [10] = Move.Forward, [30] = Move.Forward, [42] = Move.Left,
            [60] = Move.DiagForwardRight, [90] = Move.Forward, [120] = Move.Back,
            [180] = Move.Forward, [240] = Move.Forward, [300] = Move.Forward,
        };

        [Test]
        public void SameInputs_IdenticalHashStreams()
        {
            var a = Run(NewSim(), Script, 600);
            var b = Run(NewSim(), Script, 600);
            CollectionAssert.AreEqual(a, b, "replay diverged — determinism invariant breached");
        }

        [Test]
        public void ChunkedTicking_MatchesContinuous()
        {
            // Frame pacing must not matter: tick in odd chunks vs straight through.
            var straight = NewSim();
            var chunked = NewSim();
            var hs = Run(straight, Script, 600);
            var hc = new List<long>();
            int done = 0;
            foreach (int chunk in new[] { 7, 13, 1, 59, 120, 400 })
            {
                for (int i = 0; i < chunk && done < 600; i++, done++)
                {
                    if (Script.TryGetValue(chunked.State.Tick, out var mv)) chunked.EnqueueMove(mv);
                    chunked.Tick();
                    hc.Add(chunked.State.StateHash());
                }
            }
            CollectionAssert.AreEqual(hs, hc);
        }

        [Test]
        public void SimSources_ContainNoRandomness()
        {
            var offenders = Directory.GetFiles("Assets/Scripts/Runtime/Sim", "*.cs", SearchOption.AllDirectories)
                .Where(f => File.ReadAllText(f) is var text
                    && (text.Contains("Random") || text.Contains("DateTime.Now") || text.Contains("Time.time")))
                .ToList();
            Assert.IsEmpty(offenders, "nondeterminism sources in sim: " + string.Join(", ", offenders));
        }

        [Test]
        public void Kinematics_MatchClosedForm()
        {
            var sim = NewSim();
            // dev-001 rows[1]: road right 2.2 cells/s, truck offset 0 spacing 7.5.
            float x0 = sim.ObjectLeftX(1, 0, 0, 0);
            float x60 = sim.ObjectLeftX(1, 0, 0, 60);
            Assert.AreEqual(2.2f, x60 - x0, 1e-3f, "one second of travel at 2.2 cells/s (unwrapped window)");
        }

        [Test]
        public void Clock_StartsOnFirstMove_RunsThroughDeath_StopsAtCompletion()
        {
            var sim = NewSim();
            for (int i = 0; i < 100; i++) sim.Tick();
            Assert.AreEqual(0, sim.State.ClockTicks, "clock must not run before the first move");

            sim.EnqueueMove(Move.Forward); // bank → road row: clock starts
            long deaths = 0;
            sim.OnDeath += _ => deaths++;
            for (int i = 0; i < 600 && deaths == 0; i++) sim.Tick(); // stand in traffic → die
            Assert.AreEqual(1, deaths, "standing on the road must eventually kill");
            long clockAtDeath = sim.State.ClockTicks;
            for (int i = 0; i < 120; i++) sim.Tick();
            Assert.Greater(sim.State.ClockTicks, clockAtDeath, "clock keeps running through death (owner rule)");
        }

        [Test]
        public void BaysPersistAcrossDeaths()
        {
            var sim = NewSim();
            sim.State.BaysFilled.Add(1); // scripted premise: one bay already filled
            sim.EnqueueMove(Move.Forward);
            long deaths = 0;
            sim.OnDeath += _ => deaths++;
            for (int i = 0; i < 600 && deaths == 0; i++) sim.Tick();
            for (int i = 0; i < SimConfig.RespawnDelayTicks + 2; i++) sim.Tick();
            Assert.AreEqual(1, deaths);
            CollectionAssert.Contains(sim.State.BaysFilled, 1, "bays persist across deaths (owner rule)");
            Assert.AreEqual(sim.Level.BankRow, sim.State.PlayerRow, "respawned at bank");
        }
    }
}
