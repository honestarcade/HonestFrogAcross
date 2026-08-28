using System;
using System.Collections.Generic;
using System.Linq;
using FrogAcross.Pieces;
using UnityEngine;

namespace FrogAcross.Levels
{
    /// <summary>
    /// JSON text → validated LevelDefinition. Level files live under
    /// Resources/Levels/*.json (TextAssets) — NOT StreamingAssets, which would
    /// need UnityWebRequest on Android (module removed under invariant 1).
    /// </summary>
    public static class LevelLoader
    {
        public const string ResourceFolder = "Levels";

        public static LevelDefinition LoadFromResources(string levelId, PieceRegistry registry)
        {
            var text = Resources.Load<TextAsset>($"{ResourceFolder}/{levelId}");
            if (text == null) throw new InvalidOperationException($"Level resource '{levelId}' not found");
            return Parse(text.text, registry);
        }

        public static LevelDefinition Parse(string json, PieceRegistry registry)
        {
            LevelDto dto;
            try { dto = JsonUtility.FromJson<LevelDto>(json); }
            catch (Exception e) { throw new LevelFormatException(new List<string> { $"level: malformed JSON ({e.Message})" }); }

            var errors = LevelValidator.Validate(dto, registry);
            if (errors.Count > 0) throw new LevelFormatException(errors);

            return new LevelDefinition
            {
                Id = dto.id,
                Name = string.IsNullOrEmpty(dto.name) ? dto.id : dto.name,
                Columns = dto.columns,
                GoldSeconds = dto.medal.gold,
                SilverSeconds = dto.medal.silver,
                BronzeSeconds = dto.medal.bronze,
                StartColumn = dto.startColumn,
                BayColumns = dto.bays.ToList(),
                Rows = dto.rows.Select(r => BuildRow(r, registry)).ToList(),
            };
        }

        private static RowDefinition BuildRow(RowDto row, PieceRegistry registry) => new()
        {
            Kind = registry.Get<LaneKindDef>(row.kind),
            DirSign = row.dir == "left" ? -1 : row.dir == "right" ? 1 : 0,
            SpeedCellsPerSec = row.speed,
            Trains = (row.objects ?? Array.Empty<ObjectTrainDto>()).Select(t => new ObjectTrain
            {
                Def = registry.Get<LaneObjectDef>(t.pieceId),
                OffsetCells = t.offset,
                SpacingCells = t.spacing,
                PhaseTicks = t.phase,
            }).ToList(),
            Obstructions = (row.obstructions ?? Array.Empty<ObstructionDto>()).Select(o => new PlacedObstruction
            {
                Def = registry.Get<ObstructionDef>(o.pieceId),
                Column = o.column,
            }).ToList(),
        };
    }

    public sealed class LevelFormatException : Exception
    {
        public IReadOnlyList<string> Errors { get; }
        public LevelFormatException(IReadOnlyList<string> errors)
            : base("Invalid level:\n" + string.Join("\n", errors)) => Errors = errors;
    }
}
