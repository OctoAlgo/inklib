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
        public int TickRate = 60;               // ticks per real second the host calls (pacing)

        [Unverified("Template placeholder. Real libs: the game's real simulated step, measured (Rayman 3 PC: 0.016 s at 60 Hz).")]
        public double TickSeconds = 1.0 / 60;   // seconds of game time one tick simulates

        [Unverified("Template placeholder. Real libs: the game's input delay, measured (Rayman 3 PC: 1 frame).")]
        public int InputDelayTicks = 1;         // ticks between setting the pad and the game reading it, 0..8

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

        public const int MaxInputDelayTicks = 8;

        /// <summary>Returns null if the tuning can be used, else the reason it cannot.</summary>
        public string? Validate()
        {
            if (TickRate <= 0) return "TickRate must be > 0";
            if (!(TickSeconds > 0)) return "TickSeconds must be > 0";
            if (InputDelayTicks < 0 || InputDelayTicks > MaxInputDelayTicks) return "InputDelayTicks must be 0.." + MaxInputDelayTicks;
            foreach (FieldInfo f in typeof(Tuning).GetFields(BindingFlags.Public | BindingFlags.Instance))
                if (f.FieldType == typeof(double) && !((double)f.GetValue(this)! >= 0))
                    return f.Name + " must be >= 0";
            return null;
        }
    }
}
