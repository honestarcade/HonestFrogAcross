using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FrogAcross.Editor.Solver;
using FrogAcross.Levels;
using FrogAcross.Pieces;
using UnityEditor;
using UnityEngine;
using Random = System.Random;

namespace FrogAcross.Editor.Generator
{
    public sealed class GenerationReport
    {
        public readonly List<(string id, string json, long minTicks)> Accepted = new();
        public readonly List<(string id, string reason)> Rejected = new();
    }

    /// <summary>
    /// Param-driven level emitter, validate-and-solve gated inline: no unproven
    /// level ever hits disk. Deterministic per seed (sorted registry queries,
    /// seeded System.Random, JsonUtility serialization).
    /// </summary>
    public static class LevelGenerator
    {
        public const string DefaultParamsPath = "Assets/GameData/GeneratorParams/default.asset";
        public const string OutputFolder = "Assets/Resources/Levels";

        [MenuItem("FrogAcross/Levels/Generate (default params)")]
        public static void GenerateDefault()
        {
            var p = AssetDatabase.LoadAssetAtPath<GeneratorParams>(DefaultParamsPath);
            if (p == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(DefaultParamsPath)!);
                p = ScriptableObject.CreateInstance<GeneratorParams>();
                AssetDatabase.CreateAsset(p, DefaultParamsPath);
                AssetDatabase.SaveAssets();
            }
            var report = GenerateCandidates(p, PieceRegistry.Load());
            foreach (var (id, json, _) in report.Accepted)
                File.WriteAllText($"{OutputFolder}/{id}.json", json);
            AssetDatabase.Refresh();
            Debug.Log($"[LevelGenerator] accepted {report.Accepted.Count}, rejected {report.Rejected.Count}"
                + (report.Rejected.Count > 0
                    ? "\n" + string.Join("\n", report.Rejected.Select(r => $"  {r.id}: {r.reason}"))
                    : ""));
        }

        /// <summary>Pure generation (no file IO) — what tests exercise.</summary>
        public static GenerationReport GenerateCandidates(GeneratorParams p, PieceRegistry registry)
        {
            var report = new GenerationReport();
            for (int i = 0; i < p.count; i++)
            {
                string id = $"gen-{p.baseSeed + i:D5}";
                var (json, minTicks, _, reason) = TryGenerateOne(id, p, p.baseSeed + i, registry);
                if (json == null) report.Rejected.Add((id, reason));
                else report.Accepted.Add((id, json, minTicks));
            }
            return report;
        }

        /// <summary>
        /// One candidate from one seed through the validate+solve gate.
        /// requiredKinds (curve introductions, #61) reject candidates that
        /// missed a kind the band must teach. Returns (null, 0, reason) on
        /// rejection.
        /// </summary>
        public static (string json, long minTicks, int moves, string reason) TryGenerateOne(
            string id, GeneratorParams p, int seed, PieceRegistry registry, string[] requiredKinds = null)
        {
            var rng = new Random(seed);
            var dto = BuildCandidate(id, p, rng, registry);

            if (requiredKinds != null)
                foreach (var kind in requiredKinds)
                    if (!dto.rows.Any(r => r.kind == kind))
                        return (null, 0, 0, $"required kind missing: {kind}");

            var errors = LevelValidator.Validate(dto, registry);
            if (errors.Count > 0) return (null, 0, 0, "validation: " + errors[0]);

            var level = LevelLoader.Parse(JsonUtility.ToJson(dto), registry);
            var solve = LevelSolver.Solve(level, allowDiagonals: false,
                p.solverNodeBudget, p.solverTickBudget);
            if (!solve.Solved) return (null, 0, 0, "solver: " + solve.FailReason);

            // Provisional medals from the diagonal-free floor (#63 recalibrates).
            float minSec = solve.MinTicks / 60f;
            dto.medal = new MedalDto
            {
                gold = Round1(minSec * 2.0f),
                silver = Round1(minSec * 2.9f),
                bronze = Round1(minSec * 4.2f),
            };
            return (JsonUtility.ToJson(dto, true), solve.MinTicks, solve.Script.Count, null);
        }

        private static float Round1(float v) => (float)Math.Round(v, 1);

        private static LevelDto BuildCandidate(string id, GeneratorParams p, Random rng, PieceRegistry registry)
        {
            int columns = rng.Next(p.columns.x, p.columns.y + 1);
            int middle = rng.Next(p.middleRows.x, p.middleRows.y + 1);

            var rows = new List<RowDto> { new() { kind = "goal" } };
            for (int r = 0; r < middle; r++)
                rows.Add(BuildRow(p.laneKindPool[rng.Next(p.laneKindPool.Length)], columns, p, rng, registry));
            rows.Add(new RowDto { kind = "bank" });

            int bayN = Math.Min(rng.Next(p.bayCount.x, p.bayCount.y + 1), columns - 1);
            var bays = Enumerable.Range(0, columns).OrderBy(_ => rng.Next()).Take(bayN).OrderBy(x => x).ToArray();

            return new LevelDto
            {
                id = id,
                name = id,
                columns = columns,
                medal = new MedalDto { gold = 10, silver = 20, bronze = 30 }, // replaced post-solve
                startColumn = rng.Next(1, columns - 1),
                bays = bays,
                rows = rows.ToArray(),
            };
        }

        private static RowDto BuildRow(string kindId, int columns, GeneratorParams p, Random rng, PieceRegistry registry)
        {
            var kind = registry.Get<LaneKindDef>(kindId);
            var row = new RowDto { kind = kindId };

            switch (kind.semantics)
            {
                case LaneSemantics.SafeGround:
                {
                    var pool = registry.All<ObstructionDef>().OrderBy(d => d.id).ToList();
                    var obs = new List<ObstructionDto>();
                    for (int c = 0; c < columns; c++)
                        if (rng.NextDouble() < p.obstructionChance)
                            obs.Add(new ObstructionDto { pieceId = pool[rng.Next(pool.Count)].id, column = c });
                    while (obs.Count > columns - 3) obs.RemoveAt(rng.Next(obs.Count)); // never wall a row
                    row.obstructions = obs.ToArray();
                    return row;
                }
                case LaneSemantics.Conveyor:
                    row.dir = rng.Next(2) == 0 ? "left" : "right";
                    row.speed = Lerp(p.conveyorSpeed, rng);
                    return row;
            }

            var role = kind.semantics switch
            {
                LaneSemantics.DeadlyTraffic => ObjectRole.Kill,
                LaneSemantics.Water => ObjectRole.Rideable,
                LaneSemantics.CrashTraffic => ObjectRole.Crashable,
                _ => ObjectRole.Kill,
            };
            // Trains only fit on tracks; keep freight/passenger off roads.
            var pieces = registry.All<LaneObjectDef>()
                .Where(d => d.role == role)
                .Where(d => kindId != "road" || d.warnLeadTicks == 0)
                .Where(d => kindId != "tracks" || d.warnLeadTicks > 0)
                .OrderBy(d => d.id).ToList();

            row.dir = rng.Next(2) == 0 ? "left" : "right";
            row.speed = Lerp(kind.semantics switch
            {
                LaneSemantics.Water => p.waterSpeed,
                LaneSemantics.CrashTraffic => p.crashSpeed,
                _ => p.deadlySpeed,
            }, rng);

            int trainCount = kind.semantics == LaneSemantics.DeadlyTraffic && kindId == "tracks" ? 1 : rng.Next(1, 3);
            var trains = new List<ObjectTrainDto>();
            float cursor = 0f;
            for (int t = 0; t < trainCount; t++)
            {
                var piece = pieces[rng.Next(pieces.Count)];
                float spacing = piece.sizeCells + 1.2f + Lerp(p.spacingSlack, rng);
                trains.Add(new ObjectTrainDto
                {
                    pieceId = piece.id,
                    offset = Round2(cursor + (float)rng.NextDouble() * spacing),
                    spacing = Round2(spacing * trainCount), // interleaved trains share the loop
                    phase = piece.cycleActiveTicks > 0 ? rng.Next(piece.cycleActiveTicks + piece.cycleInactiveTicks) : 0,
                });
                cursor += spacing;
            }
            row.objects = trains.ToArray();
            return row;
        }

        private static float Lerp(Vector2 range, Random rng) =>
            Round2(range.x + (float)rng.NextDouble() * (range.y - range.x));

        private static float Round2(float v) => (float)Math.Round(v, 2);
    }
}
