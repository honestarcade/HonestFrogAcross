using System.Linq;
using FrogAcross.Levels;
using FrogAcross.Pieces;
using FrogAcross.Sim;
using NUnit.Framework;

namespace FrogAcross.Tests.EditMode.Sim
{
    /// <summary>
    /// Owner, 2026-09-02: "why can't the player use the whole board? The player
    /// should be able to go all the way to the edges." The movement rules are
    /// not the limit — this pins that down, so the reachable width can only
    /// ever be narrowed by level data or by the camera, never by the sim.
    /// </summary>
    public class BoardWidthTests
    {
        private static float WalkTo(GameSim sim, Move dir)
        {
            for (int i = 0; i < 400; i++)
            {
                sim.EnqueueMove(dir);
                sim.Tick();
            }
            return sim.State.PlayerX;
        }

        [Test]
        public void EveryLevel_LetsThePlayerReachBothEdgeColumns()
        {
            var registry = PieceRegistry.Load();
            foreach (int n in Enumerable.Range(1, LevelCatalog.Count))
            {
                var level = LevelLoader.LoadFromResources(LevelCatalog.IdFor(n), registry);

                var left = new GameSim(level);
                Assert.That(WalkTo(left, Move.Left), Is.EqualTo(0f).Within(0.001f),
                    $"level {n}: the player cannot reach column 0");

                var right = new GameSim(level);
                Assert.That(WalkTo(right, Move.Right), Is.EqualTo(level.Columns - 1).Within(0.001f),
                    $"level {n}: the player cannot reach column {level.Columns - 1}");
            }
        }
    }
}
