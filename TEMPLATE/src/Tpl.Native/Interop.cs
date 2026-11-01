using System.Runtime.InteropServices;

namespace Tpl.Native
{
    // Versioned structs from native/include/tpl.h. The small fixed structs (tpl_vec3, tpl_hit,
    // tpl_shape, tpl_event) are Core's Vec3, Hit, Shape and Event, which have the same layout.

    [StructLayout(LayoutKind.Sequential)]
    struct CUnitsInfo
    {
        public uint Size;
        public int UpAxis, UpSign, Handedness, VelocityPerTick, TickRate;
        public double MetresPerUnit, RadiansPerUnit;
        public double TickSeconds;
        public int InputDelayTicks, SimBits;
    }

    [StructLayout(LayoutKind.Sequential)]
    unsafe struct CCollision
    {
        public uint Size;
        public void* User;
        public delegate* unmanaged[Cdecl]<void*, Vec3*, double, Hit*, int> Ground;
        public delegate* unmanaged[Cdecl]<void*, Vec3*, Vec3*, uint, Hit*, int> Segment;
        public delegate* unmanaged[Cdecl]<void*, Shape*, Hit*, int> Overlap;
        public delegate* unmanaged[Cdecl]<void*, Shape*, Vec3*, Hit*, int> Sweep;
    }

    [StructLayout(LayoutKind.Sequential)]
    unsafe struct CWorldConfig
    {
        public uint Size;
        public int Units;
        public byte* DataPath;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct CCharConfig
    {
        public uint Size, Flags;
        public Vec3 Position;
        public double Yaw;
        public int Variant, Reserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct CInput
    {
        public uint Size, Buttons;
        public float MoveX, MoveY, LookX, LookY;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct CCharState
    {
        public uint Size;
        public int State, Anim, Grounded;
        public long Tick;
        public Vec3 Position, Velocity;
        public double Yaw;
        public uint GroundSurface, Reserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct CCamera
    {
        public uint Size;
        public int Style;
        public Vec3 Position, Target, Up;
        public double FovY;
    }

    [StructLayout(LayoutKind.Sequential)]
    unsafe struct CApi
    {
        public uint Size;
        public int ApiVersion;
        public byte* Prefix;
        public delegate* unmanaged[Cdecl]<int> ApiVersionFn;
        public delegate* unmanaged[Cdecl]<byte*, int, int> LastError;
        public delegate* unmanaged[Cdecl]<CUnitsInfo*, int> Units;
        public delegate* unmanaged[Cdecl]<CWorldConfig*, CCollision*, void*> WorldCreate;
        public delegate* unmanaged[Cdecl]<void*, void> WorldDestroy;
        public delegate* unmanaged[Cdecl]<void*, CCharConfig*, int> CharAdd;
        public delegate* unmanaged[Cdecl]<void*, int, int> CharRemove;
        public delegate* unmanaged[Cdecl]<void*, int, CInput*, int> CharInput;
        public delegate* unmanaged[Cdecl]<void*, int, int> WorldTick;
        public delegate* unmanaged[Cdecl]<void*, int, CCharState*, int> CharState;
        public delegate* unmanaged[Cdecl]<void*, int, CCamera*, int> CharCamera;
        public delegate* unmanaged[Cdecl]<void*, Event*, int, int> Events;
        public delegate* unmanaged[Cdecl]<void*, long> EventsDropped;
        public delegate* unmanaged[Cdecl]<void*, int> StateSize;
        public delegate* unmanaged[Cdecl]<void*, void*, int, int> SaveState;
        public delegate* unmanaged[Cdecl]<void*, void*, int, int> LoadState;
        public delegate* unmanaged[Cdecl]<void*, ulong> StateHash;
    }

    /// <summary>Forwards Core's collision questions to the host's C callbacks.</summary>
    sealed unsafe class CallbackCollision : ICollisionWorld
    {
        readonly CCollision _c;
        public CallbackCollision(CCollision c) { _c = c; }

        public bool Ground(Vec3 from, double maxDrop, out Hit hit)
        {
            Hit h = default;
            bool ok = _c.Ground(_c.User, &from, maxDrop, &h) != 0;
            hit = h;
            return ok;
        }

        public bool Segment(Vec3 a, Vec3 b, SegmentFlags flags, out Hit hit)
        {
            Hit h = default;
            bool ok = _c.Segment(_c.User, &a, &b, (uint)flags, &h) != 0;
            hit = h;
            return ok;
        }

        public bool Overlap(in Shape shape, out Hit hit)
        {
            Shape s = shape;
            Hit h = default;
            bool ok = _c.Overlap(_c.User, &s, &h) != 0;
            hit = h;
            return ok;
        }
    }
}
