using System.Collections.Generic;
using System.IO;
using FrogAcross.Editor.Solver;
using FrogAcross.Levels;
using FrogAcross.Pieces;
using FrogAcross.Sim;
using FrogAcross.View;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FrogAcross.Editor.Art
{
    /// <summary>
    /// #67: Play-listing screenshots from the REAL game — real level data, the
    /// shipped BoardView renderer, and a player state reached by replaying the
    /// solver's own proof-line two-thirds of the way. 1920×1080 landscape.
    /// </summary>
    public static class StoreCapture
    {
        public const string OutFolder = "ArtSource/store/screenshots";

        private static readonly (string id, string tag)[] Shots =
        {
            ("level-003", "teaching-road"),
            ("level-015", "river"),
            ("level-025", "swamp"),
            ("level-035", "tracks"),
            ("level-045", "bike-lane"),
            ("level-055", "walkway"),
            ("level-090", "endgame"),
        };

        [MenuItem("FrogAcross/Art/Capture store screenshots")]
        public static void CaptureAll()
        {
            int n = 1;
            foreach (var (id, tag) in Shots)
            {
                Capture(id, $"{OutFolder}/{n:D2}-{tag}.png");
                n++;
            }
            Debug.Log($"[StoreCapture] {Shots.Length} screenshots written to {OutFolder}");
        }

        private static void Capture(string levelId, string outPath)
        {
            // NewScene purges loaded assets — the registry must load after it
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var level = LevelLoader.LoadFromResources(levelId, PieceRegistry.Load());
            var sim = new GameSim(level);

            // replay the solver's proven line 2/3 through: an authentic mid-run state
            var solve = LevelSolver.Solve(level, allowDiagonals: false, 250_000, 10_800);
            if (solve.Solved && solve.Script.Count > 2)
            {
                var script = solve.Script;
                long endTick = script[script.Count * 2 / 3].tick;
                int si = 0;
                while (sim.State.Tick < endTick && !sim.State.Completed)
                {
                    while (si < script.Count && script[si].tick == sim.State.Tick)
                        sim.EnqueueMove(script[si++].move);
                    sim.Tick();
                }
            }

            var boardGo = new GameObject("board");
            var view = boardGo.AddComponent<BoardView>();
            view.Bind(sim);
            view.Render(sim.State.Tick);

            var camGo = new GameObject("cam");
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.157f, 0.373f); // brand navy
            float cx = (level.Columns - 1) / 2f;
            float cy = -(level.Rows.Count - 1) / 2f;
            // fit height with a slim margin; 16:9 width follows
            cam.orthographicSize = level.Rows.Count / 2f + 0.6f;
            camGo.transform.SetPositionAndRotation(new Vector3(cx, cy, -10f), Quaternion.identity);

            var rt = new RenderTexture(1920, 1080, 24);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            cam.targetTexture = null;
            RenderTexture.active = null;
            Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
            File.WriteAllBytes(outPath, tex.EncodeToPNG());
            Debug.Log($"[StoreCapture] {outPath} ({levelId} @ tick {sim.State.Tick})");
        }
    }
}
