using System.Collections.Generic;
using System.IO;
using System.Linq;
using FrogAcross.Pieces;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace FrogAcross.Editor.Art
{
    /// <summary>
    /// #46: turns the extracted PNG library into wired game art —
    /// import settings, def sprite assignments (by naming convention), and the
    /// sprite atlas. Idempotent; rerun after re-extraction.
    ///
    /// Naming: char-{id}-{facing} · {vehicle}-{livery}-{dir} · {train}-{dir} ·
    /// {rider}-{livery}-{dir}-f{n} · {crash*}-{livery}-{dir}[-f{n}] ·
    /// log*/gator*-{dir} · raft/turtle-log/lily-pad · ob-{id} · lane-{kind}.
    /// Def sprite arrays: [0..] = right-facing frames, then left-facing frames
    /// (or the four facings for characters: up, down, left, right).
    /// </summary>
    public static class SpriteLibraryImporter
    {
        public const string Folder = "Assets/Art/Sprites/extracted";
        public const string AtlasPath = "Assets/Art/Sprites/FrogAcross.spriteatlas";
        public const float PixelsPerCell = 50f; // design lanes are 50px deep = 1 cell

        [MenuItem("FrogAcross/Art/Import Sprite Library")]
        public static void ImportAll()
        {
            ConfigureImports();
            WireDefs();
            BuildAtlas();
            AssetDatabase.SaveAssets();
            Debug.Log("[SpriteLibraryImporter] import + wiring + atlas complete.");
        }

        private static void ConfigureImports()
        {
            foreach (string path in Directory.GetFiles(Folder, "*.png"))
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(path.Replace('\\', '/'));
                if (importer == null) continue;
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = PixelsPerCell;
                importer.filterMode = FilterMode.Bilinear;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }
        }

        private static Sprite Load(string name)
        {
            var s = AssetDatabase.LoadAssetAtPath<Sprite>($"{Folder}/{name}.png");
            if (s == null) Debug.LogWarning($"[SpriteLibraryImporter] missing sprite {name}");
            return s;
        }

        private static void WireDefs()
        {
            var reg = PieceRegistry.Load();
            string[] liveries = { "blue", "red", "green", "purple" };

            foreach (var ch in reg.All<CharacterDef>())
                Set(ch, new[] { $"char-{ch.id}-up", $"char-{ch.id}-down", $"char-{ch.id}-left", $"char-{ch.id}-right" });

            foreach (var def in reg.All<LaneObjectDef>())
            {
                List<string> names = def.id switch
                {
                    "truck" or "car" or "bus" =>
                        Dirs(d => liveries.Select(l => $"{def.id}-{l}-{d}")),
                    "convertible" =>
                        Dirs(d => liveries.Append("black").Select(l => $"convertible-{l}-{d}")),
                    "freight" or "passenger" => new List<string> { $"{def.id}-right", $"{def.id}-left" },
                    "cyclist" or "skater" or "runner" =>
                        Dirs(d => liveries.SelectMany(l => Enumerable.Range(0, 3).Select(f => $"{def.id}-{l}-{d}-f{f}"))),
                    "log-short" or "log" or "log-long" or "gator" =>
                        new List<string> { $"{def.id}-right", $"{def.id}-left" },
                    "turtle-log" => new List<string> { "turtle-log" },
                    "raft" => new List<string> { "raft" },
                    "lily-pad" => new List<string> { "lily-pad" },
                    _ => new List<string>(),
                };
                if (def.id == "gator")
                    names.AddRange(new[] { "gator-open-right", "gator-open-left" });
                if (names.Count > 0) Set(def, names.ToArray());
            }

            foreach (var ob in reg.All<ObstructionDef>())
                Set(ob, new[] { $"ob-{ob.id}" });

            foreach (var kind in reg.All<LaneKindDef>())
            {
                string name = kind.id == "bank" ? "lane-grass" : $"lane-{kind.id}";
                Set(kind, new[] { name });
            }
        }

        private static List<string> Dirs(System.Func<string, IEnumerable<string>> per)
            => per("right").Concat(per("left")).ToList();

        private static void Set(PieceDef def, string[] names)
        {
            def.sprites = names.Select(Load).ToArray();
            EditorUtility.SetDirty(def);
        }

        private static void BuildAtlas()
        {
            var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(AtlasPath);
            if (atlas == null)
            {
                atlas = new SpriteAtlas();
                AssetDatabase.CreateAsset(atlas, AtlasPath);
            }
            atlas.Remove(atlas.GetPackables());
            var folder = AssetDatabase.LoadAssetAtPath<Object>(Folder);
            atlas.Add(new[] { folder });
            EditorUtility.SetDirty(atlas);
        }
    }
}
