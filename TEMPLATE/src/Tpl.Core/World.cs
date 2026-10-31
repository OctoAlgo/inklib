using System;
using System.Collections.Generic;
using System.Reflection;

namespace Tpl
{
    /// <summary>A character's state in the world's unit convention. Mirrors tpl_char_state.</summary>
    public struct CharacterState
    {
        public MoveState State;
        public int Anim;
        public bool Grounded;
        public long Tick;
        public Vec3 Position;
        public Vec3 Velocity;
        public double Yaw;
        public SurfaceFlags GroundSurface;
    }

    /// <summary>The character's camera in the world's unit convention. Mirrors tpl_camera.</summary>
    public struct CameraView
    {
        public CameraStyle Style;
        public Vec3 Position, Target, Up;
        /// <summary>Vertical field of view, radians, in both unit modes.</summary>
        public double FovY;
    }

    /// <summary>
    /// One simulation: characters, the tick counter and the event buffer. This is what tpl_world is.
    /// Positions and collision answers cross this class in <see cref="Mode"/>; inside, all is native.
    /// Not thread-safe. Worlds share nothing.
    /// </summary>
    public sealed class World
    {
        /// <summary>Events kept until the host polls them. More are dropped and counted.</summary>
        public const int EventCapacity = 256;

        readonly ICollisionWorld _col;
        readonly List<Character> _chars = new List<Character>();
        readonly Queue<Event> _events = new Queue<Event>();
        readonly Action<Event> _emit;
        readonly ulong _tuningHash;
        int _nextId = 1;
        int _eventInTick;
        byte[] _hashScratch = Array.Empty<byte>();

        public Tuning Tuning { get; }
        public UnitMode Mode { get; }
        /// <summary>Ticks stepped since the world was made.</summary>
        public long Tick { get; private set; }
        public long EventsDropped { get; private set; }

        /// <param name="tuning">The character's numbers. Copied by reference; do not change it later.</param>
        /// <param name="collision">The host's collision, speaking <paramref name="mode"/>.</param>
        /// <param name="mode">The unit convention of every number that crosses this class.</param>
        public World(Tuning tuning, ICollisionWorld collision, UnitMode mode)
        {
            string? bad = tuning.Validate();
            if (bad != null) throw new ArgumentException(bad, nameof(tuning));
            Tuning = tuning;
            Mode = mode;
            _col = mode == UnitMode.Native ? collision : new ConvertingCollision(collision, mode);
            _emit = Push;
            _tuningHash = HashTuning(tuning);
        }

        void Push(Event e)
        {
            e.Id = e.Tick * 65536 + _eventInTick++;
            if (_events.Count >= EventCapacity) { EventsDropped++; return; }
            _events.Enqueue(e);
        }

        /// <summary>A snapshot only loads into a world with the same tuning.</summary>
        static ulong HashTuning(Tuning t)
        {
            ulong h = Snapshot.Fnv1a(ReadOnlySpan<byte>.Empty);
            Span<byte> b = stackalloc byte[8];
            foreach (FieldInfo f in typeof(Tuning).GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                object v = f.GetValue(t)!;
                long bits = v is double d ? BitConverter.DoubleToInt64Bits(d) : Convert.ToInt64(v);
                System.Buffers.Binary.BinaryPrimitives.WriteInt64LittleEndian(b, bits);
                h = Snapshot.Fnv1a(b, h);
            }
            return h;
        }

        /// <summary>Adds a character with its feet at <paramref name="position"/>. Returns its id (1 or more).</summary>
        public int AddCharacter(Vec3 position, double yaw, bool camera = true, int variant = 0)
        {
            var c = new Character(_nextId++, Tuning, _col, Units.PositionIn(position, Mode), Units.AngleIn(yaw, Mode), camera, variant);
            _chars.Add(c);
            return c.Id;
        }

        public bool RemoveCharacter(int id)
        {
            Character? c = Find(id);
            return c != null && _chars.Remove(c);
        }

        /// <summary>
        /// Sets the pad. Sticks are quantised to steps of 1/127 here, like a pad's bytes, so the
        /// simulation depends only on round(stick * 127) (SPEC.md section 6: inputs sent over a network).
        /// </summary>
        public bool SetInput(int id, PadInput input)
        {
            Character? c = Find(id);
            if (c == null) return false;
            input.MoveX = Quantise(input.MoveX);
            input.MoveY = Quantise(input.MoveY);
            input.LookX = Quantise(input.LookX);
            input.LookY = Quantise(input.LookY);
            c.Input = input;
            return true;
        }

        /// <summary>Stick to the nearest k / 127, k = -127..127. NaN becomes 0.</summary>
        public static float Quantise(float v)
        {
            if (float.IsNaN(v)) return 0;
            double k = Math.Round(Math.Max(-1.0, Math.Min(1.0, (double)v)) * 127.0);
            return (float)(k / 127.0);
        }

        /// <summary>Steps <paramref name="ticks"/> fixed ticks. Characters step in the order they were added.</summary>
        public void Step(int ticks)
        {
            for (int i = 0; i < ticks; i++)
            {
                Tick++;
                _eventInTick = 0;
                foreach (Character c in _chars) c.Step(Tick, _emit);
            }
        }

        public bool GetState(int id, out CharacterState state)
        {
            Character? c = Find(id);
            state = default;
            if (c == null) return false;
            state = new CharacterState
            {
                State = c.State,
                Anim = c.Anim,
                Grounded = c.Grounded,
                Tick = Tick,
                Position = Units.PositionOut(c.Position, Mode),
                Velocity = Units.VelocityOut(c.Velocity, Mode, Tuning.TickRate),
                Yaw = Units.AngleOut(c.Yaw, Mode),
                GroundSurface = c.GroundSurface,
            };
            return true;
        }

        /// <summary>False if the character does not exist or was added without a camera.</summary>
        public bool GetCamera(int id, out CameraView view)
        {
            Character? c = Find(id);
            view = default;
            if (c == null || !c.HasCamera) return false;
            view = new CameraView
            {
                Style = CameraStyle.ThirdPerson,
                Position = Units.PositionOut(c.CameraPosition, Mode),
                Target = Units.PositionOut(c.CameraTarget, Mode),
                Up = Units.DirectionOut(new Vec3(0, 1, 0), Mode),
                FovY = Tuning.CameraFovY,
            };
            return true;
        }

        /// <summary>Moves up to <c>output.Length</c> events out of the buffer, oldest first. Returns how many.</summary>
        public int DrainEvents(Span<Event> output)
        {
            int n = 0;
            while (n < output.Length && _events.Count > 0)
            {
                Event e = _events.Dequeue();
                e.Position = Units.PositionOut(e.Position, Mode);
                output[n++] = e;
            }
            return n;
        }

        // ---- rollback support (SPEC.md section 6). Costs nothing unless called.

        /// <summary>Bytes <see cref="SaveState"/> needs for the world as it is now.</summary>
        public int StateSize => Snapshot.HeaderBytes + _chars.Count * Character.SnapshotBytes;

        /// <summary>
        /// Writes a complete snapshot: tick, ids, every character with its input and camera.
        /// Returns the bytes written, or 0 if <paramref name="buffer"/> is smaller than <see cref="StateSize"/>.
        /// The event buffer is not part of it. No allocations.
        /// </summary>
        public int SaveState(Span<byte> buffer)
        {
            if (buffer.Length < StateSize) return 0;
            var w = new SnapshotWriter(buffer);
            w.U32(Snapshot.Magic);
            w.U32(Snapshot.Format);
            w.U64(_tuningHash);
            w.I64(Tick);
            w.I32(_nextId);
            w.I32(_chars.Count);
            foreach (Character c in _chars) c.Save(ref w);
            return w.Length;
        }

        /// <summary>
        /// Puts the world back to a snapshot from <see cref="SaveState"/> of a world with the same tuning.
        /// Pending events are thrown away; ticks simulated again send their events again, with the same ids.
        /// Returns null on success, else why the snapshot was refused (the world is then unchanged).
        /// </summary>
        public string? LoadState(ReadOnlySpan<byte> data)
        {
            if (data.Length < Snapshot.HeaderBytes) return "snapshot too short";
            var r = new SnapshotReader(data);
            if (r.U32() != Snapshot.Magic) return "not a tpl snapshot";
            if (r.U32() != Snapshot.Format) return "snapshot format not supported";
            if (r.U64() != _tuningHash) return "snapshot was made with other tuning";
            long tick = r.I64();
            int nextId = r.I32(), count = r.I32();
            if (count < 0 || data.Length != Snapshot.HeaderBytes + (long)count * Character.SnapshotBytes) return "snapshot size does not match its character count";
            var chars = new List<Character>(count);
            for (int i = 0; i < count; i++) chars.Add(Character.Load(ref r, Tuning, _col));
            Tick = tick;
            _nextId = nextId;
            _chars.Clear();
            _chars.AddRange(chars);
            _events.Clear();
            return null;
        }

        /// <summary>FNV-1a 64 of the snapshot. Equal on two machines = same world, bit for bit.</summary>
        public ulong StateHash()
        {
            int size = StateSize;
            if (_hashScratch.Length < size) _hashScratch = new byte[size];
            int n = SaveState(_hashScratch);
            return Snapshot.Fnv1a(new ReadOnlySpan<byte>(_hashScratch, 0, n));
        }

        Character? Find(int id)
        {
            foreach (Character c in _chars) if (c.Id == id) return c;
            return null;
        }
    }
}
