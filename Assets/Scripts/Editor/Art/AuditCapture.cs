using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FrogAcross.Editor.Art
{
    /// <summary>Headless screenshot of the sprite audit scene (evidence artifact).</summary>
    public static class AuditCapture
    {
        [MenuItem("FrogAcross/Art/Capture Audit Screenshot")]
        public static void Capture()
        {
            EditorSceneManager.OpenScene(AuditSceneBuilder.ScenePath);
            var cam = Camera.main;
            var rt = new RenderTexture(2200, 1900, 24);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            cam.targetTexture = null;
            RenderTexture.active = null;
            Directory.CreateDirectory("Builds");
            File.WriteAllBytes("Builds/sprite-audit.png", tex.EncodeToPNG());
            Debug.Log("[AuditCapture] Builds/sprite-audit.png written");
        }
    }
}
