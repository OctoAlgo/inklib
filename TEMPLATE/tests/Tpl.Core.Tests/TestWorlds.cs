using System;

namespace Tpl.Tests
{
    /// <summary>
    /// Test geometry in native units (cm, Y up). Never game geometry.
    /// Ground is a height field: floor at 0, plus optional boxes (solid from y = 0 to their top).
    /// </summary>
    sealed class TestWorld : ICollisionWorld
    {
        public struct Block { public double MinX, MaxX, MinZ, MaxZ, Top; public SurfaceFlags Surface; }

        readonly Block[] _blocks;
        public int Queries;

        public TestWorld(params Block[] blocks) { _blocks = blocks; }

        public static TestWorld Make(string name)
        {
            switch (name)
            {
                case "flat": return new TestWorld();
                // A 20 cm step from x = 200 (walkable, flagged Slide to test ground_surface), then a 100 cm wall from x = 400.
                case "step": return new TestWorld(
                    new Block { MinX = 200, MaxX = 400, MinZ = -1000, MaxZ = 1000, Top = 20, Surface = SurfaceFlags.Slide },
                    new Block { MinX = 400, MaxX = 600, MinZ = -1000, MaxZ = 1000, Top = 100 });
                default: throw new ArgumentException("unknown test world " + name);
            }
        }

        double HeightAt(double x, double z, out SurfaceFlags surface)
        {
            double h = 0;
            surface = SurfaceFlags.None;
            foreach (Block b in _blocks)
                if (x >= b.MinX && x < b.MaxX && z >= b.MinZ && z < b.MaxZ && b.Top > h) { h = b.Top; surface = b.Surface; }
            return h;
        }

        public bool Ground(Vec3 from, double maxDrop, out Hit hit)
        {
            Queries++;
            hit = default;
            double h = HeightAt(from.X, from.Z, out SurfaceFlags s);
            if (from.Y < h || from.Y - h > maxDrop) return false;
            hit = new Hit { Point = new Vec3(from.X, h, from.Z), Normal = new Vec3(0, 1, 0), T = maxDrop > 0 ? (from.Y - h) / maxDrop : 0, Surface = s };
            return true;
        }

        /// <summary>Only the floor and block tops are checked, which is enough for the template's camera.</summary>
        public bool Segment(Vec3 a, Vec3 b, SegmentFlags flags, out Hit hit)
        {
            Queries++;
            hit = default;
            if ((flags & SegmentFlags.SkipFloors) != 0 || a.Y < 0 || b.Y >= 0) return false;
            double t = a.Y / (a.Y - b.Y);
            hit = new Hit { Point = a + (b - a) * t, Normal = new Vec3(0, 1, 0), T = t };
            return true;
        }

        public bool Overlap(in Shape shape, out Hit hit)
        {
            Queries++;
            hit = default;
            if (shape.Kind != (int)ShapeKind.Box) throw new NotSupportedException("test world only answers boxes");
            double minX = shape.A.X - shape.HalfExtents.X, maxX = shape.A.X + shape.HalfExtents.X;
            double minZ = shape.A.Z - shape.HalfExtents.Z, maxZ = shape.A.Z + shape.HalfExtents.Z;
            double bottom = shape.A.Y - shape.HalfExtents.Y;
            if (bottom < 0) { hit = new Hit { Point = shape.A, Normal = new Vec3(0, 1, 0) }; return true; }
            foreach (Block b in _blocks)
                if (bottom < b.Top && maxX > b.MinX && minX < b.MaxX && maxZ > b.MinZ && minZ < b.MaxZ)
                {
                    hit = new Hit { Point = shape.A, Normal = new Vec3(-1, 0, 0), Surface = b.Surface };
                    return true;
                }
            return false;
        }
    }

    /// <summary>The same world for a host that speaks shared units (m, Z up): converts and forwards.</summary>
    sealed class SharedHost : ICollisionWorld
    {
        readonly ICollisionWorld _native;
        public SharedHost(ICollisionWorld native) { _native = native; }

        static Hit Out(Hit h)
        {
            h.Point = Units.PositionOut(h.Point, UnitMode.Shared);
            h.Normal = Units.DirectionOut(h.Normal, UnitMode.Shared);
            return h;
        }

        public bool Ground(Vec3 from, double maxDrop, out Hit hit)
        {
            bool ok = _native.Ground(Units.PositionIn(from, UnitMode.Shared), Units.LengthIn(maxDrop, UnitMode.Shared), out hit);
            hit = Out(hit);
            return ok;
        }

        public bool Segment(Vec3 a, Vec3 b, SegmentFlags flags, out Hit hit)
        {
            bool ok = _native.Segment(Units.PositionIn(a, UnitMode.Shared), Units.PositionIn(b, UnitMode.Shared), flags, out hit);
            hit = Out(hit);
            return ok;
        }

        public bool Overlap(in Shape shape, out Hit hit)
        {
            Shape s = shape;
            s.A = Units.PositionIn(shape.A, UnitMode.Shared);
            s.B = Units.PositionIn(shape.B, UnitMode.Shared);
            s.Radius = Units.LengthIn(shape.Radius, UnitMode.Shared);
            s.HalfExtents = Units.ExtentsIn(shape.HalfExtents, UnitMode.Shared);
            bool ok = _native.Overlap(s, out hit);
            hit = Out(hit);
            return ok;
        }
    }
}
