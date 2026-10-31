using System;

namespace Tpl
{
    /// <summary>Which unit convention a world talks to its host in (SPEC.md section 3).</summary>
    public enum UnitMode
    {
        /// <summary>The game's own numbers, unchanged.</summary>
        Native = 0,
        /// <summary>Metres, seconds, radians, Z up, right-handed, yaw 0 = +X towards +Y.</summary>
        Shared = 1,
    }

    /// <summary>
    /// The native units of this lib and the conversion to the shared convention.
    /// The simulation only ever sees native numbers; this class sits at the API boundary.
    /// <para>
    /// Template native units (an example; replace with your game's):
    /// 1 unit = 1 cm, angles in radians, Y up, right-handed, velocity in units per tick.
    /// Native yaw turns around +Y; yaw 0 faces +X, forward = (cos yaw, 0, -sin yaw).
    /// Mapping: shared (x, y, z) = native (x, -z, y) times <see cref="MetresPerUnit"/>.
    /// With that mapping native yaw and shared yaw are the same angle.
    /// </para>
    /// Keep the constants equal to the TPL_NATIVE_* macros in tpl.h (HeaderTests checks).
    /// </summary>
    public static class Units
    {
        public const double MetresPerUnit = 0.01;
        public const double RadiansPerUnit = 1.0;
        /// <summary>0 = X, 1 = Y, 2 = Z.</summary>
        public const int UpAxis = 1;
        public const int UpSign = 1;
        /// <summary>+1 right-handed, -1 left-handed.</summary>
        public const int Handedness = 1;
        /// <summary>1 if native velocity is units per tick, 0 if units per second.</summary>
        public const int VelocityPerTick = 1;

        // "0.0 - z" instead of "-z" so a zero stays +0 (no "-0" in logs and replays).

        /// <summary>Native position to the convention <paramref name="mode"/>.</summary>
        public static Vec3 PositionOut(Vec3 n, UnitMode mode) =>
            mode == UnitMode.Native ? n : new Vec3(n.X, 0.0 - n.Z, n.Y) * MetresPerUnit;

        /// <summary>Position in the convention <paramref name="mode"/> to native.</summary>
        public static Vec3 PositionIn(Vec3 s, UnitMode mode) =>
            mode == UnitMode.Native ? s : Scale(new Vec3(s.X, s.Z, 0.0 - s.Y));

        /// <summary>Unit direction or normal, native to <paramref name="mode"/>. No scaling.</summary>
        public static Vec3 DirectionOut(Vec3 n, UnitMode mode) =>
            mode == UnitMode.Native ? n : new Vec3(n.X, 0.0 - n.Z, n.Y);

        public static Vec3 DirectionIn(Vec3 s, UnitMode mode) =>
            mode == UnitMode.Native ? s : new Vec3(s.X, s.Z, 0.0 - s.Y);

        /// <summary>Native velocity to <paramref name="mode"/> (shared: metres per second).</summary>
        public static Vec3 VelocityOut(Vec3 n, UnitMode mode, int tickRate) =>
            mode == UnitMode.Native ? n : PositionOut(n, mode) * (VelocityPerTick == 1 ? tickRate : 1);

        /// <summary>Native length (distance, radius) to <paramref name="mode"/>.</summary>
        public static double LengthOut(double n, UnitMode mode) => mode == UnitMode.Native ? n : n * MetresPerUnit;

        public static double LengthIn(double s, UnitMode mode) => mode == UnitMode.Native ? s : s / MetresPerUnit;

        /// <summary>Native angle (yaw) to <paramref name="mode"/> (shared: radians).</summary>
        public static double AngleOut(double n, UnitMode mode) => mode == UnitMode.Native ? n : n * RadiansPerUnit;

        public static double AngleIn(double s, UnitMode mode) => mode == UnitMode.Native ? s : s / RadiansPerUnit;

        /// <summary>
        /// Box half extents are lengths along axes; swapping axes swaps them, signs do not matter.
        /// </summary>
        public static Vec3 ExtentsOut(Vec3 n, UnitMode mode) =>
            mode == UnitMode.Native ? n : new Vec3(n.X, n.Z, n.Y) * MetresPerUnit;

        public static Vec3 ExtentsIn(Vec3 s, UnitMode mode) =>
            mode == UnitMode.Native ? s : Scale(new Vec3(s.X, s.Z, s.Y));

        // Divide (not multiply by 1 / MetresPerUnit): 0.01 is inexact, and dividing by it rounds back
        // to the native number in far more cases. Integer-unit games round to the nearest integer here.
        static Vec3 Scale(Vec3 s) => new Vec3(s.X / MetresPerUnit, s.Y / MetresPerUnit, s.Z / MetresPerUnit);

        /// <summary>Wraps an angle into -pi..pi. Integer-angle games wrap with a mask instead.</summary>
        public static double WrapAngle(double a)
        {
            while (a > Math.PI) a -= 2 * Math.PI;
            while (a < -Math.PI) a += 2 * Math.PI;
            return a;
        }
    }
}
