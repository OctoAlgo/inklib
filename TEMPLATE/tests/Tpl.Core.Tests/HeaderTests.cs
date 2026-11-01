using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace Tpl.Tests
{
    /// <summary>Keeps native/include/tpl.h, native/tpl.def and Core in step.</summary>
    public class HeaderTests
    {
        static string Header() => Repo.Read("native", "include", "tpl.h");

        static string Snake(string name) => Regex.Replace(name, "(?<=[a-z])([A-Z])", "_$1").ToUpperInvariant();

        static string Macro(string name)
        {
            Match m = Regex.Match(Header(), $@"#define\s+{name}\s+(\S+)");
            Assert.True(m.Success, name + " not found in tpl.h");
            return m.Groups[1].Value;
        }

        [Fact]
        public void NativeUnitsMatchCore()
        {
            Assert.Equal(Units.MetresPerUnit, double.Parse(Macro("TPL_NATIVE_METRES_PER_UNIT"), CultureInfo.InvariantCulture));
            Assert.Equal(Units.RadiansPerUnit, double.Parse(Macro("TPL_NATIVE_RADIANS_PER_UNIT"), CultureInfo.InvariantCulture));
            Assert.Equal(Units.UpAxis, int.Parse(Macro("TPL_NATIVE_UP_AXIS")));
            Assert.Equal(Units.UpSign, int.Parse(Macro("TPL_NATIVE_UP_SIGN")));
            Assert.Equal(Units.Handedness, int.Parse(Macro("TPL_NATIVE_HANDEDNESS")));
            Assert.Equal(Units.VelocityPerTick, int.Parse(Macro("TPL_NATIVE_VELOCITY_PER_TICK")));
            Assert.Equal(new Tuning().TickRate, int.Parse(Macro("TPL_TICK_RATE")));
            Assert.Equal(new Tuning().TickSeconds, double.Parse(Macro("TPL_TICK_SECONDS"), CultureInfo.InvariantCulture));
            Assert.Equal(new Tuning().InputDelayTicks, int.Parse(Macro("TPL_INPUT_DELAY_TICKS")));
            Assert.Equal(Units.SimBits, int.Parse(Macro("TPL_SIM_BITS")));
            Assert.Equal(CollisionRules.TouchTolerance, double.Parse(Macro("TPL_TOUCH_TOLERANCE"), CultureInfo.InvariantCulture));
        }

        [Fact]
        public void EnumsMatchCore()
        {
            string h = Header();
            foreach (MoveState s in Enum.GetValues(typeof(MoveState)))
                Assert.Matches($@"TPL_STATE_{Snake(s.ToString())}\s*=\s*{(int)s},", h);
            foreach (CameraStyle s in Enum.GetValues(typeof(CameraStyle)))
                Assert.Matches($@"TPL_CAMERA_{Snake(s.ToString())}\s*=\s*{(int)s},", h);
            foreach (EventType e in Enum.GetValues(typeof(EventType)))
                Assert.Matches($@"TPL_EVENT_{Snake(e.ToString())}\s*=\s*{(int)e},", h);
            foreach (ShapeKind k in Enum.GetValues(typeof(ShapeKind)))
                Assert.Matches($@"TPL_SHAPE_{Snake(k.ToString())}\s*=\s*{(int)k},", h);
        }

        [Fact]
        public void FlagsMatchCore()
        {
            string h = Header();
            void Check(string prefix, Type flags)
            {
                foreach (object f in Enum.GetValues(flags))
                {
                    ulong v = Convert.ToUInt64(f);
                    if (v == 0) continue;
                    int bit = (int)Math.Log2(v);
                    Assert.Matches($@"{prefix}{Snake(f.ToString()!)}\s*= 1u << {bit},", h);
                }
            }
            Check("TPL_SURFACE_", typeof(SurfaceFlags));
            Check("TPL_SEGMENT_", typeof(SegmentFlags));
            Check("TPL_BUTTON_", typeof(Buttons));
        }

        /// <summary>The standard surface bits 0..9 have the same names in every inklib (SPEC.md section 4).</summary>
        [Fact]
        public void StandardSurfaceBits()
        {
            string[] names = { "NO_COLLISION", "WATER", "HURT", "DEATH", "SLIDE", "LEDGE", "CLIMB", "HANG", "BOUNCE", "CAMERA_PASS" };
            for (int bit = 0; bit < names.Length; bit++)
                Assert.Matches($@"TPL_SURFACE_{names[bit]}\s*= 1u << {bit},", Header());
        }

        /// <summary>native/tpl.def (Windows import library) exports exactly the functions in tpl.h.</summary>
        [Fact]
        public void DefFileMatchesHeader()
        {
            var inHeader = Regex.Matches(Header(), @"^[A-Za-z_][\w \*]*?\b(tpl_\w+)\(", RegexOptions.Multiline)
                .Select(m => m.Groups[1].Value).OrderBy(n => n, StringComparer.Ordinal).ToArray();
            var inDef = Regex.Matches(Repo.Read("native", "tpl.def"), @"^\s+(tpl_\w+)\s*$", RegexOptions.Multiline)
                .Select(m => m.Groups[1].Value).OrderBy(n => n, StringComparer.Ordinal).ToArray();
            Assert.NotEmpty(inHeader);
            Assert.Equal(inHeader, inDef);
        }

        /// <summary>The standard core functions are all there (SPEC.md section 2).</summary>
        [Fact]
        public void CoreFunctionsExist()
        {
            string[] core = { "api_version", "last_error", "units", "world_create", "world_destroy", "char_add", "char_remove",
                              "char_input", "world_tick", "char_state", "char_camera", "events", "events_dropped",
                              "state_size", "save_state", "load_state", "state_hash", "get_api" };
            foreach (string f in core) Assert.Matches($@"\btpl_{f}\(", Header());
        }
    }
}
