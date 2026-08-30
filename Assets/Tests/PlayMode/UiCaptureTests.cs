using System.Collections;
using System.IO;
using FrogAcross.Levels;
using FrogAcross.Pieces;
using FrogAcross.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace FrogAcross.Tests.PlayMode
{
    /// <summary>
    /// #84/#85/#88/#91: renders every shell screen (and the completion
    /// overlay) at the 1920×1080 reference into Builds/ui/*.png — the
    /// visual-review artifacts for layout work, and a smoke test that every
    /// screen builds. Batchmode-safe: canvases render through an RT camera.
    /// </summary>
    public class UiCaptureTests
    {
        private static readonly WaitForSeconds Wait1_3 = new WaitForSeconds(1.3f);

        [UnityTearDown]
        public IEnumerator UnloadScenes() { yield return SceneCleanup.UnloadAll(); }

        private static void CaptureCanvas(Canvas canvas, string outPath)
            => CaptureCanvas(canvas, outPath, 1920, 1080);

        private static void CaptureCanvas(Canvas canvas, string outPath, int w, int h)
        {
            var camGo = new GameObject("ui-capture-cam");
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = UiKit.Navy;
            cam.cullingMask = LayerMask.GetMask("UI") | 1; // default + UI

            var rt = new RenderTexture(w, h, 24);
            cam.targetTexture = rt;

            var prevMode = canvas.renderMode;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 1f;
            Canvas.ForceUpdateCanvases();
            cam.Render();

            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            cam.targetTexture = null;
            canvas.renderMode = prevMode;
            canvas.worldCamera = null;

            Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
            File.WriteAllBytes(outPath, tex.EncodeToPNG());
            Object.Destroy(camGo);
            Object.Destroy(rt);
            Object.Destroy(tex);
        }

        [UnityTest]
        public IEnumerator CaptureEveryScreen()
        {
            SceneManager.LoadScene("Shell");
            yield return null;
            var shell = Object.FindAnyObjectByType<AppShell>();
            yield return Wait1_3;
            var canvas = GameObject.Find("shell-canvas").GetComponent<Canvas>();
            var scaler = canvas.GetComponent<CanvasScaler>();
            Assert.That(scaler, Is.Not.Null, "shell canvas must scale with screen size (#84)");
            Assert.That(scaler.referenceResolution, Is.EqualTo(new Vector2(1920, 1080)));

            foreach (var screen in new[] { "loading", "menu", "levels", "character", "about", "gameplay", "settings", "studio" })
            {
                shell.Push(screen);
                yield return null;
                CaptureCanvas(canvas, $"Builds/ui/{screen}.png");
                // the owner's device (S26 Ultra class): 3120×1440 — wider than
                // 16:9, so side spill and anchor bugs show here first
                CaptureCanvas(canvas, $"Builds/ui/device/{screen}.png", 3120, 1440);
                Assert.That(File.Exists($"Builds/ui/{screen}.png"), Is.True, screen);
            }

            // the completion overlay, with real-looking data
            var host = new GameObject("overlay-host");
            var overlay = host.AddComponent<LevelCompleteOverlay>();
            var level = LevelLoader.LoadFromResources("level-001", PieceRegistry.Load());
            overlay.Show(level, 260, newBest: true, prevBest: 6.8f, levelNumber: 13);
            yield return null;
            bool sawFullTitle = false;
            foreach (var text in host.GetComponentsInChildren<UnityEngine.UI.Text>(true))
                if (text.text == "Level 13 Complete") sawFullTitle = true;
            Assert.That(sawFullTitle, Is.True, "overlay title must read 'Level N Complete', untruncated");

            var overlayCanvas = host.GetComponentInChildren<Canvas>();
            Assert.That(overlayCanvas.GetComponent<CanvasScaler>(), Is.Not.Null,
                "the overlay's canvas must scale too (#88 — it used to render raw pixels)");
            CaptureCanvas(overlayCanvas, "Builds/ui/overlay.png");
            CaptureCanvas(overlayCanvas, "Builds/ui/device/overlay.png", 3120, 1440);
            Assert.That(File.Exists("Builds/ui/overlay.png"), Is.True);
            Object.Destroy(host);
        }

        [UnityTest]
        public IEnumerator CaptureBoardAtDeviceResolution()
        {
            yield return CaptureBoard("level-001", "board");
            yield return CaptureBoard("level-090", "board-tall");
        }

        private static IEnumerator CaptureBoard(string levelId, string name)
        {
            AppShell.PendingLevelId = levelId;
            SceneManager.LoadScene("Game");
            yield return null;
            yield return null;
            var boot = Object.FindAnyObjectByType<FrogAcross.View.GameBootstrap>();
            var cam = Camera.main;
            var rt = new RenderTexture(3120, 1440, 24);
            cam.targetTexture = rt;
            cam.aspect = 3120f / 1440f;
            boot.SendMessage("FitCamera", SendMessageOptions.DontRequireReceiver);

            // every board corner must sit inside the rolled frame
            var level = boot.Sim.Level;
            float halfW = cam.orthographicSize * cam.aspect, halfH = cam.orthographicSize;
            foreach (var corner in new[]
                     {
                         new Vector3(-0.5f, 0.5f, 0f),
                         new Vector3(level.Columns - 0.5f, 0.5f, 0f),
                         new Vector3(-0.5f, -(level.BankRow + 0.5f), 0f),
                         new Vector3(level.Columns - 0.5f, -(level.BankRow + 0.5f), 0f),
                     })
            {
                var local = cam.transform.InverseTransformPoint(corner);
                Assert.That(Mathf.Abs(local.y), Is.LessThanOrEqualTo(halfH + 0.001f),
                    $"{levelId}: corner {corner} falls outside the rolled frame vertically");
                Assert.That(Mathf.Abs(local.x), Is.LessThanOrEqualTo(halfW + 0.001f),
                    $"{levelId}: corner {corner} falls outside the rolled frame horizontally");
            }
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(3120, 1440, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 3120, 1440), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            cam.targetTexture = null;
            Directory.CreateDirectory("Builds/ui/device");
            File.WriteAllBytes($"Builds/ui/device/{name}.png", tex.EncodeToPNG());
            Assert.That(File.Exists($"Builds/ui/device/{name}.png"), Is.True);
            Object.Destroy(rt);
            Object.Destroy(tex);
        }
    }
}
