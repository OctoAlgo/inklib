using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace Tpl.Tests
{
    public class ReplayTests
    {
        public static IEnumerable<object[]> Files()
        {
            foreach (string f in Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "Replays"), "*.replay"))
                yield return new object[] { Path.GetFileName(f) };
        }

        /// <summary>Every *.replay in Replays/ must match what the lib does now.</summary>
        [Theory]
        [MemberData(nameof(Files))]
        public void ReplayMatches(string file)
        {
            Replay r = Replay.Load(Path.Combine(AppContext.BaseDirectory, "Replays", file));
            Assert.NotEmpty(r.Rows);
            string? failure = r.Run();
            Assert.True(failure == null, failure);
        }

        /// <summary>
        /// Rewrites the regression replays (UNVERIFIED, recorded from the lib itself) after a deliberate
        /// change. Off unless INKLIB_RECORD_REPLAYS is set to the Replays folder:
        /// INKLIB_RECORD_REPLAYS=$PWD/tests/Tpl.Core.Tests/Replays dotnet test --filter RecordRegressionReplays
        /// </summary>
        [Fact]
        public void RecordRegressionReplays()
        {
            string? dir = Environment.GetEnvironmentVariable("INKLIB_RECORD_REPLAYS");
            if (string.IsNullOrEmpty(dir)) return;
            foreach (string name in new[] { "walk-and-jump", "step-up-shared" })
            {
                string path = Path.Combine(dir, name + ".replay");
                Replay r = Replay.Load(path);
                Assert.StartsWith("UNVERIFIED", r.Source);
                File.WriteAllText(path, r.Record(r.Source));
            }
        }
    }
}
