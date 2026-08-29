using FrogAcross.UI;
using NUnit.Framework;

namespace FrogAcross.Tests.EditMode.UI
{
    /// <summary>#58: copy is data-driven and every key the screens use resolves;
    /// the player-visible name is always the spaced "Frog Across".</summary>
    [TestFixture]
    public class CopyTests
    {
        private static readonly string[] UsedKeys =
        {
            "aboutBody", "aboutSwipe", "aboutDiagonal", "aboutFooter",
            "goal", "levelsTiming", "swiping",
            "ruleRoad", "ruleRiver", "ruleSwamp", "ruleTracks", "ruleBike",
            "ruleWalkway", "ruleMedians", "ruleBays",
            "studioBody", "studioTagline", "storedOnDevice",
            "resetConfirm", "quitConfirm",
            "promiseAds", "promiseTracking", "promiseAccounts", "promisePurchases",
            "promisePermissions", "promiseOpenSource", "promiseOffline", "studioSupport",
        };

        [SetUp]
        public void SetUp() => Copy.Invalidate();

        [Test]
        public void EveryKeyTheScreensUse_ResolvesFromCopyJson()
        {
            foreach (var key in UsedKeys)
            {
                string value = Copy.Get(key);
                Assert.That(value, Does.Not.StartWith("["), $"key '{key}' missing from copy.json");
                Assert.That(value.Length, Is.GreaterThan(10), $"key '{key}' suspiciously short");
            }
        }

        [Test]
        public void MissingKey_ReturnsVisiblePlaceholderNotCrash()
        {
            Assert.That(Copy.Get("no-such-key"), Is.EqualTo("[no-such-key]"));
        }

        [Test]
        public void PlayerVisibleName_IsAlwaysSpaced()
        {
            foreach (var key in UsedKeys)
                Assert.That(Copy.Get(key), Does.Not.Contain("FrogAcross"),
                    $"key '{key}' uses the unspaced name — owner decision is \"Frog Across\"");
        }

        [Test]
        public void ShippedRuleCopy_MatchesOwnerRules()
        {
            Assert.That(Copy.Get("ruleSwamp"), Does.Contain("closed-mouth").IgnoreCase.Or.Contain("closed mouth").IgnoreCase,
                "gator rule: back of closed-mouth gator only");
            Assert.That(Copy.Get("ruleWalkway"), Does.Contain("edge").IgnoreCase,
                "walkway rule: riding off the edge kills");
            Assert.That(Copy.Get("ruleBike"), Does.Contain("two seconds").IgnoreCase,
                "bike rule: 2-second stun, not death");
        }
    }
}
