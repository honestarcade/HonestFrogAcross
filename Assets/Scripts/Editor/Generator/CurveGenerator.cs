using System;
using System.Collections.Generic;
using System.IO;
using FrogAcross.Pieces;
using UnityEditor;
using UnityEngine;

namespace FrogAcross.Editor.Generator
{
    /// <summary>
    /// #61: drives the 100-level shipped set from the DifficultyCurve asset.
    /// Deterministic: level n draws seeds baseSeed + n*1000 + attempt until a
    /// candidate passes the required-kinds + validate + solve gate, so the
    /// whole set is reproducible from curve.asset alone.
    /// </summary>
    public static class CurveGenerator
    {
        public const string CurveAssetPath = "Assets/GameData/GeneratorParams/curve.asset";
        public const string OutputFolder = "Assets/Resources/Levels";
        public const int MaxAttempts = 400;

        public sealed class GeneratedLevel
        {
            public string Id;
            public string Json;
            public long MinTicks;
            public int Attempts;
        }

        /// <summary>
        /// The full content pipeline (#61–#63): generate the set, prove and
        /// lock it, calibrate medals from the recorded floors, re-hash.
        /// </summary>
        [MenuItem("FrogAcross/Levels/Rebuild shipped content (generate + lock + calibrate)")]
        public static void RebuildShippedContent()
        {
            GenerateAll();
            ContentLock.RegenerateFixture();
            MedalCalibrator.CalibrateAll();
            ContentLock.RefreshHashes();
            Debug.Log("[CurveGenerator] shipped content rebuilt: levels + fixture + medals in sync");
        }

        [MenuItem("FrogAcross/Levels/Generate curve set (100 levels)")]
        public static void GenerateAll()
        {
            var curve = LoadOrCreateCurve();
            var set = GenerateSet(curve, PieceRegistry.Load());
            foreach (var lvl in set)
                File.WriteAllText($"{OutputFolder}/{lvl.Id}.json", lvl.Json);
            AssetDatabase.Refresh();
            Debug.Log($"[CurveGenerator] wrote {set.Count} levels to {OutputFolder}");
        }

        public static DifficultyCurve LoadOrCreateCurve()
        {
            var curve = AssetDatabase.LoadAssetAtPath<DifficultyCurve>(CurveAssetPath);
            if (curve != null) return curve;
            curve = ScriptableObject.CreateInstance<DifficultyCurve>();
            curve.bands = DefaultBands();
            Directory.CreateDirectory(Path.GetDirectoryName(CurveAssetPath)!);
            AssetDatabase.CreateAsset(curve, CurveAssetPath);
            AssetDatabase.SaveAssets();
            return curve;
        }

        /// <summary>Pure (no file IO). Throws if any level exhausts its attempts.</summary>
        public static List<GeneratedLevel> GenerateSet(DifficultyCurve curve, PieceRegistry registry)
        {
            var result = new List<GeneratedLevel>();
            for (int n = 1; n <= curve.levelCount; n++)
            {
                var p = curve.ParamsForLevel(n);
                var band = Array.Find(curve.bands, b => n >= b.startLevel && n <= b.endLevel);
                string id = $"level-{n:D3}";
                GeneratedLevel accepted = null;
                string lastReason = "no attempts";
                for (int a = 0; a < MaxAttempts; a++)
                {
                    var (json, minTicks, moves, reason) = LevelGenerator.TryGenerateOne(
                        id, p, curve.baseSeed + n * 1000 + a, registry, band.requiredKinds);
                    if (json != null && band.maxSolverMoves > 0 && moves > band.maxSolverMoves)
                    {
                        json = null;
                        reason = $"solver line too long: {moves} > {band.maxSolverMoves} moves";
                    }
                    if (json != null)
                    {
                        accepted = new GeneratedLevel { Id = id, Json = json, MinTicks = minTicks, Attempts = a + 1 };
                        break;
                    }
                    lastReason = reason;
                }
                UnityEngine.Object.DestroyImmediate(p);
                if (accepted == null)
                    throw new InvalidOperationException(
                        $"{id}: no candidate accepted in {MaxAttempts} attempts (last: {lastReason})");
                result.Add(accepted);
            }
            return result;
        }

        /// <summary>
        /// The shipped schedule (#61): teaching road ramp, one new lane kind
        /// per decade — river 11, swamp 21, tracks 31, bike 41, walkway 51 —
        /// then combination boards deepening through 100.
        /// </summary>
        public static CurveBand[] DefaultBands() => new[]
        {
            new CurveBand
            {
                label = "first-hop", startLevel = 1, endLevel = 1,
                laneKindPool = new[] { "road", "road", "grass" },
                requiredKinds = new[] { "road" },
                middleRowsStartEnd = new Vector2Int(3, 3),
                bayCountStartEnd = new Vector2Int(1, 1), // one bay: one crossing (#64)
                maxSolverMoves = 7, // near-straight line: rows(5) + 2
                columnsRange = new Vector2Int(9, 10),
                deadlySpeedStart = new Vector2(1.0f, 1.2f), deadlySpeedEnd = new Vector2(1.0f, 1.2f),
                spacingSlackStart = new Vector2(3.5f, 4.5f), spacingSlackEnd = new Vector2(3.5f, 4.5f),
                obstructionChanceStart = 0f, obstructionChanceEnd = 0f,
            },
            new CurveBand
            {
                label = "teaching-road", startLevel = 2, endLevel = 10,
                laneKindPool = new[] { "road", "road", "grass" },
                requiredKinds = new[] { "road" },
                middleRowsStartEnd = new Vector2Int(5, 6),
                bayCountStartEnd = new Vector2Int(2, 3),
                columnsRange = new Vector2Int(9, 11),
                deadlySpeedStart = new Vector2(1.0f, 1.3f), deadlySpeedEnd = new Vector2(1.4f, 1.8f),
                spacingSlackStart = new Vector2(3.0f, 4.5f), spacingSlackEnd = new Vector2(2.2f, 3.6f),
                obstructionChanceStart = 0f, obstructionChanceEnd = 0.08f,
            },
            new CurveBand
            {
                label = "river", startLevel = 11, endLevel = 20,
                laneKindPool = new[] { "road", "grass", "river", "river" },
                requiredKinds = new[] { "river" },
                middleRowsStartEnd = new Vector2Int(5, 7),
                bayCountStartEnd = new Vector2Int(2, 3),
                columnsRange = new Vector2Int(9, 11),
                deadlySpeedStart = new Vector2(1.3f, 1.8f), deadlySpeedEnd = new Vector2(1.5f, 2.1f),
                waterSpeedStart = new Vector2(0.9f, 1.2f), waterSpeedEnd = new Vector2(1.0f, 1.5f),
                spacingSlackStart = new Vector2(2.4f, 3.8f), spacingSlackEnd = new Vector2(2.0f, 3.4f),
                obstructionChanceStart = 0.06f, obstructionChanceEnd = 0.1f,
            },
            new CurveBand
            {
                label = "swamp", startLevel = 21, endLevel = 30,
                laneKindPool = new[] { "road", "grass", "river", "swamp", "swamp" },
                requiredKinds = new[] { "swamp" },
                middleRowsStartEnd = new Vector2Int(6, 7),
                bayCountStartEnd = new Vector2Int(2, 3),
                columnsRange = new Vector2Int(9, 12),
                deadlySpeedStart = new Vector2(1.4f, 1.9f), deadlySpeedEnd = new Vector2(1.6f, 2.2f),
                waterSpeedStart = new Vector2(1.0f, 1.4f), waterSpeedEnd = new Vector2(1.1f, 1.6f),
                spacingSlackStart = new Vector2(2.2f, 3.6f), spacingSlackEnd = new Vector2(1.9f, 3.2f),
                obstructionChanceStart = 0.08f, obstructionChanceEnd = 0.12f,
            },
            new CurveBand
            {
                label = "tracks", startLevel = 31, endLevel = 40,
                laneKindPool = new[] { "road", "grass", "river", "swamp", "tracks", "concrete" },
                requiredKinds = new[] { "tracks" },
                middleRowsStartEnd = new Vector2Int(6, 8),
                bayCountStartEnd = new Vector2Int(2, 4),
                columnsRange = new Vector2Int(10, 12),
                deadlySpeedStart = new Vector2(1.5f, 2.0f), deadlySpeedEnd = new Vector2(1.7f, 2.4f),
                waterSpeedStart = new Vector2(1.0f, 1.5f), waterSpeedEnd = new Vector2(1.2f, 1.7f),
                spacingSlackStart = new Vector2(2.0f, 3.4f), spacingSlackEnd = new Vector2(1.8f, 3.0f),
                obstructionChanceStart = 0.1f, obstructionChanceEnd = 0.14f,
            },
            new CurveBand
            {
                label = "bike", startLevel = 41, endLevel = 50,
                laneKindPool = new[] { "road", "grass", "river", "swamp", "tracks", "bike", "bike", "concrete" },
                requiredKinds = new[] { "bike" },
                middleRowsStartEnd = new Vector2Int(7, 8),
                bayCountStartEnd = new Vector2Int(3, 4),
                columnsRange = new Vector2Int(10, 12),
                deadlySpeedStart = new Vector2(1.6f, 2.2f), deadlySpeedEnd = new Vector2(1.8f, 2.5f),
                waterSpeedStart = new Vector2(1.1f, 1.6f), waterSpeedEnd = new Vector2(1.2f, 1.8f),
                crashSpeedStart = new Vector2(1.8f, 2.4f), crashSpeedEnd = new Vector2(2.0f, 2.8f),
                spacingSlackStart = new Vector2(1.9f, 3.2f), spacingSlackEnd = new Vector2(1.7f, 2.9f),
                obstructionChanceStart = 0.12f, obstructionChanceEnd = 0.16f,
            },
            new CurveBand
            {
                label = "walkway", startLevel = 51, endLevel = 60,
                laneKindPool = new[] { "road", "grass", "river", "swamp", "tracks", "bike", "walkway", "walkway", "concrete" },
                requiredKinds = new[] { "walkway" },
                middleRowsStartEnd = new Vector2Int(7, 9),
                bayCountStartEnd = new Vector2Int(3, 4),
                columnsRange = new Vector2Int(10, 13),
                deadlySpeedStart = new Vector2(1.7f, 2.3f), deadlySpeedEnd = new Vector2(1.9f, 2.6f),
                waterSpeedStart = new Vector2(1.1f, 1.7f), waterSpeedEnd = new Vector2(1.3f, 1.8f),
                crashSpeedStart = new Vector2(2.0f, 2.6f), crashSpeedEnd = new Vector2(2.2f, 2.9f),
                conveyorSpeedStart = new Vector2(1.0f, 1.4f), conveyorSpeedEnd = new Vector2(1.2f, 1.8f),
                spacingSlackStart = new Vector2(1.8f, 3.0f), spacingSlackEnd = new Vector2(1.6f, 2.8f),
                obstructionChanceStart = 0.14f, obstructionChanceEnd = 0.18f,
            },
            new CurveBand
            {
                label = "combination-1", startLevel = 61, endLevel = 80,
                laneKindPool = new[] { "road", "river", "swamp", "tracks", "bike", "walkway", "grass", "concrete" },
                middleRowsStartEnd = new Vector2Int(8, 9),
                bayCountStartEnd = new Vector2Int(3, 5),
                columnsRange = new Vector2Int(11, 13),
                deadlySpeedStart = new Vector2(1.9f, 2.6f), deadlySpeedEnd = new Vector2(2.1f, 2.9f),
                waterSpeedStart = new Vector2(1.2f, 1.8f), waterSpeedEnd = new Vector2(1.4f, 1.9f),
                crashSpeedStart = new Vector2(2.2f, 2.8f), crashSpeedEnd = new Vector2(2.4f, 3.0f),
                conveyorSpeedStart = new Vector2(1.2f, 1.7f), conveyorSpeedEnd = new Vector2(1.4f, 2.0f),
                spacingSlackStart = new Vector2(1.6f, 2.8f), spacingSlackEnd = new Vector2(1.4f, 2.5f),
                obstructionChanceStart = 0.16f, obstructionChanceEnd = 0.2f,
            },
            new CurveBand
            {
                label = "combination-2", startLevel = 81, endLevel = 100,
                laneKindPool = new[] { "road", "river", "swamp", "tracks", "bike", "walkway", "concrete" },
                middleRowsStartEnd = new Vector2Int(9, 10),
                bayCountStartEnd = new Vector2Int(4, 5),
                columnsRange = new Vector2Int(11, 13),
                deadlySpeedStart = new Vector2(2.1f, 2.8f), deadlySpeedEnd = new Vector2(2.3f, 3.2f),
                waterSpeedStart = new Vector2(1.3f, 1.9f), waterSpeedEnd = new Vector2(1.5f, 2.0f),
                crashSpeedStart = new Vector2(2.4f, 3.0f), crashSpeedEnd = new Vector2(2.6f, 3.2f),
                conveyorSpeedStart = new Vector2(1.4f, 1.9f), conveyorSpeedEnd = new Vector2(1.6f, 2.2f),
                spacingSlackStart = new Vector2(1.5f, 2.6f), spacingSlackEnd = new Vector2(1.2f, 2.2f),
                obstructionChanceStart = 0.18f, obstructionChanceEnd = 0.24f,
            },
        };
    }
}
