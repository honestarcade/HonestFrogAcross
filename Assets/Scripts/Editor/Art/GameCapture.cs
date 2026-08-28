using System.IO;
using FrogAcross.Levels;
using FrogAcross.Pieces;
using FrogAcross.Sim;
using FrogAcross.View;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FrogAcross.Editor.Art
{
    /// <summary>Headless board screenshot on a chosen level after N ticks — the
    /// visual-evidence artifact for the lane-slice stories.</summary>
    public static class GameCapture
    {
        [MenuItem("FrogAcross/Art/Capture Board (dev-full)")]
        public static void CaptureDevFull() => Capture("dev-full", 260, "Builds/board-dev-full.png");

        public static void Capture(string levelId, int ticks, string outPath)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var sim = new GameSim(LevelLoader.LoadFromResources(levelId, PieceRegistry.Load()));
            for (int i = 0; i < ticks; i++) sim.Tick();

            var boardGo = new GameObject("board");
            var view = boardGo.AddComponent<BoardView>();
            view.Bind(sim);
            view.Render(sim.State.Tick);

            var camGo = new GameObject("cam");
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.086f, 0.196f, 0.121f);
            float cx = (sim.Level.Columns - 1) / 2f;
            float cy = -(sim.Level.Rows.Count - 1) / 2f;
            cam.orthographicSize = sim.Level.Rows.Count / 2f + 0.8f;
            camGo.transform.SetPositionAndRotation(new Vector3(cx, cy, -10f), Quaternion.Euler(0f, 0f, -8f));

            var rt = new RenderTexture(1500, 900, 24);
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
            Debug.Log($"[GameCapture] {outPath} written ({levelId} @ tick {sim.State.Tick})");
        }
    }
}
