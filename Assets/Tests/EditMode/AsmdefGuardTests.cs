using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace FrogAcross.Tests.EditMode
{
    /// <summary>
    /// Editor-only code must never leak into the shipped build: the Runtime
    /// assembly may not reference editor assemblies or be editor-scoped.
    /// </summary>
    public class AsmdefGuardTests
    {
        private const string RuntimeAsmdefPath = "Assets/Scripts/Runtime/FrogAcross.Runtime.asmdef";

        [Test]
        public void RuntimeAsmdef_HasNoEditorReferences()
        {
            string json = File.ReadAllText(RuntimeAsmdefPath);
            StringAssert.DoesNotContain("FrogAcross.Editor", json);
            StringAssert.DoesNotContain("UnityEditor", json);
        }

        [Test]
        public void RuntimeAsmdef_IsNotEditorScoped()
        {
            string json = File.ReadAllText(RuntimeAsmdefPath);
            var asmdef = JsonUtility.FromJson<AsmdefShape>(json);
            Assert.That(asmdef.includePlatforms, Does.Not.Contain("Editor"),
                "FrogAcross.Runtime must build for the player, not just the editor.");
        }

        [System.Serializable]
        private class AsmdefShape
        {
#pragma warning disable 0649
            public string[] includePlatforms;
#pragma warning restore 0649
        }
    }
}
