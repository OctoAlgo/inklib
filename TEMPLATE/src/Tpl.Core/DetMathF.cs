using System;

namespace Tpl
{
    /// <summary>
    /// <see cref="DetMath"/> for float32 simulations (TPL_SIM_BITS = 32): the same functions, computed
    /// in float only, so they give the same bits on every machine. Every intermediate is a float
    /// variable; constants are float literals. A float32 lib copies this file and deletes DetMath.cs.
    /// Accuracy is about 1 float ulp (1e-7). Our own code (series expansions); no source copied.
    /// </summary>
    public static class DetMathF
    {
        public const float Pi = 3.14159265f;
        public const float HalfPi = 1.57079633f;
        public const float TwoPi = 6.28318531f;

        /// <summary>IEEE 754 requires float sqrt to be correctly rounded too: same bits everywhere.</summary>
        public static float Sqrt(float x) => MathF.Sqrt(x);

        /// <summary>Sine of an angle in radians.</summary>
        public static float Sin(float x)
        {
            float k = MathF.Round(x / TwoPi);
            x = x - TwoPi * k;                              // -pi..pi
            if (x > HalfPi) x = Pi - x;                     // -pi/2..pi/2, same sine
            else if (x < -HalfPi) x = -Pi - x;
            float x2 = x * x;
            // Taylor series to x^13; the next term is below 1e-9 on this range.
            float p = 1f / 6227020800f;                     // 1/13!
            p = p * -x2 + 1f / 39916800f;                   // 1/11!
            p = p * -x2 + 1f / 362880f;                     // 1/9!
            p = p * -x2 + 1f / 5040f;                       // 1/7!
            p = p * -x2 + 1f / 120f;                        // 1/5!
            p = p * -x2 + 1f / 6f;                          // 1/3!
            p = p * -x2 + 1f;
            float r = x * p;
            return r;
        }

        /// <summary>Cosine of an angle in radians.</summary>
        public static float Cos(float x)
        {
            float y = x + HalfPi;
            return Sin(y);
        }

        /// <summary>Arc tangent, -pi/2..pi/2.</summary>
        public static float Atan(float x)
        {
            if (float.IsNaN(x)) return x;
            bool negative = x < 0;
            if (negative) x = -x;
            bool inverted = x > 1;
            if (inverted) x = 1f / x;
            // atan(x) = 2 atan(x / (1 + sqrt(1 + x^2))), twice: x drops below tan(pi/16) = 0.199.
            float s = Sqrt(1f + x * x);
            x = x / (1f + s);
            s = Sqrt(1f + x * x);
            x = x / (1f + s);
            float x2 = x * x, p = 0f;
            for (int k = 13; k >= 1; k -= 2) p = 1f / k - x2 * p;   // 1 - x^2/3 + ... to x^12/13
            float r = 4f * x * p;
            if (inverted) r = HalfPi - r;
            return negative ? -r : r;
        }

        /// <summary>Angle of the vector (x, y) in radians, -pi..pi, like MathF.Atan2.</summary>
        public static float Atan2(float y, float x)
        {
            if (x > 0) return Atan(y / x);
            if (x < 0)
            {
                float a = Atan(y / x);
                return y >= 0 ? a + Pi : a - Pi;
            }
            if (y > 0) return HalfPi;
            if (y < 0) return -HalfPi;
            return 0f;
        }
    }
}
