using System.Collections;
using System.Linq;
using FrogAcross.Levels;
using FrogAcross.Pieces;
using FrogAcross.Sim;
using FrogAcross.UI;
using FrogAcross.View;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
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

        [UnityTearDown]
        public IEnumerator UnloadScenes() { yield return SceneCleanup.UnloadAll(); }

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
            // full-bleed since the board tilts and fills the screen (owner, 2026-08-29)
            Assert.AreEqual(FrogAcross.View.BoardView.StripWidth(simA.Level), strip.size.x, 1e-3f,
                "tiled strip runs past the board on both sides");
            Assert.Greater(strip.size.x, simA.Level.Columns + 2f, "wider than the old board-plus-margin strip");
            yield return null;
        }

        [UnityTest]
        public IEnumerator Obstructions_StayInsideTheirOwnCell()
        {
            // owner: "obstructions sometimes appear between lanes… level ten
            // with the flower pots". The raw planter art is 1.28 x 1.04 cells
            // and was drawn unscaled, so it crossed the lane line and both
            // neighbouring columns.
            var (sim, view) = Spawn("level-010");
            yield return null;
            int checkedProps = 0;
            for (int r = 0; r < sim.Level.Rows.Count; r++)
            foreach (var ob in sim.Level.Rows[r].Obstructions)
            {
                var t = view.transform.Find($"ob-{ob.Def.id}");
                Assert.That(t, Is.Not.Null, $"row {r}: {ob.Def.id} not drawn");
                var sr = t.GetComponent<UnityEngine.SpriteRenderer>();
                var size = sr.sprite.bounds.size * t.localScale.x;
                Assert.That(size.x, Is.LessThanOrEqualTo(1f), $"{ob.Def.id} is wider than its cell");
                Assert.That(size.y, Is.LessThanOrEqualTo(1f), $"{ob.Def.id} is taller than its cell");
                Assert.That(t.position.y + size.y / 2f, Is.LessThanOrEqualTo(-r + 0.5f + 1e-3f),
                    $"{ob.Def.id} pokes into the lane above");
                Assert.That(t.position.y - size.y / 2f, Is.GreaterThanOrEqualTo(-r - 0.5f - 1e-3f),
                    $"{ob.Def.id} pokes into the lane below");
                checkedProps++;
            }
            Assert.That(checkedProps, Is.GreaterThan(0), "level-010 has obstructions to check");
        }

        [UnityTest]
        public IEnumerator LandingPads_AreGreyPadsCarryingTheMark()
        {
            // owner: "landing pads are black, they should be dark grey with the
            // frogger logo on them"
            var (sim, view) = Spawn();
            yield return null;
            foreach (int b in sim.Level.BayColumns)
            {
                var pad = view.transform.Find($"bay-{b}").GetComponent<UnityEngine.SpriteRenderer>();
                Assert.That(pad.drawMode, Is.EqualTo(UnityEngine.SpriteDrawMode.Sliced), "rounded pad");
                float grey = (pad.color.r + pad.color.g + pad.color.b) / 3f;
                Assert.That(grey, Is.GreaterThan(0.15f), $"bay-{b} is still nearly black");
                Assert.That(Mathf.Max(pad.color.r, pad.color.g, pad.color.b)
                    - Mathf.Min(pad.color.r, pad.color.g, pad.color.b), Is.LessThan(0.12f),
                    $"bay-{b} should read as grey, not tinted");

                var mark = view.transform.Find($"bay-mark-{b}");
                Assert.That(mark, Is.Not.Null, $"bay-{b} carries the Frog Across mark");
                Assert.That(mark.GetComponent<UnityEngine.SpriteRenderer>().sprite, Is.Not.Null);
            }
        }

        [UnityTest]
        public IEnumerator TrafficNeverAppearsOrVanishesInView()
        {
            // owner: "cars/trucks/other objects spawn part way on the screen.
            // This should never happen, ever… same for disappearing." The
            // camera is far wider than the board, so culling objects at the
            // board margin popped them in and out in plain sight.
            AppShell.PendingLevelId = "level-090"; // tall board: the more rows,
            // the wider the fitted camera, and the further past the board margin
            // it sees (a 5-row level hid the bug behind its own zoom)
            SceneManager.LoadScene("Game");
            yield return null;
            yield return null;
            var boot = Object.FindAnyObjectByType<GameBootstrap>();
            var cam = Camera.main;
            cam.aspect = 3120f / 1440f; // the owner's panel: the widest we ship to
            boot.SendMessage("FitCamera");
            var view = Object.FindAnyObjectByType<BoardView>();

            // the rolled frame, in world X
            float halfH = cam.orthographicSize, halfW = halfH * cam.aspect;
            float roll = Mathf.Abs(GameBootstrap.BoardRollDegrees) * Mathf.Deg2Rad;
            float ext = halfW * Mathf.Cos(roll) + halfH * Mathf.Sin(roll);
            float camX = cam.transform.position.x;
            float left = camX - ext, right = camX + ext;

            // Each ROW wraps at its own margin (a car lane's is 3 cells, a
            // freight lane's is 10), while the camera reaches ~20 — so a car
            // hit its wrap point in open view and jumped to the far side.
            Assert.That(right, Is.GreaterThan(boot.Sim.Level.Columns + 3f),
                "this level must be one where the camera sees past a car lane's wrap");

            // Continuity: whatever is drawn fully on screen must still be drawn
            // within one frame of travel of where it was. Wrapping is allowed
            // to move any single sprite — what may not happen is the traffic
            // itself appearing or vanishing mid-screen.
            const float maxStep = 0.35f; // 3 ticks of the fastest lane, plus slack
            var previous = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<float>>();
            int continuityChecks = 0;
            for (int frame = 0; frame < 150; frame++)
            {
                for (int i = 0; i < 3; i++) boot.Sim.Tick();
                view.Render(boot.Sim.State.Tick);

                var current = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<float>>();
                var onScreen = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<float>>();
                foreach (Transform child in view.transform)
                {
                    if (!child.name.StartsWith("obj-")) continue;
                    var sr = child.GetComponent<UnityEngine.SpriteRenderer>();
                    if (!sr.enabled) continue;
                    // copies of one object share a key: "obj-car-2-w1" -> "obj-car-2"
                    int w = child.name.LastIndexOf("-w", System.StringComparison.Ordinal);
                    string key = w > 0 ? child.name.Substring(0, w) : child.name;
                    float half = sr.sprite != null
                        ? sr.sprite.bounds.size.x * child.localScale.x / 2f : 0.5f;
                    if (!current.TryGetValue(key, out var list))
                        current[key] = list = new System.Collections.Generic.List<float>();
                    list.Add(child.position.x);
                    if (child.position.x - half > left + 0.5f && child.position.x + half < right - 0.5f)
                    {
                        if (!onScreen.TryGetValue(key, out var vis))
                            onScreen[key] = vis = new System.Collections.Generic.List<float>();
                        vis.Add(child.position.x);
                    }
                }

                foreach (var kv in previous)
                {
                    if (!current.TryGetValue(kv.Key, out var now)) now = new System.Collections.Generic.List<float>();
                    foreach (float was in kv.Value)
                    {
                        float best = float.MaxValue;
                        foreach (float x in now) best = Mathf.Min(best, Mathf.Abs(x - was));
                        Assert.That(best, Is.LessThan(maxStep),
                            $"frame {frame}: {kv.Key} was drawn at x={was:0.00} on screen and " +
                            $"nothing of it is within {maxStep} of there now — it popped out of view");
                        continuityChecks++;
                    }
                }
                previous = onScreen;
                yield return null;
            }
            Assert.That(continuityChecks, Is.GreaterThan(500), "sanity: traffic was actually rendered");
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
