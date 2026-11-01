using System;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;

namespace Tpl.Tests
{
    /// <summary>
    /// Same bits on every machine (SPEC.md section 5). The stored hashes in Replays/*.replay are the real
    /// check: CI runs them on Linux x64, Windows x64 and macOS arm64.
    /// </summary>
    public class DeterminismTests
    {
        [Theory]
        [InlineData(0.0)] [InlineData(0.5)] [InlineData(-1.2)] [InlineData(3.0)] [InlineData(100.25)] [InlineData(-1e4)]
        public void DetMathIsCloseToMath(double x)
        {
            Assert.True(Math.Abs(DetMath.Sin(x) - Math.Sin(x)) < 1e-12, "sin");
            Assert.True(Math.Abs(DetMath.Cos(x) - Math.Cos(x)) < 1e-12, "cos");
            Assert.True(Math.Abs(DetMath.Atan(x) - Math.Atan(x)) < 1e-14, "atan");
            Assert.True(Math.Abs(DetMath.Atan2(x, 0.7) - Math.Atan2(x, 0.7)) < 1e-14, "atan2");
            Assert.True(Math.Abs(DetMath.Atan2(0.3, -x) - Math.Atan2(0.3, -x)) < 1e-14, "atan2 left");
        }

        /// <summary>Golden bits: these must be the same on every machine. Changing DetMath moves them.</summary>
        [Fact]
        public void DetMathGoldenBits()
        {
            double sum = 0;
            for (int i = -500; i <= 500; i++)
            {
                double x = i * 0.0173;
                sum += DetMath.Sin(x) + DetMath.Cos(x) * 0.5 + DetMath.Atan2(x, 1.5 - x) * 0.25;
            }
            Assert.Equal(Golden, "0x" + BitConverter.DoubleToInt64Bits(sum).ToString("x16"));
        }

        const string Golden = "0x40698e7e61f762cf";

        [Theory]
        [InlineData(0f)] [InlineData(0.5f)] [InlineData(-1.2f)] [InlineData(3f)] [InlineData(100.25f)] [InlineData(-1000f)]
        public void DetMathFIsCloseToMath(float x)
        {
            double tol = 2e-6 + System.Math.Abs(x) * 4e-8;   // float 2*pi is off by ~2e-7, so big angles drift a little
            Assert.True(System.Math.Abs(DetMathF.Sin(x) - System.Math.Sin(x)) < tol, "sin");
            Assert.True(System.Math.Abs(DetMathF.Cos(x) - System.Math.Cos(x)) < tol, "cos");
            Assert.True(System.Math.Abs(DetMathF.Atan(x) - System.Math.Atan(x)) < 1e-6, "atan");
            Assert.True(System.Math.Abs(DetMathF.Atan2(x, 0.7f) - System.Math.Atan2(x, 0.7f)) < 1e-6, "atan2");
            Assert.True(System.Math.Abs(DetMathF.Atan2(0.3f, -x) - System.Math.Atan2(0.3f, -x)) < 1e-6, "atan2 left");
        }

        /// <summary>Golden bits for the float32 version: must be the same on every machine.</summary>
        [Fact]
        public void DetMathFGoldenBits()
        {
            float sum = 0;
            for (int i = -500; i <= 500; i++)
            {
                float x = i * 0.0173f;
                float term = DetMathF.Sin(x) + DetMathF.Cos(x) * 0.5f + DetMathF.Atan2(x, 1.5f - x) * 0.25f;
                sum = sum + term;
            }
            Assert.Equal(GoldenF, "0x" + System.BitConverter.SingleToInt32Bits(sum).ToString("x8"));
        }

        const string GoldenF = "0x434c73eb";

        /// <summary>Platform maths and other sources of difference must not appear in Core (outside DetMath.cs).</summary>
        [Fact]
        public void CoreUsesNoPlatformMaths()
        {
            var forbidden = new Regex(@"\bMath\.(Sin|Cos|Tan|Asin|Acos|Atan|Atan2|Sinh|Cosh|Tanh|Pow|Exp|Log|Log10|Log2|Sqrt|Cbrt|FusedMultiplyAdd|ScaleB)\b"
                + @"|\bMathF\.|System\.Numerics|System\.Runtime\.Intrinsics|\bRandom\b|DateTime|Stopwatch|Environment\.TickCount|\bHashCode\.|Parallel\.|\bThread\b|\bTask\b");
            string dir = Path.Combine(Repo.Root(), "src", "Tpl.Core");
            foreach (string file in Directory.GetFiles(dir, "*.cs"))
            {
                if (Path.GetFileName(file) == "DetMath.cs" || Path.GetFileName(file) == "DetMathF.cs") continue;
                string[] lines = File.ReadAllLines(file);
                for (int i = 0; i < lines.Length; i++)
                {
                    string code = Regex.Replace(lines[i], @"//.*$", "");
                    Assert.False(forbidden.IsMatch(code), $"{Path.GetFileName(file)}:{i + 1} uses something that differs between machines: {lines[i].Trim()}");
                }
            }
        }

        [Fact]
        public void EveryReplayStoresAHash()
        {
            foreach (string f in Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "Replays"), "*.replay"))
                Assert.True(Replay.Load(f).Hash != null, Path.GetFileName(f) + " has no hash: line");
        }
    }
}
