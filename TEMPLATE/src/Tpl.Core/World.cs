using System;
using System.Collections.Generic;

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
        int _nextId = 1;

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
        }

        void Push(Event e)
        {
            if (_events.Count >= EventCapacity) { EventsDropped++; return; }
            _events.Enqueue(e);
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

        public bool SetInput(int id, PadInput input)
        {
            Character? c = Find(id);
            if (c == null) return false;
            c.Input = input;
            return true;
        }

        /// <summary>Steps <paramref name="ticks"/> fixed ticks. Characters step in the order they were added.</summary>
        public void Step(int ticks)
        {
            for (int i = 0; i < ticks; i++)
            {
                Tick++;
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

        Character? Find(int id)
        {
            foreach (Character c in _chars) if (c.Id == id) return c;
            return null;
        }
    }
}
