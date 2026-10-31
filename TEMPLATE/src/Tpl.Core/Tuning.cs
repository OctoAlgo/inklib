using System.Reflection;

namespace Tpl
{
    /// <summary>
    /// Every gameplay number of the character. Native units: centimetres, ticks, radians, Y up
    /// (see <see cref="Units"/>). Each field has exactly one of [Verified] or [Unverified]
    /// (ProvenanceTests checks), and every [Unverified] field is listed in Docs/MEASURE.md.
    /// The template's numbers are placeholders: a real lib replaces them with the game's.
    /// </summary>
    public sealed class Tuning
    {
        [Unverified("Template placeholder. Real libs: the game's logic rate (MEASURE.md M0).")]
        public int TickRate = 60;

        [Unverified("Template placeholder (3 m/s).")]
        public double WalkSpeed = 5;            // cm per tick at full stick

        [Unverified("Template placeholder.")]
        public double StickDeadzone = 0.2;      // 0..1 stick length treated as zero

        [Unverified("Template placeholder (9 m/s^2).")]
        public double Gravity = 0.25;           // cm per tick per tick

        [Unverified("Template placeholder (4.8 m/s).")]
        public double JumpSpeed = 8;            // cm per tick, upwards at take-off

        [Unverified("Template placeholder.")]
        public double StepHeight = 30;          // cm, tallest rise walked up without a jump

        [Unverified("Template placeholder.")]
        public double BodyRadius = 40;          // cm

        [Unverified("Template placeholder.")]
        public double BodyHeight = 180;         // cm, feet to top

        [Unverified("Template placeholder.")]
        public double CameraDistance = 400;     // cm behind the character

        [Unverified("Template placeholder.")]
        public double CameraHeight = 150;       // cm above the feet

        [Unverified("Template placeholder.")]
        public double CameraTurnSpeed = 0.05;   // radians per tick at full look stick

        [Unverified("Template placeholder (about 57 degrees).")]
        public double CameraFovY = 1.0;         // radians, vertical

        /// <summary>Returns null if the tuning can be used, else the reason it cannot.</summary>
        public string? Validate()
        {
            if (TickRate <= 0) return "TickRate must be > 0";
            foreach (FieldInfo f in typeof(Tuning).GetFields(BindingFlags.Public | BindingFlags.Instance))
                if (f.FieldType == typeof(double) && !((double)f.GetValue(this)! >= 0))
                    return f.Name + " must be >= 0";
            return null;
        }
    }
}
