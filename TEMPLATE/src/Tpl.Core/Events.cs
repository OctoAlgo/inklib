namespace Tpl
{
    /// <summary>Event types. 1..255 are the same in every inklib; 256 and up are this lib's own.</summary>
    public enum EventType
    {
        /// <summary>A = old state, B = new state.</summary>
        State = 1,
        Land = 2,
        LeaveGround = 3,
        /// <summary>A = the game's sound id. The host plays it.</summary>
        Sound = 4,
    }

    /// <summary>Sound ids of this lib (the game's own ids in a real lib).</summary>
    public static class SoundIds
    {
        public const int Jump = 1;
    }

    /// <summary>One event. Same layout as tpl_event. Position is in the world's unit convention.</summary>
    public struct Event
    {
        public int Type;
        public int CharId;
        public long Tick;
        public int A, B;
        public Vec3 Position;
    }
}
