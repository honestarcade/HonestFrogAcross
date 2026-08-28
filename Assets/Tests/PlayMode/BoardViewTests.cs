using System.Collections;
using System.Linq;
using FrogAcross.Levels;
using FrogAcross.Pieces;
using FrogAcross.Sim;
using FrogAcross.View;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace FrogAcross.Tests.PlayMode
{
    public class BoardViewTests
    {
        private GameObject _root;

        [TearDown]
        public void Teardown()
        {
            if (_root != null) Object.Destroy(_root);
        }

        private (GameSim sim, BoardView view) Spawn(string levelId = "dev-001")
        {
            var sim = new GameSim(LevelLoader.LoadFromResources(levelId, PieceRegistry.Load()));
            _root = new GameObject("board");
            var view = _root.AddComponent<BoardView>();
            view.Bind(sim);
            return (sim, view);
        }

        [UnityTest]
        public IEnumerator Composition_MatchesLevelJson()
        {
            var (sim, view) = Spawn();
            yield return null;
            var names = Enumerable.Range(0, view.transform.childCount)
                .Select(i => view.transform.GetChild(i).name).ToList();
            Assert.AreEqual(10, names.Count(n => n.StartsWith("row-")), "10 lane strips");
            Assert.AreEqual(3, names.Count(n => System.Text.RegularExpressions.Regex.IsMatch(n, @"^bay-\d+$")), "3 bays (fills excluded)");
            Assert.AreEqual(4, names.Count(n => n.StartsWith("ob-")), "4 obstructions in dev-001");
            Assert.IsTrue(names.Contains("player"));
            Assert.Greater(names.Count(n => n.StartsWith("obj-")), 10, "object instances spawned");
        }

        [UnityTest]
        public IEnumerator Render_TracksSimPositionsExactly()
        {
            var (sim, view) = Spawn();
            for (int i = 0; i < 90; i++) sim.Tick();
            view.Render(sim.State.Tick);
            yield return null;

            // every visible object quad center == simulated left + size/2 (epsilon)
            int checked_ = 0;
            for (int r = 0; r < sim.Level.Rows.Count; r++)
            {
                var row = sim.Level.Rows[r];
                for (int t = 0; t < row.Trains.Count; t++)
                for (int k = 0; k < sim.InstanceCount(r, t); k++)
                {
                    float left = sim.ObjectLeftX(r, t, k, sim.State.Tick);
                    var quad = view.transform.Find($"obj-{row.Trains[t].Def.id}-{k}");
                    if (quad == null || !quad.gameObject.activeSelf) continue;
                    Assert.AreEqual(left + row.Trains[t].Def.sizeCells * 0.5f, quad.position.x, 1e-3f,
                        $"row {r} {row.Trains[t].Def.id}#{k}");
                    checked_++;
                }
            }
            Assert.Greater(checked_, 5, "sanity: several instances actually verified");
        }

        [UnityTest]
        public IEnumerator Board_ScalesToColumns()
        {
            var (simA, viewA) = Spawn();
            var strip = viewA.transform.Find("row-0-goal").GetComponent<UnityEngine.SpriteRenderer>();
            Assert.AreEqual(simA.Level.Columns + 2f, strip.size.x, 1e-3f, "tiled strip spans the board + margins");
            yield return null;
        }

        [UnityTest]
        public IEnumerator BayFill_FitsInsideItsCell()
        {
            // regression (#67 screenshots): raw character sprites are ~4 world
            // units wide — an unscaled bay fill dwarfed the goal row
            var (sim, view) = Spawn();
            foreach (int b in sim.Level.BayColumns)
            {
                var fill = view.transform.Find($"bay-fill-{b}");
                var sr = fill.GetComponent<UnityEngine.SpriteRenderer>();
                float worldWidth = sr.sprite.bounds.size.x * fill.localScale.x;
                Assert.LessOrEqual(worldWidth, 1f, $"bay-fill-{b} must fit its cell");
                Assert.Greater(worldWidth, 0.3f, $"bay-fill-{b} should be visible");
            }
            yield return null;
        }
    }
}
