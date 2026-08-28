using System.IO;
using System.Linq;
using FrogAcross.Levels;
using FrogAcross.Pieces;
using NUnit.Framework;
using UnityEngine;

namespace FrogAcross.Tests.EditMode.Levels
{
    public class LevelValidatorTests
    {
        private PieceRegistry Reg => PieceRegistry.Load();
        private const string Fixtures = "Assets/Tests/EditMode/Fixtures";

        [Test]
        public void DevLevel_LoadsAndRoundTrips()
        {
            var def = LevelLoader.LoadFromResources("dev-001", Reg);
            Assert.AreEqual("dev-001", def.Id);
            Assert.AreEqual(11, def.Columns);
            Assert.AreEqual(10, def.Rows.Count);
            Assert.AreEqual(LaneSemantics.Goal, def.Rows[0].Kind.semantics);
            Assert.AreEqual(LaneSemantics.Bank, def.Rows[def.BankRow].Kind.semantics);
            // round-trip: reserialize the DTO and re-parse to the same shape
            string json = File.ReadAllText("Assets/Resources/Levels/dev-001.json");
            var dto = JsonUtility.FromJson<LevelDto>(json);
            var again = JsonUtility.FromJson<LevelDto>(JsonUtility.ToJson(dto));
            Assert.AreEqual(dto.rows.Length, again.rows.Length);
            Assert.AreEqual(dto.bays.Length, again.bays.Length);
        }

        [Test]
        public void AllShippedLevels_Validate()
        {
            var files = Directory.GetFiles("Assets/Resources/Levels", "*.json");
            Assert.IsNotEmpty(files);
            foreach (var f in files)
            {
                var errors = LevelValidator.Validate(JsonUtility.FromJson<LevelDto>(File.ReadAllText(f)), Reg);
                Assert.IsEmpty(errors, $"{Path.GetFileName(f)}:\n" + string.Join("\n", errors));
            }
        }

        [Test]
        public void BrokenFixtures_AreRejectedForTheRightReasons()
        {
            var unknown = LevelValidator.Validate(
                JsonUtility.FromJson<LevelDto>(File.ReadAllText($"{Fixtures}/broken-unknown-piece.json")), Reg);
            Assert.IsTrue(unknown.Any(e => e.Contains("unknown piece 'hovercar'")), string.Join("\n", unknown));

            var noBays = LevelValidator.Validate(
                JsonUtility.FromJson<LevelDto>(File.ReadAllText($"{Fixtures}/broken-no-bays.json")), Reg);
            Assert.IsTrue(noBays.Any(e => e.Contains("at least one bay")), string.Join("\n", noBays));

            Assert.Throws<LevelFormatException>(() =>
                LevelLoader.Parse(File.ReadAllText($"{Fixtures}/broken-malformed.json"), Reg));
        }

        [Test]
        public void Validator_CatchesRoleMismatchAndOverlap()
        {
            string json = File.ReadAllText("Assets/Resources/Levels/dev-001.json");
            var dto = JsonUtility.FromJson<LevelDto>(json);
            dto.rows[1].objects[0].pieceId = "log";        // Rideable on DeadlyTraffic
            dto.rows[1].objects[1].spacing = 1.0f;         // car overlap
            var errors = LevelValidator.Validate(dto, Reg);
            Assert.IsTrue(errors.Any(e => e.Contains("not valid on 'road'")), string.Join("\n", errors));
            Assert.IsTrue(errors.Any(e => e.Contains("overlaps")), string.Join("\n", errors));
        }
    }
}
