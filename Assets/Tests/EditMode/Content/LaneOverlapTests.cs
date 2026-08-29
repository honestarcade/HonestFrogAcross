using System.IO;
using FrogAcross.Editor.Generator;
using FrogAcross.Levels;
using FrogAcross.Pieces;
using NUnit.Framework;
using UnityEngine;

namespace FrogAcross.Tests.EditMode.Content
{
    /// <summary>
    /// Owner report (2026-08-29): "objects on lanes should never overlap".
    /// Every object in a row rides the same loop at the same speed, so overlap
    /// is a static property of the data — provable once, for every level.
    /// </summary>
    [TestFixture]
    public class LaneOverlapTests
    {
        [Test]
        public void NoShippedLevel_HasOverlappingLaneObjects()
        {
            var registry = PieceRegistry.Load();
            int rowsChecked = 0;
            foreach (var file in ContentLock.ShippedLevelFiles())
            {
                var level = LevelLoader.Parse(File.ReadAllText(file), registry);
                for (int r = 0; r < level.Rows.Count; r++)
                {
                    float gap = LaneGeometry.SmallestGap(level.Rows[r], level.Columns);
                    if (gap == float.MaxValue) continue;
                    rowsChecked++;
                    Assert.That(gap, Is.GreaterThanOrEqualTo(LaneGeometry.MinGapCells - 0.02f),
                        $"{Path.GetFileNameWithoutExtension(file)} rows[{r}]: objects only {gap:0.00} cells apart");
                }
            }
            Assert.That(rowsChecked, Is.GreaterThan(300), "sanity: most rows carry traffic");
        }

        [Test]
        public void SmallestGap_CatchesTheWrapSeam()
        {
            // The original bug: spacing that does not divide the loop drops the
            // wrapped instance on top of the first one. Derive a spacing that
            // provably collides, whatever the piece measures.
            var registry = PieceRegistry.Load();
            float size = registry.Get<LaneObjectDef>("car").sizeCells;
            const int columns = 9;
            float loop = LaneGeometry.LoopFor(columns, size);
            float badSpacing = loop - size * 0.5f; // wrap gap becomes -size/2

            string json = $@"{{
                ""id"": ""seam-fixture"", ""name"": ""s"", ""columns"": {columns}, ""startColumn"": 4,
                ""bays"": [4], ""medal"": {{ ""gold"": 10, ""silver"": 20, ""bronze"": 30 }},
                ""rows"": [
                    {{ ""kind"": ""goal"" }},
                    {{ ""kind"": ""road"", ""dir"": ""right"", ""speed"": 2.0,
                      ""objects"": [ {{ ""pieceId"": ""car"", ""offset"": 0, ""spacing"": {badSpacing.ToString(System.Globalization.CultureInfo.InvariantCulture)} }} ] }},
                    {{ ""kind"": ""bank"" }}
                ]}}";
            var level = LevelLoader.Parse(json, registry);
            float gap = LaneGeometry.SmallestGap(level.Rows[1], columns);
            Assert.That(gap, Is.LessThan(0f),
                "a spacing that misses the loop must show as a negative gap at the seam");
        }

        [Test]
        public void Place_TilesTheLoopExactly()
        {
            const float loop = 17f, maxSize = 2f;
            var placed = LaneGeometry.Place(2, loop, maxSize, desiredPitch: 4f);
            Assert.That(placed.Length, Is.EqualTo(2));
            foreach (var (offset, spacing) in placed)
            {
                float instances = loop / spacing;
                Assert.That(instances, Is.EqualTo(Mathf.Round(instances)).Within(0.001f),
                    "spacing must divide the loop a whole number of times");
                Assert.That(offset, Is.GreaterThanOrEqualTo(0f));
            }
        }
    }
}
