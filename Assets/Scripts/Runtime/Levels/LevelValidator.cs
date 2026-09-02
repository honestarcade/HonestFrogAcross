using System.Collections.Generic;
using System.Linq;
using FrogAcross.Pieces;

namespace FrogAcross.Levels
{
    /// <summary>
    /// The executable guard for invariant 2: no malformed, dangling, or
    /// impossible-by-construction level may load. Every rejection carries a
    /// reason string (asserted by tests, surfaced in tooling).
    /// </summary>
    public static class LevelValidator
    {
        public static List<string> Validate(LevelDto dto, PieceRegistry registry)
        {
            var errors = new List<string>();
            void Err(string msg) => errors.Add(msg);

            if (dto == null) { Err("level: not parseable as a level object"); return errors; }
            if (string.IsNullOrEmpty(dto.id)) Err("level.id: required");
            if (dto.columns is < 5 or > 48) Err($"level.columns: {dto.columns} outside 5..48"); // landscape boards run wide (2026-09-02)
            if (dto.medal == null) Err("level.medal: required");
            else if (!(dto.medal.gold > 0 && dto.medal.gold < dto.medal.silver && dto.medal.silver < dto.medal.bronze))
                Err($"level.medal: need 0 < gold < silver < bronze (got {dto.medal?.gold}/{dto.medal?.silver}/{dto.medal?.bronze})");
            if (dto.rows == null || dto.rows.Length < 3) { Err("level.rows: need at least goal, one lane, bank"); return errors; }
            if (dto.startColumn < 0 || dto.startColumn >= dto.columns) Err($"level.startColumn: {dto.startColumn} out of range");

            for (int i = 0; i < dto.rows.Length; i++)
            {
                var row = dto.rows[i];
                string at = $"rows[{i}]";
                if (row == null) { Err($"{at}: null"); continue; }
                if (!registry.TryGet<LaneKindDef>(row.kind, out var kind))
                {
                    Err($"{at}.kind: unknown lane kind '{row.kind}'");
                    continue;
                }

                bool moving = kind.semantics is LaneSemantics.DeadlyTraffic or LaneSemantics.Water
                    or LaneSemantics.CrashTraffic or LaneSemantics.Conveyor;
                bool hasObjects = row.objects is { Length: > 0 };

                if (i == 0 && kind.semantics != LaneSemantics.Goal) Err("rows[0]: first row must be the goal row");
                if (i == dto.rows.Length - 1 && kind.semantics != LaneSemantics.Bank) Err($"{at}: last row must be the bank");
                if (kind.semantics == LaneSemantics.Goal && i != 0) Err($"{at}: goal kind only allowed at rows[0]");
                if (kind.semantics == LaneSemantics.Bank && i != dto.rows.Length - 1) Err($"{at}: bank kind only allowed last");

                if (hasObjects || kind.semantics == LaneSemantics.Conveyor)
                {
                    if (row.speed <= 0) Err($"{at}.speed: must be > 0 for moving content");
                    if (row.dir != "left" && row.dir != "right") Err($"{at}.dir: must be left|right");
                }

                if (hasObjects)
                {
                    if (!moving || kind.semantics == LaneSemantics.Conveyor)
                        Err($"{at}.objects: lane kind '{row.kind}' does not carry objects");
                    foreach (var train in row.objects)
                    {
                        if (!registry.TryGet<LaneObjectDef>(train.pieceId, out var obj))
                        {
                            Err($"{at}: unknown piece '{train.pieceId}'");
                            continue;
                        }
                        bool ok = kind.semantics switch
                        {
                            LaneSemantics.DeadlyTraffic => obj.role == ObjectRole.Kill,
                            LaneSemantics.Water => obj.role is ObjectRole.Rideable or ObjectRole.StaticSafe,
                            LaneSemantics.CrashTraffic => obj.role == ObjectRole.Crashable,
                            _ => false,
                        };
                        if (!ok) Err($"{at}: piece '{train.pieceId}' ({obj.role}) not valid on '{row.kind}'");
                        if (train.spacing != 0 && train.spacing < obj.sizeCells + 1)
                            Err($"{at}: '{train.pieceId}' spacing {train.spacing} overlaps (size {obj.sizeCells})");
                    }
                }

                if (row.obstructions is { Length: > 0 })
                {
                    if (kind.semantics != LaneSemantics.SafeGround)
                        Err($"{at}.obstructions: only safe-ground rows take obstructions");
                    foreach (var ob in row.obstructions)
                    {
                        if (!registry.TryGet<ObstructionDef>(ob.pieceId, out _))
                            Err($"{at}: unknown obstruction '{ob.pieceId}'");
                        if (ob.column < 0 || ob.column >= dto.columns)
                            Err($"{at}: obstruction column {ob.column} out of range");
                    }
                }
            }

            if (dto.bays == null || dto.bays.Length == 0) Err("level.bays: at least one bay");
            else
            {
                if (dto.bays.Distinct().Count() != dto.bays.Length) Err("level.bays: duplicates");
                foreach (int b in dto.bays)
                    if (b < 0 || b >= dto.columns) Err($"level.bays: column {b} out of range");
            }

            return errors;
        }
    }
}
