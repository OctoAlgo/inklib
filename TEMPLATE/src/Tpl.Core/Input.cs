namespace Tpl
{
    /// <summary>The game's pad buttons. Values match TPL_BUTTON_* in tpl.h. A real lib lists its game's pad.</summary>
    [System.Flags]
    public enum Buttons : uint
    {
        None = 0,
        Jump = 1u << 0,
    }

    /// <summary>One pad, as the player holds it. Same fields as tpl_input (without size).</summary>
    public struct PadInput
    {
        public Buttons Buttons;
        /// <summary>Move stick, -1..1 each, +y = up (away from the player).</summary>
        public float MoveX, MoveY;
        /// <summary>Look (camera) stick, -1..1 each.</summary>
        public float LookX, LookY;
    }
}
