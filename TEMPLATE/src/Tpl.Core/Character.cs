using System;

namespace Tpl
{
    /// <summary>Movement states. Values match TPL_STATE_* in tpl.h. A real lib lists its game's states.</summary>
    public enum MoveState
    {
        Idle = 0,
        Walk = 1,
        Jump = 2,
        Fall = 3,
    }

    /// <summary>Camera styles (SPEC.md section 7). Values match TPL_CAMERA_*.</summary>
    public enum CameraStyle
    {
        FirstPerson = 1,
        ThirdPerson = 2,
        Chase = 3,
        TopDown = 4,
        Fixed = 5,
    }

    /// <summary>
    /// The template's character: walks camera-relative, jumps, falls, steps up small rises, and has a
    /// third-person camera. Everything here is in native units (cm, ticks, radians, Y up).
    /// A real lib replaces this file with the game's rules.
    /// </summary>
    public sealed class Character
    {
        static readonly Vec3 Up = new Vec3(0, 1, 0);

        readonly Tuning _t;
        readonly ICollisionWorld _col;
        readonly PadInput[] _delayed;   // inputs set but not yet read by the game (ring)
        int _delayedHead;
        bool _jumpWasDown;

        public int Id { get; }
        public bool HasCamera { get; }
        public int Variant { get; }
        public Vec3 Position { get; private set; }
        /// <summary>cm per tick.</summary>
        public Vec3 Velocity { get; private set; }
        /// <summary>Facing, radians around +Y, 0 = +X.</summary>
        public double Yaw { get; private set; }
        public MoveState State { get; private set; }
        public bool Grounded { get; private set; }
        public SurfaceFlags GroundSurface { get; private set; }
        /// <summary>The game's animation id. The template has none, so it reports the state.</summary>
        public int Anim => (int)State;
        /// <summary>The pad the host set last. The game reads it <see cref="InputDelayTicks"/> ticks later.</summary>
        public PadInput Input { get; set; }
        /// <summary>The game's input delay for this character (0 if turned off).</summary>
        public int InputDelayTicks => _delayed.Length;

        /// <summary>Camera heading, radians around +Y.</summary>
        public double CameraYaw { get; private set; }
        public Vec3 CameraPosition { get; private set; }
        public Vec3 CameraTarget { get; private set; }

        Character(int id, Tuning tuning, ICollisionWorld collision, bool camera, int variant, int inputDelay)
        {
            _delayed = new PadInput[inputDelay];
            Id = id;
            _t = tuning;
            _col = collision;
            HasCamera = camera;
            Variant = variant;
        }

        internal Character(int id, Tuning tuning, ICollisionWorld collision, Vec3 feet, double yaw, bool camera, int variant, int inputDelay)
            : this(id, tuning, collision, camera, variant, inputDelay)
        {
            Position = feet;
            Yaw = yaw;
            CameraYaw = yaw;
            State = MoveState.Fall;
            if (_col.Ground(feet + Up * _t.StepHeight, _t.StepHeight * 2, out Hit g) && Math.Abs(g.Point.Y - feet.Y) <= _t.StepHeight)
            {
                Position = new Vec3(feet.X, g.Point.Y, feet.Z);
                Grounded = true;
                GroundSurface = g.Surface;
                State = MoveState.Idle;
            }
            UpdateCamera();
        }

        const int InputBytes = 5 * 4;
        internal const int FixedSnapshotBytes = 7 * 4 + 2 * 8 + 4 * 24 + InputBytes;

        /// <summary>Bytes this character takes in a snapshot.</summary>
        internal int SnapshotBytes => FixedSnapshotBytes + _delayed.Length * InputBytes;

        static void SaveInput(ref SnapshotWriter w, PadInput p)
        {
            w.U32((uint)p.Buttons);
            w.F32(p.MoveX); w.F32(p.MoveY); w.F32(p.LookX); w.F32(p.LookY);
        }

        static PadInput LoadInput(ref SnapshotReader r) =>
            new PadInput { Buttons = (Buttons)r.U32(), MoveX = r.F32(), MoveY = r.F32(), LookX = r.F32(), LookY = r.F32() };

        /// <summary>Writes everything that shapes later ticks. Keep in step with <see cref="Load"/>.</summary>
        internal void Save(ref SnapshotWriter w)
        {
            w.I32(Id);
            w.I32(Variant);
            w.I32(_delayed.Length);
            w.I32(_delayedHead);
            w.U32((HasCamera ? 1u : 0) | (Grounded ? 2u : 0) | (_jumpWasDown ? 4u : 0));
            w.I32((int)State);
            w.U32((uint)GroundSurface);
            w.V3(Position);
            w.V3(Velocity);
            w.F64(Yaw);
            w.F64(CameraYaw);
            w.V3(CameraPosition);
            w.V3(CameraTarget);
            SaveInput(ref w, Input);
            foreach (PadInput p in _delayed) SaveInput(ref w, p);
        }

        internal static Character Load(ref SnapshotReader r, Tuning tuning, ICollisionWorld collision)
        {
            int id = r.I32(), variant = r.I32(), delay = r.I32(), head = r.I32();
            if (delay < 0 || delay > Tuning.MaxInputDelayTicks || head < 0 || (head >= delay && head != 0))
                throw new FormatException("bad input delay in snapshot");
            uint flags = r.U32();
            var c = new Character(id, tuning, collision, (flags & 1) != 0, variant, delay)
            {
                _delayedHead = head,
                Grounded = (flags & 2) != 0,
                _jumpWasDown = (flags & 4) != 0,
            };
            c.State = (MoveState)r.I32();
            c.GroundSurface = (SurfaceFlags)r.U32();
            c.Position = r.V3();
            c.Velocity = r.V3();
            c.Yaw = r.F64();
            c.CameraYaw = r.F64();
            c.CameraPosition = r.V3();
            c.CameraTarget = r.V3();
            c.Input = LoadInput(ref r);
            for (int i = 0; i < delay; i++) c._delayed[i] = LoadInput(ref r);
            return c;
        }

        static Vec3 Forward(double yaw) => new Vec3(DetMath.Cos(yaw), 0, -DetMath.Sin(yaw));

        internal void Step(long tick, Action<Event> emit)
        {
            // The game reads the pad InputDelayTicks ticks after the host sets it.
            PadInput pad = Input;
            if (_delayed.Length > 0)
            {
                pad = _delayed[_delayedHead];
                _delayed[_delayedHead] = Input;
                _delayedHead = (_delayedHead + 1) % _delayed.Length;
            }
            MoveState before = State;

            CameraYaw = Units.WrapAngle(CameraYaw - pad.LookX * _t.CameraTurnSpeed);

            // Camera-relative stick: up = the way the camera looks.
            double sx = pad.MoveX, sy = pad.MoveY;
            double tilt = DetMath.Sqrt(sx * sx + sy * sy);
            if (tilt < _t.StickDeadzone) { sx = 0; sy = 0; tilt = 0; }
            else if (tilt > 1) { sx /= tilt; sy /= tilt; tilt = 1; }
            Vec3 fwd = Forward(CameraYaw);
            Vec3 right = new Vec3(DetMath.Sin(CameraYaw), 0, DetMath.Cos(CameraYaw));
            Vec3 dir = fwd * sy + right * sx;
            Vec3 v = Velocity;
            if (tilt > 0)
            {
                double len = dir.Length;
                Yaw = DetMath.Atan2(-dir.Z, dir.X);
                v.X = dir.X / len * _t.WalkSpeed * tilt;
                v.Z = dir.Z / len * _t.WalkSpeed * tilt;
            }
            else { v.X = 0; v.Z = 0; }

            bool jumpDown = (pad.Buttons & Buttons.Jump) != 0;
            bool jumpPressed = jumpDown && !_jumpWasDown;
            _jumpWasDown = jumpDown;

            if (Grounded && jumpPressed)
            {
                v.Y = _t.JumpSpeed;
                Grounded = false;
                emit(new Event { Type = (int)EventType.LeaveGround, CharId = Id, Tick = tick, Position = Position });
                emit(new Event { Type = (int)EventType.Sound, CharId = Id, Tick = tick, A = SoundIds.Jump, Position = Position });
            }
            else if (!Grounded)
            {
                v.Y -= _t.Gravity;
            }

            // Horizontal: blocked if the body above step height would touch something.
            Vec3 next = Position;
            if (v.X != 0 || v.Z != 0)
            {
                Vec3 moved = new Vec3(Position.X + v.X, Position.Y, Position.Z + v.Z);
                double half = (_t.BodyHeight - _t.StepHeight) / 2;
                var body = new Shape
                {
                    Kind = (int)ShapeKind.Box,
                    A = moved + Up * (_t.StepHeight + half),
                    HalfExtents = new Vec3(_t.BodyRadius, half, _t.BodyRadius),
                };
                if (_col.Overlap(body, out _)) { v.X = 0; v.Z = 0; }
                else next = moved;
            }

            // Vertical, then ground.
            next.Y += v.Y;
            bool wasGrounded = Grounded;
            if (v.Y <= 0)
            {
                double top = Math.Max(Position.Y, next.Y) + _t.StepHeight;
                double lowest = next.Y - (wasGrounded ? _t.StepHeight : 0);
                if (_col.Ground(new Vec3(next.X, top, next.Z), top - lowest, out Hit g) && g.Point.Y >= lowest)
                {
                    next.Y = g.Point.Y;
                    v.Y = 0;
                    Grounded = true;
                    GroundSurface = g.Surface;
                    if (!wasGrounded) emit(new Event { Type = (int)EventType.Land, CharId = Id, Tick = tick, Position = next });
                }
                else if (wasGrounded)
                {
                    Grounded = false;
                    GroundSurface = SurfaceFlags.None;
                    emit(new Event { Type = (int)EventType.LeaveGround, CharId = Id, Tick = tick, Position = next });
                }
            }

            Position = next;
            Velocity = v;
            State = Grounded ? (tilt > 0 ? MoveState.Walk : MoveState.Idle) : (v.Y > 0 ? MoveState.Jump : MoveState.Fall);
            if (State != before)
                emit(new Event { Type = (int)EventType.State, CharId = Id, Tick = tick, A = (int)before, B = (int)State, Position = Position });

            UpdateCamera();
        }

        void UpdateCamera()
        {
            if (!HasCamera) return;
            Vec3 target = Position + Up * (_t.BodyHeight * 0.5);
            Vec3 wanted = Position + Up * _t.CameraHeight - Forward(CameraYaw) * _t.CameraDistance;
            if (_col.Segment(target, wanted, SegmentFlags.Camera, out Hit h))
                wanted = target + (wanted - target) * Math.Max(0, h.T - 0.05);
            CameraTarget = target;
            CameraPosition = wanted;
        }
    }
}
