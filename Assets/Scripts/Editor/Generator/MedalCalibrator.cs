using System;
using System.IO;
using System.Linq;
using FrogAcross.Levels;
using UnityEditor;
using UnityEngine;

namespace FrogAcross.Editor.Generator
{
    /// <summary>
    /// #63: medal thresholds derive from the solver's diagonal-free floor in
    /// the solvability fixture — hand-typed medal times are drift. Gold is
    /// attainable by construction (factor > 1 over a proven line). Runs
    /// BEFORE fixture regeneration in the content pipeline (calibration is
    /// part of the locked bytes).
    /// </summary>
    public static class MedalCalibrator
    {
        // Diagonal-free floor factors (tap-region fairness). Beginners get a
        // generous multiple that tapers to 2.0× by L41 — early boards are
        // short, so a flat 2.0× would make L1 gold an esport time; the design
        // anchors early golds in relaxed tens-of-seconds territory.
        public const float SilverOverGold = 1.45f;
        public const float BronzeOverGold = 2.1f;

        public static float GoldFactorFor(int levelNumber) => levelNumber switch
        {
            <= 10 => 4.5f,
            <= 20 => 3.4f,
            <= 30 => 2.9f,
            <= 40 => 2.4f,
            _ => 2.0f,
        };

        [MenuItem("FrogAcross/Levels/Calibrate medals from fixture")]
        public static void CalibrateAll()
        {
            var fixture = ContentLock.LoadFixture();
            int written = 0;
            foreach (var file in ContentLock.ShippedLevelFiles())
            {
                string id = Path.GetFileNameWithoutExtension(file);
                var entry = fixture.entries.FirstOrDefault(e => e.id == id)
                    ?? throw new InvalidOperationException($"{id}: no solvability entry — run RegenerateFixture first");
                if (Calibrate(file, entry.minTicks)) written++;
            }
            AssetDatabase.Refresh();
            Debug.Log($"[MedalCalibrator] rewrote medals on {written} levels "
                + "(fixture hashes are now stale — run RegenerateFixture)");
        }

        /// <summary>Rewrites one file's medal block. Returns true if bytes changed.</summary>
        public static bool Calibrate(string file, long minTicks)
        {
            var dto = JsonUtility.FromJson<LevelDto>(File.ReadAllText(file));
            int number = int.Parse(Path.GetFileNameWithoutExtension(file)
                .Substring(ContentLock.ShippedPrefix.Length));
            float minSec = minTicks / 60f;
            float g = GoldFactorFor(number);
            dto.medal = new MedalDto
            {
                gold = Round1(minSec * g),
                silver = Round1(minSec * g * SilverOverGold),
                bronze = Round1(minSec * g * BronzeOverGold),
            };
            string json = JsonUtility.ToJson(dto, true);
            if (json == File.ReadAllText(file)) return false;
            File.WriteAllText(file, json);
            return true;
        }

        private static float Round1(float v) => (float)Math.Round(v, 1);
    }
}
