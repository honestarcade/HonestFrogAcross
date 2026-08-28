using System.IO;
using System.Linq;
using FrogAcross.Editor.Generator;
using NUnit.Framework;

namespace FrogAcross.Tests.EditMode.Content
{
    /// <summary>
    /// #62: every shipped level is proven completable, and silently editing
    /// one is impossible — the fixture pins file bytes to solver proof.
    /// CI path is hash-only (cheap); re-solving is the documented
    /// FrogAcross/Levels/Regenerate-solvability-fixture command.
    /// </summary>
    [TestFixture]
    public class ContentLockTests
    {
        private const string Regenerate =
            "run Unity menu 'FrogAcross/Levels/Regenerate solvability fixture' (or -executeMethod FrogAcross.Editor.Generator.ContentLock.RegenerateFixture)";

        [Test]
        public void Fixture_CoversExactlyTheShippedSet()
        {
            Assert.That(File.Exists(ContentLock.FixturePath), Is.True, $"fixture missing — {Regenerate}");
            var fixture = ContentLock.LoadFixture();
            var fixtureIds = fixture.entries.Select(e => e.id).OrderBy(x => x).ToList();
            var shippedIds = ContentLock.ShippedLevelFiles()
                .Select(Path.GetFileNameWithoutExtension).OrderBy(x => x).ToList();
            Assert.That(fixtureIds, Is.EqualTo(shippedIds),
                $"fixture and shipped set diverge — {Regenerate}");
        }

        [Test]
        public void EveryShippedLevel_MatchesItsProvenBytes()
        {
            var fixture = ContentLock.LoadFixture();
            foreach (var file in ContentLock.ShippedLevelFiles())
            {
                string id = Path.GetFileNameWithoutExtension(file);
                var entry = fixture.entries.First(e => e.id == id);
                Assert.That(ContentLock.HashFile(file), Is.EqualTo(entry.fileHash),
                    $"{id} was edited after its solver proof — {Regenerate}");
            }
        }

        [Test]
        public void EveryEntry_IsSolvedWithAPositiveFloor()
        {
            foreach (var entry in ContentLock.LoadFixture().entries)
            {
                Assert.That(entry.solved, Is.True, $"{entry.id} locked without proof");
                Assert.That(entry.minTicks, Is.GreaterThan(0), $"{entry.id} has no recorded floor");
            }
        }

        [Test]
        public void DriftMechanism_HashIsByteSensitive()
        {
            string file = ContentLock.ShippedLevelFiles().First();
            string original = ContentLock.HashFile(file);
            string tmp = Path.Combine(Path.GetTempPath(), "frogacross-drift-probe.json");
            File.WriteAllText(tmp, File.ReadAllText(file) + " ");
            try
            {
                Assert.That(ContentLock.HashFile(tmp), Is.Not.EqualTo(original),
                    "a one-byte edit must change the hash, or the drift guard is dead");
            }
            finally
            {
                File.Delete(tmp);
            }
        }
    }
}
