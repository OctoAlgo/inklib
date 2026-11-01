using System;

namespace Tpl
{
    /// <summary>
    /// Surface flags on every hit. Bits 0..15 are the same in every inklib (SPEC.md section 4);
    /// bits 16..31 are this lib's own. Values match TPL_SURFACE_* in tpl.h.
    /// </summary>
    [Flags]
    public enum SurfaceFlags : uint
    {
        None = 0,
        NoCollision = 1u << 0,
        Water = 1u << 1,
        Hurt = 1u << 2,
        Death = 1u << 3,
        Slide = 1u << 4,
        Ledge = 1u << 5,
        Climb = 1u << 6,
        Hang = 1u << 7,
        Bounce = 1u << 8,
        CameraPass = 1u << 9,
    }

    /// <summary>Collision rules shared with the host (SPEC.md section 4).</summary>
    public static class CollisionRules
    {
        /// <summary>
        /// Native length units (cm). A shape closer to a surface than this, or overlapping it by less,
        /// is touching: it blocks only a move into the surface. Same as TPL_TOUCH_TOLERANCE.
        /// </summary>
        public const double TouchTolerance = 0.5;
    }

    /// <summary>Flags for <see cref="ICollisionWorld.Segment"/>. Values match TPL_SEGMENT_*.</summary>
    [Flags]
    public enum SegmentFlags : uint
    {
        None = 0,
        /// <summary>Ignore surfaces that face up (floors).</summary>
        SkipFloors = 1u << 0,
        /// <summary>The camera is asking: ignore <see cref="SurfaceFlags.CameraPass"/> surfaces.</summary>
        Camera = 1u << 1,
    }

    /// <summary>Shape kinds. Values match TPL_SHAPE_*.</summary>
    public enum ShapeKind
    {
        Sphere = 1,
        Capsule = 2,
        /// <summary>Axis-aligned box: centre <see cref="Shape.A"/>, half size <see cref="Shape.HalfExtents"/>.</summary>
        Box = 3,
    }

    /// <summary>A query shape. Same layout as tpl_shape.</summary>
    public struct Shape
    {
        public int Kind;
        public uint Reserved;
        /// <summary>Sphere and box: centre. Capsule: one end of the core segment.</summary>
        public Vec3 A;
        /// <summary>Capsule: other end of the core segment. Unused otherwise.</summary>
        public Vec3 B;
        public double Radius;
        public Vec3 HalfExtents;
    }

    /// <summary>One answer from the host. Same layout as tpl_hit.</summary>
    public struct Hit
    {
        /// <summary>Contact point.</summary>
        public Vec3 Point;
        /// <summary>Unit normal at the contact, pointing back at the query.</summary>
        public Vec3 Normal;
        /// <summary>0..1, how far along the query the contact is.</summary>
        public double T;
        public SurfaceFlags Surface;
        /// <summary>Host id of what was hit, 0 for level geometry.</summary>
        public int Actor;
    }

    /// <summary>
    /// The host's collision world, in the world's unit convention. This is the only way Core sees
    /// geometry. Each method returns true and fills <c>hit</c> on a hit.
    /// The host never reports the character's own body.
    /// </summary>
    public interface ICollisionWorld
    {
        /// <summary>First walkable ground straight down from <paramref name="from"/> within <paramref name="maxDrop"/>.</summary>
        bool Ground(Vec3 from, double maxDrop, out Hit hit);

        /// <summary>First hit on the line from <paramref name="a"/> to <paramref name="b"/>.</summary>
        bool Segment(Vec3 a, Vec3 b, SegmentFlags flags, out Hit hit);

        /// <summary>Does <paramref name="shape"/> touch solid geometry? Normal points out of the geometry.</summary>
        bool Overlap(in Shape shape, out Hit hit);
    }

    /// <summary>
    /// Wraps a host collision world that speaks the shared convention, so the simulation can ask in
    /// native units. In native mode it is not used.
    /// </summary>
    sealed class ConvertingCollision : ICollisionWorld
    {
        readonly ICollisionWorld _host;
        readonly UnitMode _mode;

        public ConvertingCollision(ICollisionWorld host, UnitMode mode) { _host = host; _mode = mode; }

        public bool Ground(Vec3 from, double maxDrop, out Hit hit)
        {
            bool ok = _host.Ground(Units.PositionOut(from, _mode), Units.LengthOut(maxDrop, _mode), out hit);
            if (ok) hit = HitIn(hit);
            return ok;
        }

        public bool Segment(Vec3 a, Vec3 b, SegmentFlags flags, out Hit hit)
        {
            bool ok = _host.Segment(Units.PositionOut(a, _mode), Units.PositionOut(b, _mode), flags, out hit);
            if (ok) hit = HitIn(hit);
            return ok;
        }

        public bool Overlap(in Shape shape, out Hit hit)
        {
            Shape s = shape;
            s.A = Units.PositionOut(shape.A, _mode);
            s.B = Units.PositionOut(shape.B, _mode);
            s.Radius = Units.LengthOut(shape.Radius, _mode);
            s.HalfExtents = Units.ExtentsOut(shape.HalfExtents, _mode);
            bool ok = _host.Overlap(s, out hit);
            if (ok) hit = HitIn(hit);
            return ok;
        }

        Hit HitIn(Hit h)
        {
            h.Point = Units.PositionIn(h.Point, _mode);
            h.Normal = Units.DirectionIn(h.Normal, _mode);
            return h;
        }
    }
}
