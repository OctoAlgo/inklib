using System;

namespace Tpl
{
    /// <summary>
    /// Maths that gives the same bits on every machine (SPEC.md section 5). The simulation calls these
    /// instead of Math.Sin/Cos/Atan2/Pow/..., whose results come from the platform and can differ in
    /// the last bit between operating systems, CPUs and runtimes.
    /// Only +, -, *, / and Math.Round are used: IEEE 754 makes those exact the same everywhere.
    /// Accuracy is about 1e-15, plenty for a game; what matters is that it is identical everywhere.
    /// Our own code (series expansions); no source copied.
    /// </summary>
    public static class DetMath
    {
        public const double Pi = 3.141592653589793;
        public const double HalfPi = 1.5707963267948966;
        public const double TwoPi = 6.283185307179586;

        /// <summary>
        /// Square root. IEEE 754 requires sqrt to be correctly rounded, so Math.Sqrt gives the same bits on
        /// every conforming machine. It goes through here so the rule lives in one place.
        /// </summary>
        public static double Sqrt(double x) => Math.Sqrt(x);

        /// <summary>Sine of an angle in radians.</summary>
        public static double Sin(double x)
        {
            x -= TwoPi * Math.Round(x / TwoPi);           // -pi..pi
            if (x > HalfPi) x = Pi - x;                    // -pi/2..pi/2, same sine
            else if (x < -HalfPi) x = -Pi - x;
            double x2 = x * x;
            // Taylor series to x^19; the next term is below 3e-16 on this range.
            double p = 1.0 / 121645100408832000.0;         // 1/19!
            p = p * -x2 + 1.0 / 355687428096000.0;         // 1/17!
            p = p * -x2 + 1.0 / 1307674368000.0;           // 1/15!
            p = p * -x2 + 1.0 / 6227020800.0;              // 1/13!
            p = p * -x2 + 1.0 / 39916800.0;                // 1/11!
            p = p * -x2 + 1.0 / 362880.0;                  // 1/9!
            p = p * -x2 + 1.0 / 5040.0;                    // 1/7!
            p = p * -x2 + 1.0 / 120.0;                     // 1/5!
            p = p * -x2 + 1.0 / 6.0;                       // 1/3!
            p = p * -x2 + 1.0;
            return x * p;
        }

        /// <summary>Cosine of an angle in radians.</summary>
        public static double Cos(double x) => Sin(x + HalfPi);

        /// <summary>Arc tangent, -pi/2..pi/2.</summary>
        public static double Atan(double x)
        {
            if (double.IsNaN(x)) return x;
            bool negative = x < 0;
            if (negative) x = -x;
            bool inverted = x > 1;
            if (inverted) x = 1 / x;
            // atan(x) = 2 atan(x / (1 + sqrt(1 + x^2))), twice: x drops below tan(pi/16) = 0.199.
            x /= 1 + Sqrt(1 + x * x);
            x /= 1 + Sqrt(1 + x * x);
            double x2 = x * x, p = 0;
            for (int k = 21; k >= 1; k -= 2) p = 1.0 / k - x2 * p;   // 1 - x^2/3 + x^4/5 - ... to x^20/21
            double r = 4 * x * p;
            if (inverted) r = HalfPi - r;
            return negative ? -r : r;
        }

        /// <summary>Angle of the vector (x, y) in radians, -pi..pi, like Math.Atan2.</summary>
        public static double Atan2(double y, double x)
        {
            if (x > 0) return Atan(y / x);
            if (x < 0) return y >= 0 ? Atan(y / x) + Pi : Atan(y / x) - Pi;
            if (y > 0) return HalfPi;
            if (y < 0) return -HalfPi;
            return 0;
        }
    }
}
