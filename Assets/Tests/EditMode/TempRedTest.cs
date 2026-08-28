using NUnit.Framework;
namespace FrogAcross.Tests.EditMode
{
    public class TempRedTest
    {
        [Test] public void SynthesizedFailure_ForRulesetProof() => Assert.Fail("deliberate red for #23's merge-block proof");
    }
}
