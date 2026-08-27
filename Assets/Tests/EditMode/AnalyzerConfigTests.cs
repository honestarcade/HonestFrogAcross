using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;

namespace FrogAcross.Tests.EditMode
{
    /// <summary>
    /// The analyzer gate stays wired: DLL present and labeled, csc.rsp carrying
    /// the -analyzer/-ruleset flags (the load-bearing wiring on Unity 6000.5 —
    /// the label alone did not attach the analyzer), ruleset strict, and every
    /// tuned-down rule carrying a written rationale.
    /// </summary>
    public class AnalyzerConfigTests
    {
        private const string AnalyzerPath = "Assets/Microsoft.Unity.Analyzers.dll";
        private const string CscRspPath = "Assets/csc.rsp";
        private const string RulesetPath = "Assets/Default.ruleset";
        private const string SuppressionsPath = "Assets/Analyzers/SUPPRESSIONS.md";

        [Test]
        public void AnalyzerDll_ExistsWithRoslynAnalyzerLabel()
        {
            var asset = AssetDatabase.LoadMainAssetAtPath(AnalyzerPath);
            Assert.IsNotNull(asset, $"missing {AnalyzerPath}");
            CollectionAssert.Contains(AssetDatabase.GetLabels(asset), "RoslynAnalyzer");
        }

        [Test]
        public void CscRsp_WiresAnalyzerAndRuleset()
        {
            string rsp = File.ReadAllText(CscRspPath);
            StringAssert.Contains("-analyzer:\"Assets/Microsoft.Unity.Analyzers.dll\"", rsp);
            StringAssert.Contains("-ruleset:\"Assets/Default.ruleset\"", rsp);
        }

        [Test]
        public void Ruleset_KeepsIncludeAllError()
        {
            StringAssert.Contains("<IncludeAll Action=\"Error\"", File.ReadAllText(RulesetPath),
                "Default.ruleset must keep the IncludeAll=Error baseline.");
        }

        [Test]
        public void Ruleset_PinsAnalyzerRules()
        {
            string ruleset = File.ReadAllText(RulesetPath);
            int errorPins = Regex.Matches(ruleset, "<Rule\\s+Id=\"U(NT|SP)\\d{4}\"\\s+Action=\"Error\"").Count;
            Assert.GreaterOrEqual(errorPins, 60,
                "the UNT/USP rule pins (default-Info rules escalated to Error) have been removed or gutted");
        }

        [Test]
        public void EveryTunedRule_HasARationale()
        {
            string ruleset = File.ReadAllText(RulesetPath);
            string suppressions = File.ReadAllText(SuppressionsPath);

            var tunedDown = Regex.Matches(ruleset, "<Rule\\s+Id=\"(?<id>[A-Za-z0-9]+)\"\\s+Action=\"(?<action>[A-Za-z]+)\"")
                .Where(m => m.Groups["action"].Value != "Error")
                .Select(m => m.Groups["id"].Value)
                .ToList();

            foreach (string id in tunedDown)
            {
                StringAssert.IsMatch($@"(?m)^{Regex.Escape(id)}\s+—\s+\S.*$", suppressions,
                    $"Rule {id} is tuned down in Default.ruleset but has no rationale line in SUPPRESSIONS.md.");
            }
        }
    }
}
