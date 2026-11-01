using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using Xunit;

namespace Tpl.Tests
{
    /// <summary>Every gameplay number is VERIFIED (with a source) or UNVERIFIED (with a reason). SPEC.md section 7.</summary>
    public class ProvenanceTests
    {
        static FieldInfo[] Fields() => typeof(Tuning).GetFields(BindingFlags.Public | BindingFlags.Instance);

        [Fact]
        public void EveryTuningFieldIsVerifiedOrUnverified()
        {
            foreach (FieldInfo f in Fields())
            {
                bool verified = f.GetCustomAttribute<VerifiedAttribute>() != null;
                bool unverified = f.GetCustomAttribute<UnverifiedAttribute>() != null;
                Assert.True(verified ^ unverified, $"Tuning.{f.Name} needs exactly one of [Verified] or [Unverified].");
            }
        }

        [Fact]
        public void SourcesAndReasonsAreNotEmpty()
        {
            foreach (FieldInfo f in Fields())
            {
                string? text = f.GetCustomAttribute<VerifiedAttribute>()?.Source ?? f.GetCustomAttribute<UnverifiedAttribute>()?.Reason;
                Assert.False(string.IsNullOrWhiteSpace(text), $"Tuning.{f.Name} has an empty source or reason.");
            }
        }

        /// <summary>Each [Unverified] field must have a plan: Docs/MEASURE.md names it (as `Tuning.Name`).</summary>
        [Fact]
        public void EveryUnverifiedFieldIsInMeasure()
        {
            string measure = Repo.Read("Docs", "MEASURE.md");
            foreach (FieldInfo f in Fields())
                if (f.GetCustomAttribute<UnverifiedAttribute>() != null)
                    Assert.True(Regex.IsMatch(measure, $@"`Tuning\.{f.Name}`"), $"Tuning.{f.Name} is UNVERIFIED but Docs/MEASURE.md does not name it.");
        }

        /// <summary>Every replay says where it came from.</summary>
        [Fact]
        public void EveryReplayHasASource()
        {
            foreach (string path in Directory.GetFiles(Path.Combine(Repo.Root(), "tests", "Tpl.Core.Tests", "Replays"), "*.replay"))
                Assert.Matches(@"(?m)^source:\s*(VERIFIED|UNVERIFIED):\s*\S", File.ReadAllText(path));
        }
    }
}
