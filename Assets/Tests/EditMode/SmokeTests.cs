using FrogAcross;
using NUnit.Framework;

namespace FrogAcross.Tests.EditMode
{
    public class SmokeTests
    {
        [Test]
        public void RuntimeAssembly_IsReachable()
        {
            Assert.AreEqual("FrogAcross", AppInfo.ProductName);
        }
    }
}
