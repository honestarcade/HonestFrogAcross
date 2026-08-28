using System;
using System.IO;
using System.Linq;
using FrogAcross.Pieces;
using UnityEditor;
using UnityEngine;

namespace FrogAcross.Editor.Pieces
{
    /// <summary>
    /// Idempotently creates/updates the v1 piece catalogue. This method is also
    /// the worked example of "adding a piece is data": new entries here (or new
    /// assets made by hand) — never engine code.
    /// Zone/cycle defaults are shipped tuning values; rows can override speeds,
    /// phases and warn leads in level JSON.
    /// </summary>
    public static class CatalogueBuilder
    {
        private const string Root = "Assets/GameData/Pieces";
        private const string RegistryPath = "Assets/Resources/PieceRegistry.asset";

        [MenuItem("FrogAcross/Data/Rebuild Piece Catalogue")]
        public static void CreateV1Catalogue()
        {
            Directory.CreateDirectory(Root);
            Directory.CreateDirectory("Assets/Resources");

            var reg = AssetDatabase.LoadAssetAtPath<PieceRegistry>(RegistryPath);
            if (reg == null)
            {
                reg = ScriptableObject.CreateInstance<PieceRegistry>();
                AssetDatabase.CreateAsset(reg, RegistryPath);
            }
            reg.pieces.Clear();

            foreach (var (id, style) in new[]
            {
                ("frog", MoveStyle.Hop), ("bunny", MoveStyle.Hop), ("hopper", MoveStyle.Hop),
                ("roo", MoveStyle.Hop), ("dog", MoveStyle.Step), ("cat", MoveStyle.Step),
            })
                reg.pieces.Add(Upsert<CharacterDef>(id, d => d.moveStyle = style));

            foreach (var (id, sem) in new[]
            {
                ("road", LaneSemantics.DeadlyTraffic), ("tracks", LaneSemantics.DeadlyTraffic),
                ("river", LaneSemantics.Water), ("swamp", LaneSemantics.Water),
                ("bike", LaneSemantics.CrashTraffic), ("walkway", LaneSemantics.Conveyor),
                ("grass", LaneSemantics.SafeGround), ("concrete", LaneSemantics.SafeGround),
                ("goal", LaneSemantics.Goal), ("bank", LaneSemantics.Bank),
            })
                reg.pieces.Add(Upsert<LaneKindDef>(id, d => d.semantics = sem));

            // Kill traffic. warnLead only meaningful for trains.
            foreach (var (id, size, warn) in new[]
            {
                ("truck", 3.4f, 0), ("car", 1.6f, 0), ("convertible", 1.6f, 0), ("bus", 2.8f, 0),
                ("freight", 11f, 90), ("passenger", 11f, 90),
            })
                reg.pieces.Add(Upsert<LaneObjectDef>(id, d =>
                {
                    d.role = ObjectRole.Kill; d.sizeCells = size; d.warnLeadTicks = warn;
                }));

            // Crashables: 2s stun (120 ticks), 2.4s crash sequence per the design.
            foreach (var id in new[] { "cyclist", "skater", "runner" })
                reg.pieces.Add(Upsert<LaneObjectDef>(id, d =>
                {
                    d.role = ObjectRole.Crashable; d.sizeCells = 0.9f;
                    d.stunTicks = 120; d.crashSequenceTicks = 144;
                }));

            // Rideables.
            foreach (var (id, size) in new[] { ("log-short", 2.4f), ("log", 3.6f), ("log-long", 4.9f), ("raft", 3.2f) })
                reg.pieces.Add(Upsert<LaneObjectDef>(id, d =>
                {
                    d.role = ObjectRole.Rideable; d.sizeCells = size;
                    d.rideableZoneStart = 0f; d.rideableZoneEnd = 1f;
                }));

            reg.pieces.Add(Upsert<LaneObjectDef>("turtle-log", d =>
            {
                d.role = ObjectRole.Rideable; d.sizeCells = 3.3f;
                d.rideableZoneStart = 0f; d.rideableZoneEnd = 1f;
                d.cycleActiveTicks = 240; d.cycleInactiveTicks = 90; // surfaced 4s / under 1.5s default
                d.inactiveKills = false; // submerged turtle = water rules
            }));

            // OWNER RULE (ledger 2026-08-28): back only, closed-mouth only.
            // Zone fractions measured tail→head; head/snout excluded.
            reg.pieces.Add(Upsert<LaneObjectDef>("gator", d =>
            {
                d.role = ObjectRole.Rideable; d.sizeCells = 3.7f;
                d.rideableZoneStart = 0.05f; d.rideableZoneEnd = 0.55f;
                d.cycleActiveTicks = 300; d.cycleInactiveTicks = 120; // closed 5s / open 2s default
                d.inactiveKills = true; // open mouth: the whole gator kills
            }));

            reg.pieces.Add(Upsert<LaneObjectDef>("lily-pad", d =>
            {
                d.role = ObjectRole.StaticSafe; d.sizeCells = 1f;
            }));

            foreach (var id in new[] { "tree", "bush", "bench", "lamp", "planter-daisy", "planter-tulip", "planter-fern", "planter-lavender", "planter-succulent" })
                reg.pieces.Add(Upsert<ObstructionDef>(id, d => d.blocksTile = true));

            reg.InvalidateCache();
            EditorUtility.SetDirty(reg);
            AssetDatabase.SaveAssets();
            Debug.Log($"[CatalogueBuilder] Piece catalogue rebuilt: {reg.pieces.Count} pieces.");
        }

        private static T Upsert<T>(string id, Action<T> configure) where T : PieceDef
        {
            string path = $"{Root}/{typeof(T).Name}-{id}.asset";
            var def = AssetDatabase.LoadAssetAtPath<T>(path);
            if (def == null)
            {
                def = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(def, path);
            }
            def.id = id;
            def.displayName = id;
            configure(def);
            EditorUtility.SetDirty(def);
            return def;
        }
    }
}
