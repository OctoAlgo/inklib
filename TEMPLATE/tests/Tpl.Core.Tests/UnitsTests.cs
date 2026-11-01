using Xunit;

namespace Tpl.Tests
{
    /// <summary>The conversion layer between native units and the shared convention (SPEC.md section 3).</summary>
    public class UnitsTests
    {
        [Fact]
        public void NativeUpIsSharedZ()
        {
            Vec3 up = Units.DirectionOut(new Vec3(0, 1, 0), UnitMode.Shared);
            Assert.Equal(new Vec3(0, 0, 1), up);
        }

        [Fact]
        public void CentimetresBecomeMetres()
        {
            Vec3 p = Units.PositionOut(new Vec3(100, 250, -300), UnitMode.Shared);
            Assert.Equal(new Vec3(1, 3, 2.5), p);
        }

        [Fact]
        public void PositionRoundTrips()
        {
            var n = new Vec3(12.5, -7, 3.25);
            Vec3 back = Units.PositionIn(Units.PositionOut(n, UnitMode.Shared), UnitMode.Shared);
            Assert.True((back - n).Length < 1e-12, back.ToString());
        }

        [Fact]
        public void NativeModeChangesNothing()
        {
            var n = new Vec3(1, 2, 3);
            Assert.Equal(n, Units.PositionOut(n, UnitMode.Native));
            Assert.Equal(n, Units.VelocityOut(n, UnitMode.Native, 1.0 / 60));
            Assert.Equal(0.5, Units.AngleOut(0.5, UnitMode.Native));
        }

        [Fact]
        public void VelocityPerTickBecomesMetresPerSecond()
        {
            Vec3 v = Units.VelocityOut(new Vec3(5, 0, 0), UnitMode.Shared, 1.0 / 60);
            Assert.Equal(3.0, v.X, 12);
            // A game that steps 16 ms per tick (Rayman 3 PC) moves 5 cm in 0.016 s of game time.
            Assert.Equal(3.125, Units.VelocityOut(new Vec3(5, 0, 0), UnitMode.Shared, 0.016).X, 12);
        }

        /// <summary>Shared space is right-handed too: X cross Y = Z after conversion.</summary>
        [Fact]
        public void HandednessIsKept()
        {
            Vec3 x = Units.DirectionIn(new Vec3(1, 0, 0), UnitMode.Shared);
            Vec3 y = Units.DirectionIn(new Vec3(0, 1, 0), UnitMode.Shared);
            Vec3 z = Units.DirectionIn(new Vec3(0, 0, 1), UnitMode.Shared);
            var cross = new Vec3(x.Y * y.Z - x.Z * y.Y, x.Z * y.X - x.X * y.Z, x.X * y.Y - x.Y * y.X);
            Assert.Equal(z, cross);
        }
    }
}
