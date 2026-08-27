using System.Collections;
using FrogAcross;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace FrogAcross.Tests.PlayMode
{
    public class PlayModeSmokeTests
    {
        [UnityTest]
        public IEnumerator PlayerLoop_TicksAFrame()
        {
            var go = new GameObject("smoke");
            int frame = Time.frameCount;
            yield return null;
            Assert.Greater(Time.frameCount, frame);
            Assert.AreEqual("Honest Arcade", AppInfo.Company);
            Object.Destroy(go);
        }
    }
}
