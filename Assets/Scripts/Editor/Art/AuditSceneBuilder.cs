using System.Linq;
using FrogAcross.Pieces;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FrogAcross.Editor.Art
{
    /// <summary>#46's visual QA surface: every wired sprite laid out with labels.</summary>
    public static class AuditSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/SpriteAudit.unity";

        [MenuItem("FrogAcross/Art/Rebuild Sprite Audit Scene")]
        public static void Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camGo = new GameObject("Main Camera");
            var cam = camGo.AddComponent<Camera>();
            camGo.tag = "MainCamera";
            cam.orthographic = true;
            cam.orthographicSize = 14f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.043f, 0.051f, 0.071f);
            camGo.transform.position = new Vector3(14f, -12f, -10f);

            var reg = PieceRegistry.Load();
            float y = 0f;
            foreach (var group in reg.pieces.Where(p => p != null).GroupBy(p => p.GetType().Name))
            {
                float x = 0f;
                float rowMax = 1f;
                foreach (var def in group.OrderBy(p => p.id))
                {
                    for (int i = 0; i < (def.sprites?.Length ?? 0); i++)
                    {
                        var sp = def.sprites[i];
                        if (sp == null) continue;
                        var go = new GameObject($"{def.id}[{i}]");
                        var sr = go.AddComponent<SpriteRenderer>();
                        sr.sprite = sp;
                        float w = sp.bounds.size.x;
                        go.transform.position = new Vector3(x + w / 2f, y - sp.bounds.size.y / 2f, 0f);
                        x += w + 0.4f;
                        rowMax = Mathf.Max(rowMax, sp.bounds.size.y);
                        if (x > 28f) { x = 0f; y -= rowMax + 0.6f; rowMax = 1f; }
                    }
                }
                y -= rowMax + 1.6f;
            }
            // Fit the camera to the full layout.
            var renderers = Object.FindObjectsByType<SpriteRenderer>();
            if (renderers.Length > 0)
            {
                var b = renderers[0].bounds;
                foreach (var r in renderers) b.Encapsulate(r.bounds);
                cam.orthographicSize = Mathf.Max(b.extents.y + 1f, (b.extents.x + 1f) * 0.875f);
                camGo.transform.position = new Vector3(b.center.x, b.center.y, -10f);
            }

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[AuditSceneBuilder] rebuilt {ScenePath} with {renderers.Length} sprites");
        }
    }
}
