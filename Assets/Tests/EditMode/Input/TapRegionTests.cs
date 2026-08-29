using FrogAcross.Input;
using FrogAcross.Sim;
using NUnit.Framework;
using UnityEngine;

namespace FrogAcross.Tests.EditMode.Input
{
    public class TapRegionTests
    {
        [TestCase(0.10f, 0.5f, Move.Left)]
        [TestCase(0.19f, 0.5f, Move.Left)]     // just inside the left band
        [TestCase(0.334f, 0.9f, Move.Forward)]  // just inside the middle, top
        [TestCase(0.5f, 0.51f, Move.Forward)]
        [TestCase(0.5f, 0.49f, Move.Back)]
        [TestCase(0.665f, 0.1f, Move.Back)]
        [TestCase(0.81f, 0.1f, Move.Right)]    // just inside the right band
        [TestCase(0.95f, 0.9f, Move.Right)]
        public void RegionGeometry(float x, float y, Move expected)
        {
            Assert.AreEqual(expected, TapRegionMapper.Map(new Vector2(x, y)));
        }

        [Test]
        public void RegionTapThresholds()
        {
            Assert.IsTrue(TapRegionMapper.IsRegionTap(new Vector2(0.3f, 0.2f), 0.2f));
            Assert.IsFalse(TapRegionMapper.IsRegionTap(new Vector2(1.5f, 0f), 0.2f), "a drag is not a region tap");
            Assert.IsFalse(TapRegionMapper.IsRegionTap(Vector2.zero, 0.6f), "a hold is not a region tap");
        }

        [Test]
        public void SchemeSetting_PersistsAndDefaultsToSwipe()
        {
            PlayerPrefs.DeleteKey(ControlSchemeSetting.PrefKey);
            Assert.AreEqual(ControlScheme.Swipe, ControlSchemeSetting.Current, "fresh installs default to swipe");
            ControlSchemeSetting.Current = ControlScheme.TapRegions;
            Assert.AreEqual(ControlScheme.TapRegions, ControlSchemeSetting.Current);
            ControlSchemeSetting.Current = ControlScheme.Swipe;
        }
    }
}
