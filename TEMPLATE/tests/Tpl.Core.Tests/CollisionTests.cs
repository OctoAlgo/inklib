using Xunit;

namespace Tpl.Tests
{
    /// <summary>The touch tolerance rule of SPEC.md section 4, on the test world's box sweep.</summary>
    public class CollisionTests
    {
        static readonly Vec3 Half = new Vec3(40, 90, 40);
        const double Tol = CollisionRules.TouchTolerance;

        // A wall block from x = 400 (the "step" world's 100 cm wall starts there, the 20 cm step before it).
        static TestWorld World() => TestWorld.Make("step");

        [Theory]
        [InlineData(0.0)]          // exactly on the floor
        [InlineData(0.3)]          // just above, within the tolerance
        [InlineData(-0.3)]         // sunk in by less than the tolerance
        public void SlidesAlongTheFloorItTouches(double gap)
        {
            Assert.False(World().SweepBox(new Vec3(0, 90 + gap, 0), Half, new Vec3(150, 0, 0), out _));
            Assert.False(World().SweepBox(new Vec3(0, 90 + gap, 0), Half, new Vec3(0, 10, 0), out _), "moving away is free");
        }

        [Theory]
        [InlineData(0.0)] [InlineData(0.3)] [InlineData(-0.3)]
        public void IsBlockedMovingIntoTheFloor(double gap)
        {
            Assert.True(World().SweepBox(new Vec3(0, 90 + gap, 0), Half, new Vec3(10, -5, 0), out Hit h));
            Assert.Equal(0, h.T);
            Assert.Equal(new Vec3(0, 1, 0), h.Normal);
        }

        [Theory]
        [InlineData(0.0)] [InlineData(0.3)] [InlineData(-0.3)]
        public void SlidesAlongAWallItTouches(double gap)
        {
            // Standing on the 20 cm step, touching the wall face at x = 400.
            var c = new Vec3(400 - 40 - gap, 20 + 90 + 1, 0);
            Assert.False(World().SweepBox(c, Half, new Vec3(0, 0, 200), out _));
            Assert.False(World().SweepBox(c, Half, new Vec3(-50, 0, 0), out _), "moving away is free");
        }

        [Theory]
        [InlineData(0.0)] [InlineData(0.3)] [InlineData(-0.3)]
        public void IsBlockedMovingIntoTheWall(double gap)
        {
            var c = new Vec3(400 - 40 - gap, 20 + 90 + 1, 0);
            Assert.True(World().SweepBox(c, Half, new Vec3(10, 0, 50), out Hit h));
            Assert.Equal(0, h.T);
            Assert.Equal(new Vec3(-1, 0, 0), h.Normal);
        }

        [Fact]
        public void DeepStartOverlapIsAHit()
        {
            Assert.True(World().SweepBox(new Vec3(0, 90 - 2 * Tol, 0), Half, new Vec3(10, 0, 0), out Hit h));
            Assert.Equal(0, h.T);
        }

        [Fact]
        public void ApartHitsAtFirstContact()
        {
            Assert.True(World().SweepBox(new Vec3(400 - 40 - 100, 20 + 90 + 1, 0), Half, new Vec3(200, 0, 0), out Hit h));
            Assert.Equal(0.5, h.T, 12);
            Assert.Equal(new Vec3(-1, 0, 0), h.Normal);
            Assert.False(World().SweepBox(new Vec3(400 - 40 - 100, 20 + 90 + 1, 0), Half, new Vec3(50, 0, 0), out _));
        }
    }
}
